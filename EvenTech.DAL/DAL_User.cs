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
                "SELECT Id, Username, PasswordHash, CreatedAt, PerfilId, Activo, Blocked, FailedAttempts FROM dbo.Users ORDER BY Username",
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
                        PerfilId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                        Activo = r.GetBoolean(5),
                        Blocked = r.GetBoolean(6),
                        FailedAttempts = r.GetInt32(7)
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
                    "SELECT Id, Username, PasswordHash, CreatedAt, PerfilId, Activo, Blocked, FailedAttempts FROM dbo.Users WHERE Username = @username",
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
                            PerfilId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            Activo = reader.GetBoolean(5),
                            Blocked = reader.GetBoolean(6),
                            FailedAttempts = reader.GetInt32(7)
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

        // Suma 1 al contador de intentos fallidos y devuelve el nuevo total.
        public static int IncrementFailedAttempts(string username)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "UPDATE dbo.Users SET FailedAttempts = FailedAttempts + 1 WHERE Username = @u; " +
                "SELECT FailedAttempts FROM dbo.Users WHERE Username = @u;",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 50).Value = username ?? string.Empty;
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : (int)o;
            }
        }

        public static void ResetFailedAttempts(string username)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("UPDATE dbo.Users SET FailedAttempts = 0 WHERE Username = @u", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 50).Value = username ?? string.Empty;
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetBlocked(string username, bool blocked)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("UPDATE dbo.Users SET Blocked = @b WHERE Username = @u", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@b", SqlDbType.Bit).Value = blocked;
                cmd.Parameters.Add("@u", SqlDbType.NVarChar, 50).Value = username ?? string.Empty;
                cmd.ExecuteNonQuery();
            }
        }

        // Desbloqueo por admin: quita el bloqueo y resetea el contador de intentos.
        public static void Desbloquear(int userId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("UPDATE dbo.Users SET Blocked = 0, FailedAttempts = 0 WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = userId;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
