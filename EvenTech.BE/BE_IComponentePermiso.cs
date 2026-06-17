using System.Collections.Generic;

namespace EvenTech.BE
{
    // Interfaz comun del patron Composite para el arbol de permisos. Tanto la
    // hoja (BE_Permiso) como el compuesto (BE_GrupoPermisos) la implementan, lo
    // que permite tratarlos de forma uniforme.
    public interface BE_IComponentePermiso
    {
        int Id { get; }
        string Nombre { get; }
        bool EsGrupo { get; }

        // Operacion polimorfica clave del patron: una hoja se devuelve a si misma;
        // un grupo delega recursivamente en sus hijos.
        List<BE_Permiso> ObtenerPermisosEfectivos();

        bool EsHoja();
    }
}
