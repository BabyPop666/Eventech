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
    public static class CryptoService_704ILR
    {
        private const string Prefijo_704ILR = "ENC:";
        private const int IvBytes_704ILR = 16;

        private static readonly object _lock_704ILR = new object();
        private static byte[] _key_704ILR;

        public static string Proteger_704ILR(string textoPlano_704ILR)
        {
            if (string.IsNullOrEmpty(textoPlano_704ILR) || EstaProtegido_704ILR(textoPlano_704ILR))
                return textoPlano_704ILR;

            using (var aes_704ILR = Aes.Create())
            {
                aes_704ILR.Key = GetKey_704ILR();
                aes_704ILR.GenerateIV();

                byte[] plano_704ILR = Encoding.UTF8.GetBytes(textoPlano_704ILR);
                byte[] cifrado_704ILR;
                using (var enc_704ILR = aes_704ILR.CreateEncryptor())
                    cifrado_704ILR = enc_704ILR.TransformFinalBlock(plano_704ILR, 0, plano_704ILR.Length);

                byte[] paquete_704ILR = new byte[IvBytes_704ILR + cifrado_704ILR.Length];
                Buffer.BlockCopy(aes_704ILR.IV, 0, paquete_704ILR, 0, IvBytes_704ILR);
                Buffer.BlockCopy(cifrado_704ILR, 0, paquete_704ILR, IvBytes_704ILR, cifrado_704ILR.Length);
                return Prefijo_704ILR + Convert.ToBase64String(paquete_704ILR);
            }
        }

        public static string Desproteger_704ILR(string almacenado_704ILR)
        {
            // Sin prefijo es un dato legado en texto plano: se devuelve tal cual.
            if (!EstaProtegido_704ILR(almacenado_704ILR)) return almacenado_704ILR;

            try
            {
                byte[] paquete_704ILR = Convert.FromBase64String(almacenado_704ILR.Substring(Prefijo_704ILR.Length));
                using (var aes_704ILR = Aes.Create())
                {
                    aes_704ILR.Key = GetKey_704ILR();
                    byte[] iv_704ILR = new byte[IvBytes_704ILR];
                    Buffer.BlockCopy(paquete_704ILR, 0, iv_704ILR, 0, IvBytes_704ILR);
                    aes_704ILR.IV = iv_704ILR;

                    using (var dec_704ILR = aes_704ILR.CreateDecryptor())
                    {
                        byte[] plano_704ILR = dec_704ILR.TransformFinalBlock(paquete_704ILR, IvBytes_704ILR, paquete_704ILR.Length - IvBytes_704ILR);
                        return Encoding.UTF8.GetString(plano_704ILR);
                    }
                }
            }
            catch (CryptographicException)
            {
                // Clave distinta o dato corrupto: no se puede recuperar; se devuelve
                // lo almacenado para que la lectura no rompa la pantalla.
                return almacenado_704ILR;
            }
            catch (FormatException)
            {
                return almacenado_704ILR;
            }
        }

        public static bool EstaProtegido_704ILR(string valor_704ILR) =>
            valor_704ILR != null && valor_704ILR.StartsWith(Prefijo_704ILR, StringComparison.Ordinal);

        private static byte[] GetKey_704ILR()
        {
            if (_key_704ILR != null) return _key_704ILR;
            lock (_lock_704ILR)
            {
                _key_704ILR ??= CargarOCrearClave_704ILR();
                return _key_704ILR;
            }
        }

        private static byte[] CargarOCrearClave_704ILR()
        {
            string dir_704ILR = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EvenTech");
            string ruta_704ILR = Path.Combine(dir_704ILR, "crypto.key");

            if (File.Exists(ruta_704ILR))
                return ProtectedData.Unprotect(File.ReadAllBytes(ruta_704ILR), null, DataProtectionScope.LocalMachine);

            byte[] clave_704ILR = RandomNumberGenerator.GetBytes(32); // 256 bits
            try
            {
                Directory.CreateDirectory(dir_704ILR);
                File.WriteAllBytes(ruta_704ILR, ProtectedData.Protect(clave_704ILR, null, DataProtectionScope.LocalMachine));
            }
            catch (Exception ex_704ILR) when (ex_704ILR is UnauthorizedAccessException || ex_704ILR is IOException)
            {
                // Sin escritura en ProgramData la clave no se puede persistir. Se
                // traduce a un error de operacion con la causa a la vista: de lo
                // contrario el alta de un cliente falla con un mensaje del sistema de
                // archivos que no dice que hacer.
                throw new InvalidOperationException(
                    "No se pudo crear la clave de cifrado en " + ruta_704ILR + ": " + ex_704ILR.Message, ex_704ILR);
            }
            return clave_704ILR;
        }
    }
}
