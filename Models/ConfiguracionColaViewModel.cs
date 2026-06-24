namespace SimuladorEmergencias.Models
{
    public class ConfiguracionColaViewModel
    {
        public int IdTipo { get; set; }
        public string NombreTipo { get; set; } = "";
        public string ColorHex { get; set; } = "#000000";
        public int Prioridad { get; set; }

        public string Algoritmo { get; set; } = "FIFO";

        public int Quantum { get; set; } = 2;
    }
}