namespace SimuladorEmergencias.Models
{
    public class SimulacionViewModel
    {
        public List<ConfiguracionColaViewModel> ConfiguracionColas { get; set; } = new();

        public List<PacienteViewModel> Pacientes { get; set; } = new();

        public int NProcesos { get; set; } = 5;

        public int QuantumGlobal { get; set; } = 2;

        public int IdSesion { get; set; }

        public List<GanttSlotViewModel> GanttSlots { get; set; } = new();
        public List<ReporteAlgoritmoViewModel> Reportes { get; set; } = new();

        public bool SimulacionEjecutada { get; set; } = false;

        public string Mensaje { get; set; } = "";
    }
}