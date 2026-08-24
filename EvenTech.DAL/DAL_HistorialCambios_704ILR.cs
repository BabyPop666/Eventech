using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_HistorialCambios_704ILR
    {
        public static void Insert_704ILR(BE_CambioEntry_704ILR c_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.HistorialCambios (Entidad, EntidadId, NombreCampo, ValorAnterior, ValorNuevo, Usuario, Fecha) " +
                "VALUES (@entidad, @entidadId, @campo, @anterior, @nuevo, @usuario, @fecha)",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@entidad", SqlDbType.NVarChar, 50).Value = c_704ILR.Entidad_704ILR;
                cmd_704ILR.Parameters.Add("@entidadId", SqlDbType.Int).Value = c_704ILR.EntidadId_704ILR;
                cmd_704ILR.Parameters.Add("@campo", SqlDbType.NVarChar, 100).Value = c_704ILR.NombreCampo_704ILR;
                cmd_704ILR.Parameters.Add("@anterior", SqlDbType.NVarChar, 500).Value = (object)c_704ILR.ValorAnterior_704ILR ?? DBNull.Value;
                cmd_704ILR.Parameters.Add("@nuevo", SqlDbType.NVarChar, 500).Value = (object)c_704ILR.ValorNuevo_704ILR ?? DBNull.Value;
                cmd_704ILR.Parameters.Add("@usuario", SqlDbType.NVarChar, 50).Value = (object)c_704ILR.Usuario_704ILR ?? "Sistema";
                cmd_704ILR.Parameters.Add("@fecha", SqlDbType.DateTime).Value = c_704ILR.Fecha_704ILR == default ? DateTime.Now : c_704ILR.Fecha_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        // Historial de una entidad puntual, ordenado cronologicamente (mas reciente primero).
        public static List<BE_CambioEntry_704ILR> GetByEntidad_704ILR(string entidad_704ILR, int entidadId_704ILR)
        {
            var list_704ILR = new List<BE_CambioEntry_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT Id, Entidad, EntidadId, NombreCampo, ValorAnterior, ValorNuevo, Usuario, Fecha " +
                "FROM dbo.HistorialCambios WHERE Entidad = @entidad AND EntidadId = @entidadId ORDER BY Id DESC",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@entidad", SqlDbType.NVarChar, 50).Value = entidad_704ILR;
                cmd_704ILR.Parameters.Add("@entidadId", SqlDbType.Int).Value = entidadId_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    while (r_704ILR.Read())
                    {
                        list_704ILR.Add(new BE_CambioEntry_704ILR
                        {
                            Id_704ILR = r_704ILR.GetInt32(0),
                            Entidad_704ILR = r_704ILR.GetString(1),
                            EntidadId_704ILR = r_704ILR.GetInt32(2),
                            NombreCampo_704ILR = r_704ILR.GetString(3),
                            ValorAnterior_704ILR = r_704ILR.IsDBNull(4) ? null : r_704ILR.GetString(4),
                            ValorNuevo_704ILR = r_704ILR.IsDBNull(5) ? null : r_704ILR.GetString(5),
                            Usuario_704ILR = r_704ILR.GetString(6),
                            Fecha_704ILR = r_704ILR.GetDateTime(7)
                        });
                    }
                }
            }
            return list_704ILR;
        }
    }
}
