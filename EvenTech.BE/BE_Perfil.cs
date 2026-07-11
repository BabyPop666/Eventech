using System.Collections.Generic;
using System.Linq;

namespace EvenTech.BE
{
    // Perfil de usuario. Participa del patron Composite en dos niveles:
    //  - agrega componentes del arbol de permisos (grupos u hojas), y
    //  - puede INCLUIR otros perfiles: un perfil compuesto hereda los permisos
    //    de los que contiene (p.ej. Gerencial contiene a Vendedor).
    // Implementa BE_IComponentePermiso, de modo que un perfil se trata en forma
    // uniforme con cualquier otro nodo del arbol de permisos.
    public class BE_Perfil : BE_IComponentePermiso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public List<BE_IComponentePermiso> ComponentesAsignados { get; } = new List<BE_IComponentePermiso>();
        public List<BE_Perfil> PerfilesIncluidos { get; } = new List<BE_Perfil>();

        public bool EsGrupo => true;

        public bool EsHoja() => ComponentesAsignados.Count == 0 && PerfilesIncluidos.Count == 0;

        public void Asignar(BE_IComponentePermiso componente)
        {
            if (componente != null) ComponentesAsignados.Add(componente);
        }

        // Composicion de perfiles: el perfil pasa a contener a 'perfil' y hereda
        // sus permisos efectivos. La ausencia de ciclos la garantiza la BLL al
        // persistir; aca solo se evita la auto-inclusion directa.
        public void IncluirPerfil(BE_Perfil perfil)
        {
            if (perfil != null && perfil.Id != Id) PerfilesIncluidos.Add(perfil);
        }

        // Operacion polimorfica del Composite: junta los permisos de los
        // componentes propios y los de los perfiles incluidos, recursivamente.
        public List<BE_Permiso> ObtenerPermisosEfectivos() =>
            ObtenerPermisosEfectivos(new HashSet<int>());

        private List<BE_Permiso> ObtenerPermisosEfectivos(HashSet<int> perfilesVisitados)
        {
            var permisos = new List<BE_Permiso>();
            // Cada perfil se visita una sola vez: corta referencias circulares y
            // evita reprocesar un perfil incluido por mas de un camino.
            if (!perfilesVisitados.Add(Id)) return permisos;

            foreach (var c in ComponentesAsignados)
                permisos.AddRange(c.ObtenerPermisosEfectivos());

            foreach (var incluido in PerfilesIncluidos)
                permisos.AddRange(incluido.ObtenerPermisosEfectivos(perfilesVisitados));

            // sin duplicados (un permiso puede llegar por varios grupos o perfiles)
            return permisos.GroupBy(p => p.Id).Select(g => g.First()).ToList();
        }

        public bool TienePermiso(string clave)
            => ObtenerPermisosEfectivos().Any(p => p.Clave == clave);

        public override string ToString() => Nombre;
    }
}
