using System;
using System.Collections.Generic;
using System.Threading;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Reglas de la configuracion de conexion a la base. La UI no arma cadenas ni
    // toca archivos: pide instancias, prueba y guarda a traves de esta clase.
    //
    // Todo el modulo corre ANTES del login (sin sesion, y a veces sin base), asi
    // que los registros en bitacora se hacen best-effort: si la base no esta,
    // BLL_Bitacora ya los descarta en silencio sin romper el arranque.
    public static class BLL_Conexion_704ILR
    {
        public static string CadenaActual_704ILR => ConfiguracionConexion_704ILR.Actual_704ILR;
        public static string ServidorActual_704ILR => ConfiguracionConexion_704ILR.ServidorActual_704ILR;
        public static string BaseDatosActual_704ILR => ConfiguracionConexion_704ILR.BaseDatosActual_704ILR;
        public static bool EstaConfigurada_704ILR => ConfiguracionConexion_704ILR.EstaConfigurada_704ILR;

        public static List<string> GetInstancias_704ILR() => DAL_DB_Connection_704ILR.DetectarInstancias_704ILR();

        public static string Construir_704ILR(string servidor_704ILR, string baseDatos_704ILR)
            => ConfiguracionConexion_704ILR.Construir_704ILR(servidor_704ILR, baseDatos_704ILR);

        // Verifica la conectividad con la cadena vigente. Es el chequeo del arranque.
        public static bool VerificarActual_704ILR(out string mensaje_704ILR)
            => DAL_DB_Connection_704ILR.ProbarActual_704ILR(out mensaje_704ILR);

        // Prueba una configuracion candidata sin guardarla (boton "Probar").
        public static bool Probar_704ILR(string servidor_704ILR, string baseDatos_704ILR, out string mensaje_704ILR)
        {
            if (!Completa_704ILR(servidor_704ILR, baseDatos_704ILR, out mensaje_704ILR)) return false;
            return DAL_DB_Connection_704ILR.Probar_704ILR(ConfiguracionConexion_704ILR.Construir_704ILR(servidor_704ILR, baseDatos_704ILR), out mensaje_704ILR);
        }

        // Guarda la configuracion solo si conecta: evita dejar la app apuntando a
        // una instancia inexistente y tener que reconfigurar a ciegas.
        public static bool Guardar_704ILR(string servidor_704ILR, string baseDatos_704ILR, out string mensaje_704ILR)
            => Guardar_704ILR(servidor_704ILR, baseDatos_704ILR, CancellationToken.None, out mensaje_704ILR);

        // Variante para quien guarda fuera del hilo de la interfaz (la pantalla de
        // configuracion): la prueba puede demorar y, mientras tanto, la pantalla puede
        // cerrarse o dejar de mostrar lo que se esta guardando. La cancelacion se
        // consulta despues de la prueba, justo antes de escribir: si se pidio, no se
        // escribe ni se asienta nada y se devuelve false sin mensaje.
        public static bool Guardar_704ILR(string servidor_704ILR, string baseDatos_704ILR, CancellationToken cancelacion_704ILR, out string mensaje_704ILR)
            => Guardar_704ILR(servidor_704ILR, baseDatos_704ILR, () => !cancelacion_704ILR.IsCancellationRequested, out mensaje_704ILR);

        // Variante con confirmacion de escritura. Consultar un indicador de cancelacion
        // no alcanza cuando quien espera el resultado puede irse en cualquier momento:
        // si la pantalla se cierra justo despues de esa consulta, la configuracion queda
        // escrita, el asiento se pierde con el proceso y la pantalla informa que no se
        // guardo nada. 'confirmarEscritura' se invoca una sola vez, despues de la prueba
        // y antes de tocar el archivo: si devuelve false no se escribe ni se asienta nada
        // y se devuelve false sin mensaje; si devuelve true, la escritura y el asiento se
        // completan sin volver a consultarla. Quien la implementa decide ahi, de forma
        // atomica, entre dejar escribir y dejar cerrar: la pantalla de configuracion la
        // usa para no cerrar a mitad de un guardado ya comprometido.
        public static bool Guardar_704ILR(string servidor_704ILR, string baseDatos_704ILR, Func<bool> confirmarEscritura_704ILR, out string mensaje_704ILR)
        {
            if (!Completa_704ILR(servidor_704ILR, baseDatos_704ILR, out mensaje_704ILR)) return false;

            string cadena_704ILR = ConfiguracionConexion_704ILR.Construir_704ILR(servidor_704ILR, baseDatos_704ILR);
            if (!DAL_DB_Connection_704ILR.Probar_704ILR(cadena_704ILR, out mensaje_704ILR)) return false;

            if (confirmarEscritura_704ILR != null && !confirmarEscritura_704ILR())
            {
                mensaje_704ILR = null;
                return false;
            }

            try
            {
                ConfiguracionConexion_704ILR.Guardar_704ILR(cadena_704ILR);
            }
            catch (Exception ex_704ILR)
            {
                // El mensaje de .NET no se muestra: no esta traducido y trae la ruta del perfil de
                // Windows. La pantalla informa una causa legible en el idioma activo. No se asienta: la
                // conexion vigente sigue siendo la anterior (en el arranque, la que no responde) y
                // esperar su tiempo de conexion dejaba la pantalla en "Guardando" sin poder cerrarse.
                string causa_704ILR = ex_704ILR is UnauthorizedAccessException
                    ? Texto_704ILR("CONN_CAUSA_ACCESO", "No hay permiso para escribir el archivo de configuración.")
                    : ex_704ILR is System.IO.IOException
                        ? Texto_704ILR("CONN_CAUSA_EN_USO", "El archivo de configuración está en uso o no se pudo escribir.")
                        : null;
                mensaje_704ILR = Texto_704ILR("CONN_NO_GUARDADA", "No se pudo guardar la configuración.")
                    + (causa_704ILR == null ? string.Empty : " " + causa_704ILR);
                return false;
            }

            BLL_Bitacora_704ILR.Registrar_704ILR("Conexion", "Configuracion de conexion", CriticidadBitacora_704ILR.Advertencia,
                $"Se cambio la conexion a servidor '{servidor_704ILR}', base '{baseDatos_704ILR}'.");
            mensaje_704ILR = null;
            return true;
        }

        // Vuelve a la configuracion de fabrica (borra el archivo cifrado).
        public static void Restablecer_704ILR()
        {
            ConfiguracionConexion_704ILR.Borrar_704ILR();
            BLL_Bitacora_704ILR.Registrar_704ILR("Conexion", "Restablecer conexion", CriticidadBitacora_704ILR.Advertencia,
                "Se borro la configuracion guardada: vuelve a la conexion por defecto.");
        }

        // Instancia y base son obligatorias. Antes una entrada en blanco se
        // reemplazaba en silencio por la de fabrica: la pantalla informaba "conexion
        // correcta" o guardaba EvenTechDB mientras mostraba los campos vacios. El
        // rechazo no se asienta en bitacora, igual que una prueba que no conecta: la
        // pantalla corre antes del login y justamente con la base vigente sin responder.
        private static bool Completa_704ILR(string servidor_704ILR, string baseDatos_704ILR, out string mensaje_704ILR)
        {
            mensaje_704ILR = null;
            if (string.IsNullOrWhiteSpace(servidor_704ILR))
                mensaje_704ILR = Texto_704ILR("CONN_FALTA_SERVIDOR", "Falta la instancia de SQL Server.");
            else if (string.IsNullOrWhiteSpace(baseDatos_704ILR))
                mensaje_704ILR = Texto_704ILR("CONN_FALTA_BASE", "Falta el nombre de la base de datos.");
            return mensaje_704ILR == null;
        }

        // Traduccion con respaldo: al arrancar el diccionario todavia no esta
        // cargado y se usa el texto por defecto del codigo.
        private static string Texto_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string texto_704ILR = GestorDeIdioma_704ILR.GetInstance_704ILR.Traducir_704ILR(clave_704ILR);
            return texto_704ILR == clave_704ILR ? defecto_704ILR : texto_704ILR;
        }
    }
}
