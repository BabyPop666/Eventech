using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Reserva
    {
        // SELECT base con JOIN a salon y cliente para proyectar sus nombres.
        private const string SelectBase =
            "SELECT r.Id, r.ClienteId, LTRIM(ISNULL(c.Nombre,'') + ISNULL(' ' + c.Apellido,'')) AS ClienteNombre, " +
            "r.SalonId, s.Nombre, r.FechaEvento, r.Estado, r.Monto, r.CreatedAt, r.Dvh " +
            "FROM dbo.Reservas r " +
            "INNER JOIN dbo.Salones s ON s.Id = r.SalonId " +
            "LEFT JOIN dbo.Clientes c ON c.Id = r.ClienteId ";

        public static List<BE_Reserva> GetAll()
        {
            var list = new List<BE_Reserva>();
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(SelectBase + "ORDER BY r.FechaEvento DESC", cn.OpenConnection()))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(Map(r));
                }
            }
            return list;
        }

        public static BE_Reserva GetById(int id)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(SelectBase + "WHERE r.Id = @id", cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    using (var r = cmd.ExecuteReader())
                    {
                        return r.Read() ? Map(r) : null;
                    }
                }
            }
        }

        // Anti-solapamiento: hay otra reserva NO cancelada para ese salon y fecha
        // (excluyendo la propia reserva en edicion)?
        public static bool SalonOcupado(int salonId, DateTime fecha, int excluirId)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Reservas " +
                "WHERE SalonId = @s AND CAST(FechaEvento AS DATE) = @f AND Estado <> 'CANCELADA' AND Id <> @ex",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@s", SqlDbType.Int).Value = salonId;
                cmd.Parameters.Add("@f", SqlDbType.Date).Value = fecha.Date;
                cmd.Parameters.Add("@ex", SqlDbType.Int).Value = excluirId;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static int Insert(BE_Reserva reserva)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.Reservas (ClienteId, SalonId, FechaEvento, Estado, Monto, Dvh) " +
                    "OUTPUT INSERTED.Id " +
                    "VALUES (@cliente, @salon, @fecha, @estado, @monto, @dvh)",
                    cn.OpenConnection()))
                {
                    BindEditable(cmd, reserva);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public static void Update(BE_Reserva reserva)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "UPDATE dbo.Reservas SET ClienteId = @cliente, SalonId = @salon, " +
                    "FechaEvento = @fecha, Estado = @estado, Monto = @monto, Dvh = @dvh WHERE Id = @id",
                    cn.OpenConnection()))
                {
                    BindEditable(cmd, reserva);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = reserva.Id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Actualiza solo el DV horizontal (usado al recalcular la linea base).
        public static void UpdateDvh(int id, string dvh)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("UPDATE dbo.Reservas SET Dvh = @dvh WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@dvh", SqlDbType.NVarChar, 64).Value = (object)dvh ?? DBNull.Value;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cmd.ExecuteNonQuery();
            }
        }

        private static void BindEditable(SqlCommand cmd, BE_Reserva reserva)
        {
            cmd.Parameters.Add("@cliente", SqlDbType.Int).Value = reserva.ClienteId;
            cmd.Parameters.Add("@salon", SqlDbType.Int).Value = reserva.SalonId;
            cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = reserva.FechaEvento;
            cmd.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = reserva.Estado.ToString();
            cmd.Parameters.Add("@monto", SqlDbType.Decimal).Value = reserva.Monto;
            cmd.Parameters.Add("@dvh", SqlDbType.NVarChar, 64).Value = (object)reserva.Dvh ?? DBNull.Value;
        }

        private static BE_Reserva Map(SqlDataReader r) => new BE_Reserva
        {
            Id = r.GetInt32(0),
            ClienteId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
            ClienteNombre = r.IsDBNull(2) ? string.Empty : r.GetString(2),
            SalonId = r.GetInt32(3),
            SalonNombre = r.GetString(4),
            FechaEvento = r.GetDateTime(5),
            Estado = (EstadoReserva)Enum.Parse(typeof(EstadoReserva), r.GetString(6)),
            Monto = r.GetDecimal(7),
            CreatedAt = r.GetDateTime(8),
            Dvh = r.IsDBNull(9) ? null : r.GetString(9)
        };
    }
}
