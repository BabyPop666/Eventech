using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_User
    {
        // Todos los usuarios (para la asignacion de perfiles).
        public static List<BE_User> GetAll()
        {
            var list = new List<BE_User>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Username, PasswordHash, CreatedAt, PerfilId FROM dbo.Users ORDER BY Username",
                cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new BE_User
                    {
                        Id = r.GetInt32(0),
                        Username = r.GetString(1),
                        PasswordHash = r.GetString(2),
                        CreatedAt = r.GetDateTime(3),
                        PerfilId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4)
                    });
            }
            return list;
        }

        // Asigna (o quita, con null) el perfil de un usuario.
        public static void SetPerfil(int userId, int? perfilId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("UPDATE dbo.Users SET PerfilId = @p WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@p", SqlDbType.Int).Value = (object)perfilId ?? DBNull.Value;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId;
                cmd.ExecuteNonQuery();
            }
        }

        public static BE_User GetByUsername(string username)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT Id, Username, PasswordHash, CreatedAt, PerfilId FROM dbo.Users WHERE Username = @username",
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
                            CreatedAt = reader.GetDateTime(3),
                            PerfilId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
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
