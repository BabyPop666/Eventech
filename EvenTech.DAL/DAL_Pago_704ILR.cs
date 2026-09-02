using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Pago_704ILR
    {
        public static List<BE_Pago_704ILR> GetByReserva_704ILR(int reservaId_704ILR)
        {
            var list_704ILR = new List<BE_Pago_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT p.Id, p.ReservaId, p.MetodoPagoId, m.Nombre, p.Monto, p.Fecha, p.Observacion " +
                "FROM dbo.Pagos p INNER JOIN dbo.MetodosPago m ON m.Id = p.MetodoPagoId " +
                "WHERE p.ReservaId = @r ORDER BY p.Fecha, p.Id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = reservaId_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    while (r_704ILR.Read())
                        list_704ILR.Add(new BE_Pago_704ILR
                        {
                            Id_704ILR = r_704ILR.GetInt32(0),
                            ReservaId_704ILR = r_704ILR.GetInt32(1),
                            MetodoPagoId_704ILR = r_704ILR.GetInt32(2),
                            MetodoNombre_704ILR = r_704ILR.GetString(3),
                            Monto_704ILR = r_704ILR.GetDecimal(4),
                            Fecha_704ILR = r_704ILR.GetDateTime(5),
                            Observacion_704ILR = r_704ILR.IsDBNull(6) ? null : r_704ILR.GetString(6)
                        });
            }
            return list_704ILR;
        }

        // Un pago puntual, para poder validar la anulacion (que exista y que sea de la
        // reserva que dice la pantalla) antes de borrarlo.
        public static BE_Pago_704ILR GetById_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT p.Id, p.ReservaId, p.MetodoPagoId, m.Nombre, p.Monto, p.Fecha, p.Observacion " +
                "FROM dbo.Pagos p INNER JOIN dbo.MetodosPago m ON m.Id = p.MetodoPagoId " +
                "WHERE p.Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    if (!r_704ILR.Read()) return null;
                    return new BE_Pago_704ILR
                    {
                        Id_704ILR = r_704ILR.GetInt32(0),
                        ReservaId_704ILR = r_704ILR.GetInt32(1),
                        MetodoPagoId_704ILR = r_704ILR.GetInt32(2),
                        MetodoNombre_704ILR = r_704ILR.GetString(3),
                        Monto_704ILR = r_704ILR.GetDecimal(4),
                        Fecha_704ILR = r_704ILR.GetDateTime(5),
                        Observacion_704ILR = r_704ILR.IsDBNull(6) ? null : r_704ILR.GetString(6)
                    };
                }
            }
        }

        public static decimal TotalPagado_704ILR(int reservaId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT ISNULL(SUM(Monto), 0) FROM dbo.Pagos WHERE ReservaId = @r", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = reservaId_704ILR;
                return (decimal)cmd_704ILR.ExecuteScalar();
            }
        }

        public static int Insert_704ILR(BE_Pago_704ILR p_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Pagos (ReservaId, MetodoPagoId, Monto, Fecha, Observacion) " +
                "OUTPUT INSERTED.Id VALUES (@r, @m, @mo, GETDATE(), @o)", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@r", SqlDbType.Int).Value = p_704ILR.ReservaId_704ILR;
                cmd_704ILR.Parameters.Add("@m", SqlDbType.Int).Value = p_704ILR.MetodoPagoId_704ILR;
                cmd_704ILR.Parameters.Add("@mo", SqlDbType.Decimal).Value = p_704ILR.Monto_704ILR;
                cmd_704ILR.Parameters.Add("@o", SqlDbType.NVarChar, 200).Value = (object)p_704ILR.Observacion_704ILR ?? System.DBNull.Value;
                return (int)cmd_704ILR.ExecuteScalar();
            }
        }

        public static void Delete_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("DELETE FROM dbo.Pagos WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }
    }
}
