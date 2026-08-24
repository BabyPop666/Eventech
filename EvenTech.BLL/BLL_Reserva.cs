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
        NoModificable,  // la reserva esta cancelada: es un estado terminal
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

        // Una reserva cancelada es un estado terminal: no admite mas ediciones.
        // (La fecha pasada no se contempla aca a proposito: Validar ya rechaza
        // guardar con fecha anterior a hoy, y bloquear por la fecha VIEJA
        // impediria reprogramar un evento vencido, que si es una operacion valida.)
        public static bool PuedeModificar(BE_Reserva reserva)
            => reserva != null && reserva.Estado != EstadoReserva.CANCELADA;

        public static ReservaResult Actualizar(BE_Reserva reserva)
        {
            BE_Reserva antes = reserva.Id > 0 ? DAL_Reserva.GetById(reserva.Id) : null;
            if (antes == null) return ReservaResult.NotFound;

            // Se evalua sobre el estado PERSISTIDO: lo que el usuario mando en el
            // formulario no puede habilitar la edicion de una reserva ya cancelada.
            if (!PuedeModificar(antes))
            {
                BLL_Bitacora.Registrar("Reservas", "Modificacion rechazada", CriticidadBitacora.Advertencia,
                    $"Reserva #{reserva.Id} cancelada: no admite modificaciones.");
                return ReservaResult.NoModificable;
            }

            var validacion = Validar(reserva);
            if (validacion != ReservaResult.Success) return validacion;

            // Memento: antes de pisar el estado actual se guarda una version
            // completa (reserva + servicios) para poder volver atras.
            CaretakerReserva.GuardarVersion(antes);

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

        // Restaura la reserva al estado de una version previa (patron Memento).
        // El estado vigente se versiona antes de pisarlo, de modo que la propia
        // restauracion tambien se puede deshacer.
        public static ReservaResult RestaurarVersion(int reservaId, int mementoId)
        {
            BE_Reserva actual = DAL_Reserva.GetById(reservaId);
            if (actual == null) return ReservaResult.NotFound;

            BE_ReservaMemento memento = CaretakerReserva.GetVersion(mementoId);
            if (memento == null || memento.ReservaId != reservaId) return ReservaResult.NotFound;

            BE_Reserva restaurada = DAL_Reserva.GetById(reservaId);
            restaurada.RestaurarDesde(memento);

            // La fecha del evento de esa version puede haber quedado en el pasado:
            // se admite (es un estado historico valido), pero el resto de las
            // reglas (cliente/salon existentes, anti-solapamiento) sigue vigente.
            var validacion = Validar(restaurada, permitirFechaPasada: true);
            if (validacion != ReservaResult.Success) return validacion;

            CaretakerReserva.GuardarVersion(actual);

            restaurada.Dvh = ValidadorDeIntegridad.CalcularDVH(restaurada);
            DAL_Reserva.Update(restaurada);
            BLL_ReservaServicio.Guardar(reservaId, memento.Servicios);
            BLL_Integridad.RecalcularDVVerticalReservas();

            // El control de cambios registra la restauracion como una modificacion
            // mas, campo por campo (queda trazado en el historial de la reserva).
            int cambios = RegistradorDeCambios.RegistrarCambios("Reserva", reservaId, actual, restaurada, CamposAuditados);
            BLL_Bitacora.Registrar("Reservas", "Restauracion de version", CriticidadBitacora.Info,
                $"Reserva #{reservaId} restaurada a la version #{mementoId} ({cambios} campo(s) repuestos)");
            return ReservaResult.Success;
        }

        private static ReservaResult Validar(BE_Reserva reserva, bool permitirFechaPasada = false)
        {
            if (reserva == null || reserva.ClienteId <= 0 || !DAL_Cliente.Exists(reserva.ClienteId))
                return ReservaResult.InvalidCliente;

            if (reserva.SalonId <= 0 || !DAL_Salon.Exists(reserva.SalonId))
                return ReservaResult.InvalidSalon;

            // Una reserva nueva no puede agendarse en el pasado (al restaurar una
            // version historica esta regla se relaja).
            if (reserva.FechaEvento == default ||
                (!permitirFechaPasada && reserva.FechaEvento.Date < DateTime.Today))
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
