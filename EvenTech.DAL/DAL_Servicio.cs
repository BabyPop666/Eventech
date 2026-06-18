using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Servicio
    {
        private const string SelectBase =
            "SELECT Id, Nombre, Descripcion, Precio, Activo, CreatedAt FROM dbo.Servicios ";

        public static List<BE_Servicio> GetAll()
        {
            var list = new List<BE_Servicio>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(SelectBase + "ORDER BY Nombre", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(Map(r));
            return list;
        }

        // Solo los activos (para ofrecerlos al contratar una reserva).
        public static List<BE_Servicio> GetActivos()
        {
            var list = new List<BE_Servicio>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(SelectBase + "WHERE Activo = 1 ORDER BY Nombre", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(Map(r));
            return list;
        }

        public static bool Exists(int id)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Servicios WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static bool ExistsNombre(string nombre, int excluirId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Servicios WHERE Nombre = @n AND Id <> @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre ?? string.Empty;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = excluirId;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static int Insert(BE_Servicio s)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Servicios (Nombre, Descripcion, Precio, Activo) " +
                "OUTPUT INSERTED.Id VALUES (@n, @d, @p, @a)", cn.OpenConnection()))
            {
                Bind(cmd, s);
                return (int)cmd.ExecuteScalar();
            }
        }

        public static void Update(BE_Servicio s)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "UPDATE dbo.Servicios SET Nombre=@n, Descripcion=@d, Precio=@p, Activo=@a WHERE Id=@id",
                cn.OpenConnection()))
            {
                Bind(cmd, s);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = s.Id;
                cmd.ExecuteNonQuery();
            }
        }

        private static void Bind(SqlCommand cmd, BE_Servicio s)
        {
            cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = s.Nombre ?? string.Empty;
            cmd.Parameters.Add("@d", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(s.Descripcion) ? (object)DBNull.Value : s.Descripcion.Trim();
            cmd.Parameters.Add("@p", SqlDbType.Decimal).Value = s.Precio;
            cmd.Parameters.Add("@a", SqlDbType.Bit).Value = s.Activo;
        }

        private static BE_Servicio Map(SqlDataReader r) => new BE_Servicio
        {
            Id = r.GetInt32(0),
            Nombre = r.GetString(1),
            Descripcion = r.IsDBNull(2) ? null : r.GetString(2),
            Precio = r.GetDecimal(3),
            Activo = r.GetBoolean(4),
            CreatedAt = r.GetDateTime(5)
        };
    }
}
