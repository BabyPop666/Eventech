using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Bitacora_704ILR
    {
        public static void Insert_704ILR(BE_BitacoraEntry_704ILR e_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Bitacora (Fecha, Usuario, Modulo, Accion, Criticidad, Detalle) " +
                "VALUES (@fecha, @usuario, @modulo, @accion, @criticidad, @detalle)",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@fecha", SqlDbType.DateTime).Value = e_704ILR.Fecha_704ILR == default ? DateTime.Now : e_704ILR.Fecha_704ILR;
                cmd_704ILR.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = (object)e_704ILR.Usuario_704ILR ?? "Sistema";
                cmd_704ILR.Parameters.Add("@modulo", SqlDbType.NVarChar, 50).Value = (object)e_704ILR.Modulo_704ILR ?? DBNull.Value;
                cmd_704ILR.Parameters.Add("@accion", SqlDbType.NVarChar, 100).Value = (object)e_704ILR.Accion_704ILR ?? DBNull.Value;
                cmd_704ILR.Parameters.Add("@criticidad", SqlDbType.TinyInt).Value = (byte)e_704ILR.Criticidad_704ILR;
                cmd_704ILR.Parameters.Add("@detalle", SqlDbType.NVarChar, 1000).Value = (object)e_704ILR.Detalle_704ILR ?? DBNull.Value;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        // Busqueda combinada: cada filtro es opcional y se concatena con AND solo
        // si viene informado (patron WHERE 1=1 + parametros opcionales).
        public static List<BE_BitacoraEntry_704ILR> Buscar_704ILR(BitacoraFiltros_704ILR f_704ILR)
        {
            var sb_704ILR = new StringBuilder(
                "SELECT Id, Fecha, Usuario, Modulo, Accion, Criticidad, Detalle FROM dbo.Bitacora WHERE 1=1 ");
            var ps_704ILR = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(f_704ILR.Usuario_704ILR))
            {
                sb_704ILR.Append("AND Usuario LIKE @usuario ");
                ps_704ILR.Add(new SqlParameter("@usuario", "%" + f_704ILR.Usuario_704ILR.Trim() + "%"));
            }
            if (f_704ILR.FechaInicio_704ILR.HasValue)
            {
                sb_704ILR.Append("AND Fecha >= @desde ");
                ps_704ILR.Add(new SqlParameter("@desde", f_704ILR.FechaInicio_704ILR.Value.Date));
            }
            if (f_704ILR.FechaFin_704ILR.HasValue)
            {
                sb_704ILR.Append("AND Fecha < @hasta ");
                ps_704ILR.Add(new SqlParameter("@hasta", f_704ILR.FechaFin_704ILR.Value.Date.AddDays(1)));
            }
            if (!string.IsNullOrWhiteSpace(f_704ILR.Modulo_704ILR))
            {
                sb_704ILR.Append("AND Modulo = @modulo ");
                ps_704ILR.Add(new SqlParameter("@modulo", f_704ILR.Modulo_704ILR.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(f_704ILR.Accion_704ILR))
            {
                sb_704ILR.Append("AND Accion LIKE @accion ");
                ps_704ILR.Add(new SqlParameter("@accion", "%" + f_704ILR.Accion_704ILR.Trim() + "%"));
            }
            if (f_704ILR.Criticidad_704ILR.HasValue)
            {
                sb_704ILR.Append("AND Criticidad = @criticidad ");
                ps_704ILR.Add(new SqlParameter("@criticidad", (byte)f_704ILR.Criticidad_704ILR.Value));
            }
            sb_704ILR.Append("ORDER BY Id DESC");

            var list_704ILR = new List<BE_BitacoraEntry_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(sb_704ILR.ToString(), cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.AddRange(ps_704ILR.ToArray());
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    while (r_704ILR.Read())
                    {
                        list_704ILR.Add(new BE_BitacoraEntry_704ILR
                        {
                            Id_704ILR = r_704ILR.GetInt32(0),
                            Fecha_704ILR = r_704ILR.GetDateTime(1),
                            Usuario_704ILR = r_704ILR.GetString(2),
                            Modulo_704ILR = r_704ILR.IsDBNull(3) ? null : r_704ILR.GetString(3),
                            Accion_704ILR = r_704ILR.IsDBNull(4) ? null : r_704ILR.GetString(4),
                            Criticidad_704ILR = (CriticidadBitacora_704ILR)r_704ILR.GetByte(5),
                            Detalle_704ILR = r_704ILR.IsDBNull(6) ? null : r_704ILR.GetString(6)
                        });
                    }
                }
            }
            return list_704ILR;
        }

        // Modulos distintos, para poblar el combo de filtro.
        public static List<string> GetModulos_704ILR()
        {
            var list_704ILR = new List<string>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT DISTINCT Modulo FROM dbo.Bitacora WHERE Modulo IS NOT NULL ORDER BY Modulo",
                cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read()) list_704ILR.Add(r_704ILR.GetString(0));
            }
            return list_704ILR;
        }
    }
}
