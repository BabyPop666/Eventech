using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum PagoResult_704ILR
    {
        Success,
        MontoInvalido,
        MetodoInvalido,
        ExcedeSaldo,
        ReservaInvalida,
        ReservaCancelada   // estado terminal: no admite movimientos de cobro
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
            if (p_704ILR == null || p_704ILR.ReservaId_704ILR <= 0) return PagoResult_704ILR.ReservaInvalida;
            if (p_704ILR.MetodoPagoId_704ILR <= 0) return PagoResult_704ILR.MetodoInvalido;
            if (p_704ILR.Monto_704ILR <= 0) return PagoResult_704ILR.MontoInvalido;

            // La reserva se consulta a traves de su propia regla de negocio.
            var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(p_704ILR.ReservaId_704ILR);
            if (reserva_704ILR == null) return PagoResult_704ILR.ReservaInvalida;

            // Una reserva cancelada es estado terminal: tampoco admite cobros.
            // La regla vive aca (y no solo en la UI) porque los pagos persisten en
            // el acto, sin pasar por la validacion de BLL_Reserva.Actualizar.
            if (!BLL_Reserva_704ILR.PuedeModificar_704ILR(reserva_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Pago rechazado", CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{p_704ILR.ReservaId_704ILR} cancelada: no admite movimientos de cobro.");
                return PagoResult_704ILR.ReservaCancelada;
            }

            // Tope: no se puede pagar mas que el total de la reserva.
            if (DAL_Pago_704ILR.TotalPagado_704ILR(p_704ILR.ReservaId_704ILR) + p_704ILR.Monto_704ILR > reserva_704ILR.Monto_704ILR)
                return PagoResult_704ILR.ExcedeSaldo;

            nuevoId_704ILR = DAL_Pago_704ILR.Insert_704ILR(p_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Registro de pago", CriticidadBitacora_704ILR.Info,
                $"Pago de {p_704ILR.Monto_704ILR:0.00} en reserva #{p_704ILR.ReservaId_704ILR} (metodo #{p_704ILR.MetodoPagoId_704ILR})");
            return PagoResult_704ILR.Success;
        }

        public static void Eliminar_704ILR(int pagoId_704ILR, int reservaId_704ILR)
        {
            DAL_Pago_704ILR.Delete_704ILR(pagoId_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Pagos", "Anulacion de pago", CriticidadBitacora_704ILR.Advertencia,
                $"Pago #{pagoId_704ILR} de la reserva #{reservaId_704ILR} anulado");
        }
    }
}
