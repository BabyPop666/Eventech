namespace EvenTech.BE
{
    // Metodo de pago (catalogo: Efectivo, Tarjeta, Transferencia, MercadoPago...).
    public class BE_MetodoPago
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public override string ToString() => Nombre;
    }
}
