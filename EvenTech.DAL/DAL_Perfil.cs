using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Perfil
    {
        public static bool ExistsNombre(string nombre)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Perfiles WHERE Nombre = @n", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre ?? string.Empty;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static int Insert(string nombre, string descripcion)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Perfiles (Nombre, Descripcion) VALUES (@n, @d); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 80).Value = nombre;
                cmd.Parameters.Add("@d", SqlDbType.NVarChar, 250).Value = (object)descripcion ?? DBNull.Value;
                return (int)cmd.ExecuteScalar();
            }
        }

        public static List<BE_Perfil> GetAll()
        {
            var list = new List<BE_Perfil>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Nombre, Descripcion FROM dbo.Perfiles ORDER BY Nombre",
                cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new BE_Perfil
                    {
                        Id = r.GetInt32(0),
                        Nombre = r.GetString(1),
                        Descripcion = r.IsDBNull(2) ? null : r.GetString(2)
                    });
                }
            }
            return list;
        }

        public static BE_Perfil GetById(int id)
        {
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Nombre, Descripcion FROM dbo.Perfiles WHERE Id = @id", cn.OpenConnection()))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new BE_Perfil
                    {
                        Id = r.GetInt32(0),
                        Nombre = r.GetString(1),
                        Descripcion = r.IsDBNull(2) ? null : r.GetString(2)
                    };
                }
            }
        }

        // Ids de los perfiles incluidos dentro de un perfil (Composite de perfiles).
        public static HashSet<int> GetIncluidos(int perfilId)
        {
            var set = new HashSet<int>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT PerfilHijoId FROM dbo.PerfilIncluido WHERE PerfilPadreId = @p",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) set.Add(r.GetInt32(0));
            }
            return set;
        }

        // Grafo completo de inclusiones (padre -> hijos). Lo usa la BLL para
        // detectar ciclos antes de persistir una composicion.
        public static Dictionary<int, List<int>> GetTodasLasInclusiones()
        {
            var grafo = new Dictionary<int, List<int>>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT PerfilPadreId, PerfilHijoId FROM dbo.PerfilIncluido", cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int padre = r.GetInt32(0);
                    if (!grafo.TryGetValue(padre, out var hijos))
                        grafo[padre] = hijos = new List<int>();
                    hijos.Add(r.GetInt32(1));
                }
            }
            return grafo;
        }

        // Reemplaza la composicion completa del perfil (permisos + perfiles
        // incluidos) en una unica transaccion.
        public static void SetComposicion(int perfilId, IEnumerable<int> permisoIds, IEnumerable<int> perfilesIncluidos)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = new SqlCommand("DELETE FROM dbo.PerfilPermiso WHERE PerfilId = @p", conn, tx))
                    {
                        del.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                        del.ExecuteNonQuery();
                    }
                    foreach (int permisoId in permisoIds)
                    {
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.PerfilPermiso (PerfilId, PermisoId) VALUES (@p, @perm)", conn, tx))
                        {
                            ins.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                            ins.Parameters.Add("@perm", SqlDbType.Int).Value = permisoId;
                            ins.ExecuteNonQuery();
                        }
                    }

                    using (var del = new SqlCommand("DELETE FROM dbo.PerfilIncluido WHERE PerfilPadreId = @p", conn, tx))
                    {
                        del.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                        del.ExecuteNonQuery();
                    }
                    foreach (int hijoId in perfilesIncluidos)
                    {
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.PerfilIncluido (PerfilPadreId, PerfilHijoId) VALUES (@p, @h)", conn, tx))
                        {
                            ins.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                            ins.Parameters.Add("@h", SqlDbType.Int).Value = hijoId;
                            ins.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }

        // Ids de componentes (grupos u hojas) asignados a un perfil.
        public static HashSet<int> GetPermisoIds(int perfilId)
        {
            var set = new HashSet<int>();
            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT PermisoId FROM dbo.PerfilPermiso WHERE PerfilId = @p",
                cn.OpenConnection()))
            {
                cmd.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) set.Add(r.GetInt32(0));
            }
            return set;
        }

        // Reemplaza el set de permisos del perfil dentro de una transaccion.
        public static void SetPermisos(int perfilId, IEnumerable<int> permisoIds)
        {
            using (var cn = new DAL_DB_Connection())
            {
                var conn = cn.OpenConnection();
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = new SqlCommand("DELETE FROM dbo.PerfilPermiso WHERE PerfilId = @p", conn, tx))
                    {
                        del.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                        del.ExecuteNonQuery();
                    }
                    foreach (int permisoId in permisoIds)
                    {
                        using (var ins = new SqlCommand(
                            "INSERT INTO dbo.PerfilPermiso (PerfilId, PermisoId) VALUES (@p, @perm)", conn, tx))
                        {
                            ins.Parameters.Add("@p", SqlDbType.Int).Value = perfilId;
                            ins.Parameters.Add("@perm", SqlDbType.Int).Value = permisoId;
                            ins.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }
    }
}
