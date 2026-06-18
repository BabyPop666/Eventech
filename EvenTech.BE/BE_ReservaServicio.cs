namespace EvenTech.BE
{
    // Servicio contratado dentro de una reserva (linea de la M:N). El precio se
    // congela al momento de contratar (PrecioUnitario), independiente del catalogo.
    public class BE_ReservaServicio
    {
        public int Id { get; set; }
        public int ReservaId { get; set; }
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; }   // proyectado en lecturas (JOIN)
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
