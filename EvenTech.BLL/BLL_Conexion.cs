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
    public static class BLL_Conexion
    {
        public static string CadenaActual => ConfiguracionConexion.Actual;
        public static string ServidorActual => ConfiguracionConexion.ServidorActual;
        public static string BaseDatosActual => ConfiguracionConexion.BaseDatosActual;
        public static bool EstaConfigurada => ConfiguracionConexion.EstaConfigurada;

        public static List<string> GetInstancias() => DAL_DB_Connection.DetectarInstancias();

        public static string Construir(string servidor, string baseDatos)
            => ConfiguracionConexion.Construir(servidor, baseDatos);

        // Verifica la conectividad con la cadena vigente. Es el chequeo del arranque.
        public static bool VerificarActual(out string mensaje)
            => DAL_DB_Connection.ProbarActual(out mensaje);

        // Prueba una configuracion candidata sin guardarla (boton "Probar").
        public static bool Probar(string servidor, string baseDatos, out string mensaje)
            => DAL_DB_Connection.Probar(ConfiguracionConexion.Construir(servidor, baseDatos), out mensaje);

        // Guarda la configuracion solo si conecta: evita dejar la app apuntando a
        // una instancia inexistente y tener que reconfigurar a ciegas.
        public static bool Guardar(string servidor, string baseDatos, out string mensaje)
        {
            string cadena = ConfiguracionConexion.Construir(servidor, baseDatos);
            if (!DAL_DB_Connection.Probar(cadena, out mensaje)) return false;

            try
            {
                ConfiguracionConexion.Guardar(cadena);
            }
            catch (Exception ex)
            {
                mensaje = "No se pudo guardar la configuracion: " + ex.Message;
                return false;
            }

            BLL_Bitacora.Registrar("Conexion", "Configuracion de conexion", CriticidadBitacora.Advertencia,
                $"Se cambio la conexion a servidor '{servidor}', base '{baseDatos}'.");
            mensaje = null;
            return true;
        }

        // Vuelve a la configuracion de fabrica (borra el archivo cifrado).
        public static void Restablecer()
        {
            ConfiguracionConexion.Borrar();
            BLL_Bitacora.Registrar("Conexion", "Restablecer conexion", CriticidadBitacora.Advertencia,
                "Se borro la configuracion guardada: vuelve a la conexion por defecto.");
        }
    }
}
