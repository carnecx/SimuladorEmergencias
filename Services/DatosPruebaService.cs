using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class DatosPruebaService
    {
        public List<Paciente> ObtenerPacientes()
        {
            return new List<Paciente>
            {
                new Paciente
                {
                    IdPaciente = 1,
                    IdSesion = 1,
                    IdTipo = 1,
                    Nombre = "Carlos Mora",
                    TiempoLlegada = 0,
                    TiempoRafaga = 8,
                    TiempoRestante = 8,
                    Prioridad = 1,
                    Estado = "esperando",
                    Fuente = "manual",
                    Tipo = new TipoPaciente
                    {
                        IdTipo = 1,
                        Nombre = "Rojo",
                        Prioridad = 1,
                        ColorHex = "#FF0000"
                    }
                },
                new Paciente
                {
                    IdPaciente = 2,
                    IdSesion = 1,
                    IdTipo = 2,
                    Nombre = "María Solano",
                    TiempoLlegada = 1,
                    TiempoRafaga = 5,
                    TiempoRestante = 3,
                    Prioridad = 2,
                    Estado = "en_atencion",
                    TiempoEspera = 1,
                    Fuente = "manual",
                    Tipo = new TipoPaciente
                    {
                        IdTipo = 2,
                        Nombre = "Amarillo",
                        Prioridad = 2,
                        ColorHex = "#FFC000"
                    }
                },
                new Paciente
                {
                    IdPaciente = 3,
                    IdSesion = 1,
                    IdTipo = 6,
                    Nombre = "Ana Rodríguez",
                    TiempoLlegada = 2,
                    TiempoRafaga = 3,
                    TiempoRestante = 0,
                    Prioridad = 6,
                    Estado = "finalizado",
                    TiempoInicio = 3,
                    TiempoFin = 6,
                    TiempoEspera = 1,
                    TiempoRetorno = 4,
                    Fuente = "archivo",
                    Tipo = new TipoPaciente
                    {
                        IdTipo = 6,
                        Nombre = "Seguimiento",
                        Prioridad = 6,
                        ColorHex = "#7030A0"
                    }
                }
            };
        }

        public List<ReporteAlgoritmo> ObtenerReportes()
        {
            return new List<ReporteAlgoritmo>
            {
                new ReporteAlgoritmo
                {
                    
                    Algoritmo = "FIFO",
                    ColaPrioridad = 1,
                    NombreCola = "Rojo",
                    AvgEspera = 1.50,
                    AvgRetorno = 6.80,
                    UtilizacionCpu = 92.00,
                    TotalProcesos = 5
                },
                new ReporteAlgoritmo
                {
                  
                    Algoritmo = "SJF",
                    ColaPrioridad = 2,
                    NombreCola = "Amarillo",
                    AvgEspera = 1.20,
                    AvgRetorno = 5.40,
                    UtilizacionCpu = 95.00,
                    TotalProcesos = 4
                },
                new ReporteAlgoritmo
                {
                   
                    Algoritmo = "RR",
                    ColaPrioridad = 6,
                    NombreCola = "Seguimiento",
                    AvgEspera = 2.10,
                    AvgRetorno = 7.30,
                    UtilizacionCpu = 89.00,
                    TotalProcesos = 6
                }
            };
        }
    }
}