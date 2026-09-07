using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public static class BLL_LoginAudit_704ILR
    {
        public static void Register_704ILR(string username_704ILR, LoginAuditAction_704ILR action_704ILR, string details_704ILR = null)
        {
            try
            {
                var entry_704ILR = new BE_LoginAuditEntry_704ILR
                {
                    Username_704ILR = username_704ILR,
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

        public static List<BE_LoginAuditEntry_704ILR> GetAll_704ILR(int top_704ILR = 200) => DAL_LoginAudit_704ILR.GetAll_704ILR(top_704ILR);
    }
}
