using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_User
    {
        public static BE_User GetByUsername(string username)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT Id, Username, PasswordHash, CreatedAt FROM dbo.Users WHERE Username = @username",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username ?? string.Empty;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        return new BE_User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            PasswordHash = reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3)
                        };
                    }
                }
            }
        }

        public static bool ExistsUsername(string username)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.Users WHERE Username = @username",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username ?? string.Empty;
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public static void Insert(string username, string passwordHash)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (@username, @passwordHash)",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username;
                    cmd.Parameters.Add("@passwordHash", SqlDbType.NVarChar, 64).Value = passwordHash;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
