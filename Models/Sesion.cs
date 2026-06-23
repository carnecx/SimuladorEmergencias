namespace SimuladorEmergencias.Models
{
    public class Sesion
    {
        public int IdSesion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int NProcesos { get; set; }
        public string? Notas { get; set; }

        public List<Paciente> Pacientes { get; set; } = new();
    }
}