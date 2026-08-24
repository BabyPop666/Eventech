using System.Data;
using Microsoft.Data.SqlClient;

namespace EvenTech.DAL
{
    // Persistencia del digito verificador vertical (uno por tabla protegida).
    public static class DAL_DVVertical_704ILR
    {
        public static string Get_704ILR(string tabla_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT Dvv FROM dbo.DVVertical WHERE Tabla = @t", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@t", SqlDbType.NVarChar, 50).Value = tabla_704ILR;
                var r_704ILR = cmd_704ILR.ExecuteScalar();
                return r_704ILR == null || r_704ILR == System.DBNull.Value ? null : (string)r_704ILR;
            }
        }

        // Inserta o actualiza el DVV de la tabla (upsert).
        public static void Upsert_704ILR(string tabla_704ILR, string dvv_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM dbo.DVVertical WHERE Tabla = @t) " +
                "  UPDATE dbo.DVVertical SET Dvv = @v, CalculadoEn = GETDATE() WHERE Tabla = @t; " +
                "ELSE " +
                "  INSERT INTO dbo.DVVertical (Tabla, Dvv, CalculadoEn) VALUES (@t, @v, GETDATE());",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@t", SqlDbType.NVarChar, 50).Value = tabla_704ILR;
                cmd_704ILR.Parameters.Add("@v", SqlDbType.NVarChar, 64).Value = dvv_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }
    }
}
