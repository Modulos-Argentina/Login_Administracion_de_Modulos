using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace Login_Administracion_de_Modulos.Areas.Modulo05.Models
{
    /// <summary>
    /// Modelo para el formulario de registro de sesión psicológica
    /// Mapea con la tabla atenciones_psicologia de PostgreSQL
    /// </summary>
    public class SesionPsicologicaItem
    {
        public int IdAtencionPsico { get; set; }

        [Required(ErrorMessage = "El estudiante es obligatorio.")]
        [Display(Name = "ID del Estudiante")]
        public int PersonaId { get; set; }

        [Display(Name = "Psicólogo Responsable")]
        public int PsicologoId { get; set; }

        [Required(ErrorMessage = "La fecha y hora de la sesión es obligatoria.")]
        [Display(Name = "Fecha y Hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
        [StringLength(250, ErrorMessage = "El motivo de consulta no puede superar los 250 caracteres.")]
        [Display(Name = "Motivo de Consulta y Contexto Académico / Familiar")]
        public string MotivoConsulta { get; set; } = string.Empty;

        [Display(Name = "Diagnóstico Preliminar y Notas Confidenciales de Terapia")]
        public string? ObservacionNotas { get; set; }

        [Required(ErrorMessage = "El estado clínico es obligatorio.")]
        [StringLength(30)]
        [Display(Name = "Estado Clínico")]
        public string EstadoAtencion { get; set; } = "Pendiente";

        // Campos complementarios para la identificación visual del estudiante
        [Display(Name = "DNI")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener 8 dígitos numéricos.")]
        public string? NumeroDocumento { get; set; }

        [Display(Name = "Nombres")]
        public string? EstudianteNombres { get; set; }

        [Display(Name = "Apellidos")]
        public string? EstudianteApellidos { get; set; }

        [Display(Name = "Apellidos y Nombres")]
        public string NombreCompleto => $"{EstudianteApellidos} {EstudianteNombres}".Trim();

        [Display(Name = "Carrera / Programa")]
        public string Carrera { get; set; } = "Desarrollo de Sistemas de Información";

        [Display(Name = "Ciclo / Turno")]
        public string CicloTurno { get; set; } = "IV Ciclo / Turno Noche";
    }

    /// <summary>
    /// Repositorio de acceso a datos para el Módulo 05 (Psicología y Bienestar)
    /// </summary>
    public class ExpedientePsicoRepository
    {
        private readonly string _connectionString;

        public ExpedientePsicoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Busca un estudiante en la tabla personas por su número de DNI
        /// </summary>
        public SesionPsicologicaItem? BuscarEstudiantePorDni(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni)) return null;

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
                SELECT p.id_persona, p.numero_documento, p.nombres, p.apellidos
                FROM personas p
                WHERE p.numero_documento = @dni
                LIMIT 1";

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

        /// <summary>
        /// Obtiene un ID de psicólogo válido garantizando integridad referencial con usuarios(id_usuario)
        /// </summary>
        public int ObtenerIdPsicologoValido(int? sugeridoId = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            if (sugeridoId.HasValue && sugeridoId.Value > 0)
            {
                using var cmdCheck = new NpgsqlCommand("SELECT id_usuario FROM usuarios WHERE id_usuario = @id AND estado = 'Activo' LIMIT 1", conn);
                cmdCheck.Parameters.AddWithValue("@id", sugeridoId.Value);
                var res = cmdCheck.ExecuteScalar();
                if (res != null) return Convert.ToInt32(res);
            }

            // Buscar usuario con rol de Psicología
            const string sqlPsico = @"
                SELECT u.id_usuario 
                FROM usuarios u
                INNER JOIN usuarios_roles ur ON u.id_usuario = ur.id_usuario
                INNER JOIN roles r ON ur.id_rol = r.id_rol
                WHERE r.nombre ILIKE '%Psicolog%' AND u.estado = 'Activo'
                LIMIT 1";
            using var cmdPsico = new NpgsqlCommand(sqlPsico, conn);
            var idPsico = cmdPsico.ExecuteScalar();
            if (idPsico != null) return Convert.ToInt32(idPsico);

            // Fallback a cualquier usuario activo para evitar violación de FK
            using var cmdFallback = new NpgsqlCommand("SELECT id_usuario FROM usuarios WHERE estado = 'Activo' LIMIT 1", conn);
            var idFallback = cmdFallback.ExecuteScalar();
            return idFallback != null ? Convert.ToInt32(idFallback) : 1;
        }

        /// <summary>
        /// Inserta una nueva sesión en atenciones_psicologia validando el CHECK de estado_atencion
        /// </summary>
        public bool RegistrarSesion(SesionPsicologicaItem item)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            int psicologoId = ObtenerIdPsicologoValido(item.PsicologoId);

            // Sanear estado clínico para cumplir estrictamente con CHECK (estado_atencion IN ('Pendiente','En proceso','Dado de Alta','Derivado externamente'))
            string estadoValidado = item.EstadoAtencion switch
            {
                "En proceso" or "En Proceso" => "En proceso",
                "Dado de Alta" or "Alta" => "Dado de Alta",
                "Derivado externamente" or "Derivado" => "Derivado externamente",
                _ => "Pendiente"
            };

            const string sql = @"
                INSERT INTO atenciones_psicologia 
                (persona_id, psicologo_id, fecha_hora, motivo_consulta, observacion_notas, estado_atencion)
                VALUES (@persona_id, @psicologo_id, @fecha_hora, @motivo, @notas, @estado)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@persona_id", item.PersonaId);
            cmd.Parameters.AddWithValue("@psicologo_id", psicologoId);
            cmd.Parameters.AddWithValue("@fecha_hora", item.FechaHora);
            cmd.Parameters.AddWithValue("@motivo", item.MotivoConsulta.Length > 250 ? item.MotivoConsulta.Substring(0, 250) : item.MotivoConsulta);
            cmd.Parameters.AddWithValue("@notas", string.IsNullOrWhiteSpace(item.ObservacionNotas) ? (object)DBNull.Value : item.ObservacionNotas);
            cmd.Parameters.AddWithValue("@estado", estadoValidado);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        /// <summary>
        /// Consulta el expediente histórico de atenciones de un alumno por su DNI
        /// </summary>
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
                    EstadoAtencion = reader.IsDBNull(6) ? "Pendiente" : reader.GetString(6),
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
