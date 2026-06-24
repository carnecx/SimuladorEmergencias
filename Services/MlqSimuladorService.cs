using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class MlqSimuladorService
    {
        public (List<GanttSlotViewModel> gantt, List<ReporteAlgoritmoViewModel> reportes)
            Simular(List<PacienteViewModel> pacientes,
                    List<ConfiguracionColaViewModel> configuraciones)
        {
            var gantt = new List<GanttSlotViewModel>();
            var reportes = new List<ReporteAlgoritmoViewModel>();

            var colasOrdenadas = configuraciones.OrderBy(c => c.Prioridad).ToList();

            int tiempoActual = 0;

            foreach (var cola in colasOrdenadas)
            {
                var pacientesCola = pacientes
                    .Where(p => p.IdTipo == cola.IdTipo)
                    .OrderBy(p => p.TiempoLlegada)
                    .ToList();

                if (!pacientesCola.Any()) continue;

                int tiempoBase = Math.Max(tiempoActual,
                                          pacientesCola.Min(p => p.TiempoLlegada));

                List<GanttSlotViewModel> slotsCola;

                switch (cola.Algoritmo.ToUpper())
                {
                    case "SJF":
                        slotsCola = EjecutarSJF(pacientesCola, tiempoBase, cola);
                        break;
                    case "RR":
                        slotsCola = EjecutarRR(pacientesCola, tiempoBase, cola);
                        break;
                    default: 
                        slotsCola = EjecutarFIFO(pacientesCola, tiempoBase, cola);
                        break;
                }

                gantt.AddRange(slotsCola);

                if (slotsCola.Any())
                    tiempoActual = slotsCola.Max(s => s.TiempoFin);

                var reporte = CalcularReporte(pacientesCola, slotsCola, cola, tiempoActual);
                reportes.Add(reporte);
            }

            return (gantt, reportes);
        }

        private List<GanttSlotViewModel> EjecutarFIFO(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();
            int tiempo = tiempoBase;

            var ordenados = pacientes.OrderBy(p => p.TiempoLlegada).ToList();

            foreach (var p in ordenados)
            {
                if (tiempo < p.TiempoLlegada) tiempo = p.TiempoLlegada;

                int fin = tiempo + p.TiempoRafaga;

                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "FIFO",
                    ColaPrioridad = cola.Prioridad
                });

                p.TiempoInicio = tiempo;
                p.TiempoFin = fin;
                p.TiempoEspera = tiempo - p.TiempoLlegada;
                p.TiempoRetorno = fin - p.TiempoLlegada;
                p.Estado = "finalizado";

                tiempo = fin;
            }

            return slots;
        }

        private List<GanttSlotViewModel> EjecutarSJF(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();
            var pendientes = pacientes.ToList();
            int tiempo = tiempoBase;

            while (pendientes.Any())
            {
                var disponibles = pendientes
                    .Where(p => p.TiempoLlegada <= tiempo)
                    .OrderBy(p => p.TiempoRafaga)
                    .ToList();

                if (!disponibles.Any())
                {
                    tiempo = pendientes.Min(p => p.TiempoLlegada);
                    continue;
                }

                var p = disponibles.First();
                pendientes.Remove(p);

                int fin = tiempo + p.TiempoRafaga;

                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "SJF",
                    ColaPrioridad = cola.Prioridad
                });

                p.TiempoInicio = tiempo;
                p.TiempoFin = fin;
                p.TiempoEspera = tiempo - p.TiempoLlegada;
                p.TiempoRetorno = fin - p.TiempoLlegada;
                p.Estado = "finalizado";

                tiempo = fin;
            }

            return slots;
        }

        private List<GanttSlotViewModel> EjecutarRR(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();
            int quantum = cola.Quantum > 0 ? cola.Quantum : 2;

            var cola_rr = pacientes
                .OrderBy(p => p.TiempoLlegada)
                .Select(p => new {
                    Original = p,
                    Restante = p.TiempoRafaga,
                    Inicio = (int?)null
                })
                .ToList();
            var trabajos = cola_rr.Select(x => new RRTrabajo
            {
                Paciente = x.Original,
                Restante = x.Restante,
                PrimerInicio = null
            }).ToList();

            var queue = new Queue<RRTrabajo>();
            int tiempo = tiempoBase;
            int idx = 0; 
            while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                queue.Enqueue(trabajos[idx++]);

            while (queue.Any() || idx < trabajos.Count)
            {
                if (!queue.Any())
                {
                    tiempo = trabajos[idx].Paciente.TiempoLlegada;
                    while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                        queue.Enqueue(trabajos[idx++]);
                }

                var trabajo = queue.Dequeue();
                var p = trabajo.Paciente;

                if (trabajo.PrimerInicio == null)
                    trabajo.PrimerInicio = tiempo;

                int ejecutar = Math.Min(quantum, trabajo.Restante);
                int fin = tiempo + ejecutar;

                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "RR",
                    ColaPrioridad = cola.Prioridad
                });

                trabajo.Restante -= ejecutar;
                tiempo = fin;

                while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                    queue.Enqueue(trabajos[idx++]);

                if (trabajo.Restante > 0)
                {
                    queue.Enqueue(trabajo);
                }
                else
                {
                    p.TiempoInicio = trabajo.PrimerInicio;
                    p.TiempoFin = fin;
                    p.TiempoEspera = (trabajo.PrimerInicio ?? 0) - p.TiempoLlegada;
                    p.TiempoRetorno = fin - p.TiempoLlegada;
                    p.Estado = "finalizado";
                }
            }

            return slots;
        }
        private class RRTrabajo
        {
            public PacienteViewModel Paciente { get; set; } = null!;
            public int Restante { get; set; }
            public int? PrimerInicio { get; set; }
        }
        private ReporteAlgoritmoViewModel CalcularReporte(
            List<PacienteViewModel> pacientes,
            List<GanttSlotViewModel> slots,
            ConfiguracionColaViewModel cola,
            int tiempoTotal)
        {
            var finalizados = pacientes.Where(p => p.Estado == "finalizado").ToList();
            if (!finalizados.Any())
                return new ReporteAlgoritmoViewModel { Algoritmo = cola.Algoritmo };

            decimal avgEspera = finalizados.Any(p => p.TiempoEspera.HasValue)
                ? (decimal)finalizados.Where(p => p.TiempoEspera.HasValue)
                                      .Average(p => p.TiempoEspera!.Value)
                : 0;

            decimal avgRetorno = finalizados.Any(p => p.TiempoRetorno.HasValue)
                ? (decimal)finalizados.Where(p => p.TiempoRetorno.HasValue)
                                      .Average(p => p.TiempoRetorno!.Value)
                : 0;

            int tiempoActivo = slots.Sum(s => s.TiempoFin - s.TiempoInicio);
            int duracionCola = slots.Any()
                ? slots.Max(s => s.TiempoFin) - slots.Min(s => s.TiempoInicio)
                : 1;

            decimal utilizacion = duracionCola > 0
                ? Math.Round((decimal)tiempoActivo / duracionCola * 100, 2)
                : 100;

            return new ReporteAlgoritmoViewModel
            {
                Algoritmo = cola.Algoritmo,
                ColaPrioridad = cola.Prioridad,
                NombreCola = cola.NombreTipo,
                ColorHex = cola.ColorHex,
                AvgEspera = Math.Round(avgEspera, 2),
                AvgRetorno = Math.Round(avgRetorno, 2),
                UtilizacionCpu = utilizacion,
                TotalProcesos = finalizados.Count
            };
        }
    }
}