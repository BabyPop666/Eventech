using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Reserva
    {
        // SELECT base con JOIN al salon para proyectar el nombre.
        private const string SelectBase =
            "SELECT r.Id, r.ClienteNombre, r.SalonId, s.Nombre, r.FechaEvento, r.Estado, r.Monto, r.CreatedAt, r.Dvh " +
            "FROM dbo.Reservas r INNER JOIN dbo.Salones s ON s.Id = r.SalonId ";

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

        public static int Insert(BE_Reserva reserva)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.Reservas (ClienteNombre, SalonId, FechaEvento, Estado, Monto, Dvh) " +
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
                    "UPDATE dbo.Reservas SET ClienteNombre = @cliente, SalonId = @salon, " +
                    "FechaEvento = @fecha, Estado = @estado, Monto = @monto, Dvh = @dvh WHERE Id = @id",
                    cn.OpenConnection()))
                {
                    BindEditable(cmd, reserva);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = reserva.Id;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void BindEditable(SqlCommand cmd, BE_Reserva reserva)
        {
            cmd.Parameters.Add("@cliente", SqlDbType.NVarChar, 150).Value = reserva.ClienteNombre ?? string.Empty;
            cmd.Parameters.Add("@salon", SqlDbType.Int).Value = reserva.SalonId;
            cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = reserva.FechaEvento;
            cmd.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = reserva.Estado.ToString();
            cmd.Parameters.Add("@monto", SqlDbType.Decimal).Value = reserva.Monto;
            cmd.Parameters.Add("@dvh", SqlDbType.NVarChar, 64).Value = (object)reserva.Dvh ?? DBNull.Value;
        }

        private static BE_Reserva Map(SqlDataReader r) => new BE_Reserva
        {
            Id = r.GetInt32(0),
            ClienteNombre = r.GetString(1),
            SalonId = r.GetInt32(2),
            SalonNombre = r.GetString(3),
            FechaEvento = r.GetDateTime(4),
            Estado = (EstadoReserva)Enum.Parse(typeof(EstadoReserva), r.GetString(5)),
            Monto = r.GetDecimal(6),
            CreatedAt = r.GetDateTime(7),
            Dvh = r.IsDBNull(8) ? null : r.GetString(8)
        };
    }
}
