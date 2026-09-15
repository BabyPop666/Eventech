using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_LoginAudit_704ILR
    {
        // Ancho de LoginAuditLog.Username en la base.
        private const int AnchoUsername_704ILR = 50;

        public static void Insert_704ILR(BE_LoginAuditEntry_704ILR entry_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(
                    "INSERT INTO dbo.LoginAuditLog (Username, [Action], [Timestamp], MachineName, Details) " +
                    "VALUES (@username, @action, @timestamp, @machine, @details)",
                    cn_704ILR.OpenConnection_704ILR()))
                {
                    cmd_704ILR.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = entry_704ILR.Username_704ILR ?? string.Empty;
                    cmd_704ILR.Parameters.Add("@action", SqlDbType.NVarChar, 20).Value = entry_704ILR.Action_704ILR.ToString();
                    cmd_704ILR.Parameters.Add("@timestamp", SqlDbType.DateTime).Value = entry_704ILR.Timestamp_704ILR == default ? DateTime.Now : entry_704ILR.Timestamp_704ILR;
                    cmd_704ILR.Parameters.Add("@machine", SqlDbType.NVarChar, 100).Value = (object)entry_704ILR.MachineName_704ILR ?? DBNull.Value;
                    cmd_704ILR.Parameters.Add("@details", SqlDbType.NVarChar, 500).Value = (object)entry_704ILR.Details_704ILR ?? DBNull.Value;

                    cmd_704ILR.ExecuteNonQuery();
                }
            }
        }

        public static List<BE_LoginAuditEntry_704ILR> GetAll_704ILR(int top_704ILR = 200)
        {
            var list_704ILR = new List<BE_LoginAuditEntry_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(
                    "SELECT TOP (@top) Id, Username, [Action], [Timestamp], MachineName, Details " +
                    "FROM dbo.LoginAuditLog ORDER BY Id DESC",
                    cn_704ILR.OpenConnection_704ILR()))
                {
                    cmd_704ILR.Parameters.Add("@top", SqlDbType.Int).Value = top_704ILR;
                    using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    {
                        while (r_704ILR.Read())
                            list_704ILR.Add(Map_704ILR(r_704ILR));
                    }
                }
            }
            return list_704ILR;
        }

        // Busqueda combinada de la auditoria de accesos. Los filtros se resuelven en
        // la base (WHERE 1=1 + parametros opcionales, como la bitacora general), asi
        // alcanzan a toda la tabla y no solo a los ultimos registros leidos. El
        // usuario se busca como texto literal contenido (sin comodines de LIKE) y la
        // accion por codigo exacto: una fila con un codigo fuera de dominio aparece
        // sin filtro de accion, pero no coincide con ninguna accion del filtro.
        public static List<BE_LoginAuditEntry_704ILR> Buscar_704ILR(LoginAuditFiltros_704ILR f_704ILR)
        {
            var sb_704ILR = new StringBuilder(
                "SELECT Id, Username, [Action], [Timestamp], MachineName, Details FROM dbo.LoginAuditLog WHERE 1=1 ");
            var ps_704ILR = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(f_704ILR.Usuario_704ILR))
            {
                sb_704ILR.Append("AND Username LIKE @usuario ");
                ps_704ILR.Add(new SqlParameter("@usuario", SqlDbType.NVarChar, DAL_Bitacora_704ILR.LargoPatron_704ILR(AnchoUsername_704ILR))
                    { Value = DAL_Bitacora_704ILR.PatronContiene_704ILR(f_704ILR.Usuario_704ILR, AnchoUsername_704ILR) });
            }
            if (f_704ILR.FechaInicio_704ILR.HasValue)
            {
                sb_704ILR.Append("AND [Timestamp] >= @desde ");
                ps_704ILR.Add(new SqlParameter("@desde", SqlDbType.DateTime) { Value = f_704ILR.FechaInicio_704ILR.Value.Date });
            }
            if (f_704ILR.FechaFin_704ILR.HasValue)
            {
                sb_704ILR.Append("AND [Timestamp] < @hasta ");
                ps_704ILR.Add(new SqlParameter("@hasta", SqlDbType.DateTime) { Value = f_704ILR.FechaFin_704ILR.Value.Date.AddDays(1) });
            }
            if (f_704ILR.Accion_704ILR.HasValue)
            {
                sb_704ILR.Append("AND [Action] COLLATE Latin1_General_BIN = @accion ");
                ps_704ILR.Add(new SqlParameter("@accion", SqlDbType.NVarChar, 20) { Value = f_704ILR.Accion_704ILR.Value.ToString() });
            }
            sb_704ILR.Append("ORDER BY Id DESC");

            var list_704ILR = new List<BE_LoginAuditEntry_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(sb_704ILR.ToString(), cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.AddRange(ps_704ILR.ToArray());
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    while (r_704ILR.Read())
                        list_704ILR.Add(Map_704ILR(r_704ILR));
                }
            }
            return list_704ILR;
        }

        // Un codigo de accion que no es del dominio no corta la lectura de toda la
        // auditoria: la fila se devuelve con el codigo guardado y la accion en null.
        // Se reconoce solo el nombre exacto (Enum.IsDefined sobre el texto): un "1"
        // o un "login_ok" no se toman por una accion valida.
        private static BE_LoginAuditEntry_704ILR Map_704ILR(SqlDataReader r_704ILR)
        {
            string codigo_704ILR = r_704ILR.GetString(2);
            return new BE_LoginAuditEntry_704ILR
            {
                Id_704ILR = r_704ILR.GetInt32(0),
                Username_704ILR = r_704ILR.GetString(1),
                Action_704ILR = Enum.IsDefined(typeof(LoginAuditAction_704ILR), codigo_704ILR)
                    ? (LoginAuditAction_704ILR)Enum.Parse(typeof(LoginAuditAction_704ILR), codigo_704ILR)
                    : (LoginAuditAction_704ILR?)null,
                ActionCodigo_704ILR = codigo_704ILR,
                Timestamp_704ILR = r_704ILR.GetDateTime(3),
                MachineName_704ILR = r_704ILR.IsDBNull(4) ? null : r_704ILR.GetString(4),
                Details_704ILR = r_704ILR.IsDBNull(5) ? null : r_704ILR.GetString(5)
            };
        }
    }
}
