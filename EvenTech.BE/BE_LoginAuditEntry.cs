using System;

namespace EvenTech.BE
{
    public enum LoginAuditAction
    {
        LOGIN_OK,
        LOGIN_FAIL,
        LOGOUT
    }

    public class BE_LoginAuditEntry
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public LoginAuditAction Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string MachineName { get; set; }
        public string Details { get; set; }
    }
}
