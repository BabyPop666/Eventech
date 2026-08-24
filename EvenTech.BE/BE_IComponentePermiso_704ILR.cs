using System.Collections.Generic;

namespace EvenTech.BE
{
    // Interfaz comun del patron Composite para el arbol de permisos. Tanto la
    // hoja (BE_Permiso) como el compuesto (BE_GrupoPermisos) la implementan, lo
    // que permite tratarlos de forma uniforme.
    public interface BE_IComponentePermiso_704ILR
    {
        int Id_704ILR { get; }
        string Nombre_704ILR { get; }
        bool EsGrupo_704ILR { get; }

        // Operacion polimorfica clave del patron: una hoja se devuelve a si misma;
        // un grupo delega recursivamente en sus hijos.
        List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR();

        bool EsHoja_704ILR();
    }
}
