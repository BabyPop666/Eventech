using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Idioma
    {
        public static List<BE_Idioma> GetIdiomas()
        {
            var list = new List<BE_Idioma>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Codigo, Nombre FROM dbo.Idiomas ORDER BY Nombre",
                cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new BE_Idioma
                    {
                        Id = r.GetInt32(0),
                        Codigo = r.GetString(1),
                        Nombre = r.GetString(2)
                    });
                }
            }
            return list;
        }

        // Traducciones de un idioma como diccionario clave -> texto.
        public static Dictionary<string, string> GetTraducciones(int idiomaId)
        {
            var dict = new Dictionary<string, string>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Clave, Texto FROM dbo.Traducciones WHERE IdiomaId = @id",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idiomaId;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        dict[r.GetString(0)] = r.GetString(1);
                }
            }
            return dict;
        }
    }
}
