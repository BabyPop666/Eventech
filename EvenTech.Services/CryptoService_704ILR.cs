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
            // Sin la forma de un paquete AES-CBC (IV + al menos un bloque, multiplo de 16) no
            // es descifrable: un dato legado en texto plano, un "ENC:" tipeado a mano o un
            // dato truncado se devuelven tal cual, igual que un dato de otra maquina, para
            // que una fila rota no voltee el listado completo.
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
            catch (ArgumentException)
            {
                // Red de seguridad para un paquete con forma inesperada.
                return almacenado_704ILR;
            }
            catch (InvalidOperationException)
            {
                // La clave local no se puede leer (ver CargarOCrearClave): la lectura
                // tolera el fallo y muestra el dato como quedo almacenado; el guardado
                // es el que lo informa.
                return almacenado_704ILR;
            }
        }

        // "Ya cifrado" es SOLO la forma exacta que produce Proteger: prefijo, Base64
        // canonico y un paquete de IV mas al menos un bloque, con largo multiplo de 16.
        // Mirar solo el prefijo confundia un paquete que no se puede abrir (la base
        // restaurada en otra PC) con un texto tipeado que empieza con "ENC:": ese texto
        // salteaba la validacion del email y se guardaba sin cifrar.
        public static bool EstaProtegido_704ILR(string valor_704ILR)
        {
            if (valor_704ILR == null || !valor_704ILR.StartsWith(Prefijo_704ILR, StringComparison.Ordinal)) return false;
            string cuerpo_704ILR = valor_704ILR.Substring(Prefijo_704ILR.Length);
            if (cuerpo_704ILR.Length == 0 || cuerpo_704ILR.Length % 4 != 0) return false;
            byte[] paquete_704ILR;
            try { paquete_704ILR = Convert.FromBase64String(cuerpo_704ILR); }
            catch (FormatException) { return false; }
            return paquete_704ILR.Length >= IvBytes_704ILR * 2
                && paquete_704ILR.Length % IvBytes_704ILR == 0
                && Convert.ToBase64String(paquete_704ILR) == cuerpo_704ILR;
        }

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
            {
                try
                {
                    return ProtectedData.Unprotect(File.ReadAllBytes(ruta_704ILR), null, DataProtectionScope.LocalMachine);
                }
                catch (Exception ex_704ILR) when (ex_704ILR is CryptographicException || ex_704ILR is IOException || ex_704ILR is UnauthorizedAccessException)
                {
                    // Archivo truncado, copiado de otra PC o ProgramData restaurado: DPAPI
                    // no lo abre. Se informa con la ruta y que hacer, en vez de dejar que
                    // el alta de un cliente muera con una excepcion cruda del framework.
                    throw new InvalidOperationException(
                        Texto_704ILR("CRYPTO_CLAVE_INVALIDA",
                            "La clave de cifrado {0} no se puede leer: está dañada o fue creada en otra máquina. " +
                            "Restaure el archivo original o elimínelo para generar una clave nueva " +
                            "(los contactos ya cifrados quedarán ilegibles).", ruta_704ILR), ex_704ILR);
                }
            }

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

        // Mensaje traducido con respaldo: si la clave no esta cargada (o el texto
        // editado esta mal formado) se usa el texto por defecto del codigo.
        // Mismo criterio que Tr_704ILR.F_704ILR: una plantilla traducida que no formatea
        // o que solo agrega relleno desmesurado cae al texto por defecto.
        private static string Texto_704ILR(string clave_704ILR, string defecto_704ILR, params object[] args_704ILR)
            => GestorDeIdioma_704ILR.GetInstance_704ILR.Formatear_704ILR(clave_704ILR, defecto_704ILR, args_704ILR);
    }
}
