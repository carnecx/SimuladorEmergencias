namespace SimuladorEmergencias.Models
{
    public class GanttEjecucion
    {
        public int IdGantt { get; set; }
        public int IdSesion { get; set; }
        public int IdPaciente { get; set; }

        public int TiempoInicio { get; set; }
        public int TiempoFin { get; set; }

        public string? Algoritmo { get; set; }
        public int ColaPrioridad { get; set; }

        public Paciente? Paciente { get; set; }
    }
}