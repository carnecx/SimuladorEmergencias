namespace SimuladorEmergencias.Models
{
    public class ReporteAlgoritmo
    {
        public string? Algoritmo { get; set; }
        public int ColaPrioridad { get; set; }
        public string? NombreCola { get; set; }

        public double AvgEspera { get; set; }
        public double AvgRetorno { get; set; }
        public double UtilizacionCpu { get; set; }

        public int TotalProcesos { get; set; }
    }
}
