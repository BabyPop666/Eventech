namespace EvenTech.BE
{
    // Salon donde se realiza el evento. Entidad de catalogo (lookup) usada por
    // las reservas. Se mantiene minima a proposito en esta etapa.
    public class BE_Salon_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }
        public int Capacidad_704ILR { get; set; }

        public override string ToString() => Nombre_704ILR;
    }
}
