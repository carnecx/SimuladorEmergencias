namespace SimuladorEmergencias.Models
{
    public class DashboardViewModel
    {
        public int TotalPacientes { get; set; }
        public int PacientesEnEspera { get; set; }
        public int PacientesEnAtencion { get; set; }
        public int PacientesFinalizados { get; set; }

        public double PromedioEspera { get; set; }
        public double PromedioRetorno { get; set; }
        public double UtilizacionCpu { get; set; }

        public List<Paciente> Pacientes { get; set; } = new();
        public List<ReporteAlgoritmo> Reportes { get; set; } = new();

        public List<Paciente> PacientesMasLentos { get; set; } = new();
    }
}