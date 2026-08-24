using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_LoginAudit_704ILR
    {
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
                        {
                            list_704ILR.Add(new BE_LoginAuditEntry_704ILR
                            {
                                Id_704ILR = r_704ILR.GetInt32(0),
                                Username_704ILR = r_704ILR.GetString(1),
                                Action_704ILR = (LoginAuditAction_704ILR)Enum.Parse(typeof(LoginAuditAction_704ILR), r_704ILR.GetString(2)),
                                Timestamp_704ILR = r_704ILR.GetDateTime(3),
                                MachineName_704ILR = r_704ILR.IsDBNull(4) ? null : r_704ILR.GetString(4),
                                Details_704ILR = r_704ILR.IsDBNull(5) ? null : r_704ILR.GetString(5)
                            });
                        }
                    }
                }
            }
            return list_704ILR;
        }
    }
}
