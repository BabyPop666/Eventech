using System;
using System.Collections.Generic;
using System.Text;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Bitacora general del sistema (T06). Registra cualquier operacion de negocio
    // y permite busqueda combinada. Un fallo de bitacora nunca debe romper el
    // flujo principal, por eso Registrar atrapa sus propias excepciones.
    public static class BLL_Bitacora
    {
        public static void Registrar(string modulo, string accion, CriticidadBitacora criticidad, string detalle)
        {
            try
            {
                DAL_Bitacora.Insert(new BE_BitacoraEntry
                {
                    Fecha = DateTime.Now,
                    Usuario = UsuarioActual(),
                    Modulo = modulo,
                    Accion = accion,
                    Criticidad = criticidad,
                    Detalle = detalle
                });
            }
            catch
            {
                // Silencioso a proposito: la bitacora no debe propagar errores.
            }
        }

        // Registro estandar de excepciones tecnicas en la bitacora.
        public static void RegistrarExcepcion(Exception ex, string modulo, string contexto)
        {
            var sb = new StringBuilder();
            sb.Append("ERROR en ").Append(contexto).Append(": ").Append(ex.Message);
            if (ex.InnerException != null)
                sb.Append(" | Inner: ").Append(ex.InnerException.Message);

            Registrar(modulo, "Error", CriticidadBitacora.Error, sb.ToString());
        }

        public static List<BE_BitacoraEntry> Buscar(BitacoraFiltros filtros) => DAL_Bitacora.Buscar(filtros);

        public static List<string> GetModulos() => DAL_Bitacora.GetModulos();

        private static string UsuarioActual()
        {
            try
            {
                return SessionManager.IsSessionActive
                    ? SessionManager.GetInstance.User.Username
                    : "Sistema";
            }
            catch { return "Sistema"; }
        }
    }
}
