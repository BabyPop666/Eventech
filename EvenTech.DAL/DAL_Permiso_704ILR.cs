using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;

namespace EvenTech.DAL
{
    public static class DAL_Permiso_704ILR
    {
        // Lee la tabla reflexiva completa y reconstruye el arbol Composite en
        // memoria, devolviendo los nodos raiz (PermisoPadreId IS NULL).
        public static List<BE_IComponentePermiso_704ILR> GetArbol_704ILR()
        {
            var nodos_704ILR = new Dictionary<int, BE_IComponentePermiso_704ILR>();
            var padres_704ILR = new Dictionary<int, int?>();
            var orden_704ILR = new List<int>();

            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            using (var cmd_704ILR = new SqlCommand(
                "SELECT Id, Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId FROM dbo.Permisos ORDER BY Id",
                cn_704ILR.OpenConnection_704ILR()))
            using (var r_704ILR = cmd_704ILR.ExecuteReader())
            {
                while (r_704ILR.Read())
                {
                    int id_704ILR = r_704ILR.GetInt32(0);
                    string nombre_704ILR = r_704ILR.GetString(1);
                    string descripcion_704ILR = r_704ILR.IsDBNull(2) ? null : r_704ILR.GetString(2);
                    bool esGrupo_704ILR = r_704ILR.GetBoolean(3);
                    string clave_704ILR = r_704ILR.IsDBNull(4) ? null : r_704ILR.GetString(4);
                    int? padreId_704ILR = r_704ILR.IsDBNull(5) ? (int?)null : r_704ILR.GetInt32(5);

                    BE_IComponentePermiso_704ILR nodo_704ILR = esGrupo_704ILR
                        ? (BE_IComponentePermiso_704ILR)new BE_GrupoPermisos_704ILR { Id_704ILR = id_704ILR, Nombre_704ILR = nombre_704ILR, Descripcion_704ILR = descripcion_704ILR }
                        : new BE_Permiso_704ILR { Id_704ILR = id_704ILR, Nombre_704ILR = nombre_704ILR, Descripcion_704ILR = descripcion_704ILR, Clave_704ILR = clave_704ILR };

                    nodos_704ILR[id_704ILR] = nodo_704ILR;
                    padres_704ILR[id_704ILR] = padreId_704ILR;
                    orden_704ILR.Add(id_704ILR);
                }
            }

            // Enlazar hijos con sus padres (los padres siempre son grupos).
            var raices_704ILR = new List<BE_IComponentePermiso_704ILR>();
            foreach (int id_704ILR in orden_704ILR)
            {
                int? padreId_704ILR = padres_704ILR[id_704ILR];
                if (padreId_704ILR.HasValue && nodos_704ILR.ContainsKey(padreId_704ILR.Value) &&
                    nodos_704ILR[padreId_704ILR.Value] is BE_GrupoPermisos_704ILR grupoPadre_704ILR)
                {
                    grupoPadre_704ILR.Agregar_704ILR(nodos_704ILR[id_704ILR]);
                }
                else
                {
                    raices_704ILR.Add(nodos_704ILR[id_704ILR]);
                }
            }
            return raices_704ILR;
        }
    }
}
