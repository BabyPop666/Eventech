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
    public class BE_Perfil_704ILR : BE_IComponentePermiso_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }
        public string Descripcion_704ILR { get; set; }

        public List<BE_IComponentePermiso_704ILR> ComponentesAsignados_704ILR { get; } = new List<BE_IComponentePermiso_704ILR>();
        public List<BE_Perfil_704ILR> PerfilesIncluidos_704ILR { get; } = new List<BE_Perfil_704ILR>();

        public bool EsGrupo_704ILR => true;

        public bool EsHoja_704ILR() => ComponentesAsignados_704ILR.Count == 0 && PerfilesIncluidos_704ILR.Count == 0;

        public void Asignar_704ILR(BE_IComponentePermiso_704ILR componente_704ILR)
        {
            if (componente_704ILR != null) ComponentesAsignados_704ILR.Add(componente_704ILR);
        }

        // Composicion de perfiles: el perfil pasa a contener a 'perfil' y hereda
        // sus permisos efectivos. La ausencia de ciclos la garantiza la BLL al
        // persistir; aca solo se evita la auto-inclusion directa.
        public void IncluirPerfil_704ILR(BE_Perfil_704ILR perfil_704ILR)
        {
            if (perfil_704ILR != null && perfil_704ILR.Id_704ILR != Id_704ILR) PerfilesIncluidos_704ILR.Add(perfil_704ILR);
        }

        // Operacion polimorfica del Composite: junta los permisos de los
        // componentes propios y los de los perfiles incluidos, recursivamente.
        public List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR() =>
            ObtenerPermisosEfectivos_704ILR(new HashSet<int>());

        private List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR(HashSet<int> perfilesVisitados_704ILR)
        {
            var permisos_704ILR = new List<BE_Permiso_704ILR>();
            // Cada perfil se visita una sola vez: corta referencias circulares y
            // evita reprocesar un perfil incluido por mas de un camino.
            if (!perfilesVisitados_704ILR.Add(Id_704ILR)) return permisos_704ILR;

            foreach (var c_704ILR in ComponentesAsignados_704ILR)
                permisos_704ILR.AddRange(c_704ILR.ObtenerPermisosEfectivos_704ILR());

            foreach (var incluido_704ILR in PerfilesIncluidos_704ILR)
                permisos_704ILR.AddRange(incluido_704ILR.ObtenerPermisosEfectivos_704ILR(perfilesVisitados_704ILR));

            // sin duplicados (un permiso puede llegar por varios grupos o perfiles)
            return permisos_704ILR.GroupBy(p_704ILR => p_704ILR.Id_704ILR).Select(g_704ILR => g_704ILR.First()).ToList();
        }

        public bool TienePermiso_704ILR(string clave_704ILR)
            => ObtenerPermisosEfectivos_704ILR().Any(p_704ILR => p_704ILR.Clave_704ILR == clave_704ILR);

        public override string ToString() => Nombre_704ILR;
    }
}
