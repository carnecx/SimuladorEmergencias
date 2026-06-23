namespace SimuladorEmergencias.Models
{
    public class ConfiguracionCola
    {
        public int IdConfig { get; set; }
        public int IdTipo { get; set; }
        public string? Algoritmo { get; set; }
        public int? Quantum { get; set; }

        public TipoPaciente? TipoPaciente { get; set; }
    }
}