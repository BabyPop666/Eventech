using System.Collections.Generic;

namespace EvenTech.BE
{
    // Compuesto del patron Composite: agrupa otros componentes (hojas u otros
    // grupos), permitiendo anidar grupos dentro de grupos. La recursividad del
    // patron aparece en ObtenerPermisosEfectivos.
    public class BE_GrupoPermisos_704ILR : BE_ComponentePermiso_704ILR
    {
        public List<BE_IComponentePermiso_704ILR> Hijos_704ILR { get; } = new List<BE_IComponentePermiso_704ILR>();

        public override bool EsGrupo_704ILR => true;

        public override bool EsHoja_704ILR() => Hijos_704ILR.Count == 0;

        public void Agregar_704ILR(BE_IComponentePermiso_704ILR componente_704ILR)
        {
            if (componente_704ILR != null) Hijos_704ILR.Add(componente_704ILR);
        }

        // Cada nodo delega la misma operacion en sus hijos (recursion del Composite).
        public override List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR()
        {
            var permisos_704ILR = new List<BE_Permiso_704ILR>();
            foreach (var hijo_704ILR in Hijos_704ILR)
                permisos_704ILR.AddRange(hijo_704ILR.ObtenerPermisosEfectivos_704ILR());
            return permisos_704ILR;
        }
    }
}
