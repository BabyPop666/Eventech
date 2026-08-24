using System;
using System.Collections.Generic;
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
            => DAL_DB_Connection_704ILR.Probar_704ILR(ConfiguracionConexion_704ILR.Construir_704ILR(servidor_704ILR, baseDatos_704ILR), out mensaje_704ILR);

        // Guarda la configuracion solo si conecta: evita dejar la app apuntando a
        // una instancia inexistente y tener que reconfigurar a ciegas.
        public static bool Guardar_704ILR(string servidor_704ILR, string baseDatos_704ILR, out string mensaje_704ILR)
        {
            string cadena_704ILR = ConfiguracionConexion_704ILR.Construir_704ILR(servidor_704ILR, baseDatos_704ILR);
            if (!DAL_DB_Connection_704ILR.Probar_704ILR(cadena_704ILR, out mensaje_704ILR)) return false;

            try
            {
                ConfiguracionConexion_704ILR.Guardar_704ILR(cadena_704ILR);
            }
            catch (Exception ex_704ILR)
            {
                mensaje_704ILR = "No se pudo guardar la configuracion: " + ex_704ILR.Message;
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
    }
}
