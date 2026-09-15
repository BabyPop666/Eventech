using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public static class BLL_LoginAudit_704ILR
    {
        // Ancho de LoginAuditLog.Username.
        private const int LargoUsername_704ILR = 50;

        public static void Register_704ILR(string username_704ILR, LoginAuditAction_704ILR action_704ILR, string details_704ILR = null)
        {
            try
            {
                var entry_704ILR = new BE_LoginAuditEntry_704ILR
                {
                    Username_704ILR = NombreParaAuditoria_704ILR(username_704ILR),
                    Action_704ILR = action_704ILR,
                    Timestamp_704ILR = DateTime.Now,
                    MachineName_704ILR = Environment.MachineName,
                    Details_704ILR = details_704ILR
                };
                DAL_LoginAudit_704ILR.Insert_704ILR(entry_704ILR);
            }
            catch (Exception ex_704ILR)
            {
                // Un fallo de auditoria no rompe el flujo de login, pero tampoco se
                // pierde: queda asentado en bitacora (que es best-effort: si tambien
                // falla, descarta en silencio y el ingreso sigue).
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Login", "Auditoria de acceso");
            }
        }

        // Un nombre tipeado de mas de 50 caracteres (el login lo trata como inexistente)
        // lo truncaba en silencio el parametro de la DAL, y con espacios intermedios se
        // leia como el de una cuenta real ("dsosa" + 45 espacios). Se recorta aca, con
        // una marca de corte al final, para que nunca coincida con otro nombre.
        private static string NombreParaAuditoria_704ILR(string username_704ILR)
        {
            if (username_704ILR == null || username_704ILR.Length <= LargoUsername_704ILR) return username_704ILR;
            return username_704ILR.Substring(0, LargoUsername_704ILR - 1) + "\u2026";
        }

        public static List<BE_LoginAuditEntry_704ILR> GetAll_704ILR(int top_704ILR = 200) => DAL_LoginAudit_704ILR.GetAll_704ILR(top_704ILR);

        // Busqueda combinada de la auditoria de accesos: los filtros se resuelven en
        // la base, sobre toda la tabla.
        public static List<BE_LoginAuditEntry_704ILR> Buscar_704ILR(LoginAuditFiltros_704ILR filtros_704ILR)
            => DAL_LoginAudit_704ILR.Buscar_704ILR(filtros_704ILR ?? new LoginAuditFiltros_704ILR());
    }
}
