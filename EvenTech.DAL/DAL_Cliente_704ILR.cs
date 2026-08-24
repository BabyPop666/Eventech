using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;
using EvenTech.Services;

namespace EvenTech.DAL
{
    public static class DAL_Cliente_704ILR
    {
        private const string SelectBase_704ILR =
            "SELECT Id, Nombre, Apellido, Dni, Email, Telefono, CreatedAt FROM dbo.Clientes ";

        public static List<BE_Cliente_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_Cliente_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "ORDER BY Nombre, Apellido", cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read()) list_704ILR.Add(Map_704ILR(r_704ILR));
            }
            return list_704ILR;
        }

        public static BE_Cliente_704ILR GetById_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(SelectBase_704ILR + "WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    return r_704ILR.Read() ? Map_704ILR(r_704ILR) : null;
            }
        }

        public static bool Exists_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT COUNT(1) FROM dbo.Clientes WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        // DNI duplicado (ignorando el propio registro en edicion).
        public static bool ExistsDni_704ILR(string dni_704ILR, int excluirId_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Clientes WHERE Dni = @dni AND Id <> @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@dni", SqlDbType.NVarChar, 20).Value = dni_704ILR ?? string.Empty;
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = excluirId_704ILR;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        public static int Insert_704ILR(BE_Cliente_704ILR c_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Clientes (Nombre, Apellido, Dni, Email, Telefono) " +
                "OUTPUT INSERTED.Id VALUES (@n, @a, @d, @e, @t)", cn_704ILR.OpenConnection_704ILR()))
            {
                Bind_704ILR(cmd_704ILR, c_704ILR);
                return (int)cmd_704ILR.ExecuteScalar();
            }
        }

        public static void Update_704ILR(BE_Cliente_704ILR c_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "UPDATE dbo.Clientes SET Nombre=@n, Apellido=@a, Dni=@d, Email=@e, Telefono=@t WHERE Id=@id",
                cn_704ILR.OpenConnection_704ILR()))
            {
                Bind_704ILR(cmd_704ILR, c_704ILR);
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = c_704ILR.Id_704ILR;
                cmd_704ILR.ExecuteNonQuery();
            }
        }

        // Email y Telefono se persisten cifrados (AES reversible, CryptoService):
        // son datos de contacto sensibles que la app necesita leer de vuelta.
        // DNI queda en claro porque participa del indice unico y de busquedas por
        // igualdad en SQL (el cifrado con IV aleatorio rompe ambas cosas).
        private static void Bind_704ILR(SqlCommand cmd_704ILR, BE_Cliente_704ILR c_704ILR)
        {
            cmd_704ILR.Parameters.Add("@n", SqlDbType.NVarChar, 60).Value = c_704ILR.Nombre_704ILR ?? string.Empty;
            cmd_704ILR.Parameters.Add("@a", SqlDbType.NVarChar, 60).Value = Nz_704ILR(c_704ILR.Apellido_704ILR);
            cmd_704ILR.Parameters.Add("@d", SqlDbType.NVarChar, 20).Value = Nz_704ILR(c_704ILR.Dni_704ILR);
            cmd_704ILR.Parameters.Add("@e", SqlDbType.NVarChar, 400).Value = NzCifrado_704ILR(c_704ILR.Email_704ILR);
            cmd_704ILR.Parameters.Add("@t", SqlDbType.NVarChar, 200).Value = NzCifrado_704ILR(c_704ILR.Telefono_704ILR);
        }

        // string vacio/blanco -> NULL (para respetar el indice unico filtrado de DNI).
        private static object Nz_704ILR(string s_704ILR) => string.IsNullOrWhiteSpace(s_704ILR) ? (object)DBNull.Value : s_704ILR.Trim();

        private static object NzCifrado_704ILR(string s_704ILR) =>
            string.IsNullOrWhiteSpace(s_704ILR) ? (object)DBNull.Value : CryptoService_704ILR.Proteger_704ILR(s_704ILR.Trim());

        private static BE_Cliente_704ILR Map_704ILR(SqlDataReader r_704ILR) => new BE_Cliente_704ILR
        {
            Id_704ILR = r_704ILR.GetInt32(0),
            Nombre_704ILR = r_704ILR.GetString(1),
            Apellido_704ILR = r_704ILR.IsDBNull(2) ? null : r_704ILR.GetString(2),
            Dni_704ILR = r_704ILR.IsDBNull(3) ? null : r_704ILR.GetString(3),
            Email_704ILR = r_704ILR.IsDBNull(4) ? null : CryptoService_704ILR.Desproteger_704ILR(r_704ILR.GetString(4)),
            Telefono_704ILR = r_704ILR.IsDBNull(5) ? null : CryptoService_704ILR.Desproteger_704ILR(r_704ILR.GetString(5)),
            CreatedAt_704ILR = r_704ILR.GetDateTime(6)
        };
    }
}
