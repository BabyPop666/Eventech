using System.Collections.Generic;

namespace EvenTech.BE
{
    // Clase abstracta base del Composite: concentra el estado compartido entre la
    // hoja y el compuesto (Id, Nombre, Descripcion) y declara las operaciones
    // polimorficas que cada subclase resuelve a su manera.
    public abstract class BE_ComponentePermiso_704ILR : BE_IComponentePermiso_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }
        public string Descripcion_704ILR { get; set; }

        public abstract bool EsGrupo_704ILR { get; }
        public abstract bool EsHoja_704ILR();
        public abstract List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR();
    }
}
