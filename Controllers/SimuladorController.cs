using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Models;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    public class SimuladorController : Controller
    {
        // Lista en memoria para la sesión actual
        //private static List<Paciente> _pacientes = new List<Paciente>();
        //private static List<GanttEjecucion> _gantt = new List<GanttEjecucion>();
        //private static List<ReporteAlgoritmo> _reportes = new List<ReporteAlgoritmo>();
        private readonly SesionSimulacionService _sesion;

        public SimuladorController(SesionSimulacionService sesion)
        {
            _sesion = sesion;
        }

        private readonly FIFO _fifo = new FIFO();
        private readonly SJF _sjf = new SJF();
        private readonly RoundRobin _roundRobin = new RoundRobin();

        public IActionResult Index()
        {
            ViewBag.Tipos = ObtenerTipos();
            ViewBag.Pacientes = _sesion.Pacientes;
            return View();
        }

        [HttpPost]
        public IActionResult AgregarPaciente(Paciente paciente)
        {
            paciente.IdPaciente = _sesion.Pacientes.Count + 1;
            paciente.TiempoRestante = paciente.TiempoRafaga;
            paciente.Estado = "esperando";
            paciente.Fuente = "manual";
            paciente.Tipo = ObtenerTipos()
                                     .FirstOrDefault(t => t.IdTipo == paciente.IdTipo);
            paciente.Prioridad = paciente.Tipo?.Prioridad ?? 0;

            _sesion.Pacientes.Add(paciente);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CargarArchivo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Error = "Selecciona un archivo válido.";
                return RedirectToAction("Index");
            }

            var tipos = ObtenerTipos();

            using (var reader = new StreamReader(archivo.OpenReadStream()))
            {
                string linea;
                while ((linea = reader.ReadLine()) != null)
                {
                    // Saltar líneas vacías o comentarios
                    if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("#"))
                        continue;

                    var partes = linea.Split(',');
                    if (partes.Length < 4) continue;

                    var paciente = new Paciente
                    {
                        IdPaciente = _sesion.Pacientes.Count + 1,
                        Nombre = partes[0].Trim(),
                        IdTipo = int.Parse(partes[1].Trim()),
                        TiempoLlegada = int.Parse(partes[2].Trim()),
                        TiempoRafaga = int.Parse(partes[3].Trim()),
                        Estado = "esperando",
                        Fuente = "archivo"
                    };

                    paciente.TiempoRestante = paciente.TiempoRafaga;
                    paciente.Tipo = tipos.FirstOrDefault(t => t.IdTipo == paciente.IdTipo);
                    paciente.Prioridad = paciente.Tipo?.Prioridad ?? 0;

                    _sesion.Pacientes.Add(paciente);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Ejecutar(string algoritmo, int quantum = 2)
        {
            if (!_sesion.Pacientes.Any())
            {
                TempData["Error"] = "Agrega al menos un paciente antes de ejecutar.";
                return RedirectToAction("Index");
            }

            // Copia para no modificar la lista original
            var copia = _sesion.Pacientes.Select(p => new Paciente
            {
                IdPaciente = p.IdPaciente,
                Nombre = p.Nombre,
                IdTipo = p.IdTipo,
                TiempoLlegada = p.TiempoLlegada,
                TiempoRafaga = p.TiempoRafaga,
                TiempoRestante = p.TiempoRafaga,
                Prioridad = p.Prioridad,
                Estado = "esperando",
                Fuente = p.Fuente,
                Tipo = p.Tipo
            }).ToList();

            _sesion.Gantt.Clear();
            _sesion.Reportes.Clear();

            List<Paciente> resultado;

            switch (algoritmo)
            {
                case "FIFO":
                    resultado = _fifo.Ejecutar(copia);
                    _sesion.Reportes.Add(_fifo.GenerarReporte(resultado, 0));
                    break;

                case "SJF":
                    resultado = _sjf.Ejecutar(copia);
                    _sesion.Reportes.Add(_sjf.GenerarReporte(resultado, 0));
                    break;

                case "RR":
                    var (pacientesRR, ganttRR) = _roundRobin.Ejecutar(copia, quantum, 0);
                    resultado = pacientesRR;
                    _sesion.Gantt.AddRange(ganttRR);
                    _sesion.Reportes.Add(_roundRobin.GenerarReporte(resultado, 0));
                    break;

                default:
                    TempData["Error"] = "Algoritmo no reconocido.";
                    return RedirectToAction("Index");
            }

            _sesion.Pacientes = resultado;

            return RedirectToAction("Resultado");
        }

        public IActionResult Resultado()
        {
            ViewBag.Pacientes = _sesion.Pacientes;
            ViewBag.Gantt = _sesion.Gantt;
            ViewBag.Reportes = _sesion.Reportes;
            return View();
        }
        public IActionResult Limpiar()
        {
            _sesion.Pacientes.Clear();
            _sesion.Gantt.Clear();
            _sesion.Reportes.Clear();
            return RedirectToAction("Index");
        }

        private List<TipoPaciente> ObtenerTipos()
        {
            return new List<TipoPaciente>
            {
                new TipoPaciente { IdTipo=1, Nombre="Rojo",        Prioridad=1, ColorHex="#FF0000" },
                new TipoPaciente { IdTipo=2, Nombre="Amarillo",    Prioridad=2, ColorHex="#FFC000" },
                new TipoPaciente { IdTipo=3, Nombre="Embarazada",  Prioridad=3, ColorHex="#FF69B4" },
                new TipoPaciente { IdTipo=4, Nombre="Verde",       Prioridad=4, ColorHex="#00B050" },
                new TipoPaciente { IdTipo=5, Nombre="Cita",        Prioridad=5, ColorHex="#4472C4" },
                new TipoPaciente { IdTipo=6, Nombre="Seguimiento", Prioridad=6, ColorHex="#7030A0" }
            };
        }
    }
}