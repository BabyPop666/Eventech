using System;

namespace EvenTech.BE
{
    // Pago registrado contra una reserva (Proceso 1, paso 5: cobro de adelanto/saldo).
    public class BE_Pago_704ILR
    {
        public int Id_704ILR { get; set; }
        public int ReservaId_704ILR { get; set; }
        public int MetodoPagoId_704ILR { get; set; }
        public string MetodoNombre_704ILR { get; set; }   // display (JOIN MetodosPago)
        public decimal Monto_704ILR { get; set; }
        public DateTime Fecha_704ILR { get; set; }
        public string Observacion_704ILR { get; set; }
    }
}
