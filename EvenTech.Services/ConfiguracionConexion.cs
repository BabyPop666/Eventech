using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EvenTech.Services
{
    // Configuracion de la conexion a la base, persistida fuera del binario.
    //
    // La cadena de conexion puede contener credenciales, asi que no se guarda en
    // claro: se cifra con DPAPI (ambito usuario) en
    // %APPDATA%\EvenTech\connection.cfg. Es el mismo criterio que CryptoService
    // usa para la clave AES, pero con ambito CurrentUser porque la conexion es
    // una preferencia de quien opera la estacion de trabajo, no de la maquina.
    //
    // Si el archivo no existe, se usa PorDefecto: asi una instalacion nueva
    // arranca sin pedir configuracion cuando la base esta donde se espera.
    public static class ConfiguracionConexion
    {
        // Instancia y base esperadas en una instalacion estandar.
        public const string ServidorPorDefecto = ".";
        public const string BaseDatosPorDefecto = "EvenTechDB";

        public static string PorDefecto => Construir(ServidorPorDefecto, BaseDatosPorDefecto);

        private static readonly object _lock = new object();
        private static string _cache;

        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvenTech");
        private static string Archivo => Path.Combine(Dir, "connection.cfg");

        public static bool EstaConfigurada => File.Exists(Archivo);

        // Cadena vigente: la guardada si existe, la de fabrica si no.
        public static string Actual
        {
            get
            {
                if (_cache != null) return _cache;
                lock (_lock)
                {
                    _cache ??= Leer() ?? PorDefecto;
                    return _cache;
                }
            }
        }

        // Arma una cadena estandar (autenticacion integrada de Windows: el sistema
        // no maneja usuario/clave de SQL, de modo que no hay credenciales que
        // custodiar mas alla de la sesion del propio usuario).
        public static string Construir(string servidor, string baseDatos)
        {
            servidor = string.IsNullOrWhiteSpace(servidor) ? ServidorPorDefecto : servidor.Trim();
            baseDatos = string.IsNullOrWhiteSpace(baseDatos) ? BaseDatosPorDefecto : baseDatos.Trim();
            return $"Data Source={servidor};Initial Catalog={baseDatos};Integrated Security=True;TrustServerCertificate=True";
        }

        // Extrae un valor de la cadena vigente (para precargar el formulario de
        // configuracion sin acoplar la UI al formato de la cadena).
        public static string ValorDe(string clave, string porDefecto)
        {
            foreach (var parte in (Actual ?? "").Split(';'))
            {
                int i = parte.IndexOf('=');
                if (i <= 0) continue;
                if (parte.Substring(0, i).Trim().Equals(clave, StringComparison.OrdinalIgnoreCase))
                    return parte.Substring(i + 1).Trim();
            }
            return porDefecto;
        }

        public static string ServidorActual => ValorDe("Data Source", ServidorPorDefecto);
        public static string BaseDatosActual => ValorDe("Initial Catalog", BaseDatosPorDefecto);

        public static void Guardar(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("La cadena de conexion no puede estar vacia.", nameof(connectionString));

            lock (_lock)
            {
                Directory.CreateDirectory(Dir);
                byte[] datos = Encoding.UTF8.GetBytes(connectionString);
                File.WriteAllBytes(Archivo, ProtectedData.Protect(datos, null, DataProtectionScope.CurrentUser));
                _cache = connectionString;
            }
        }

        // Borra la configuracion guardada: la proxima lectura vuelve a la de fabrica.
        public static void Borrar()
        {
            lock (_lock)
            {
                try { if (File.Exists(Archivo)) File.Delete(Archivo); }
                catch { /* si no se puede borrar, la cadena guardada sigue vigente */ }
                _cache = null;
            }
        }

        private static string Leer()
        {
            try
            {
                if (!File.Exists(Archivo)) return null;
                byte[] plano = ProtectedData.Unprotect(File.ReadAllBytes(Archivo), null, DataProtectionScope.CurrentUser);
                string cs = Encoding.UTF8.GetString(plano);
                return string.IsNullOrWhiteSpace(cs) ? null : cs;
            }
            catch (CryptographicException)
            {
                // Archivo de otro usuario o corrupto: se ignora y se cae a la de
                // fabrica, de modo que la app siempre tenga por donde intentar.
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
