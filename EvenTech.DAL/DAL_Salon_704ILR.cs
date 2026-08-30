using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Salon_704ILR
    {
        public static List<BE_Salon_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_Salon_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(
                    "SELECT Id, Nombre, Capacidad FROM dbo.Salones ORDER BY Nombre",
                    cn_704ILR.OpenConnection_704ILR()))
                {
                    using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    {
                        while (r_704ILR.Read())
                        {
                            list_704ILR.Add(new BE_Salon_704ILR
                            {
                                Id_704ILR = r_704ILR.GetInt32(0),
                                Nombre_704ILR = r_704ILR.GetString(1),
                                Capacidad_704ILR = r_704ILR.GetInt32(2)
                            });
                        }
                    }
                }
            }
            return list_704ILR;
        }

        // Capacidad del salon, o 0 si no existe. La necesita la RN-06 para
        // verificar que el salon pueda alojar a los invitados de la reserva.
        public static int Capacidad_704ILR(int salonId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(
                    "SELECT Capacidad FROM dbo.Salones WHERE Id = @id",
                    cn_704ILR.OpenConnection_704ILR()))
                {
                    cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = salonId_704ILR;
                    object v_704ILR = cmd_704ILR.ExecuteScalar();
                    return v_704ILR == null || v_704ILR == DBNull.Value ? 0 : (int)v_704ILR;
                }
            }
        }

        public static bool Exists_704ILR(int salonId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                using (var cmd_704ILR = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.Salones WHERE Id = @id",
                    cn_704ILR.OpenConnection_704ILR()))
                {
                    cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = salonId_704ILR;
                    return (int)cmd_704ILR.ExecuteScalar() > 0;
                }
            }
        }
    }
}
