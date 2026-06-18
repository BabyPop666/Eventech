using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum ReservaResult
    {
        Success,
        InvalidCliente,
        InvalidSalon,
        InvalidFecha,
        InvalidMonto,
        SalonOcupado,   // ya hay otra reserva activa para ese salon y fecha
        NotFound
    }

    // Reglas de negocio de reservas: validacion de datos y estados antes de
    // delegar al DAL. Las validaciones viven aca (no en la UI ni en el DAL)
    // para mantener cohesion y permitir reuso desde otros frentes.
    public static class BLL_Reserva
    {
        public static List<BE_Reserva> GetAll() => DAL_Reserva.GetAll();

        public static BE_Reserva GetById(int id) => DAL_Reserva.GetById(id);

        // Campos auditados por el control de cambios (T06b).
        private static readonly string[] CamposAuditados =
            { "ClienteId", "SalonId", "FechaEvento", "Estado", "Monto" };

        public static ReservaResult Crear(BE_Reserva reserva, out int nuevoId)
        {
            nuevoId = 0;
            var validacion = Validar(reserva);
            if (validacion != ReservaResult.Success) return validacion;

            // DV horizontal: se calcula sobre los campos de negocio antes de persistir.
            reserva.Dvh = ValidadorDeIntegridad.CalcularDVH(reserva);
            nuevoId = DAL_Reserva.Insert(reserva);
            BLL_Integridad.RecalcularDVVerticalReservas();

            BLL_Bitacora.Registrar("Reservas", "Alta de reserva", CriticidadBitacora.Info,
                $"Reserva #{nuevoId} - cliente #{reserva.ClienteId}, monto {reserva.Monto:0.00}");
            return ReservaResult.Success;
        }

        public static ReservaResult Actualizar(BE_Reserva reserva)
        {
            BE_Reserva antes = reserva.Id > 0 ? DAL_Reserva.GetById(reserva.Id) : null;
            if (antes == null) return ReservaResult.NotFound;

            var validacion = Validar(reserva);
            if (validacion != ReservaResult.Success) return validacion;

            // Recalcular DV horizontal con los nuevos valores antes de persistir.
            reserva.Dvh = ValidadorDeIntegridad.CalcularDVH(reserva);
            DAL_Reserva.Update(reserva);
            BLL_Integridad.RecalcularDVVerticalReservas();

            // Control de cambios: registra campo por campo lo que cambio.
            int cambios = RegistradorDeCambios.RegistrarCambios("Reserva", reserva.Id, antes, reserva, CamposAuditados);
            BLL_Bitacora.Registrar("Reservas", "Modificacion de reserva", CriticidadBitacora.Info,
                $"Reserva #{reserva.Id} - {cambios} campo(s) modificado(s)");
            return ReservaResult.Success;
        }

        private static ReservaResult Validar(BE_Reserva reserva)
        {
            if (reserva == null || reserva.ClienteId <= 0 || !DAL_Cliente.Exists(reserva.ClienteId))
                return ReservaResult.InvalidCliente;

            if (reserva.SalonId <= 0 || !DAL_Salon.Exists(reserva.SalonId))
                return ReservaResult.InvalidSalon;

            // Una reserva nueva no puede agendarse en el pasado.
            if (reserva.FechaEvento == default || reserva.FechaEvento.Date < DateTime.Today)
                return ReservaResult.InvalidFecha;

            if (reserva.Monto < 0)
                return ReservaResult.InvalidMonto;

            // Anti-solapamiento: el salon se compromete solo al CONFIRMAR. Una
            // cotizacion o una reserva pendiente no bloquean (puede haber varias
            // para la misma fecha); recien al confirmar se verifica que no haya
            // otra reserva firme ese dia. Se excluye la propia reserva.
            if (reserva.Estado == EstadoReserva.CONFIRMADA &&
                DAL_Reserva.SalonOcupado(reserva.SalonId, reserva.FechaEvento, reserva.Id))
                return ReservaResult.SalonOcupado;

            return ReservaResult.Success;
        }
    }
}
