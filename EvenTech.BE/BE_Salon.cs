namespace EvenTech.BE
{
    // Salon donde se realiza el evento. Entidad de catalogo (lookup) usada por
    // las reservas. Se mantiene minima a proposito en esta etapa.
    public class BE_Salon
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Capacidad { get; set; }

        public override string ToString() => Nombre;
    }
}
