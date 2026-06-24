using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class SesionSimulacionService
    {
        public List<Paciente> Pacientes { get; set; } = new();
        public List<GanttEjecucion> Gantt { get; set; } = new();
        public List<ReporteAlgoritmo> Reportes { get; set; } = new();

        public void Limpiar()
        {
            Pacientes.Clear();
            Gantt.Clear();
            Reportes.Clear();
        }
    }
}