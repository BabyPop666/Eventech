namespace EvenTech.BE
{
    // Metodo de pago (catalogo: Efectivo, Tarjeta, Transferencia, MercadoPago...).
    public class BE_MetodoPago_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Nombre_704ILR { get; set; }

        public override string ToString() => Nombre_704ILR;
    }
}
