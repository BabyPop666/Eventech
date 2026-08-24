using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_ReservaServicio_704ILR
    {
        public static List<BE_ReservaServicio_704ILR> GetByReserva_704ILR(int reservaId_704ILR)
        {
            var list_704ILR = new List<BE_ReservaServicio_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT rs.Id, rs.ReservaId, rs.ServicioId, s.Nombre, rs.Cantidad, rs.PrecioUnitario " +
                "FROM dbo.ReservaServicio rs INNER JOIN dbo.Servicios s ON s.Id = rs.ServicioId " +
                "WHERE rs.ReservaId = @r ORDER BY s.Nombre", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = reservaId_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    while (r_704ILR.Read())
                        list_704ILR.Add(new BE_ReservaServicio_704ILR
                        {
                            Id_704ILR = r_704ILR.GetInt32(0),
                            ReservaId_704ILR = r_704ILR.GetInt32(1),
                            ServicioId_704ILR = r_704ILR.GetInt32(2),
                            ServicioNombre_704ILR = r_704ILR.GetString(3),
                            Cantidad_704ILR = r_704ILR.GetInt32(4),
                            PrecioUnitario_704ILR = r_704ILR.GetDecimal(5)
                        });
            }
            return list_704ILR;
        }

        // Reemplaza (en una transaccion) el set de servicios de la reserva.
        public static void ReplaceForReserva_704ILR(int reservaId_704ILR, IEnumerable<BE_ReservaServicio_704ILR> items_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                var conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (var tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    using (var del_704ILR = new SqlCommand("DELETE FROM dbo.ReservaServicio WHERE ReservaId = @r", conn_704ILR, tx_704ILR))
                    {
                        del_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = reservaId_704ILR;
                        del_704ILR.ExecuteNonQuery();
                    }
                    foreach (var it_704ILR in items_704ILR)
                    {
                        using (var ins_704ILR = new SqlCommand(
                            "INSERT INTO dbo.ReservaServicio (ReservaId, ServicioId, Cantidad, PrecioUnitario) " +
                            "VALUES (@r, @s, @c, @p)", conn_704ILR, tx_704ILR))
                        {
                            ins_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = reservaId_704ILR;
                            ins_704ILR.Parameters.Add("@s", SqlDbType.Int).Value = it_704ILR.ServicioId_704ILR;
                            ins_704ILR.Parameters.Add("@c", SqlDbType.Int).Value = it_704ILR.Cantidad_704ILR;
                            ins_704ILR.Parameters.Add("@p", SqlDbType.Decimal).Value = it_704ILR.PrecioUnitario_704ILR;
                            ins_704ILR.ExecuteNonQuery();
                        }
                    }
                    tx_704ILR.Commit();
                }
            }
        }
    }
}
