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

        // Inserta el pago verificando el tope en una unica transaccion serializable:
        // lee la suma con UPDLOCK/HOLDLOCK, valida que (pagado + monto) no supere el
        // tope y recien inserta. Bloquea la carrera de dos pagos simultaneos que, con
        // el chequeo por separado, podian pasar ambos y sobrepasar el total.
        // Devuelve el Id del pago, o -1 si excederia el tope.
        public static int InsertConTope(BE_Pago p, decimal tope)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                using (var tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        decimal pagado;
                        using (var q = new SqlCommand(
                            "SELECT ISNULL(SUM(Monto), 0) FROM dbo.Pagos WITH (UPDLOCK, HOLDLOCK) WHERE ReservaId = @r", conn, tx))
                        {
                            q.Parameters.Add("@r", SqlDbType.Int).Value = p.ReservaId;
                            pagado = (decimal)q.ExecuteScalar();
                        }

                        if (pagado + p.Monto > tope)
                        {
                            tx.Rollback();
                            return -1;
                        }

                        int nuevoId;
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.Pagos (ReservaId, MetodoPagoId, Monto, Fecha, Observacion) " +
                            "OUTPUT INSERTED.Id VALUES (@r, @m, @mo, GETDATE(), @o)", conn, tx))
                        {
                            ins.Parameters.Add("@r", SqlDbType.Int).Value = p.ReservaId;
                            ins.Parameters.Add("@m", SqlDbType.Int).Value = p.MetodoPagoId;
                            ins.Parameters.Add("@mo", SqlDbType.Decimal).Value = p.Monto;
                            ins.Parameters.Add("@o", SqlDbType.NVarChar, 200).Value = (object)p.Observacion ?? System.DBNull.Value;
                            nuevoId = (int)ins.ExecuteScalar();
                        }

                        tx.Commit();
                        return nuevoId;
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        // Borra el pago verificando que pertenezca a la reserva indicada. Devuelve la
        // cantidad de filas afectadas (0 = no existia / no era de esa reserva).
        public static int Delete(int id, int reservaId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("DELETE FROM dbo.Pagos WHERE Id = @id AND ReservaId = @r", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
