using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Perfil_704ILR
    {
        public static bool ExistsNombre_704ILR(string nombre_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand("SELECT COUNT(1) FROM dbo.Perfiles WHERE Nombre = @n", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre_704ILR ?? string.Empty;
                return (int)cmd_704ILR.ExecuteScalar() > 0;
            }
        }

        public static int Insert_704ILR(string nombre_704ILR, string descripcion_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "INSERT INTO dbo.Perfiles (Nombre, Descripcion) VALUES (@n, @d); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre_704ILR;
                cmd_704ILR.Parameters.Add("@d", SqlDbType.NVarChar, 250).Value = (object)descripcion_704ILR ?? DBNull.Value;
                return (int)cmd_704ILR.ExecuteScalar();
            }
        }

        public static List<BE_Perfil_704ILR> GetAll_704ILR()
        {
            var list_704ILR = new List<BE_Perfil_704ILR>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT Id, Nombre, Descripcion FROM dbo.Perfiles ORDER BY Nombre",
                cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read())
                {
                    list_704ILR.Add(new BE_Perfil_704ILR
                    {
                        Id_704ILR = r_704ILR.GetInt32(0),
                        Nombre_704ILR = r_704ILR.GetString(1),
                        Descripcion_704ILR = r_704ILR.IsDBNull(2) ? null : r_704ILR.GetString(2)
                    });
                }
            }
            return list_704ILR;
        }

        public static BE_Perfil_704ILR GetById_704ILR(int id_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT Id, Nombre, Descripcion FROM dbo.Perfiles WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@id", SqlDbType.Int).Value = id_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                {
                    if (!r_704ILR.Read()) return null;
                    return new BE_Perfil_704ILR
                    {
                        Id_704ILR = r_704ILR.GetInt32(0),
                        Nombre_704ILR = r_704ILR.GetString(1),
                        Descripcion_704ILR = r_704ILR.IsDBNull(2) ? null : r_704ILR.GetString(2)
                    };
                }
            }
        }

        // Ids de los perfiles incluidos dentro de un perfil (Composite de perfiles).
        public static HashSet<int> GetIncluidos_704ILR(int perfilId_704ILR)
        {
            var set_704ILR = new HashSet<int>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT PerfilHijoId FROM dbo.PerfilIncluido WHERE PerfilPadreId = @p",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    while (r_704ILR.Read()) set_704ILR.Add(r_704ILR.GetInt32(0));
            }
            return set_704ILR;
        }

        // Grafo completo de inclusiones (padre -> hijos). Lo usa la BLL para
        // detectar ciclos antes de persistir una composicion.
        public static Dictionary<int, List<int>> GetTodasLasInclusiones_704ILR()
        {
            var grafo_704ILR = new Dictionary<int, List<int>>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT PerfilPadreId, PerfilHijoId FROM dbo.PerfilIncluido", cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read())
                {
                    int padre_704ILR = r_704ILR.GetInt32(0);
                    if (!grafo_704ILR.TryGetValue(padre_704ILR, out var hijos_704ILR))
                        grafo_704ILR[padre_704ILR] = hijos_704ILR = new List<int>();
                    hijos_704ILR.Add(r_704ILR.GetInt32(1));
                }
            }
            return grafo_704ILR;
        }

        // Reemplaza la composicion completa del perfil (permisos + perfiles
        // incluidos) en una unica transaccion.
        public static void SetComposicion_704ILR(int perfilId_704ILR, IEnumerable<int> permisoIds_704ILR, IEnumerable<int> perfilesIncluidos_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                var conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (var tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    using (var del_704ILR = new SqlCommand("DELETE FROM dbo.PerfilPermiso WHERE PerfilId = @p", conn_704ILR, tx_704ILR))
                    {
                        del_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                        del_704ILR.ExecuteNonQuery();
                    }
                    foreach (int permisoId_704ILR in permisoIds_704ILR)
                    {
                        using (var ins_704ILR = new SqlCommand(
                            "INSERT INTO dbo.PerfilPermiso (PerfilId, PermisoId) VALUES (@p, @perm)", conn_704ILR, tx_704ILR))
                        {
                            ins_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                            ins_704ILR.Parameters.Add("@perm", SqlDbType.Int).Value = permisoId_704ILR;
                            ins_704ILR.ExecuteNonQuery();
                        }
                    }

                    using (var del_704ILR = new SqlCommand("DELETE FROM dbo.PerfilIncluido WHERE PerfilPadreId = @p", conn_704ILR, tx_704ILR))
                    {
                        del_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                        del_704ILR.ExecuteNonQuery();
                    }
                    foreach (int hijoId_704ILR in perfilesIncluidos_704ILR)
                    {
                        using (var ins_704ILR = new SqlCommand(
                            "INSERT INTO dbo.PerfilIncluido (PerfilPadreId, PerfilHijoId) VALUES (@p, @h)", conn_704ILR, tx_704ILR))
                        {
                            ins_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                            ins_704ILR.Parameters.Add("@h", SqlDbType.Int).Value = hijoId_704ILR;
                            ins_704ILR.ExecuteNonQuery();
                        }
                    }

                    tx_704ILR.Commit();
                }
            }
        }

        // Ids de componentes (grupos u hojas) asignados a un perfil.
        public static HashSet<int> GetPermisoIds_704ILR(int perfilId_704ILR)
        {
            var set_704ILR = new HashSet<int>();
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT PermisoId FROM dbo.PerfilPermiso WHERE PerfilId = @p",
                cn_704ILR.OpenConnection_704ILR()))
            {
                cmd_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                using (var r_704ILR = cmd_704ILR.ExecuteReader())
                    while (r_704ILR.Read()) set_704ILR.Add(r_704ILR.GetInt32(0));
            }
            return set_704ILR;
        }

        // Reemplaza el set de permisos del perfil dentro de una transaccion.
        public static void SetPermisos_704ILR(int perfilId_704ILR, IEnumerable<int> permisoIds_704ILR)
        {
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                var conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (var tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    using (var del_704ILR = new SqlCommand("DELETE FROM dbo.PerfilPermiso WHERE PerfilId = @p", conn_704ILR, tx_704ILR))
                    {
                        del_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                        del_704ILR.ExecuteNonQuery();
                    }
                    foreach (int permisoId_704ILR in permisoIds_704ILR)
                    {
                        using (var ins_704ILR = new SqlCommand(
                            "INSERT INTO dbo.PerfilPermiso (PerfilId, PermisoId) VALUES (@p, @perm)", conn_704ILR, tx_704ILR))
                        {
                            ins_704ILR.Parameters.Add("@p", SqlDbType.Int).Value = perfilId_704ILR;
                            ins_704ILR.Parameters.Add("@perm", SqlDbType.Int).Value = permisoId_704ILR;
                            ins_704ILR.ExecuteNonQuery();
                        }
                    }
                    tx_704ILR.Commit();
                }
            }
        }
    }
}
