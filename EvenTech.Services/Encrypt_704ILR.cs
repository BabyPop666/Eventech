using System.Security.Cryptography;
using System.Text;

namespace EvenTech.Services
{
    // SHA-256 hex de 64 caracteres. Mismo formato que la columna PasswordHash
    // de la tabla Users en DB.
    public static class Encrypt_704ILR
    {
        public static string HashValue_704ILR(string password_704ILR)
        {
            using (SHA256 sha_704ILR = SHA256.Create())
            {
                byte[] bytes_704ILR = sha_704ILR.ComputeHash(Encoding.UTF8.GetBytes(password_704ILR ?? string.Empty));
                StringBuilder sb_704ILR = new StringBuilder(bytes_704ILR.Length * 2);
                foreach (byte b_704ILR in bytes_704ILR)
                {
                    sb_704ILR.AppendFormat("{0:x2}", b_704ILR);
                }
                return sb_704ILR.ToString();
            }
        }
    }
}
