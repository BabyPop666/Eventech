using System.Collections.Generic;

namespace EvenTech.BE
{
    // Clase abstracta base del Composite: concentra el estado compartido entre la
    // hoja y el compuesto (Id, Nombre, Descripcion) y declara las operaciones
    // polimorficas que cada subclase resuelve a su manera.
    public abstract class BE_ComponentePermiso : BE_IComponentePermiso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public abstract bool EsGrupo { get; }
        public abstract bool EsHoja();
        public abstract List<BE_Permiso> ObtenerPermisosEfectivos();
    }
}
