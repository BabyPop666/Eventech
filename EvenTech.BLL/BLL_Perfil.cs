using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Logica de negocio de perfiles y arbol de permisos (Composite).
    public static class BLL_Perfil
    {
        public static List<BE_IComponentePermiso> GetArbolPermisos() => DAL_Permiso.GetArbol();

        public static List<BE_Perfil> GetPerfiles() => DAL_Perfil.GetAll();

        public static HashSet<int> GetPermisosAsignados(int perfilId) => DAL_Perfil.GetPermisoIds(perfilId);

        public static void GuardarAsignaciones(int perfilId, IEnumerable<int> permisoIds)
        {
            DAL_Perfil.SetPermisos(perfilId, permisoIds);
            BLL_Bitacora.Registrar("Perfiles", "Actualizacion de permisos", CriticidadBitacora.Info,
                $"Se actualizaron los permisos del perfil #{perfilId}");
        }

        // Recorre el arbol recursivamente y devuelve los permisos efectivos (hojas)
        // que cubren los componentes asignados al perfil.
        public static List<BE_Permiso> CalcularPermisosEfectivos(
            List<BE_IComponentePermiso> arbol, HashSet<int> asignados)
        {
            var efectivos = new Dictionary<int, BE_Permiso>();
            foreach (var nodo in arbol)
                Recolectar(nodo, asignados, false, efectivos);
            return new List<BE_Permiso>(efectivos.Values);
        }

        private static void Recolectar(BE_IComponentePermiso nodo, HashSet<int> asignados,
            bool heredado, Dictionary<int, BE_Permiso> acumulado)
        {
            bool activo = heredado || asignados.Contains(nodo.Id);

            if (nodo is BE_GrupoPermisos grupo)
            {
                foreach (var hijo in grupo.Hijos)
                    Recolectar(hijo, asignados, activo, acumulado);
            }
            else if (nodo is BE_Permiso permiso && activo)
            {
                acumulado[permiso.Id] = permiso;
            }
        }
    }
}
