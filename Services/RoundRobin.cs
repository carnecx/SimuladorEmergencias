using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class RoundRobin
    {
        public (List<Paciente> Pacientes, List<GanttEjecucion> Gantt) Ejecutar(
            List<Paciente> pacientes, int quantum, int colaPrioridad)
        {
            // Inicializar tiempo restante de cada paciente
            foreach (var p in pacientes)
                p.TiempoRestante = p.TiempoRafaga;

            var cola = new Queue<Paciente>();
            var pendientes = pacientes.OrderBy(p => p.TiempoLlegada).ToList();
            var gantt = new List<GanttEjecucion>();
            var resultado = new List<Paciente>();
            int tiempoActual = 0;
            int indice = 0;

            // Agregar los que llegan en tiempo 0
            while (indice < pendientes.Count &&
                   pendientes[indice].TiempoLlegada <= tiempoActual)
            {
                cola.Enqueue(pendientes[indice]);
                indice++;
            }

            while (cola.Any() || indice < pendientes.Count)
            {
                // Si la cola está vacía pero hay pendientes, avanzar el tiempo
                if (!cola.Any())
                {
                    tiempoActual = pendientes[indice].TiempoLlegada;
                    while (indice < pendientes.Count &&
                           pendientes[indice].TiempoLlegada <= tiempoActual)
                    {
                        cola.Enqueue(pendientes[indice]);
                        indice++;
                    }
                }

                var paciente = cola.Dequeue();

                // Si es la primera vez que se atiende, registrar inicio
                if (!paciente.TiempoInicio.HasValue)
                    paciente.TiempoInicio = tiempoActual;

                // Calcular cuánto tiempo usa en este quantum
                int tiempoEjecucion = Math.Min(quantum, paciente.TiempoRestante);

                // Registrar en el Gantt
                gantt.Add(new GanttEjecucion
                {
                    IdPaciente = paciente.IdPaciente,
                    TiempoInicio = tiempoActual,
                    TiempoFin = tiempoActual + tiempoEjecucion,
                    Algoritmo = "RR",
                    ColaPrioridad = colaPrioridad
                });

                // Avanzar tiempo y reducir restante
                tiempoActual += tiempoEjecucion;
                paciente.TiempoRestante -= tiempoEjecucion;

                // Agregar nuevos pacientes que llegaron mientras se ejecutaba
                while (indice < pendientes.Count &&
                       pendientes[indice].TiempoLlegada <= tiempoActual)
                {
                    cola.Enqueue(pendientes[indice]);
                    indice++;
                }

                // Si terminó
                if (paciente.TiempoRestante == 0)
                {
                    paciente.TiempoFin = tiempoActual;
                    paciente.TiempoEspera = paciente.TiempoFin - paciente.TiempoRafaga
                                             - paciente.TiempoLlegada;
                    paciente.TiempoRetorno = paciente.TiempoFin - paciente.TiempoLlegada;
                    paciente.Estado = "finalizado";
                    resultado.Add(paciente);
                }
                else
                {
                    // No terminó, vuelve al final de la cola
                    paciente.Estado = "esperando";
                    cola.Enqueue(paciente);
                }
            }

            return (resultado, gantt);
        }

        public ReporteAlgoritmo GenerarReporte(List<Paciente> pacientes, int colaPrioridad)
        {
            int tiempoTotal = pacientes.Any() ? pacientes.Max(p => p.TiempoFin ?? 0) : 0;
            int tiempoCpuUsado = pacientes.Sum(p => p.TiempoRafaga);

            return new ReporteAlgoritmo
            {
                Algoritmo = "RR",
                ColaPrioridad = colaPrioridad,
                AvgEspera = pacientes.Any()
                                 ? pacientes.Average(p => p.TiempoEspera ?? 0)
                                 : 0,
                AvgRetorno = pacientes.Any()
                                 ? pacientes.Average(p => p.TiempoRetorno ?? 0)
                                 : 0,
                UtilizacionCpu = tiempoTotal > 0
                                 ? (double)tiempoCpuUsado / tiempoTotal * 100
                                 : 0,
                TotalProcesos = pacientes.Count
            };
        }
    }
}