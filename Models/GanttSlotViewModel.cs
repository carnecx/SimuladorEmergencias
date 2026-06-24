namespace SimuladorEmergencias.Models
{
    public class GanttSlotViewModel
    {
        public int IdPaciente { get; set; }
        public string NombrePaciente { get; set; } = "";
        public string NombreTipo { get; set; } = "";
        public string ColorHex { get; set; } = "#4472C4";
        public int TiempoInicio { get; set; }
        public int TiempoFin { get; set; }
        public string Algoritmo { get; set; } = "";
        public int ColaPrioridad { get; set; }
    }
}