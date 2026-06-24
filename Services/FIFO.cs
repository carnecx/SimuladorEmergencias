using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class FIFO
    {

        public List<Paciente> Ejecutar(List<Paciente> pacientes)
        {
            // Ordenar por tiempo de llegada
            var ordenados = pacientes.OrderBy(p => p.TiempoLlegada).ToList();

            int tiempoActual = 0;

            foreach (var paciente in ordenados)
            {
                // Si el CPU está libre antes de que llegue el paciente, avanza el tiempo
                if (tiempoActual < paciente.TiempoLlegada)
                    tiempoActual = paciente.TiempoLlegada;

                // Calcular tiempos
                paciente.TiempoInicio = tiempoActual;
                paciente.TiempoFin = tiempoActual + paciente.TiempoRafaga;
                paciente.TiempoEspera = paciente.TiempoInicio - paciente.TiempoLlegada;
                paciente.TiempoRetorno = paciente.TiempoFin - paciente.TiempoLlegada;
                paciente.Estado = "finalizado";

                // Avanzar el tiempo actual
                tiempoActual = paciente.TiempoFin.Value;
            }

            return ordenados;
        }

        public ReporteAlgoritmo GenerarReporte(List<Paciente> pacientes, int colaPrioridad)
        {
            int tiempoTotal = pacientes.Any()
                ? pacientes.Max(p => p.TiempoFin ?? 0)
                : 0;

            int tiempoCpuUsado = pacientes.Sum(p => p.TiempoRafaga);

            return new ReporteAlgoritmo
            {
                Algoritmo = "FIFO",
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