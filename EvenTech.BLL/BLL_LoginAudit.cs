using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public static class BLL_LoginAudit
    {
        public static void Register(string username, LoginAuditAction action, string details = null)
        {
            try
            {
                var entry = new BE_LoginAuditEntry
                {
                    Username = username,
                    Action = action,
                    Timestamp = DateTime.Now,
                    MachineName = Environment.MachineName,
                    Details = details
                };
                DAL_LoginAudit.Insert(entry);
            }
            catch
            {
                // No queremos que un fallo de auditoria rompa el flujo de login.
            }
        }

        public static List<BE_LoginAuditEntry> GetAll(int top = 200) => DAL_LoginAudit.GetAll(top);
    }
}
