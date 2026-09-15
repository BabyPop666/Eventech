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
        private string _actionCodigo_704ILR;

        public int Id_704ILR { get; set; }
        public string Username_704ILR { get; set; }

        // Accion reconocida. Queda en null cuando LoginAuditLog trae un codigo que
        // no pertenece al dominio (dato alterado por fuera del sistema): la fila se
        // lista igual, con el codigo guardado, en vez de cortar toda la lectura.
        public LoginAuditAction_704ILR? Action_704ILR { get; set; }

        // Codigo tal como esta guardado en LoginAuditLog.Action. Si no se informo
        // (registro armado en memoria) es el nombre de la accion.
        public string ActionCodigo_704ILR
        {
            get => _actionCodigo_704ILR ?? Action_704ILR?.ToString();
            set => _actionCodigo_704ILR = value;
        }

        public DateTime Timestamp_704ILR { get; set; }
        public string MachineName_704ILR { get; set; }
        public string Details_704ILR { get; set; }
    }

    // Filtros opcionales de la auditoria de accesos. Cualquier propiedad en
    // null/vacia se ignora (no filtra por ese campo).
    public class LoginAuditFiltros_704ILR
    {
        public string Usuario_704ILR { get; set; }
        public DateTime? FechaInicio_704ILR { get; set; }
        public DateTime? FechaFin_704ILR { get; set; }
        public LoginAuditAction_704ILR? Accion_704ILR { get; set; }
    }
}
