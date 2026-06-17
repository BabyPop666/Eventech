using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_HistorialCambios
    {
        public static void Insert(BE_CambioEntry c)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.HistorialCambios (Entidad, EntidadId, NombreCampo, ValorAnterior, ValorNuevo, Usuario, Fecha) " +
                "VALUES (@entidad, @entidadId, @campo, @anterior, @nuevo, @usuario, @fecha)",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@entidad", SqlDbType.NVarChar, 50).Value = c.Entidad;
                cmd.Parameters.Add("@entidadId", SqlDbType.Int).Value = c.EntidadId;
                cmd.Parameters.Add("@campo", SqlDbType.NVarChar, 100).Value = c.NombreCampo;
                cmd.Parameters.Add("@anterior", SqlDbType.NVarChar, 500).Value = (object)c.ValorAnterior ?? DBNull.Value;
                cmd.Parameters.Add("@nuevo", SqlDbType.NVarChar, 500).Value = (object)c.ValorNuevo ?? DBNull.Value;
                cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = (object)c.Usuario ?? "Sistema";
                cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = c.Fecha == default ? DateTime.Now : c.Fecha;
                cmd.ExecuteNonQuery();
            }
        }

        // Historial de una entidad puntual, ordenado cronologicamente (mas reciente primero).
        public static List<BE_CambioEntry> GetByEntidad(string entidad, int entidadId)
        {
            var list = new List<BE_CambioEntry>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Entidad, EntidadId, NombreCampo, ValorAnterior, ValorNuevo, Usuario, Fecha " +
                "FROM dbo.HistorialCambios WHERE Entidad = @entidad AND EntidadId = @entidadId ORDER BY Id DESC",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@entidad", SqlDbType.NVarChar, 50).Value = entidad;
                cmd.Parameters.Add("@entidadId", SqlDbType.Int).Value = entidadId;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new BE_CambioEntry
                        {
                            Id = r.GetInt32(0),
                            Entidad = r.GetString(1),
                            EntidadId = r.GetInt32(2),
                            NombreCampo = r.GetString(3),
                            ValorAnterior = r.IsDBNull(4) ? null : r.GetString(4),
                            ValorNuevo = r.IsDBNull(5) ? null : r.GetString(5),
                            Usuario = r.GetString(6),
                            Fecha = r.GetDateTime(7)
                        });
                    }
                }
            }
            return list;
        }
    }
}
