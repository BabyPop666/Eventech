using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Servicio_704ILR
    {
        private const string SelectBase_704ILR =
            "SELECT Id, Nombre, Descripcion, Precio, Activo, CreatedAt FROM dbo.Servicios ";

        public static List<BE_Servicio_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_Servicio_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "ORDER BY Nombre", cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
                while (r_704ILR.Read()) list_704ILR.Add(Map_704ILR(r_704ILR));
            return list_704ILR;
        }

        // Solo los activos (para ofrecerlos al contratar una reserva).
        public static List<BE_Servicio_704ILR> GetActivos_704ILR()
        {
            var list_704ILR = new List<BE_Servicio_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "WHERE Activo = 1 ORDER BY Nombre", cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
                while (r_704ILR.Read()) list_704ILR.Add(Map_704ILR(r_704ILR));
            return list_704ILR;
        }

        public static bool Exists_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT COUNT(1) FROM dbo.Servicios WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        public static bool ExistsNombre_704ILR(string nombre_704ILR, int excluirId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT COUNT(1) FROM dbo.Servicios WHERE Nombre = @n AND Id <> @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre_704ILR ?? string.Empty;
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = excluirId_704ILR;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        public static int Insert_704ILR(BE_Servicio_704ILR s_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Servicios (Nombre, Descripcion, Precio, Activo) " +
                "OUTPUT INSERTED.Id VALUES (@n, @d, @p, @a)", cn_704ILR.OpenConnection_704ILR()))
            {
                Bind_704ILR(cmd_704ILR, s_704ILR);
                return (int)cmd_704ILR.ExecuteScalar();
            }
        }

        public static void Update_704ILR(BE_Servicio_704ILR s_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "UPDATE dbo.Servicios SET Nombre=@n, Descripcion=@d, Precio=@p, Activo=@a WHERE Id=@id",
                cn_704ILR.OpenConnection_704ILR()))
            {
                Bind_704ILR(cmd_704ILR, s_704ILR);
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = s_704ILR.Id_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        private static void Bind_704ILR(SqlCommand cmd_704ILR, BE_Servicio_704ILR s_704ILR)
        {
            cmd_704ILR.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = s_704ILR.Nombre_704ILR ?? string.Empty;
            cmd_704ILR.Parameters.Add("@d", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(s_704ILR.Descripcion_704ILR) ? (object)DBNull.Value : s_704ILR.Descripcion_704ILR.Trim();
            cmd_704ILR.Parameters.Add("@p", SqlDbType.Decimal).Value = s_704ILR.Precio_704ILR;
            cmd_704ILR.Parameters.Add("@a", SqlDbType.Bit).Value = s_704ILR.Activo_704ILR;
        }

        private static BE_Servicio_704ILR Map_704ILR(SqlDataReader r_704ILR) => new BE_Servicio_704ILR
        {
            Id_704ILR = r_704ILR.GetInt32(0),
            Nombre_704ILR = r_704ILR.GetString(1),
            Descripcion_704ILR = r_704ILR.IsDBNull(2) ? null : r_704ILR.GetString(2),
            Precio_704ILR = r_704ILR.GetDecimal(3),
            Activo_704ILR = r_704ILR.GetBoolean(4),
            CreatedAt_704ILR = r_704ILR.GetDateTime(5)
        };
    }
}
