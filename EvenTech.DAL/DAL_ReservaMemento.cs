using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    // Persistencia de los mementos de reserva (patron Memento). Cada version es
    // una cabecera (ReservaMemento) + sus lineas de servicios congeladas
    // (ReservaMementoServicio), guardadas en la misma transaccion.
    public static class DAL_ReservaMemento
    {
        // Cabecera + nombres actuales de cliente/salon proyectados para mostrar.
        private const string SelectBase =
            "SELECT m.Id, m.ReservaId, m.ClienteId, m.SalonId, m.FechaEvento, m.Estado, m.Monto, m.Usuario, m.Fecha, " +
            "LTRIM(ISNULL(c.Nombre,'') + ISNULL(' ' + c.Apellido,'')) AS ClienteNombre, s.Nombre AS SalonNombre " +
            "FROM dbo.ReservaMemento m " +
            "LEFT JOIN dbo.Clientes c ON c.Id = m.ClienteId " +
            "LEFT JOIN dbo.Salones s ON s.Id = m.SalonId ";

        public static int Insert(BE_ReservaMemento m)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                using (var tx = conn.BeginTransaction())
                {
                    int id;
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.ReservaMemento (ReservaId, ClienteId, SalonId, FechaEvento, Estado, Monto, Usuario, Fecha) " +
                        "OUTPUT INSERTED.Id " +
                        "VALUES (@reserva, @cliente, @salon, @fechaEvento, @estado, @monto, @usuario, @fecha)",
                        conn, tx))
                    {
                        cmd.Parameters.Add("@reserva", SqlDbType.Int).Value = m.ReservaId;
                        cmd.Parameters.Add("@cliente", SqlDbType.Int).Value = m.ClienteId;
                        cmd.Parameters.Add("@salon", SqlDbType.Int).Value = m.SalonId;
                        cmd.Parameters.Add("@fechaEvento", SqlDbType.DateTime).Value = m.FechaEvento;
                        cmd.Parameters.Add("@estado", SqlDbType.NVarChar, 20).Value = m.Estado.ToString();
                        cmd.Parameters.Add("@monto", SqlDbType.Decimal).Value = m.Monto;
                        cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = m.Usuario ?? "Sistema";
                        cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = m.Fecha;
                        id = (int)cmd.ExecuteScalar();
                    }

                    foreach (var sv in m.Servicios)
                    {
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.ReservaMementoServicio (MementoId, ServicioId, Cantidad, PrecioUnitario) " +
                            "VALUES (@m, @s, @c, @p)", conn, tx))
                        {
                            ins.Parameters.Add("@m", SqlDbType.Int).Value = id;
                            ins.Parameters.Add("@s", SqlDbType.Int).Value = sv.ServicioId;
                            ins.Parameters.Add("@c", SqlDbType.Int).Value = sv.Cantidad;
                            ins.Parameters.Add("@p", SqlDbType.Decimal).Value = sv.PrecioUnitario;
                            ins.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                    return id;
                }
            }
        }

        // Listado de versiones de una reserva, de la mas reciente a la mas vieja.
        // No carga las lineas de servicios (solo hacen falta al restaurar).
        public static List<BE_ReservaMemento> GetByReserva(int reservaId)
        {
            var list = new List<BE_ReservaMemento>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(SelectBase + "WHERE m.ReservaId = @r ORDER BY m.Id DESC", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@r", SqlDbType.Int).Value = reservaId;
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(Map(r, null));
            }
            return list;
        }

        // Version puntual con sus lineas de servicios (lo que necesita la restauracion).
        public static BE_ReservaMemento GetById(int id)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                BE_ReservaMemento header = null;
                using (var cmd = new SqlCommand(SelectBase + "WHERE m.Id = @id", conn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) header = Map(r, null);
                }
                if (header == null) return null;

                var servicios = new List<BE_ReservaServicio>();
                using (var cmd = new SqlCommand(
                    "SELECT ms.ServicioId, s.Nombre, ms.Cantidad, ms.PrecioUnitario " +
                    "FROM dbo.ReservaMementoServicio ms " +
                    "INNER JOIN dbo.Servicios s ON s.Id = ms.ServicioId " +
                    "WHERE ms.MementoId = @m ORDER BY s.Nombre", conn))
                {
                    cmd.Parameters.Add("@m", SqlDbType.Int).Value = id;
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            servicios.Add(new BE_ReservaServicio
                            {
                                ReservaId = header.ReservaId,
                                ServicioId = r.GetInt32(0),
                                ServicioNombre = r.GetString(1),
                                Cantidad = r.GetInt32(2),
                                PrecioUnitario = r.GetDecimal(3)
                            });
                }

                // Se rearma el memento con las lineas (es inmutable: no hay setter).
                return new BE_ReservaMemento(header.Id, header.ReservaId, header.ClienteId, header.SalonId,
                    header.FechaEvento, header.Estado, header.Monto, header.Usuario, header.Fecha,
                    header.ClienteNombre, header.SalonNombre, servicios);
            }
        }

        private static BE_ReservaMemento Map(SqlDataReader r, List<BE_ReservaServicio> servicios) =>
            new BE_ReservaMemento(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetInt32(2),
                r.GetInt32(3),
                r.GetDateTime(4),
                (EstadoReserva)Enum.Parse(typeof(EstadoReserva), r.GetString(5)),
                r.GetDecimal(6),
                r.GetString(7),
                r.GetDateTime(8),
                r.IsDBNull(9) ? string.Empty : r.GetString(9),
                r.IsDBNull(10) ? string.Empty : r.GetString(10),
                servicios);
    }
}
