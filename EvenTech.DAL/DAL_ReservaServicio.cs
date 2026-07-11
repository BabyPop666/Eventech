using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_ReservaServicio
    {
        public static List<BE_ReservaServicio> GetByReserva(int reservaId)
        {
            var list = new List<BE_ReservaServicio>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT rs.Id, rs.ReservaId, rs.ServicioId, s.Nombre, rs.Cantidad, rs.PrecioUnitario " +
                "FROM dbo.ReservaServicio rs INNER JOIN dbo.Servicios s ON s.Id = rs.ServicioId " +
                "WHERE rs.ReservaId = @r ORDER BY s.Nombre", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new BE_ReservaServicio
                        {
                            Id = r.GetInt32(0),
                            ReservaId = r.GetInt32(1),
                            ServicioId = r.GetInt32(2),
                            ServicioNombre = r.GetString(3),
                            Cantidad = r.GetInt32(4),
                            PrecioUnitario = r.GetDecimal(5)
                        });
            }
            return list;
        }

        // Reemplaza (en una transaccion) el set de servicios de la reserva.
        public static void ReplaceForReserva(int reservaId, IEnumerable<BE_ReservaServicio> items)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = new SqlCommand("DELETE FROM dbo.ReservaServicio WHERE ReservaId = @r", conn, tx))
                    {
                        del.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                        del.ExecuteNonQuery();
                    }
                    foreach (var it in items)
                    {
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.ReservaServicio (ReservaId, ServicioId, Cantidad, PrecioUnitario) " +
                            "VALUES (@r, @s, @c, @p)", conn, tx))
                        {
                            ins.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                            ins.Parameters.Add("@s", SqlDbType.Int).Value = it.ServicioId;
                            ins.Parameters.Add("@c", SqlDbType.Int).Value = it.Cantidad;
                            ins.Parameters.Add("@p", SqlDbType.Decimal).Value = it.PrecioUnitario;
                            ins.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }
    }
}
