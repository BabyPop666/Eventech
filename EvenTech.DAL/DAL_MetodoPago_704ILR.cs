using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_MetodoPago_704ILR
    {
        public static List<BE_MetodoPago_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_MetodoPago_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT Id, Nombre FROM dbo.MetodosPago ORDER BY Id", cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
                while (r_704ILR.Read())
                    list_704ILR.Add(new BE_MetodoPago_704ILR { Id_704ILR = r_704ILR.GetInt32(0), Nombre_704ILR = r_704ILR.GetString(1) });
            return list_704ILR;
        }
    }
}
