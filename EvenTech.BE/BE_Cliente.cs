using System;

namespace EvenTech.BE
{
    // Cliente del salon (Proceso 1). Una reserva referencia a un cliente por Id.
    public class BE_Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public DateTime CreatedAt { get; set; }

        public string NombreCompleto =>
            string.IsNullOrWhiteSpace(Apellido) ? (Nombre ?? string.Empty) : $"{Nombre} {Apellido}";

        // Se usa como display en combos.
        public override string ToString() => NombreCompleto;
    }
}
