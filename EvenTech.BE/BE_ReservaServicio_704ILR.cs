namespace EvenTech.BE
{
    // Servicio contratado dentro de una reserva (linea de la M:N). El precio se
    // congela al momento de contratar (PrecioUnitario), independiente del catalogo.
    public class BE_ReservaServicio_704ILR
    {
        public int Id_704ILR { get; set; }
        public int ReservaId_704ILR { get; set; }
        public int ServicioId_704ILR { get; set; }
        public string ServicioNombre_704ILR { get; set; }   // proyectado en lecturas (JOIN)
        public int Cantidad_704ILR { get; set; } = 1;
        public decimal PrecioUnitario_704ILR { get; set; }

        public decimal Subtotal_704ILR => Cantidad_704ILR * PrecioUnitario_704ILR;
    }
}
