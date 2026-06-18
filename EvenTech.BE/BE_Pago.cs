using System;

namespace EvenTech.BE
{
    // Pago registrado contra una reserva (Proceso 1, paso 5: cobro de adelanto/saldo).
    public class BE_Pago
    {
        public int Id { get; set; }
        public int ReservaId { get; set; }
        public int MetodoPagoId { get; set; }
        public string MetodoNombre { get; set; }   // display (JOIN MetodosPago)
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacion { get; set; }
    }
}
