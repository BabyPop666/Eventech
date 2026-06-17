using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Perfil
    {
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
