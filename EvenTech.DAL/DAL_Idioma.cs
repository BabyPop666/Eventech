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

        public static bool ExistsCodigo(string codigo)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Idiomas WHERE Codigo = @c", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 5).Value = codigo ?? string.Empty;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static int InsertIdioma(string codigo, string nombre)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Idiomas (Codigo, Nombre) OUTPUT INSERTED.Id VALUES (@c, @n)",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 5).Value = codigo;
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 50).Value = nombre;
                return (int)cmd.ExecuteScalar();
            }
        }

        // Claves distintas existentes (para inicializar un idioma nuevo).
        public static List<string> GetClaves()
        {
            var list = new List<string>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT DISTINCT Clave FROM dbo.Traducciones ORDER BY Clave", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) list.Add(r.GetString(0));
            }
            return list;
        }

        // Inserta o actualiza el texto de una clave para un idioma (upsert).
        public static void UpsertTraduccion(int idiomaId, string clave, string texto)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM dbo.Traducciones WHERE IdiomaId = @i AND Clave = @c) " +
                "  UPDATE dbo.Traducciones SET Texto = @t WHERE IdiomaId = @i AND Clave = @c; " +
                "ELSE " +
                "  INSERT INTO dbo.Traducciones (IdiomaId, Clave, Texto) VALUES (@i, @c, @t);",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@i", SqlDbType.Int).Value = idiomaId;
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 60).Value = clave;
                cmd.Parameters.Add("@t", SqlDbType.NVarChar, 250).Value = texto ?? string.Empty;
                cmd.ExecuteNonQuery();
            }
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
