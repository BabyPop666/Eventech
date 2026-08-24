using System.Collections.Generic;

namespace EvenTech.BE
{
    // Hoja del patron Composite: un permiso concreto, identificado por su Clave
    // (ej. "RESERVA_CREAR"). No tiene hijos.
    public class BE_Permiso_704ILR : BE_ComponentePermiso_704ILR
    {
        public string Clave_704ILR { get; set; }

        public override bool EsGrupo_704ILR => false;

        public override bool EsHoja_704ILR() => true;

        // Una hoja se devuelve a si misma como unico permiso efectivo.
        public override List<BE_Permiso_704ILR> ObtenerPermisosEfectivos_704ILR() => new List<BE_Permiso_704ILR> { this };
    }
}
