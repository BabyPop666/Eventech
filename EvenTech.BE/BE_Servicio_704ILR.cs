using System;

namespace EvenTech.BE
{
    // Servicio/producto del catalogo que puede contratarse en una reserva (Proceso 1).
    public class BE_Servicio_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }
        public string Descripcion_704ILR { get; set; }
        public decimal Precio_704ILR { get; set; }
        public bool Activo_704ILR { get; set; } = true;
        public DateTime CreatedAt_704ILR { get; set; }

        public override string ToString() => Nombre_704ILR;
    }
}
