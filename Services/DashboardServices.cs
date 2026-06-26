using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    
    /// Servicio encargado de generar la información necesaria
    /// para la visualización del Dashboard de la simulación.
    /// Calcula indicadores, promedios y consolida los datos
    /// que serán mostrados en la interfaz.
    /// </summary>
    public class DashboardService
    {
       
        /// Genera el modelo del Dashboard a partir de la lista de pacientes
        /// procesados y los reportes obtenidos durante la simulación.
        /// </summary>
        /// <param name="pacientes">Lista de pacientes procesados en la simulación.</param>
        /// <param name="reportes">Lista de reportes generados por cada algoritmo.</param>
        /// <returns>Objeto DashboardViewModel con la información consolidada.</returns>
        public DashboardViewModel GenerarDashboard(List<Paciente> pacientes, List<ReporteAlgoritmo> reportes)
        {
            // Evita referencias nulas inicializando listas vacías en caso necesario.
            pacientes ??= new List<Paciente>();
            reportes ??= new List<ReporteAlgoritmo>();

            // Obtiene únicamente los pacientes que poseen tiempos de espera y retorno calculados.
            var pacientesConEspera = pacientes.Where(p => p.TiempoEspera.HasValue).ToList();
            var pacientesConRetorno = pacientes.Where(p => p.TiempoRetorno.HasValue).ToList();

            // Construye el modelo que será utilizado por la vista del Dashboard.
            return new DashboardViewModel
            {
                // Cantidad total de pacientes registrados en la simulación.
                TotalPacientes = pacientes.Count,

                // Cantidad de pacientes según su estado actual.
                PacientesEnEspera = pacientes.Count(p => p.Estado == "esperando"),
                PacientesEnAtencion = pacientes.Count(p => p.Estado == "en_atencion"),
                PacientesFinalizados = pacientes.Count(p => p.Estado == "finalizado"),

                // Calcula el tiempo promedio de espera.
                PromedioEspera = pacientesConEspera.Any()
                    ? pacientesConEspera.Average(p => p.TiempoEspera!.Value)
                    : 0,

                // Calcula el tiempo promedio de retorno.
                PromedioRetorno = pacientesConRetorno.Any()
                    ? pacientesConRetorno.Average(p => p.TiempoRetorno!.Value)
                    : 0,

                // Calcula el porcentaje promedio de utilización del CPU
                // considerando todos los algoritmos ejecutados.
                UtilizacionCpu = reportes.Any()
                    ? reportes.Average(r => r.UtilizacionCpu)
                    : 0,

                // Obtiene los cinco pacientes con mayor tiempo de retorno,
                // utilizados para el análisis de rendimiento.
                PacientesMasLentos = pacientes
                    .Where(p => p.TiempoRetorno.HasValue)
                    .OrderByDescending(p => p.TiempoRetorno!.Value)
                    .Take(5)
                    .ToList(),

                // Asigna las colecciones completas al modelo para su visualización.
                Pacientes = pacientes,
                Reportes = reportes
            };
        }
    }
}