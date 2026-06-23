namespace SimuladorEmergencias.Models
{
    public class TipoPaciente
    {
        public int IdTipo { get; set; }
        public string? Nombre { get; set; }
        public int Prioridad { get; set; }
        public string? ColorHex { get; set; }
        public string? Descripcion { get; set; }
    }
}