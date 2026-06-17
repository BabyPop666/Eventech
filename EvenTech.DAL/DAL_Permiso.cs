using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Permiso
    {
        // Lee la tabla reflexiva completa y reconstruye el arbol Composite en
        // memoria, devolviendo los nodos raiz (PermisoPadreId IS NULL).
        public static List<BE_IComponentePermiso> GetArbol()
        {
            var nodos = new Dictionary<int, BE_IComponentePermiso>();
            var padres = new Dictionary<int, int?>();
            var orden = new List<int>();

            using (var cn = new DAL_DB_Connection())
            using (var cmd = new SqlCommand(
                "SELECT Id, Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId FROM dbo.Permisos ORDER BY Id",
                cn.OpenConnection()))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int id = r.GetInt32(0);
                    string nombre = r.GetString(1);
                    string descripcion = r.IsDBNull(2) ? null : r.GetString(2);
                    bool esGrupo = r.GetBoolean(3);
                    string clave = r.IsDBNull(4) ? null : r.GetString(4);
                    int? padreId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5);

                    BE_IComponentePermiso nodo = esGrupo
                        ? (BE_IComponentePermiso)new BE_GrupoPermisos { Id = id, Nombre = nombre, Descripcion = descripcion }
                        : new BE_Permiso { Id = id, Nombre = nombre, Descripcion = descripcion, Clave = clave };

                    nodos[id] = nodo;
                    padres[id] = padreId;
                    orden.Add(id);
                }
            }

            // Enlazar hijos con sus padres (los padres siempre son grupos).
            var raices = new List<BE_IComponentePermiso>();
            foreach (int id in orden)
            {
                int? padreId = padres[id];
                if (padreId.HasValue && nodos.ContainsKey(padreId.Value) &&
                    nodos[padreId.Value] is BE_GrupoPermisos grupoPadre)
                {
                    grupoPadre.Agregar(nodos[id]);
                }
                else
                {
                    raices.Add(nodos[id]);
                }
            }
            return raices;
        }
    }
}
