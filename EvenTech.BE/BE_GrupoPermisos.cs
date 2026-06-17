using System.Collections.Generic;

namespace EvenTech.BE
{
    // Compuesto del patron Composite: agrupa otros componentes (hojas u otros
    // grupos), permitiendo anidar grupos dentro de grupos. La recursividad del
    // patron aparece en ObtenerPermisosEfectivos.
    public class BE_GrupoPermisos : BE_ComponentePermiso
    {
        public List<BE_IComponentePermiso> Hijos { get; } = new List<BE_IComponentePermiso>();

        public override bool EsGrupo => true;

        public override bool EsHoja() => Hijos.Count == 0;

        public void Agregar(BE_IComponentePermiso componente)
        {
            if (componente != null) Hijos.Add(componente);
        }

        // Cada nodo delega la misma operacion en sus hijos (recursion del Composite).
        public override List<BE_Permiso> ObtenerPermisosEfectivos()
        {
            var permisos = new List<BE_Permiso>();
            foreach (var hijo in Hijos)
                permisos.AddRange(hijo.ObtenerPermisosEfectivos());
            return permisos;
        }
    }
}
