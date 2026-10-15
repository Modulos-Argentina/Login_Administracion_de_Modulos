using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Login_Administracion_de_Modulos.Seguridad
{
    public static class ConfiguracionSegura
    {
        private const string MarcadorConfiguracion = "CONFIGURAR_EN_ENV";

        /// <summary>
        /// Busca y carga automáticamente un archivo .env en la raíz del proyecto o solución,
        /// además de leer variables de entorno del sistema operativo y Secrets de GitHub.
        /// </summary>
        public static void CargarVariablesEntorno(WebApplicationBuilder builder)
        {
            var envDictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // 1. Rutas potenciales donde puede encontrarse el archivo .env
            var pathsToProbe = new List<string>
            {
                Path.Combine(builder.Environment.ContentRootPath, ".env"),
                Path.Combine(builder.Environment.ContentRootPath, "..", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                Path.Combine(AppContext.BaseDirectory, ".env"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")
            };

            string? foundEnvPath = null;
            foreach (var probePath in pathsToProbe)
            {
                try
                {
                    var fullPath = Path.GetFullPath(probePath);
                    if (File.Exists(fullPath))
                    {
                        foundEnvPath = fullPath;
                        break;
                    }
                }
                catch
                {
                    // Ignorar errores de ruta inválida
                }
            }

            // 2. Si se encuentra el archivo .env, parsear clave=valor
            if (foundEnvPath != null)
            {
                try
                {
                    var lines = File.ReadAllLines(foundEnvPath);
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        {
                            continue;
                        }

                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex <= 0)
                        {
                            continue;
                        }

                        string key = line.Substring(0, separatorIndex).Trim();
                        string value = line.Substring(separatorIndex + 1).Trim();

                        // Limpiar comillas iniciales y finales si existen
                        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                            (value.StartsWith("'") && value.EndsWith("'")))
                        {
                            if (value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                        }

                        envDictionary[key] = value;
                    }
                }
                catch
                {
                    // En caso de error de lectura, continuamos con variables de entorno del sistema
                }
            }

            // 3. Extraer variables (priorizando variables de entorno del sistema sobre .env local)
            string? dbConn = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                             ?? (envDictionary.TryGetValue("DB_CONNECTION_STRING", out var envDb) ? envDb : null);

            string? googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                                    ?? (envDictionary.TryGetValue("GOOGLE_CLIENT_ID", out var envGId) ? envGId : null);

            string? googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
                                        ?? (envDictionary.TryGetValue("GOOGLE_CLIENT_SECRET", out var envGSec) ? envGSec : null);

            // 4. Mapear al árbol de configuración IConfiguration de ASP.NET Core
            var inMemoryConfig = new Dictionary<string, string?>();

            if (!string.IsNullOrWhiteSpace(dbConn) && !dbConn.Contains(MarcadorConfiguracion))
            {
                inMemoryConfig["ConnectionStrings:DefaultConnection"] = dbConn;
            }

            if (!string.IsNullOrWhiteSpace(googleClientId) && !googleClientId.Contains(MarcadorConfiguracion))
            {
                inMemoryConfig["Authentication:Google:ClientId"] = googleClientId;
            }

            if (!string.IsNullOrWhiteSpace(googleClientSecret) && !googleClientSecret.Contains(MarcadorConfiguracion))
            {
                inMemoryConfig["Authentication:Google:ClientSecret"] = googleClientSecret;
            }

            if (inMemoryConfig.Count > 0)
            {
                builder.Configuration.AddInMemoryCollection(inMemoryConfig);
            }
        }

        /// <summary>
        /// Devuelve la cadena de conexión real de DefaultConnection o string.Empty
        /// si es nula, vacía o conserva el texto de marcador.
        /// </summary>
        public static string ObtenerCadenaConexion(IConfiguration configuration)
        {
            string? conn = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(conn))
            {
                return string.Empty;
            }

            if (conn.Contains(MarcadorConfiguracion, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return conn.Trim();
        }
    }
}
