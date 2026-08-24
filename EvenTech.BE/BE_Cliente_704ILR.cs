using System;

namespace EvenTech.BE
{
    // Cliente del salon (Proceso 1). Una reserva referencia a un cliente por Id.
    public class BE_Cliente_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }
        public string Apellido_704ILR { get; set; }
        public string Dni_704ILR { get; set; }
        public string Email_704ILR { get; set; }
        public string Telefono_704ILR { get; set; }
        public DateTime CreatedAt_704ILR { get; set; }

        public string NombreCompleto_704ILR =>
            string.IsNullOrWhiteSpace(Apellido_704ILR) ? (Nombre_704ILR ?? string.Empty) : $"{Nombre_704ILR} {Apellido_704ILR}";

        // Se usa como display en combos.
        public override string ToString() => NombreCompleto_704ILR;
    }
}
