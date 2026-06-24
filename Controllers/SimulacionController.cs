using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Models;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    public class SimulacionController : Controller
    {
        private readonly SimulacionDbService _db;
        private readonly MlqSimuladorService _mlq;

        public SimulacionController(SimulacionDbService db, MlqSimuladorService mlq)
        {
            _db = db;
            _mlq = mlq;
        }

        [HttpGet]
        public IActionResult Configurar()
        {
            var vm = new SimulacionViewModel
            {
                ConfiguracionColas = _db.ObtenerTiposPaciente()
            };
            return View(vm);
        }
        [HttpPost]
        public IActionResult Configurar(SimulacionViewModel vm)
        {
            var tipos = _db.ObtenerTiposPaciente();
            foreach (var cola in vm.ConfiguracionColas)
            {
                var tipo = tipos.FirstOrDefault(t => t.IdTipo == cola.IdTipo);
                if (tipo != null)
                {
                    cola.NombreTipo = tipo.NombreTipo;
                    cola.ColorHex = tipo.ColorHex;
                    cola.Prioridad = tipo.Prioridad;
                }
            }

            TempData["ConfigJson"] = System.Text.Json.JsonSerializer.Serialize(vm.ConfiguracionColas);
            TempData["NProcesos"] = vm.NProcesos;

            return RedirectToAction("IngresarPacientes");
        }

        [HttpGet]
        public IActionResult IngresarPacientes()
        {
            var tipos = _db.ObtenerTiposPaciente();
            ViewBag.TiposPaciente = tipos;
            ViewBag.NProcesos = TempData.Peek("NProcesos") ?? 5;
            TempData.Keep("ConfigJson");
            TempData.Keep("NProcesos");
            return View(new List<PacienteViewModel>());
        }
        [HttpPost]
        public IActionResult EjecutarSimulacion(List<PacienteViewModel> pacientes)
        {
            var configJson = TempData["ConfigJson"]?.ToString() ?? "[]";
            var nProcesos = Convert.ToInt32(TempData["NProcesos"] ?? 5);

            var configuraciones = System.Text.Json.JsonSerializer
                .Deserialize<List<ConfiguracionColaViewModel>>(configJson)
                ?? new List<ConfiguracionColaViewModel>();

            var tipos = _db.ObtenerTiposPaciente();
            foreach (var p in pacientes)
            {
                var tipo = tipos.FirstOrDefault(t => t.IdTipo == p.IdTipo);
                if (tipo != null)
                {
                    p.NombreTipo = tipo.NombreTipo;
                    p.ColorHex = tipo.ColorHex;
                    p.Prioridad = tipo.Prioridad;
                }
                p.TiempoRestante = p.TiempoRafaga;
            }

            int idSesion = _db.CrearSesion(pacientes.Count, "Simulación MLQ");

            foreach (var p in pacientes)
            {
                p.IdSesion = idSesion;
                p.IdPaciente = _db.GuardarPaciente(p);
            }

            var (gantt, reportes) = _mlq.Simular(pacientes, configuraciones);

            foreach (var p in pacientes)
                _db.ActualizarPacienteSimulado(p);

            _db.GuardarGantt(gantt, idSesion);
            _db.GuardarReportes(reportes, idSesion);
            _db.CerrarSesion(idSesion);

            var vm = new SimulacionViewModel
            {
                Pacientes = pacientes,
                GanttSlots = gantt,
                Reportes = reportes,
                ConfiguracionColas = configuraciones,
                IdSesion = idSesion,
                SimulacionEjecutada = true,
                NProcesos = nProcesos
            };

            return View("Resultados", vm);
        }
        [HttpPost]
        public IActionResult CargarArchivo(IFormFile archivo)
        {
            var pacientes = new List<PacienteViewModel>();
            var tipos = _db.ObtenerTiposPaciente();

            if (archivo != null && archivo.Length > 0)
            {
                using var reader = new StreamReader(archivo.OpenReadStream());
                string? linea;
                while ((linea = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("#")) continue;

                    var partes = linea.Split(',');
                    if (partes.Length < 4) continue;

                    if (int.TryParse(partes[1].Trim(), out int idTipo) &&
                        int.TryParse(partes[2].Trim(), out int llegada) &&
                        int.TryParse(partes[3].Trim(), out int rafaga))
                    {
                        var tipo = tipos.FirstOrDefault(t => t.IdTipo == idTipo);
                        pacientes.Add(new PacienteViewModel
                        {
                            Nombre = partes[0].Trim(),
                            IdTipo = idTipo,
                            NombreTipo = tipo?.NombreTipo ?? "",
                            ColorHex = tipo?.ColorHex ?? "#000000",
                            Prioridad = tipo?.Prioridad ?? 6,
                            TiempoLlegada = llegada,
                            TiempoRafaga = rafaga,
                            TiempoRestante = rafaga,
                            Fuente = "archivo"
                        });
                    }
                }
            }

            TempData.Keep("ConfigJson");
            TempData.Keep("NProcesos");
            ViewBag.TiposPaciente = tipos;
            ViewBag.NProcesos = TempData.Peek("NProcesos") ?? 5;
            return View("IngresarPacientes", pacientes);
        }
    }
}