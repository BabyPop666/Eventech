using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_LoginAudit
    {
        public static void Insert(BE_LoginAuditEntry entry)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.LoginAuditLog (Username, [Action], [Timestamp], MachineName, Details) " +
                    "VALUES (@username, @action, @timestamp, @machine, @details)",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = entry.Username ?? string.Empty;
                    cmd.Parameters.Add("@action", SqlDbType.NVarChar, 20).Value = entry.Action.ToString();
                    cmd.Parameters.Add("@timestamp", SqlDbType.DateTime).Value = entry.Timestamp == default ? DateTime.Now : entry.Timestamp;
                    cmd.Parameters.Add("@machine", SqlDbType.NVarChar, 100).Value = (object)entry.MachineName ?? DBNull.Value;
                    cmd.Parameters.Add("@details", SqlDbType.NVarChar, 500).Value = (object)entry.Details ?? DBNull.Value;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<BE_LoginAuditEntry> GetAll(int top = 200)
        {
            var list = new List<BE_LoginAuditEntry>();
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT TOP (@top) Id, Username, [Action], [Timestamp], MachineName, Details " +
                    "FROM dbo.LoginAuditLog ORDER BY Id DESC",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@top", SqlDbType.Int).Value = top;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new BE_LoginAuditEntry
                            {
                                Id = r.GetInt32(0),
                                Username = r.GetString(1),
                                Action = (LoginAuditAction)Enum.Parse(typeof(LoginAuditAction), r.GetString(2)),
                                Timestamp = r.GetDateTime(3),
                                MachineName = r.IsDBNull(4) ? null : r.GetString(4),
                                Details = r.IsDBNull(5) ? null : r.GetString(5)
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}
