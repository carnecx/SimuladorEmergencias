namespace SimuladorEmergencias.Models
{
    public class ReporteAlgoritmoViewModel
    {
        public string Algoritmo { get; set; } = "";
        public int ColaPrioridad { get; set; }
        public string NombreCola { get; set; } = "";
        public string ColorHex { get; set; } = "#000000";
        public decimal AvgEspera { get; set; }
        public decimal AvgRetorno { get; set; }
        public decimal UtilizacionCpu { get; set; }
        public int TotalProcesos { get; set; }
    }
}