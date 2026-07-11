using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Pago
    {
        public static List<BE_Pago> GetByReserva(int reservaId)
        {
            var list = new List<BE_Pago>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT p.Id, p.ReservaId, p.MetodoPagoId, m.Nombre, p.Monto, p.Fecha, p.Observacion " +
                "FROM dbo.Pagos p INNER JOIN dbo.MetodosPago m ON m.Id = p.MetodoPagoId " +
                "WHERE p.ReservaId = @r ORDER BY p.Fecha, p.Id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new BE_Pago
                        {
                            Id = r.GetInt32(0),
                            ReservaId = r.GetInt32(1),
                            MetodoPagoId = r.GetInt32(2),
                            MetodoNombre = r.GetString(3),
                            Monto = r.GetDecimal(4),
                            Fecha = r.GetDateTime(5),
                            Observacion = r.IsDBNull(6) ? null : r.GetString(6)
                        });
            }
            return list;
        }

        public static decimal TotalPagado(int reservaId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT ISNULL(SUM(Monto), 0) FROM dbo.Pagos WHERE ReservaId = @r", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                return (decimal)cmd.ExecuteScalar();
            }
        }

        public static int Insert(BE_Pago p)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Pagos (ReservaId, MetodoPagoId, Monto, Fecha, Observacion) " +
                "OUTPUT INSERTED.Id VALUES (@r, @m, @mo, GETDATE(), @o)", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = p.ReservaId;
                cmd.Parameters.Add("@m", SqlDbType.Int).Value = p.MetodoPagoId;
                cmd.Parameters.Add("@mo", SqlDbType.Decimal).Value = p.Monto;
                cmd.Parameters.Add("@o", SqlDbType.NVarChar, 200).Value = (object)p.Observacion ?? System.DBNull.Value;
                return (int)cmd.ExecuteScalar();
            }
        }

        public static void Delete(int id)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("DELETE FROM dbo.Pagos WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
