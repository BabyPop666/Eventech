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
    public class DAL_DB_Connection_704ILR : IDisposable
    {
        public static string ConnectionString_704ILR => ConfiguracionConexion_704ILR.Actual_704ILR;

        private readonly SqlConnection _connection_704ILR;

        public DAL_DB_Connection_704ILR()
        {
            _connection_704ILR = new SqlConnection(ConnectionString_704ILR);
        }

        public SqlConnection Connection_704ILR => _connection_704ILR;

        public SqlConnection OpenConnection_704ILR()
        {
            if (_connection_704ILR.State == ConnectionState.Closed)
                _connection_704ILR.Open();
            return _connection_704ILR;
        }

        public void CloseConnection_704ILR()
        {
            if (_connection_704ILR.State == ConnectionState.Open)
                _connection_704ILR.Close();
        }

        public void Dispose()
        {
            if (_connection_704ILR != null)
            {
                if (_connection_704ILR.State == ConnectionState.Open)
                    _connection_704ILR.Close();
                _connection_704ILR.Dispose();
            }
        }

        // ================== Diagnostico de conectividad ==================

        // Verifica que se pueda abrir la conexion Y que la base exista. Abrir con
        // Initial Catalog inexistente ya falla, pero se consulta sys.databases
        // igual para poder distinguir "no llego al servidor" de "el servidor esta
        // pero le falta la base", que son dos problemas con soluciones distintas.
        public static bool Probar_704ILR(string connectionString_704ILR, out string mensaje_704ILR)
        {
            mensaje_704ILR = null;
            if (string.IsNullOrWhiteSpace(connectionString_704ILR))
            {
                mensaje_704ILR = "La cadena de conexion esta vacia.";
                return false;
            }

            string baseDatos_704ILR;
            try
            {
                baseDatos_704ILR = new SqlConnectionStringBuilder(connectionString_704ILR).InitialCatalog;
            }
            catch (Exception ex_704ILR)
            {
                mensaje_704ILR = "La cadena de conexion no es valida: " + ex_704ILR.Message;
                return false;
            }

            // Primero contra master: si esto anda, el servidor responde y lo unico
            // que puede faltar es la base.
            var aMaster_704ILR = new SqlConnectionStringBuilder(connectionString_704ILR)
            {
                InitialCatalog = "master",
                ConnectTimeout = 5
            };

            try
            {
                using (var cn_704ILR = new SqlConnection(aMaster_704ILR.ConnectionString))
                {
                    cn_704ILR.Open();
                    using (var cmd_704ILR = new SqlCommand("SELECT COUNT(1) FROM sys.databases WHERE name = @db", cn_704ILR))
                    {
                        cmd_704ILR.Parameters.Add("@db", SqlDbType.NVarChar, 128).Value = baseDatos_704ILR ?? "";
                        int existe_704ILR = Convert.ToInt32(cmd_704ILR.ExecuteScalar());
                        if (existe_704ILR == 0)
                        {
                            mensaje_704ILR = $"El servidor responde, pero no existe la base '{baseDatos_704ILR}'. " +
                                      "Verifica el nombre o ejecuta el script de creacion.";
                            return false;
                        }
                    }
                }
            }
            catch (SqlException ex_704ILR)
            {
                mensaje_704ILR = "No se pudo conectar al servidor: " + ex_704ILR.Message;
                return false;
            }
            catch (Exception ex_704ILR)
            {
                mensaje_704ILR = "No se pudo conectar: " + ex_704ILR.Message;
                return false;
            }

            // La base existe: se confirma que se pueda abrir Y que tenga el esquema.
            // Sin esta ultima verificacion se podria guardar una conexion a una base
            // vacia: el arranque pasaria y la app fallaria en cada pantalla, sin
            // volver a ofrecer la configuracion.
            try
            {
                var conBase_704ILR = new SqlConnectionStringBuilder(connectionString_704ILR) { ConnectTimeout = 5 };
                using (var cn_704ILR = new SqlConnection(conBase_704ILR.ConnectionString))
                {
                    cn_704ILR.Open();
                    using (var cmd_704ILR = new SqlCommand("SELECT OBJECT_ID('dbo.Users','U')", cn_704ILR))
                    {
                        if (cmd_704ILR.ExecuteScalar() == DBNull.Value)
                        {
                            mensaje_704ILR = $"La base '{baseDatos_704ILR}' existe pero no tiene el esquema de la aplicacion. " +
                                      "Ejecuta db/schema.sql sobre esa base o elegi otra.";
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex_704ILR)
            {
                mensaje_704ILR = $"La base '{baseDatos_704ILR}' existe pero no se pudo abrir: " + ex_704ILR.Message;
                return false;
            }
        }

        // Prueba la cadena vigente (la que usa realmente la app).
        public static bool ProbarActual_704ILR(out string mensaje_704ILR) => Probar_704ILR(ConnectionString_704ILR, out mensaje_704ILR);

        // Instancias candidatas para el combo de configuracion. Intenta detectarlas
        // con "sqlcmd -L"; si la herramienta no esta o no responde, cae a la lista
        // de instalaciones tipicas. Nunca lanza: es una ayuda, no un requisito.
        public static List<string> DetectarInstancias_704ILR()
        {
            var instancias_704ILR = new List<string>();

            try
            {
                var psi_704ILR = new ProcessStartInfo("sqlcmd", "-L")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc_704ILR = Process.Start(psi_704ILR))
                {
                    if (proc_704ILR != null)
                    {
                        // Ambas tuberias se drenan en paralelo y de forma asincronica:
                        // un ReadToEnd() sincronico bloquearia hasta que sqlcmd cierre
                        // la salida (dejando el timeout de abajo como codigo muerto), y
                        // no leer stderr puede llenar su buffer y trabar al hijo.
                        var tOut_704ILR = proc_704ILR.StandardOutput.ReadToEndAsync();
                        var tErr_704ILR = proc_704ILR.StandardError.ReadToEndAsync();

                        if (!proc_704ILR.WaitForExit(5000))
                        {
                            try { proc_704ILR.Kill(true); } catch { }
                            return Fijas_704ILR(instancias_704ILR);   // sin deteccion: se usa la lista de respaldo
                        }

                        // El proceso ya termino: las lecturas cierran enseguida.
                        string salida_704ILR = tOut_704ILR.Wait(2000) ? tOut_704ILR.Result : string.Empty;
                        tErr_704ILR.Wait(500);

                        foreach (string linea_704ILR in salida_704ILR.Split('\n'))
                        {
                            string s_704ILR = linea_704ILR.Trim();
                            // La primera linea es el encabezado ("Servers:") y las
                            // entradas remotas vienen con doble barra inicial.
                            if (s_704ILR.Length == 0 || s_704ILR.EndsWith(":", StringComparison.Ordinal)) continue;
                            s_704ILR = s_704ILR.TrimStart('\\');
                            if (s_704ILR.Length > 0 && !instancias_704ILR.Contains(s_704ILR)) instancias_704ILR.Add(s_704ILR);
                        }
                    }
                }
            }
            catch { /* sqlcmd ausente o sin permisos: se usa la lista de respaldo */ }

            return Fijas_704ILR(instancias_704ILR);
        }

        // Completa la lista con las instalaciones tipicas, sin repetir.
        private static List<string> Fijas_704ILR(List<string> instancias_704ILR)
        {
            foreach (string fija_704ILR in new[] { ".", @".\SQLEXPRESS", "localhost", @"localhost\SQLEXPRESS", @"(localdb)\MSSQLLocalDB" })
                if (!instancias_704ILR.Contains(fija_704ILR)) instancias_704ILR.Add(fija_704ILR);
            return instancias_704ILR;
        }
    }
}
