using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Bitacora
    {
        public static void Insert(BE_BitacoraEntry e)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Bitacora (Fecha, Usuario, Modulo, Accion, Criticidad, Detalle) " +
                "VALUES (@fecha, @usuario, @modulo, @accion, @criticidad, @detalle)",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = e.Fecha == default ? DateTime.Now : e.Fecha;
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = (object)e.Usuario ?? "Sistema";
                cmd.Parameters.Add("@modulo", SqlDbType.NVarChar, 50).Value = (object)e.Modulo ?? DBNull.Value;
                cmd.Parameters.Add("@accion", SqlDbType.NVarChar, 100).Value = (object)e.Accion ?? DBNull.Value;
                cmd.Parameters.Add("@criticidad", SqlDbType.TinyInt).Value = (byte)e.Criticidad;
                cmd.Parameters.Add("@detalle", SqlDbType.NVarChar, 1000).Value = (object)e.Detalle ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
        }

        // Busqueda combinada: cada filtro es opcional y se concatena con AND solo
        // si viene informado (patron WHERE 1=1 + parametros opcionales).
        public static List<BE_BitacoraEntry> Buscar(BitacoraFiltros f)
        {
            var sb = new StringBuilder(
                "SELECT Id, Fecha, Usuario, Modulo, Accion, Criticidad, Detalle FROM dbo.Bitacora WHERE 1=1 ");
            var ps = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(f.Usuario))
            {
                sb.Append("AND Usuario LIKE @usuario ");
                ps.Add(new SqlParameter("@usuario", "%" + f.Usuario.Trim() + "%"));
            }
            if (f.FechaInicio.HasValue)
            {
                sb.Append("AND Fecha >= @desde ");
                ps.Add(new SqlParameter("@desde", f.FechaInicio.Value.Date));
            }
            if (f.FechaFin.HasValue)
            {
                sb.Append("AND Fecha < @hasta ");
                ps.Add(new SqlParameter("@hasta", f.FechaFin.Value.Date.AddDays(1)));
            }
            if (!string.IsNullOrWhiteSpace(f.Modulo))
            {
                sb.Append("AND Modulo = @modulo ");
                ps.Add(new SqlParameter("@modulo", f.Modulo.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(f.Accion))
            {
                sb.Append("AND Accion LIKE @accion ");
                ps.Add(new SqlParameter("@accion", "%" + f.Accion.Trim() + "%"));
            }
            if (f.Criticidad.HasValue)
            {
                sb.Append("AND Criticidad = @criticidad ");
                ps.Add(new SqlParameter("@criticidad", (byte)f.Criticidad.Value));
            }
            sb.Append("ORDER BY Id DESC");

            var list = new List<BE_BitacoraEntry>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(sb.ToString(), cn.OpenConnection()))
            {
                cmd.Parameters.AddRange(ps.ToArray());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new BE_BitacoraEntry
                        {
                            Id = r.GetInt32(0),
                            Fecha = r.GetDateTime(1),
                            Usuario = r.GetString(2),
                            Modulo = r.IsDBNull(3) ? null : r.GetString(3),
                            Accion = r.IsDBNull(4) ? null : r.GetString(4),
                            Criticidad = (CriticidadBitacora)r.GetByte(5),
                            Detalle = r.IsDBNull(6) ? null : r.GetString(6)
                        });
                    }
                }
            }
            return list;
        }

        // Modulos distintos, para poblar el combo de filtro.
        public static List<string> GetModulos()
        {
            var list = new List<string>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT DISTINCT Modulo FROM dbo.Bitacora WHERE Modulo IS NOT NULL ORDER BY Modulo",
                cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) list.Add(r.GetString(0));
            }
            return list;
        }
    }
}
