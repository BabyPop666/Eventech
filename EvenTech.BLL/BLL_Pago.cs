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
        EstadoNoPermitido   // la reserva esta CANCELADA o es solo una COTIZACION
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

            // Solo se cobra sobre reservas reales: una cotizacion todavia no es una
            // venta y una cancelada no debe recibir pagos.
            if (reserva.Estado == EstadoReserva.CANCELADA || reserva.Estado == EstadoReserva.COTIZACION)
                return PagoResult.EstadoNoPermitido;

            // Tope atomico: el chequeo (suma de pagos + monto <= total) y la insercion
            // corren en una unica transaccion serializable, sin ventana de carrera.
            nuevoId = DAL_Pago.InsertConTope(p, reserva.Monto);
            if (nuevoId < 0)
            {
                nuevoId = 0;
                return PagoResult.ExcedeSaldo;
            }

            BLL_Bitacora.Registrar("Pagos", "Registro de pago", CriticidadBitacora.Info,
                $"Pago de {p.Monto:0.00} en reserva #{p.ReservaId} (metodo #{p.MetodoPagoId})");
            return PagoResult.Success;
        }

        // Anula un pago verificando que exista y pertenezca a la reserva. Solo
        // registra en bitacora si efectivamente se borro algo (evita anotar
        // "anulaciones" fantasma por doble clic o grillas desactualizadas).
        public static bool Eliminar(int pagoId, int reservaId)
        {
            int filas = DAL_Pago.Delete(pagoId, reservaId);
            if (filas > 0)
                BLL_Bitacora.Registrar("Pagos", "Anulacion de pago", CriticidadBitacora.Advertencia,
                    $"Pago #{pagoId} de la reserva #{reservaId} anulado");
            return filas > 0;
        }
    }
}
