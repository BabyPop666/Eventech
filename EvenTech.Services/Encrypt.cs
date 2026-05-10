using System.Security.Cryptography;
using System.Text;

namespace EvenTech.Services
{
    // SHA-256 hex de 64 caracteres. Mismo formato que la columna PasswordHash
    // de la tabla Users en DB.
    public static class Encrypt
    {
        public static string HashValue(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }
        }
    }
}
