namespace SimuladorEmergencias.Models
{
   
    public class PacienteViewModel
    {
        public int IdPaciente { get; set; }
        public int IdSesion { get; set; }
        public int IdTipo { get; set; }
        public string Nombre { get; set; } = "";
        public int TiempoLlegada { get; set; }
        public int TiempoRafaga { get; set; }
        public int TiempoRestante { get; set; }
        public int Prioridad { get; set; }
        public string Estado { get; set; } = "esperando";
        public int? TiempoInicio { get; set; }
        public int? TiempoFin { get; set; }
        public int? TiempoEspera { get; set; }
        public int? TiempoRetorno { get; set; }
        public string Fuente { get; set; } = "manual";

        public string NombreTipo { get; set; } = "";
        public string ColorHex { get; set; } = "#000000";
    }
}