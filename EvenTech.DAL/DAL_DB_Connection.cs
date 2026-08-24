using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using EvenTech.Services;

namespace EvenTech.DAL
{
    // Conexion centralizada a SQL Server. La cadena ya no esta hardcodeada: se
    // resuelve desde ConfiguracionConexion (archivo cifrado con DPAPI), lo que
    // permite apuntar la app a otra instancia sin recompilar.
    public class DAL_DB_Connection : IDisposable
    {
        public static string ConnectionString => ConfiguracionConexion.Actual;

        private readonly SqlConnection _connection;

        public DAL_DB_Connection()
        {
            _connection = new SqlConnection(ConnectionString);
        }

        public SqlConnection Connection => _connection;

        public SqlConnection OpenConnection()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
            return _connection;
        }

        public void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
                _connection.Dispose();
            }
        }

        // ================== Diagnostico de conectividad ==================

        // Verifica que se pueda abrir la conexion Y que la base exista. Abrir con
        // Initial Catalog inexistente ya falla, pero se consulta sys.databases
        // igual para poder distinguir "no llego al servidor" de "el servidor esta
        // pero le falta la base", que son dos problemas con soluciones distintas.
        public static bool Probar(string connectionString, out string mensaje)
        {
            mensaje = null;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                mensaje = "La cadena de conexion esta vacia.";
                return false;
            }

            string baseDatos;
            try
            {
                baseDatos = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            }
            catch (Exception ex)
            {
                mensaje = "La cadena de conexion no es valida: " + ex.Message;
                return false;
            }

            // Primero contra master: si esto anda, el servidor responde y lo unico
            // que puede faltar es la base.
            var aMaster = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 5
            };

            try
            {
                using (var cn = new SqlConnection(aMaster.ConnectionString))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT COUNT(1) FROM sys.databases WHERE name = @db", cn))
                    {
                        cmd.Parameters.Add("@db", SqlDbType.NVarChar, 128).Value = baseDatos ?? "";
                        int existe = Convert.ToInt32(cmd.ExecuteScalar());
                        if (existe == 0)
                        {
                            mensaje = $"El servidor responde, pero no existe la base '{baseDatos}'. " +
                                      "Verifica el nombre o ejecuta el script de creacion.";
                            return false;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                mensaje = "No se pudo conectar al servidor: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensaje = "No se pudo conectar: " + ex.Message;
                return false;
            }

            // La base existe: se confirma que se pueda abrir Y que tenga el esquema.
            // Sin esta ultima verificacion se podria guardar una conexion a una base
            // vacia: el arranque pasaria y la app fallaria en cada pantalla, sin
            // volver a ofrecer la configuracion.
            try
            {
                var conBase = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5 };
                using (var cn = new SqlConnection(conBase.ConnectionString))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT OBJECT_ID('dbo.Users','U')", cn))
                    {
                        if (cmd.ExecuteScalar() == DBNull.Value)
                        {
                            mensaje = $"La base '{baseDatos}' existe pero no tiene el esquema de la aplicacion. " +
                                      "Ejecuta db/schema.sql sobre esa base o elegi otra.";
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"La base '{baseDatos}' existe pero no se pudo abrir: " + ex.Message;
                return false;
            }
        }

        // Prueba la cadena vigente (la que usa realmente la app).
        public static bool ProbarActual(out string mensaje) => Probar(ConnectionString, out mensaje);

        // Instancias candidatas para el combo de configuracion. Intenta detectarlas
        // con "sqlcmd -L"; si la herramienta no esta o no responde, cae a la lista
        // de instalaciones tipicas. Nunca lanza: es una ayuda, no un requisito.
        public static List<string> DetectarInstancias()
        {
            var instancias = new List<string>();

            try
            {
                var psi = new ProcessStartInfo("sqlcmd", "-L")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        // Ambas tuberias se drenan en paralelo y de forma asincronica:
                        // un ReadToEnd() sincronico bloquearia hasta que sqlcmd cierre
                        // la salida (dejando el timeout de abajo como codigo muerto), y
                        // no leer stderr puede llenar su buffer y trabar al hijo.
                        var tOut = proc.StandardOutput.ReadToEndAsync();
                        var tErr = proc.StandardError.ReadToEndAsync();

                        if (!proc.WaitForExit(5000))
                        {
                            try { proc.Kill(true); } catch { }
                            return Fijas(instancias);   // sin deteccion: se usa la lista de respaldo
                        }

                        // El proceso ya termino: las lecturas cierran enseguida.
                        string salida = tOut.Wait(2000) ? tOut.Result : string.Empty;
                        tErr.Wait(500);

                        foreach (string linea in salida.Split('\n'))
                        {
                            string s = linea.Trim();
                            // La primera linea es el encabezado ("Servers:") y las
                            // entradas remotas vienen con doble barra inicial.
                            if (s.Length == 0 || s.EndsWith(":", StringComparison.Ordinal)) continue;
                            s = s.TrimStart('\\');
                            if (s.Length > 0 && !instancias.Contains(s)) instancias.Add(s);
                        }
                    }
                }
            }
            catch { /* sqlcmd ausente o sin permisos: se usa la lista de respaldo */ }

            return Fijas(instancias);
        }

        // Completa la lista con las instalaciones tipicas, sin repetir.
        private static List<string> Fijas(List<string> instancias)
        {
            foreach (string fija in new[] { ".", @".\SQLEXPRESS", "localhost", @"localhost\SQLEXPRESS", @"(localdb)\MSSQLLocalDB" })
                if (!instancias.Contains(fija)) instancias.Add(fija);
            return instancias;
        }
    }
}
