using System.Collections.Generic;

namespace EvenTech.BE
{
    // Hoja del patron Composite: un permiso concreto, identificado por su Clave
    // (ej. "RESERVA_CREAR"). No tiene hijos.
    public class BE_Permiso : BE_ComponentePermiso
    {
        public string Clave { get; set; }

        public override bool EsGrupo => false;

        public override bool EsHoja() => true;

        // Una hoja se devuelve a si misma como unico permiso efectivo.
        public override List<BE_Permiso> ObtenerPermisosEfectivos() => new List<BE_Permiso> { this };
    }
}
