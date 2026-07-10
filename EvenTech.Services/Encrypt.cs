using System;
using System.Security.Cryptography;
using System.Text;

namespace EvenTech.Services
{
    // Hashing de contrasenas. Esquema actual: PBKDF2 (SHA-256) con salt aleatorio
    // por usuario, serializado como "PBKDF2$iteraciones$saltB64$hashB64". El salt
    // evita rainbow tables y hace que el hash almacenado NO sea reutilizable como
    // credencial (la verificacion es server-side, con el salt de la base).
    //
    // Compatibilidad: los usuarios legacy tienen SHA-256 hex (64 chars) sin salt.
    // Verify() los reconoce y siguen entrando; BLL_Login los migra a PBKDF2 en el
    // proximo login exitoso. HashValue() se conserva solo para ese camino legacy.
    public static class Encrypt
    {
        private const int Iteraciones = 100_000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const string Prefijo = "PBKDF2$";

        // SHA-256 hex de 64 caracteres (esquema legacy). Se mantiene para poder
        // validar y migrar los hashes viejos ya persistidos.
        public static string HashValue(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
        }

        // Genera un hash salteado PBKDF2 autocontenido para almacenar en la base.
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty, salt, Iteraciones, HashAlgorithmName.SHA256, HashBytes);
            return Prefijo + Iteraciones + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        // Verifica una contrasena en claro contra el hash almacenado, soportando
        // tanto el formato PBKDF2 nuevo como el SHA-256 legacy. Comparacion en
        // tiempo constante en ambos casos.
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            if (stored.StartsWith(Prefijo, StringComparison.Ordinal))
            {
                string[] parts = stored.Split('$');
                if (parts.Length != 4) return false;
                if (!int.TryParse(parts[1], out int iter) || iter <= 0) return false;
                byte[] salt, esperado;
                try
                {
                    salt = Convert.FromBase64String(parts[2]);
                    esperado = Convert.FromBase64String(parts[3]);
                }
                catch (FormatException) { return false; }

                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password ?? string.Empty, salt, iter, HashAlgorithmName.SHA256, esperado.Length);
                return CryptographicOperations.FixedTimeEquals(actual, esperado);
            }

            // Legacy: SHA-256 hex sin salt.
            return AuthenticationService.CompareHashedPasswords(HashValue(password), stored);
        }

        // Indica si el hash almacenado esta en el formato legacy (para migrarlo).
        public static bool EsLegacy(string stored) =>
            !string.IsNullOrEmpty(stored) && !stored.StartsWith(Prefijo, StringComparison.Ordinal);
    }
}
