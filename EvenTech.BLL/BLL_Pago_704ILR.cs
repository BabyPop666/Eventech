using System.Collections.Generic;
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

            // La reserva se consulta a traves de su propia regla de negocio.
            var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(p_704ILR.ReservaId_704ILR);
            if (reserva_704ILR == null) return PagoResult_704ILR.ReservaInvalida_704ILR;

            // Una reserva cancelada es estado terminal: tampoco admite cobros.
            // La regla vive aca (y no solo en la UI) porque los pagos persisten en
            // el acto, sin pasar por la validacion de BLL_Reserva.Actualizar.
            if (!BLL_Reserva_704ILR.PuedeModificar_704ILR(reserva_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Pago rechazado", CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{p_704ILR.ReservaId_704ILR} cancelada: no admite movimientos de cobro.");
                return PagoResult_704ILR.ReservaCancelada_704ILR;
            }

            // Tope: no se puede pagar mas que el total de la reserva.
            if (DAL_Pago_704ILR.TotalPagado_704ILR(p_704ILR.ReservaId_704ILR) + p_704ILR.Monto_704ILR > reserva_704ILR.Monto_704ILR)
                return PagoResult_704ILR.ExcedeSaldo_704ILR;

            nuevoId_704ILR = DAL_Pago_704ILR.Insert_704ILR(p_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Registro de pago", CriticidadBitacora_704ILR.Info,
                $"Pago de {p_704ILR.Monto_704ILR:0.00} en reserva #{p_704ILR.ReservaId_704ILR} (metodo #{p_704ILR.MetodoPagoId_704ILR})");
            return PagoResult_704ILR.Success_704ILR;
        }

        // Anular un pago es un movimiento de cobranza mas y pasa por las mismas reglas
        // que registrarlo: el pago tiene que existir, pertenecer a la reserva que la
        // pantalla dice, y la reserva tiene que admitir movimientos (una CANCELADA es
        // estado terminal, RN-04). Antes esto borraba la fila sin mirar nada y el
        // numero de reserva solo se usaba para armar el texto del asiento.
        public static PagoResult_704ILR Eliminar_704ILR(int pagoId_704ILR, int reservaId_704ILR)
        {
            if (pagoId_704ILR <= 0 || reservaId_704ILR <= 0) return PagoResult_704ILR.ReservaInvalida_704ILR;

            var pago_704ILR = DAL_Pago_704ILR.GetById_704ILR(pagoId_704ILR);
            if (pago_704ILR == null || pago_704ILR.ReservaId_704ILR != reservaId_704ILR)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                    $"Pago #{pagoId_704ILR} inexistente o ajeno a la reserva #{reservaId_704ILR}.");
                return PagoResult_704ILR.PagoInvalido_704ILR;
            }

            var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (reserva_704ILR == null) return PagoResult_704ILR.ReservaInvalida_704ILR;

            if (!BLL_Reserva_704ILR.PuedeModificar_704ILR(reserva_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR} cancelada: no admite movimientos de cobro (RN-04).");
                return PagoResult_704ILR.ReservaCancelada_704ILR;
            }

            DAL_Pago_704ILR.Delete_704ILR(pagoId_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion de pago", CriticidadBitacora_704ILR.Advertencia,
                $"Pago #{pagoId_704ILR} de {pago_704ILR.Monto_704ILR:0.00} en la reserva #{reservaId_704ILR} anulado");
            return PagoResult_704ILR.Success_704ILR;
        }
    }
}
