using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum PagoResult
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
    public static class BLL_Pago
    {
        public static List<BE_MetodoPago> GetMetodos() => DAL_MetodoPago.GetAll();

        public static List<BE_Pago> GetByReserva(int reservaId) => DAL_Pago.GetByReserva(reservaId);

        public static decimal TotalPagado(int reservaId) => DAL_Pago.TotalPagado(reservaId);

        public static decimal MontoReserva(int reservaId)
        {
            var r = DAL_Reserva.GetById(reservaId);
            return r == null ? 0m : r.Monto;
        }

        // Saldo pendiente = total de la reserva - lo ya pagado.
        public static decimal Saldo(int reservaId) => MontoReserva(reservaId) - TotalPagado(reservaId);

        public static PagoResult Registrar(BE_Pago p, out int nuevoId)
        {
            nuevoId = 0;
            if (p == null || p.ReservaId <= 0) return PagoResult.ReservaInvalida;
            if (p.MetodoPagoId <= 0) return PagoResult.MetodoInvalido;
            if (p.Monto <= 0) return PagoResult.MontoInvalido;

            var reserva = DAL_Reserva.GetById(p.ReservaId);
            if (reserva == null) return PagoResult.ReservaInvalida;

            // Una reserva cancelada es estado terminal: tampoco admite cobros.
            // La regla vive aca (y no solo en la UI) porque los pagos persisten en
            // el acto, sin pasar por la validacion de BLL_Reserva.Actualizar.
            if (!BLL_Reserva.PuedeModificar(reserva))
            {
                BLL_Bitacora.Registrar("Pagos", "Pago rechazado", CriticidadBitacora.Advertencia,
                    $"Reserva #{p.ReservaId} cancelada: no admite movimientos de cobro.");
                return PagoResult.ReservaCancelada;
            }

            // Tope: no se puede pagar mas que el total de la reserva.
            if (DAL_Pago.TotalPagado(p.ReservaId) + p.Monto > reserva.Monto)
                return PagoResult.ExcedeSaldo;

            nuevoId = DAL_Pago.Insert(p);
            BLL_Bitacora.Registrar("Pagos", "Registro de pago", CriticidadBitacora.Info,
                $"Pago de {p.Monto:0.00} en reserva #{p.ReservaId} (metodo #{p.MetodoPagoId})");
            return PagoResult.Success;
        }

        public static void Eliminar(int pagoId, int reservaId)
        {
            DAL_Pago.Delete(pagoId);
            BLL_Bitacora.Registrar("Pagos", "Anulacion de pago", CriticidadBitacora.Advertencia,
                $"Pago #{pagoId} de la reserva #{reservaId} anulado");
        }
    }
}
