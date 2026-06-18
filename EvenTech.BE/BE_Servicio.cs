using System;

namespace EvenTech.BE
{
    // Servicio/producto del catalogo que puede contratarse en una reserva (Proceso 1).
    public class BE_Servicio
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public override string ToString() => Nombre;
    }
}
