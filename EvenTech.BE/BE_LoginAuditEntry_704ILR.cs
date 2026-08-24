using System;

namespace EvenTech.BE
{
    public enum LoginAuditAction_704ILR
    {
        LOGIN_OK,
        LOGIN_FAIL,
        LOGOUT
    }

    public class BE_LoginAuditEntry_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Username_704ILR { get; set; }
        public LoginAuditAction_704ILR Action_704ILR { get; set; }
        public DateTime Timestamp_704ILR { get; set; }
        public string MachineName_704ILR { get; set; }
        public string Details_704ILR { get; set; }
    }
}
