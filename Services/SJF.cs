using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class SJF
    {

        public List<Paciente> Ejecutar(List<Paciente> pacientes)
        {
            var pendientes = new List<Paciente>(pacientes);
            var resultado = new List<Paciente>();
            int tiempoActual = 0;

            while (pendientes.Any())
            {
                // Filtrar los que ya llegaron al sistema
                var disponibles = pendientes
                    .Where(p => p.TiempoLlegada <= tiempoActual)
                    .OrderBy(p => p.TiempoRafaga)
                    .ToList();

                // Si ninguno ha llegado aún, avanzar el tiempo al próximo
                if (!disponibles.Any())
                {
                    tiempoActual = pendientes.Min(p => p.TiempoLlegada);
                    continue;
                }

                // Tomar el de menor ráfaga
                var paciente = disponibles.First();
                pendientes.Remove(paciente);

                // Calcular tiempos
                paciente.TiempoInicio = tiempoActual;
                paciente.TiempoFin = tiempoActual + paciente.TiempoRafaga;
                paciente.TiempoEspera = paciente.TiempoInicio - paciente.TiempoLlegada;
                paciente.TiempoRetorno = paciente.TiempoFin - paciente.TiempoLlegada;
                paciente.Estado = "finalizado";

                tiempoActual = paciente.TiempoFin.Value;
                resultado.Add(paciente);
            }

            return resultado;
        }

        public ReporteAlgoritmo GenerarReporte(List<Paciente> pacientes, int colaPrioridad)
        {
            int tiempoTotal = pacientes.Any() ? pacientes.Max(p => p.TiempoFin ?? 0) : 0;
            int tiempoCpuUsado = pacientes.Sum(p => p.TiempoRafaga);

            return new ReporteAlgoritmo
            {
                Algoritmo = "SJF",
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