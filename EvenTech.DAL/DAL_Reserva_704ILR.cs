using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Reserva_704ILR
    {
        // SELECT base con JOIN a salon y cliente para proyectar sus nombres.
        private const string SelectBase_704ILR =
            "SELECT r.Id, r.ClienteId, LTRIM(ISNULL(c.Nombre,'') + ISNULL(' ' + c.Apellido,'')) AS ClienteNombre, " +
            "r.SalonId, s.Nombre, r.FechaEvento, r.Estado, r.Monto, r.CantidadInvitados, " +
            "r.CreatedAt, r.Dvh, r.VenceEl " +
            "FROM dbo.Reservas r " +
            "INNER JOIN dbo.Salones s ON s.Id = r.SalonId " +
            "LEFT JOIN dbo.Clientes c ON c.Id = r.ClienteId ";

        public static List<BE_Reserva_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_Reserva_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "ORDER BY r.FechaEvento DESC", cn_704ILR.OpenConnection_704ILR()))
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    while (r_704ILR.Read()) list_704ILR.Add(Map_704ILR(r_704ILR));
                }
            }
            return list_704ILR;
        }

        public static BE_Reserva_704ILR GetById_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "WHERE r.Id = @id", cn_704ILR.OpenConnection_704ILR()))
                {
                    cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                    using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    {
                        return r_704ILR.Read() ? Map_704ILR(r_704ILR) : null;
                    }
                }
            }
        }

        // Anti-solapamiento: hay otra reserva CONFIRMADA para ese salon y fecha
        // (excluyendo la propia reserva en edicion)? Las cotizaciones y reservas
        // pendientes no comprometen el salon: solo una reserva firme lo bloquea.
        public static bool SalonOcupado_704ILR(int salonId_704ILR, DateTime fecha_704ILR, int excluirId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Reservas " +
                "WHERE SalonId = @s AND CAST(FechaEvento AS DATE) = @f AND Estado = 'CONFIRMADA' AND Id <> @ex",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@s", SqlDbType.Int).Value = salonId_704ILR;
                cmd_704ILR.Parameters.Add("@f", SqlDbType.Date).Value = fecha_704ILR.Date;
                cmd_704ILR.Parameters.Add("@ex", SqlDbType.Int).Value = excluirId_704ILR;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        // Fechas comprometidas por salon en un rango: dias con una reserva
        // CONFIRMADA. Una sola query para toda la consulta de disponibilidad
        // (evita ir a la base salon por salon y dia por dia).
        public static Dictionary<int, HashSet<DateTime>> FechasConfirmadasPorSalon_704ILR(DateTime desde_704ILR, DateTime hasta_704ILR)
        {
            var map_704ILR = new Dictionary<int, HashSet<DateTime>>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT SalonId, CAST(FechaEvento AS DATE) FROM dbo.Reservas " +
                "WHERE Estado = 'CONFIRMADA' AND CAST(FechaEvento AS DATE) BETWEEN @d AND @h",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@d", SqlDbType.Date).Value = desde_704ILR.Date;
                cmd_704ILR.Parameters.Add("@h", SqlDbType.Date).Value = hasta_704ILR.Date;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    while (r_704ILR.Read())
                    {
                        int salonId_704ILR = r_704ILR.GetInt32(0);
                        if (!map_704ILR.TryGetValue(salonId_704ILR, out var fechas_704ILR))
                            map_704ILR[salonId_704ILR] = fechas_704ILR = new HashSet<DateTime>();
                        fechas_704ILR.Add(r_704ILR.GetDateTime(1).Date);
                    }
                }
            }
            return map_704ILR;
        }

        public static int Insert_704ILR(BE_Reserva_704ILR reserva_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
                return Insert_704ILR(reserva_704ILR, cn_704ILR.OpenConnection_704ILR(), null);
        }

        // Sobrecarga transaccional: escribe sobre la conexion y la transaccion que
        // le pasan, para que la reserva y sus servicios contratados entren o no
        // entren juntos. La usa BLL_Reserva cuando orquesta la operacion completa.
        public static int Insert_704ILR(BE_Reserva_704ILR reserva_704ILR,
            SqlConnection conn_704ILR, SqlTransaction tx_704ILR)
        {
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Reservas (ClienteId, SalonId, FechaEvento, Estado, Monto, CantidadInvitados, Dvh, VenceEl) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@cliente, @salon, @fecha, @estado, @monto, @invitados, @dvh, @vence)",
                conn_704ILR, tx_704ILR))
            {
                BindEditable_704ILR(cmd_704ILR, reserva_704ILR);
                return (int)cmd_704ILR.ExecuteScalar();
            }
        }

        public static void Update_704ILR(BE_Reserva_704ILR reserva_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
                Update_704ILR(reserva_704ILR, cn_704ILR.OpenConnection_704ILR(), null);
        }

        // Sobrecarga transaccional (ver Insert_704ILR).
        public static void Update_704ILR(BE_Reserva_704ILR reserva_704ILR,
            SqlConnection conn_704ILR, SqlTransaction tx_704ILR)
        {
            using (var cmd_704ILR = new SqlCommand(
                "UPDATE dbo.Reservas SET ClienteId = @cliente, SalonId = @salon, " +
                "FechaEvento = @fecha, Estado = @estado, Monto = @monto, " +
                "CantidadInvitados = @invitados, Dvh = @dvh, " +
                "VenceEl = @vence WHERE Id = @id",
                conn_704ILR, tx_704ILR))
            {
                BindEditable_704ILR(cmd_704ILR, reserva_704ILR);
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = reserva_704ILR.Id_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        // Actualiza solo el DV horizontal (usado al recalcular la linea base).
        public static void UpdateDvh_704ILR(int id_704ILR, string dvh_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("UPDATE dbo.Reservas SET Dvh = @dvh WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@dvh", SqlDbType.NVarChar, 64).Value = (object)dvh_704ILR ?? DBNull.Value;
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        private static void BindEditable_704ILR(SqlCommand cmd_704ILR, BE_Reserva_704ILR reserva_704ILR)
        {
            cmd_704ILR.Parameters.Add("@cliente", SqlDbType.Int).Value = reserva_704ILR.ClienteId_704ILR;
            cmd_704ILR.Parameters.Add("@salon", SqlDbType.Int).Value = reserva_704ILR.SalonId_704ILR;
            cmd_704ILR.Parameters.Add("@fecha", SqlDbType.DateTime).Value = reserva_704ILR.FechaEvento_704ILR;
            cmd_704ILR.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = reserva_704ILR.Estado_704ILR.ToString();
            cmd_704ILR.Parameters.Add("@monto", SqlDbType.Decimal).Value = reserva_704ILR.Monto_704ILR;
            cmd_704ILR.Parameters.Add("@invitados", SqlDbType.Int).Value = reserva_704ILR.CantidadInvitados_704ILR;
            cmd_704ILR.Parameters.Add("@dvh", SqlDbType.NVarChar, 64).Value = (object)reserva_704ILR.Dvh_704ILR ?? DBNull.Value;
            cmd_704ILR.Parameters.Add("@vence", SqlDbType.DateTime).Value =
                reserva_704ILR.VenceEl_704ILR.HasValue ? (object)reserva_704ILR.VenceEl_704ILR.Value : DBNull.Value;
        }

        private static BE_Reserva_704ILR Map_704ILR(SqlDataReader r_704ILR) => new BE_Reserva_704ILR
        {
            Id_704ILR = r_704ILR.GetInt32(0),
            ClienteId_704ILR = r_704ILR.IsDBNull(1) ? 0 : r_704ILR.GetInt32(1),
            ClienteNombre_704ILR = r_704ILR.IsDBNull(2) ? string.Empty : r_704ILR.GetString(2),
            SalonId_704ILR = r_704ILR.GetInt32(3),
            SalonNombre_704ILR = r_704ILR.GetString(4),
            FechaEvento_704ILR = r_704ILR.GetDateTime(5),
            Estado_704ILR = (EstadoReserva_704ILR)Enum.Parse(typeof(EstadoReserva_704ILR), r_704ILR.GetString(6)),
            Monto_704ILR = r_704ILR.GetDecimal(7),
            CantidadInvitados_704ILR = r_704ILR.IsDBNull(8) ? 0 : r_704ILR.GetInt32(8),
            CreatedAt_704ILR = r_704ILR.GetDateTime(9),
            Dvh_704ILR = r_704ILR.IsDBNull(10) ? null : r_704ILR.GetString(10),
            VenceEl_704ILR = r_704ILR.IsDBNull(11) ? (DateTime?)null : r_704ILR.GetDateTime(11)
        };
    }
}
