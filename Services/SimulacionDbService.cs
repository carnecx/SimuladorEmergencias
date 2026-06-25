using MySql.Data.MySqlClient;
using SimuladorEmergencias.Models;
using System.Data;

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
        public int ObtenerUltimaSesion()
        {
            using var conn = new MySqlConnection(_connStr);
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT IFNULL(MAX(id_sesion),0) FROM sesion",
                conn);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Paciente> ObtenerPacientesPorSesion(int idSesion)
        {
            var lista = new List<Paciente>();

            using var conn = new MySqlConnection(_connStr);
            conn.Open();

            var cmd = new MySqlCommand(@"
        SELECT
            p.id_paciente,
            p.id_sesion,
            p.id_tipo,
            p.nombre,
            p.tiempo_llegada,
            p.tiempo_rafaga,
            p.tiempo_restante,
            p.prioridad,
            p.estado,
            p.tiempo_inicio,
            p.tiempo_fin,
            p.tiempo_espera,
            p.tiempo_retorno,
            p.fuente,
            tp.nombre as tipo_nombre,
            tp.color_hex
        FROM paciente p
        INNER JOIN tipo_paciente tp
            ON p.id_tipo = tp.id_tipo
        WHERE p.id_sesion = @sesion
        ORDER BY p.id_paciente",
                conn);

            cmd.Parameters.AddWithValue("@sesion", idSesion);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Paciente
                {
                    IdPaciente = reader.GetInt32("id_paciente"),
                    IdSesion = reader.GetInt32("id_sesion"),
                    IdTipo = reader.GetInt32("id_tipo"),
                    Nombre = reader.GetString("nombre"),

                    TiempoLlegada = reader.GetInt32("tiempo_llegada"),
                    TiempoRafaga = reader.GetInt32("tiempo_rafaga"),
                    TiempoRestante = reader.GetInt32("tiempo_restante"),

                    Prioridad = reader.GetInt32("prioridad"),
                    Estado = reader.GetString("estado"),

                    TiempoInicio = reader.IsDBNull("tiempo_inicio")
                        ? null
                        : reader.GetInt32("tiempo_inicio"),

                    TiempoFin = reader.IsDBNull("tiempo_fin")
                        ? null
                        : reader.GetInt32("tiempo_fin"),

                    TiempoEspera = reader.IsDBNull("tiempo_espera")
                        ? null
                        : reader.GetInt32("tiempo_espera"),

                    TiempoRetorno = reader.IsDBNull("tiempo_retorno")
                        ? null
                        : reader.GetInt32("tiempo_retorno"),

                    Fuente = reader.GetString("fuente"),

                    Tipo = new TipoPaciente
                    {
                        IdTipo = reader.GetInt32("id_tipo"),
                        Nombre = reader.GetString("tipo_nombre"),
                        ColorHex = reader.GetString("color_hex")
                    }
                });
            }

            return lista;
        }
        public List<ReporteAlgoritmo> ObtenerReportesPorSesion(int idSesion)
        {
            var lista = new List<ReporteAlgoritmo>();

            using var conn = new MySqlConnection(_connStr);
            conn.Open();

            var cmd = new MySqlCommand(@"
    SELECT
        r.algoritmo,
        r.cola_prioridad,
        r.avg_espera,
        r.avg_retorno,
        r.utilizacion_cpu,
        r.total_procesos,
        tp.nombre AS nombre_cola
    FROM reporte_algoritmo r
    LEFT JOIN tipo_paciente tp
        ON r.cola_prioridad = tp.prioridad
    WHERE r.id_sesion = @sesion
    ORDER BY r.cola_prioridad",
       conn); ;

            cmd.Parameters.AddWithValue("@sesion", idSesion);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ReporteAlgoritmo
                {
                    Algoritmo = reader.GetString("algoritmo"),
                    ColaPrioridad = reader.GetInt32("cola_prioridad"),
                    NombreCola = reader.IsDBNull(reader.GetOrdinal("nombre_cola"))
           ? $"Cola {reader.GetInt32("cola_prioridad")}"
           : reader.GetString("nombre_cola"),
                    AvgEspera = Convert.ToDouble(reader.GetDecimal("avg_espera")),
                    AvgRetorno = Convert.ToDouble(reader.GetDecimal("avg_retorno")),
                    UtilizacionCpu = Convert.ToDouble(reader.GetDecimal("utilizacion_cpu")),
                    TotalProcesos = reader.GetInt32("total_procesos")
                });
            }

            return lista;
        }

    }
}