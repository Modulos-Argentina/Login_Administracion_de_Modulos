using System;
using System.Collections.Generic;
using Npgsql;

namespace Login_Administracion_de_Modulos.Areas.Modulo05.Models
{
    public class SesionPsicologicaItem
    {
        public int IdAtencionPsico { get; set; }
        public int PersonaId { get; set; }
        public int PsicologoId { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string MotivoConsulta { get; set; } = string.Empty;
        public string? ObservacionNotas { get; set; }
        public string? EstadoAtencion { get; set; }

        // Datos del estudiante
        public string? NumeroDocumento { get; set; }
        public string? EstudianteNombres { get; set; }
        public string? EstudianteApellidos { get; set; }
        public string Carrera { get; set; } = "Desarrollo de Sistemas de Información";
        public string CicloTurno { get; set; } = "IV Ciclo / Turno Noche";
    }

    public class ExpedientePsicoRepository
    {
        private readonly string _connectionString;

        public ExpedientePsicoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SesionPsicologicaItem? BuscarEstudiantePorDni(string dni)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            const string sql = @"SELECT id_persona, numero_documento, nombres, apellidos 
                                 FROM personas 
                                 WHERE numero_documento = @dni LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@dni", dni.Trim());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new SesionPsicologicaItem
                {
                    PersonaId = reader.GetInt32(0),
                    NumeroDocumento = reader.GetString(1),
                    EstudianteNombres = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    EstudianteApellidos = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Carrera = "Desarrollo de Sistemas de Información",
                    CicloTurno = "IV Ciclo / Turno Noche"
                };
            }
            return null;
        }

        public bool RegistrarSesion(SesionPsicologicaItem item)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            const string sql = @"
                INSERT INTO atenciones_psicologia 
                (persona_id, psicologo_id, fecha_hora, motivo_consulta, observacion_notas, estado_atencion)
                VALUES (@persona_id, @psicologo_id, @fecha_hora, @motivo, @notas, @estado)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@persona_id", item.PersonaId);
            cmd.Parameters.AddWithValue("@psicologo_id", item.PsicologoId > 0 ? item.PsicologoId : 1);
            cmd.Parameters.AddWithValue("@fecha_hora", item.FechaHora);
            cmd.Parameters.AddWithValue("@motivo", item.MotivoConsulta ?? "");
            cmd.Parameters.AddWithValue("@notas", (object?)item.ObservacionNotas ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@estado", (object?)item.EstadoAtencion ?? DBNull.Value);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public List<SesionPsicologicaItem> ObtenerExpedientePorDni(string dni)
        {
            var lista = new List<SesionPsicologicaItem>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            const string sql = @"
                SELECT a.id_atencion_psico, a.persona_id, a.psicologo_id, a.fecha_hora, 
                       a.motivo_consulta, a.observacion_notas, a.estado_atencion,
                       p.numero_documento, p.nombres, p.apellidos
                FROM atenciones_psicologia a
                INNER JOIN personas p ON a.persona_id = p.id_persona
                WHERE p.numero_documento = @dni
                ORDER BY a.fecha_hora DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@dni", dni.Trim());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new SesionPsicologicaItem
                {
                    IdAtencionPsico = reader.GetInt32(0),
                    PersonaId = reader.GetInt32(1),
                    PsicologoId = reader.GetInt32(2),
                    FechaHora = reader.GetDateTime(3),
                    MotivoConsulta = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ObservacionNotas = reader.IsDBNull(5) ? null : reader.GetString(5),
                    EstadoAtencion = reader.IsDBNull(6) ? null : reader.GetString(6),
                    NumeroDocumento = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    EstudianteNombres = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    EstudianteApellidos = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Carrera = "Desarrollo de Sistemas de Información",
                    CicloTurno = "IV Ciclo / Turno Noche"
                });
            }
            return lista;
        }
    }
}
