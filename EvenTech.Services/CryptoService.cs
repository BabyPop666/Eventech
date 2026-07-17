using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EvenTech.Services
{
    // Cifrado simetrico reversible (AES-256) para datos sensibles que el sistema
    // SI necesita leer de vuelta (email/telefono de clientes). Complementa al
    // hashing de Encrypt, que es unidireccional y se reserva para credenciales:
    // hash para verificar, AES para recuperar.
    //
    // Clave: 256 bits generada al azar en el primer uso y persistida en
    // %ProgramData%\EvenTech\crypto.key protegida con DPAPI (ambito maquina),
    // de modo que nunca queda hardcodeada ni en texto plano en disco.
    //
    // Formato almacenado: "ENC:" + Base64(IV de 16 bytes + ciphertext). El IV es
    // aleatorio por dato (dos textos iguales cifran distinto) y el prefijo permite
    // convivir con datos legados en texto plano, que se cifran al re-guardarse.
    public static class CryptoService
    {
        private const string Prefijo = "ENC:";
        private const int IvBytes = 16;

        private static readonly object _lock = new object();
        private static byte[] _key;

        public static string Proteger(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano) || EstaProtegido(textoPlano))
                return textoPlano;

            using (var aes = Aes.Create())
            {
                aes.Key = GetKey();
                aes.GenerateIV();

                byte[] plano = Encoding.UTF8.GetBytes(textoPlano);
                byte[] cifrado;
                using (var enc = aes.CreateEncryptor())
                    cifrado = enc.TransformFinalBlock(plano, 0, plano.Length);

                byte[] paquete = new byte[IvBytes + cifrado.Length];
                Buffer.BlockCopy(aes.IV, 0, paquete, 0, IvBytes);
                Buffer.BlockCopy(cifrado, 0, paquete, IvBytes, cifrado.Length);
                return Prefijo + Convert.ToBase64String(paquete);
            }
        }

        public static string Desproteger(string almacenado)
        {
            // Sin prefijo es un dato legado en texto plano: se devuelve tal cual.
            if (!EstaProtegido(almacenado)) return almacenado;

            try
            {
                byte[] paquete = Convert.FromBase64String(almacenado.Substring(Prefijo.Length));
                using (var aes = Aes.Create())
                {
                    aes.Key = GetKey();
                    byte[] iv = new byte[IvBytes];
                    Buffer.BlockCopy(paquete, 0, iv, 0, IvBytes);
                    aes.IV = iv;

                    using (var dec = aes.CreateDecryptor())
                    {
                        byte[] plano = dec.TransformFinalBlock(paquete, IvBytes, paquete.Length - IvBytes);
                        return Encoding.UTF8.GetString(plano);
                    }
                }
            }
            catch (CryptographicException)
            {
                // Clave distinta o dato corrupto: no se puede recuperar; se devuelve
                // lo almacenado para que la lectura no rompa la pantalla.
                return almacenado;
            }
            catch (FormatException)
            {
                return almacenado;
            }
        }

        public static bool EstaProtegido(string valor) =>
            valor != null && valor.StartsWith(Prefijo, StringComparison.Ordinal);

        private static byte[] GetKey()
        {
            if (_key != null) return _key;
            lock (_lock)
            {
                _key ??= CargarOCrearClave();
                return _key;
            }
        }

        private static byte[] CargarOCrearClave()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EvenTech");
            string ruta = Path.Combine(dir, "crypto.key");

            if (File.Exists(ruta))
                return ProtectedData.Unprotect(File.ReadAllBytes(ruta), null, DataProtectionScope.LocalMachine);

            byte[] clave = RandomNumberGenerator.GetBytes(32); // 256 bits
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(ruta, ProtectedData.Protect(clave, null, DataProtectionScope.LocalMachine));
            return clave;
        }
    }
}
