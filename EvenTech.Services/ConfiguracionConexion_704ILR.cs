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
    public static class ConfiguracionConexion_704ILR
    {
        // Instancia y base esperadas en una instalacion estandar.
        public const string ServidorPorDefecto_704ILR = ".";
        public const string BaseDatosPorDefecto_704ILR = "EvenTechDB";

        public static string PorDefecto_704ILR => Construir_704ILR(ServidorPorDefecto_704ILR, BaseDatosPorDefecto_704ILR);

        private static readonly object _lock_704ILR = new object();
        private static string _cache_704ILR;

        private static string Dir_704ILR =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvenTech");
        private static string Archivo_704ILR => Path.Combine(Dir_704ILR, "connection.cfg");

        public static bool EstaConfigurada_704ILR => File.Exists(Archivo_704ILR);

        // Cadena vigente: la guardada si existe, la de fabrica si no.
        public static string Actual_704ILR
        {
            get
            {
                if (_cache_704ILR != null) return _cache_704ILR;
                lock (_lock_704ILR)
                {
                    _cache_704ILR ??= Leer_704ILR() ?? PorDefecto_704ILR;
                    return _cache_704ILR;
                }
            }
        }

        // Arma una cadena estandar (autenticacion integrada de Windows: el sistema
        // no maneja usuario/clave de SQL, de modo que no hay credenciales que
        // custodiar mas alla de la sesion del propio usuario).
        public static string Construir_704ILR(string servidor_704ILR, string baseDatos_704ILR)
        {
            servidor_704ILR = string.IsNullOrWhiteSpace(servidor_704ILR) ? ServidorPorDefecto_704ILR : servidor_704ILR.Trim();
            baseDatos_704ILR = string.IsNullOrWhiteSpace(baseDatos_704ILR) ? BaseDatosPorDefecto_704ILR : baseDatos_704ILR.Trim();
            return $"Data Source={servidor_704ILR};Initial Catalog={baseDatos_704ILR};Integrated Security=True;TrustServerCertificate=True";
        }

        // Extrae un valor de la cadena vigente (para precargar el formulario de
        // configuracion sin acoplar la UI al formato de la cadena).
        public static string ValorDe_704ILR(string clave_704ILR, string porDefecto_704ILR)
        {
            foreach (var parte_704ILR in (Actual_704ILR ?? "").Split(';'))
            {
                int i_704ILR = parte_704ILR.IndexOf('=');
                if (i_704ILR <= 0) continue;
                if (parte_704ILR.Substring(0, i_704ILR).Trim().Equals(clave_704ILR, StringComparison.OrdinalIgnoreCase))
                    return parte_704ILR.Substring(i_704ILR + 1).Trim();
            }
            return porDefecto_704ILR;
        }

        public static string ServidorActual_704ILR => ValorDe_704ILR("Data Source", ServidorPorDefecto_704ILR);
        public static string BaseDatosActual_704ILR => ValorDe_704ILR("Initial Catalog", BaseDatosPorDefecto_704ILR);

        public static void Guardar_704ILR(string connectionString_704ILR)
        {
            if (string.IsNullOrWhiteSpace(connectionString_704ILR))
                throw new ArgumentException("La cadena de conexion no puede estar vacia.", nameof(connectionString_704ILR));

            lock (_lock_704ILR)
            {
                Directory.CreateDirectory(Dir_704ILR);
                byte[] datos_704ILR = Encoding.UTF8.GetBytes(connectionString_704ILR);
                File.WriteAllBytes(Archivo_704ILR, ProtectedData.Protect(datos_704ILR, null, DataProtectionScope.CurrentUser));
                _cache_704ILR = connectionString_704ILR;
            }
        }

        // Borra la configuracion guardada: la proxima lectura vuelve a la de fabrica.
        public static void Borrar_704ILR()
        {
            lock (_lock_704ILR)
            {
                try { if (File.Exists(Archivo_704ILR)) File.Delete(Archivo_704ILR); }
                catch { /* si no se puede borrar, la cadena guardada sigue vigente */ }
                _cache_704ILR = null;
            }
        }

        private static string Leer_704ILR()
        {
            try
            {
                if (!File.Exists(Archivo_704ILR)) return null;
                byte[] plano_704ILR = ProtectedData.Unprotect(File.ReadAllBytes(Archivo_704ILR), null, DataProtectionScope.CurrentUser);
                string cs_704ILR = Encoding.UTF8.GetString(plano_704ILR);
                return string.IsNullOrWhiteSpace(cs_704ILR) ? null : cs_704ILR;
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
