using System.Collections.Generic;
using System.Linq;

namespace EvenTech.BE
{
    // Perfil de usuario: agrega los componentes (grupos u hojas) que tiene
    // asignados. Reusa la operacion polimorfica del Composite para resolver el
    // conjunto efectivo de permisos sin importar la profundidad del arbol.
    public class BE_Perfil
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public List<BE_IComponentePermiso> ComponentesAsignados { get; } = new List<BE_IComponentePermiso>();

        public void Asignar(BE_IComponentePermiso componente)
        {
            if (componente != null) ComponentesAsignados.Add(componente);
        }

        public List<BE_Permiso> ObtenerPermisosEfectivos()
        {
            var permisos = new List<BE_Permiso>();
            foreach (var c in ComponentesAsignados)
                permisos.AddRange(c.ObtenerPermisosEfectivos());
            // sin duplicados (un permiso puede llegar por varios grupos)
            return permisos.GroupBy(p => p.Id).Select(g => g.First()).ToList();
        }

        public bool TienePermiso(string clave)
            => ObtenerPermisosEfectivos().Any(p => p.Clave == clave);

        public override string ToString() => Nombre;
    }
}
