using MySql.Data.MySqlClient;
using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    public class SimulacionDbService
    {
        private readonly string _connStr;

        public SimulacionDbService(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")!;
        }

        public List<ConfiguracionColaViewModel> ObtenerTiposPaciente()
        {
            var lista = new List<ConfiguracionColaViewModel>();
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            var cmd = new MySqlCommand(
                "SELECT id_tipo, nombre, prioridad, color_hex FROM tipo_paciente ORDER BY prioridad", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new ConfiguracionColaViewModel
                {
                    IdTipo = reader.GetInt32("id_tipo"),
                    NombreTipo = reader.GetString("nombre"),
                    Prioridad = reader.GetInt32("prioridad"),
                    ColorHex = reader.IsDBNull(reader.GetOrdinal("color_hex"))
                                 ? "#000000"
                                 : reader.GetString("color_hex"),
                    Algoritmo = "FIFO",
                    Quantum = 2
                });
            }
            return lista;
        }
        public int CrearSesion(int nProcesos, string notas = "")
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            var cmd = new MySqlCommand(
                "INSERT INTO sesion (n_procesos, notas) VALUES (@n, @notas); SELECT LAST_INSERT_ID();",
                conn);
            cmd.Parameters.AddWithValue("@n", nProcesos);
            cmd.Parameters.AddWithValue("@notas", notas);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int GuardarPaciente(PacienteViewModel p)
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            var cmd = new MySqlCommand(@"
                INSERT INTO paciente
                    (id_sesion, id_tipo, nombre, tiempo_llegada, tiempo_rafaga,
                     tiempo_restante, prioridad, estado, fuente)
                VALUES
                    (@sesion, @tipo, @nombre, @llegada, @rafaga,
                     @restante, @prioridad, 'esperando', @fuente);
                SELECT LAST_INSERT_ID();", conn);

            cmd.Parameters.AddWithValue("@sesion", p.IdSesion);
            cmd.Parameters.AddWithValue("@tipo", p.IdTipo);
            cmd.Parameters.AddWithValue("@nombre", p.Nombre);
            cmd.Parameters.AddWithValue("@llegada", p.TiempoLlegada);
            cmd.Parameters.AddWithValue("@rafaga", p.TiempoRafaga);
            cmd.Parameters.AddWithValue("@restante", p.TiempoRafaga);
            cmd.Parameters.AddWithValue("@prioridad", p.Prioridad);
            cmd.Parameters.AddWithValue("@fuente", p.Fuente);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void ActualizarPacienteSimulado(PacienteViewModel p)
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            var cmd = new MySqlCommand(@"
                UPDATE paciente SET
                    estado          = 'finalizado',
                    tiempo_inicio   = @inicio,
                    tiempo_fin      = @fin,
                    tiempo_espera   = @espera,
                    tiempo_retorno  = @retorno
                WHERE id_paciente = @id", conn);

            cmd.Parameters.AddWithValue("@inicio", p.TiempoInicio);
            cmd.Parameters.AddWithValue("@fin", p.TiempoFin);
            cmd.Parameters.AddWithValue("@espera", p.TiempoEspera);
            cmd.Parameters.AddWithValue("@retorno", p.TiempoRetorno);
            cmd.Parameters.AddWithValue("@id", p.IdPaciente);
            cmd.ExecuteNonQuery();
        }
        public void GuardarGantt(List<GanttSlotViewModel> slots, int idSesion)
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            foreach (var s in slots)
            {
                var cmd = new MySqlCommand(@"
                    INSERT INTO gantt_ejecucion
                        (id_sesion, id_paciente, tiempo_inicio, tiempo_fin, algoritmo, cola_prioridad)
                    VALUES
                        (@sesion, @paciente, @inicio, @fin, @alg, @cola)", conn);

                cmd.Parameters.AddWithValue("@sesion", idSesion);
                cmd.Parameters.AddWithValue("@paciente", s.IdPaciente);
                cmd.Parameters.AddWithValue("@inicio", s.TiempoInicio);
                cmd.Parameters.AddWithValue("@fin", s.TiempoFin);
                cmd.Parameters.AddWithValue("@alg", s.Algoritmo);
                cmd.Parameters.AddWithValue("@cola", s.ColaPrioridad);
                cmd.ExecuteNonQuery();
            }
        }

        public void GuardarReportes(List<ReporteAlgoritmoViewModel> reportes, int idSesion)
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            foreach (var r in reportes)
            {
                var cmd = new MySqlCommand(@"
                    INSERT INTO reporte_algoritmo
                        (id_sesion, algoritmo, cola_prioridad, avg_espera,
                         avg_retorno, utilizacion_cpu, total_procesos)
                    VALUES
                        (@sesion, @alg, @cola, @espera, @retorno, @cpu, @total)", conn);

                cmd.Parameters.AddWithValue("@sesion", idSesion);
                cmd.Parameters.AddWithValue("@alg", r.Algoritmo);
                cmd.Parameters.AddWithValue("@cola", r.ColaPrioridad);
                cmd.Parameters.AddWithValue("@espera", r.AvgEspera);
                cmd.Parameters.AddWithValue("@retorno", r.AvgRetorno);
                cmd.Parameters.AddWithValue("@cpu", r.UtilizacionCpu);
                cmd.Parameters.AddWithValue("@total", r.TotalProcesos);
                cmd.ExecuteNonQuery();
            }
        }

        public void CerrarSesion(int idSesion)
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();
            var cmd = new MySqlCommand(
                "UPDATE sesion SET fecha_fin = NOW() WHERE id_sesion = @id", conn);
            cmd.Parameters.AddWithValue("@id", idSesion);
            cmd.ExecuteNonQuery();
        }
    }
}