using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Salon
    {
        public static List<BE_Salon> GetAll()
        {
            var list = new List<BE_Salon>();
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT Id, Nombre, Capacidad FROM dbo.Salones ORDER BY Nombre",
                    cn.OpenConnection()))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new BE_Salon
                            {
                                Id = r.GetInt32(0),
                                Nombre = r.GetString(1),
                                Capacidad = r.GetInt32(2)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public static bool Exists(int salonId)
        {
            using (var cn = new DAL_DB_Connection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.Salones WHERE Id = @id",
                    cn.OpenConnection()))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = salonId;
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }
    }
}
