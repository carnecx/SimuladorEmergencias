using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class DashboardService
    {
        public DashboardViewModel GenerarDashboard(List<Paciente> pacientes, List<ReporteAlgoritmo> reportes)
        {
            pacientes ??= new List<Paciente>();
            reportes ??= new List<ReporteAlgoritmo>();

            var pacientesConEspera = pacientes.Where(p => p.TiempoEspera.HasValue).ToList();
            var pacientesConRetorno = pacientes.Where(p => p.TiempoRetorno.HasValue).ToList();

            return new DashboardViewModel
            {
                TotalPacientes = pacientes.Count,
                PacientesEnEspera = pacientes.Count(p => p.Estado == "esperando"),
                PacientesEnAtencion = pacientes.Count(p => p.Estado == "en_atencion"),
                PacientesFinalizados = pacientes.Count(p => p.Estado == "finalizado"),

                PromedioEspera = pacientesConEspera.Any()
                    ? pacientesConEspera.Average(p => p.TiempoEspera!.Value)
                    : 0,

                PromedioRetorno = pacientesConRetorno.Any()
                    ? pacientesConRetorno.Average(p => p.TiempoRetorno!.Value)
                    : 0,

                UtilizacionCpu = reportes.Any()
                    ? reportes.Average(r => r.UtilizacionCpu)
                    : 0,

                Pacientes = pacientes,
                Reportes = reportes
            };
        }
    }
}