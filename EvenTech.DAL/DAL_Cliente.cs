using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;
using EvenTech.Services;

namespace EvenTech.DAL
{
    public static class DAL_Cliente
    {
        private const string SelectBase =
            "SELECT Id, Nombre, Apellido, Dni, Email, Telefono, CreatedAt FROM dbo.Clientes ";

        public static List<BE_Cliente> GetAll()
        {
            var list = new List<BE_Cliente>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(SelectBase + "ORDER BY Nombre, Apellido", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public static BE_Cliente GetById(int id)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(SelectBase + "WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Map(r) : null;
            }
        }

        public static bool Exists(int id)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Clientes WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        // DNI duplicado (ignorando el propio registro en edicion).
        public static bool ExistsDni(string dni, int excluirId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Clientes WHERE Dni = @dni AND Id <> @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@dni", SqlDbType.NVarChar, 20).Value = dni ?? string.Empty;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = excluirId;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static int Insert(BE_Cliente c)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Clientes (Nombre, Apellido, Dni, Email, Telefono) " +
                "OUTPUT INSERTED.Id VALUES (@n, @a, @d, @e, @t)", cn.OpenConnection()))
            {
                Bind(cmd, c);
                return (int)cmd.ExecuteScalar();
            }
        }

        public static void Update(BE_Cliente c)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "UPDATE dbo.Clientes SET Nombre=@n, Apellido=@a, Dni=@d, Email=@e, Telefono=@t WHERE Id=@id",
                cn.OpenConnection()))
            {
                Bind(cmd, c);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = c.Id;
                cmd.ExecuteNonQuery();
            }
        }

        // Email y Telefono se persisten cifrados (AES reversible, CryptoService):
        // son datos de contacto sensibles que la app necesita leer de vuelta.
        // DNI queda en claro porque participa del indice unico y de busquedas por
        // igualdad en SQL (el cifrado con IV aleatorio rompe ambas cosas).
        private static void Bind(SqlCommand cmd, BE_Cliente c)
        {
            cmd.Parameters.Add("@n", SqlDbType.NVarChar, 60).Value = c.Nombre ?? string.Empty;
            cmd.Parameters.Add("@a", SqlDbType.NVarChar, 60).Value = Nz(c.Apellido);
            cmd.Parameters.Add("@d", SqlDbType.NVarChar, 20).Value = Nz(c.Dni);
            cmd.Parameters.Add("@e", SqlDbType.NVarChar, 400).Value = NzCifrado(c.Email);
            cmd.Parameters.Add("@t", SqlDbType.NVarChar, 200).Value = NzCifrado(c.Telefono);
        }

        // string vacio/blanco -> NULL (para respetar el indice unico filtrado de DNI).
        private static object Nz(string s) => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static object NzCifrado(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : CryptoService.Proteger(s.Trim());

        private static BE_Cliente Map(SqlDataReader r) => new BE_Cliente
        {
            Id = r.GetInt32(0),
            Nombre = r.GetString(1),
            Apellido = r.IsDBNull(2) ? null : r.GetString(2),
            Dni = r.IsDBNull(3) ? null : r.GetString(3),
            Email = r.IsDBNull(4) ? null : CryptoService.Desproteger(r.GetString(4)),
            Telefono = r.IsDBNull(5) ? null : CryptoService.Desproteger(r.GetString(5)),
            CreatedAt = r.GetDateTime(6)
        };
    }
}
