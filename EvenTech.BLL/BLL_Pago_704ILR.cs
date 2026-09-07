using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum PagoResult_704ILR
    {
        Success_704ILR,
        MontoInvalido_704ILR,
        MetodoInvalido_704ILR,
        ExcedeSaldo_704ILR,
        ReservaInvalida_704ILR,
        ReservaCancelada_704ILR,  // estado terminal: no admite movimientos de cobro
        PagoInvalido_704ILR       // el pago a anular no existe o es de otra reserva
    }

    // Reglas de negocio de pagos (Proceso 1, paso 5): cobro de adelanto/saldo de
    // una reserva. El total de la reserva (Monto = suma de servicios) actua como
    // tope: la suma de pagos nunca puede superarlo.
    public static class BLL_Pago_704ILR
    {
        public static List<BE_MetodoPago_704ILR> GetMetodos_704ILR() => DAL_MetodoPago_704ILR.GetAll_704ILR();

        public static List<BE_Pago_704ILR> GetByReserva_704ILR(int reservaId_704ILR) => DAL_Pago_704ILR.GetByReserva_704ILR(reservaId_704ILR);

        public static decimal TotalPagado_704ILR(int reservaId_704ILR) => DAL_Pago_704ILR.TotalPagado_704ILR(reservaId_704ILR);

        public static decimal MontoReserva_704ILR(int reservaId_704ILR)
        {
            var r_704ILR = BLL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            return r_704ILR == null ? 0m : r_704ILR.Monto_704ILR;
        }

        // Saldo pendiente = total de la reserva - lo ya pagado.
        public static decimal Saldo_704ILR(int reservaId_704ILR) => MontoReserva_704ILR(reservaId_704ILR) - TotalPagado_704ILR(reservaId_704ILR);

        public static PagoResult_704ILR Registrar_704ILR(BE_Pago_704ILR p_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            if (p_704ILR == null || p_704ILR.ReservaId_704ILR <= 0) return PagoResult_704ILR.ReservaInvalida_704ILR;
            if (p_704ILR.MetodoPagoId_704ILR <= 0) return PagoResult_704ILR.MetodoInvalido_704ILR;
            if (p_704ILR.Monto_704ILR <= 0) return PagoResult_704ILR.MontoInvalido_704ILR;

            // Validacion y alta en UNA transaccion: la cabecera de la reserva se lee
            // con bloqueo de actualizacion y el total cobrado se relee adentro, asi
            // dos cobros simultaneos sobre la misma reserva se serializan y el
            // segundo valida contra lo que el primero ya dejo escrito. Antes cada
            // paso abria su propia conexion y los dos podian pasar el tope RN-04.
            PagoResult_704ILR resultado_704ILR;
            decimal montoReserva_704ILR = 0m, pagado_704ILR = 0m;
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                SqlConnection conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (SqlTransaction tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    var reserva_704ILR = DAL_Reserva_704ILR.GetById_704ILR(p_704ILR.ReservaId_704ILR, conn_704ILR, tx_704ILR);
                    if (reserva_704ILR == null)
                        resultado_704ILR = PagoResult_704ILR.ReservaInvalida_704ILR;
                    // Una reserva cancelada es estado terminal: tampoco admite cobros.
                    // La regla vive aca (y no solo en la UI) porque los pagos persisten en
                    // el acto, sin pasar por la validacion de BLL_Reserva.Actualizar.
                    else if (!BLL_Reserva_704ILR.PuedeModificar_704ILR(reserva_704ILR))
                        resultado_704ILR = PagoResult_704ILR.ReservaCancelada_704ILR;
                    else
                    {
                        // Tope: no se puede pagar mas que el total de la reserva (RN-04).
                        montoReserva_704ILR = reserva_704ILR.Monto_704ILR;
                        pagado_704ILR = DAL_Pago_704ILR.TotalPagado_704ILR(p_704ILR.ReservaId_704ILR, conn_704ILR, tx_704ILR);
                        if (pagado_704ILR + p_704ILR.Monto_704ILR > montoReserva_704ILR)
                            resultado_704ILR = PagoResult_704ILR.ExcedeSaldo_704ILR;
                        else
                        {
                            nuevoId_704ILR = DAL_Pago_704ILR.Insert_704ILR(p_704ILR, conn_704ILR, tx_704ILR);
                            tx_704ILR.Commit();
                            resultado_704ILR = PagoResult_704ILR.Success_704ILR;
                        }
                    }
                    // Si no hubo Commit, cerrar la transaccion la deshace y libera el bloqueo.
                }
            }

            // Los asientos van despues de cerrar la transaccion, como en el resto de
            // las operaciones: la bitacora no participa del bloqueo. El rechazo por
            // tope se asienta —es una regla de negocio, no un error de tipeo— con el
            // mismo formato de Advertencia que usan los demas rechazos de cobro.
            switch (resultado_704ILR)
            {
                case PagoResult_704ILR.ReservaCancelada_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Pago rechazado", CriticidadBitacora_704ILR.Advertencia,
                        $"Reserva #{p_704ILR.ReservaId_704ILR} cancelada: no admite movimientos de cobro.");
                    break;
                case PagoResult_704ILR.ExcedeSaldo_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Pago rechazado", CriticidadBitacora_704ILR.Advertencia,
                        $"Reserva #{p_704ILR.ReservaId_704ILR}: un cobro de {p_704ILR.Monto_704ILR:0.00} " +
                        $"supera el saldo pendiente ({montoReserva_704ILR - pagado_704ILR:0.00}) (RN-04).");
                    break;
                case PagoResult_704ILR.Success_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Registro de pago", CriticidadBitacora_704ILR.Info,
                        $"Pago de {p_704ILR.Monto_704ILR:0.00} en reserva #{p_704ILR.ReservaId_704ILR} (metodo #{p_704ILR.MetodoPagoId_704ILR})");
                    break;
            }
            return resultado_704ILR;
        }

        // Anular un pago es un movimiento de cobranza mas y pasa por las mismas reglas
        // que registrarlo: el pago tiene que existir, pertenecer a la reserva que la
        // pantalla dice, y la reserva tiene que admitir movimientos (una CANCELADA es
        // estado terminal, RN-04). Antes esto borraba la fila sin mirar nada y el
        // numero de reserva solo se usaba para armar el texto del asiento.
        public static PagoResult_704ILR Eliminar_704ILR(int pagoId_704ILR, int reservaId_704ILR)
        {
            if (pagoId_704ILR <= 0 || reservaId_704ILR <= 0) return PagoResult_704ILR.ReservaInvalida_704ILR;

            // Misma transaccion que el cobro (ver Registrar_704ILR): la cabecera se
            // lee bloqueada y el pago se valida y se borra sin que otro movimiento
            // pueda meterse en el medio.
            PagoResult_704ILR resultado_704ILR;
            decimal montoPago_704ILR = 0m;
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                SqlConnection conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (SqlTransaction tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    var pago_704ILR = DAL_Pago_704ILR.GetById_704ILR(pagoId_704ILR, conn_704ILR, tx_704ILR);
                    if (pago_704ILR == null || pago_704ILR.ReservaId_704ILR != reservaId_704ILR)
                        resultado_704ILR = PagoResult_704ILR.PagoInvalido_704ILR;
                    else
                    {
                        montoPago_704ILR = pago_704ILR.Monto_704ILR;
                        var reserva_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR, conn_704ILR, tx_704ILR);
                        if (reserva_704ILR == null)
                            resultado_704ILR = PagoResult_704ILR.ReservaInvalida_704ILR;
                        else if (!BLL_Reserva_704ILR.PuedeModificar_704ILR(reserva_704ILR))
                            resultado_704ILR = PagoResult_704ILR.ReservaCancelada_704ILR;
                        else
                        {
                            DAL_Pago_704ILR.Delete_704ILR(pagoId_704ILR, conn_704ILR, tx_704ILR);
                            tx_704ILR.Commit();
                            resultado_704ILR = PagoResult_704ILR.Success_704ILR;
                        }
                    }
                }
            }

            switch (resultado_704ILR)
            {
                case PagoResult_704ILR.PagoInvalido_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                        $"Pago #{pagoId_704ILR} inexistente o ajeno a la reserva #{reservaId_704ILR}.");
                    break;
                case PagoResult_704ILR.ReservaCancelada_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                        $"Reserva #{reservaId_704ILR} cancelada: no admite movimientos de cobro (RN-04).");
                    break;
                case PagoResult_704ILR.Success_704ILR:
                    BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion de pago", CriticidadBitacora_704ILR.Advertencia,
                        $"Pago #{pagoId_704ILR} de {montoPago_704ILR:0.00} en la reserva #{reservaId_704ILR} anulado");
                    break;
            }
            return resultado_704ILR;
        }
    }
}
