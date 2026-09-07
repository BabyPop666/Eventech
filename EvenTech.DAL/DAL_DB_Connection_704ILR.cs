using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
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

        public SqlConnection OpenConnection_704ILR()
        {
            if (_connection_704ILR.State == ConnectionState.Closed)
                _connection_704ILR.Open();
            return _connection_704ILR;
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

        // Contrato minimo del esquema que la app necesita para arrancar: las 20
        // tablas de db/schema.sql y las columnas que ese script agrega por
        // migracion (una base de una revision anterior tiene Users pero no estas).
        // Nombres de tabla/columna sin sufijo: son datos de la base, no
        // identificadores del codigo.
        private static readonly string[] TablasRequeridas_704ILR =
        {
            "Users", "LoginAuditLog", "Perfiles", "Permisos", "PerfilPermiso", "PerfilIncluido",
            "Clientes", "Salones", "Reservas", "Servicios", "ReservaServicio", "Pagos", "MetodosPago",
            "Bitacora", "HistorialCambios", "ReservaMemento", "ReservaMementoServicio",
            "Idiomas", "Traducciones", "DVVertical"
        };
        private static readonly string[] ColumnasRequeridas_704ILR =
        {
            "Users.PerfilId", "Users.Activo", "Users.Blocked", "Users.FailedAttempts",
            "Reservas.VenceEl", "Reservas.CantidadInvitados", "Reservas.Dvh"
        };

        // Verifica que se pueda abrir la conexion Y que la base exista. Abrir con
        // Initial Catalog inexistente ya falla, pero se consulta sys.databases
        // igual para poder distinguir "no llego al servidor" de "el servidor esta
        // pero le falta la base", que son dos problemas con soluciones distintas.
        // Con la base abierta se distingue ademas "sin esquema" (no existe Users)
        // de "esquema incompleto o anterior" (falta alguna tabla o columna).
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

            // La base existe: se confirma que se pueda abrir Y que tenga el esquema
            // completo. Sin esta ultima verificacion se podria guardar una conexion a
            // una base vacia o de una revision anterior: el arranque pasaria y la app
            // fallaria en el login o en cada pantalla, sin volver a ofrecer la
            // configuracion.
            try
            {
                var conBase_704ILR = new SqlConnectionStringBuilder(connectionString_704ILR) { ConnectTimeout = 5 };
                using (var cn_704ILR = new SqlConnection(conBase_704ILR.ConnectionString))
                {
                    cn_704ILR.Open();
                    List<string> faltantes_704ILR = ObjetosFaltantes_704ILR(cn_704ILR);
                    if (faltantes_704ILR.Contains("Users"))
                    {
                        mensaje_704ILR = $"La base '{baseDatos_704ILR}' existe pero no tiene el esquema de la aplicacion. " +
                                  "Ejecuta db/schema.sql sobre esa base o elegi otra.";
                        return false;
                    }
                    if (faltantes_704ILR.Count > 0)
                    {
                        mensaje_704ILR = Texto_704ILR("CONN_ESQUEMA_INCOMPLETO",
                            "La base '{0}' existe pero su esquema esta incompleto o es de una version anterior (faltan: {1}). " +
                            "Completalo con db/schema.sql (agrega solo lo que falta) antes de usarla.",
                            baseDatos_704ILR, string.Join(", ", faltantes_704ILR));
                        return false;
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

        // Tablas y columnas del contrato que la base abierta NO tiene, en una sola
        // consulta: sys.tables para las tablas y COL_LENGTH para las columnas. Las
        // listas VALUES se arman desde los arreglos constantes de arriba (nombres
        // fijos del codigo, no entrada del usuario).
        private static List<string> ObjetosFaltantes_704ILR(SqlConnection cn_704ILR)
        {
            var tablas_704ILR = new StringBuilder();
            foreach (string t_704ILR in TablasRequeridas_704ILR)
                tablas_704ILR.Append(tablas_704ILR.Length == 0 ? "" : ",").Append("('").Append(t_704ILR).Append("')");

            var columnas_704ILR = new StringBuilder();
            foreach (string c_704ILR in ColumnasRequeridas_704ILR)
            {
                string[] partes_704ILR = c_704ILR.Split('.');
                columnas_704ILR.Append(columnas_704ILR.Length == 0 ? "" : ",")
                    .Append("('").Append(partes_704ILR[0]).Append("','").Append(partes_704ILR[1]).Append("')");
            }

            string sql_704ILR =
                "SELECT v.n FROM (VALUES " + tablas_704ILR + ") v(n) " +
                "WHERE NOT EXISTS (SELECT 1 FROM sys.tables s WHERE s.name = v.n AND SCHEMA_NAME(s.schema_id) = 'dbo') " +
                "UNION ALL " +
                "SELECT v.t + '.' + v.c FROM (VALUES " + columnas_704ILR + ") v(t, c) " +
                "WHERE OBJECT_ID('dbo.' + v.t, 'U') IS NOT NULL AND COL_LENGTH('dbo.' + v.t, v.c) IS NULL";

            var faltantes_704ILR = new List<string>();
            using (var cmd_704ILR = new SqlCommand(sql_704ILR, cn_704ILR))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read()) faltantes_704ILR.Add(r_704ILR.GetString(0));
            }
            return faltantes_704ILR;
        }

        // Mensaje traducido con respaldo: al arrancar las traducciones todavia no
        // estan cargadas (o el texto editado puede estar mal formado), y en ese caso
        // se usa el texto por defecto del codigo.
        private static string Texto_704ILR(string clave_704ILR, string defecto_704ILR, params object[] args_704ILR)
        {
            string plantilla_704ILR = GestorDeIdioma_704ILR.GetInstance_704ILR.Traducir_704ILR(clave_704ILR);
            if (plantilla_704ILR == clave_704ILR) plantilla_704ILR = defecto_704ILR;
            try { return string.Format(plantilla_704ILR, args_704ILR); }
            catch (FormatException) { return string.Format(defecto_704ILR, args_704ILR); }
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
