using System.Data;
using Microsoft.Data.SqlClient;

namespace EvenTech.DAL
{
    // Persistencia del digito verificador vertical (uno por tabla protegida).
    public static class DAL_DVVertical
    {
        public static string Get(string tabla)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Dvv FROM dbo.DVVertical WHERE Tabla = @t", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@t", SqlDbType.NVarChar, 50).Value = tabla;
                var r = cmd.ExecuteScalar();
                return r == null || r == System.DBNull.Value ? null : (string)r;
            }
        }

        // Inserta o actualiza el DVV de la tabla (upsert).
        public static void Upsert(string tabla, string dvv)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM dbo.DVVertical WHERE Tabla = @t) " +
                "  UPDATE dbo.DVVertical SET Dvv = @v, CalculadoEn = GETDATE() WHERE Tabla = @t; " +
                "ELSE " +
                "  INSERT INTO dbo.DVVertical (Tabla, Dvv, CalculadoEn) VALUES (@t, @v, GETDATE());",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@t", SqlDbType.NVarChar, 50).Value = tabla;
                cmd.Parameters.Add("@v", SqlDbType.NVarChar, 64).Value = dvv;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
