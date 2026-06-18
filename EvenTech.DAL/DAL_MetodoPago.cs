using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_MetodoPago
    {
        public static List<BE_MetodoPago> GetAll()
        {
            var list = new List<BE_MetodoPago>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT Id, Nombre FROM dbo.MetodosPago ORDER BY Id", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new BE_MetodoPago { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            return list;
        }
    }
}
