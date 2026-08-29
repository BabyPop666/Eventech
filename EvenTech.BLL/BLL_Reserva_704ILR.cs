using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum ReservaResult_704ILR
    {
        Success,
        InvalidCliente,
        InvalidSalon,
        InvalidFecha,
        InvalidMonto,
        SalonOcupado,   // ya hay otra reserva activa para ese salon y fecha
        NoModificable,  // la reserva esta cancelada: es un estado terminal
        Vencida,        // se quiso confirmar una cotizacion/pendiente cuyo plazo expiro (RN-01)
        NotFound
    }

    // Reglas de negocio de reservas: validacion de datos y estados antes de
    // delegar al DAL. Las validaciones viven aca (no en la UI ni en el DAL)
    // para mantener cohesion y permitir reuso desde otros frentes.
    public static class BLL_Reserva_704ILR
    {
        public static List<BE_Reserva_704ILR> GetAll_704ILR() => DAL_Reserva_704ILR.GetAll_704ILR();

        public static BE_Reserva_704ILR GetById_704ILR(int id_704ILR) => DAL_Reserva_704ILR.GetById_704ILR(id_704ILR);

        // ---------------------------------------------------------------
        // RN-01 — Vigencia de la operacion.
        // Una COTIZACION vale DiasValidezCotizacion dias desde su emision; una
        // reserva PENDIENTE vale HorasValidezPendiente horas desde que quedo en
        // ese estado. Vencido el plazo la operacion no puede confirmarse: hay que
        // renovarla. CONFIRMADA y CANCELADA no tienen plazo (VenceEl queda null).
        public const int DiasValidezCotizacion_704ILR = 15;
        public const int HorasValidezPendiente_704ILR = 72;

        // RN-02 — Politica de cancelacion.
        // Cancelando con DiasCancelacionSinPenalidad dias o mas de antelacion a la
        // fecha del evento se reintegra el 100% de lo cobrado; con menos, se retiene
        // el PorcentajeRetencion. El sistema calcula y deja asentado el importe: el
        // movimiento fisico del dinero es una gestion administrativa externa.
        public const int DiasCancelacionSinPenalidad_704ILR = 30;
        public const int PorcentajeRetencion_704ILR = 50;

        // Vencimiento que le corresponde a un estado, contado desde 'desde'.
        public static DateTime? CalcularVencimiento_704ILR(EstadoReserva_704ILR estado_704ILR, DateTime desde_704ILR)
        {
            if (estado_704ILR == EstadoReserva_704ILR.COTIZACION)
                return desde_704ILR.AddDays(DiasValidezCotizacion_704ILR);
            if (estado_704ILR == EstadoReserva_704ILR.PENDIENTE)
                return desde_704ILR.AddHours(HorasValidezPendiente_704ILR);
            return null;   // CONFIRMADA / CANCELADA no vencen
        }

        // Renueva el plazo de una cotizacion o pendiente que ya expiro.
        public static ReservaResult_704ILR Renovar_704ILR(int reservaId_704ILR)
        {
            BE_Reserva_704ILR r_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (r_704ILR == null) return ReservaResult_704ILR.NotFound;
            if (!PuedeModificar_704ILR(r_704ILR)) return ReservaResult_704ILR.NoModificable;

            r_704ILR.VenceEl_704ILR = CalcularVencimiento_704ILR(r_704ILR.Estado_704ILR, DateTime.Now);
            DAL_Reserva_704ILR.Update_704ILR(r_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Renovacion de vigencia", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reservaId_704ILR} renovada hasta {r_704ILR.VenceEl_704ILR:yyyy-MM-dd HH:mm}");
            return ReservaResult_704ILR.Success;
        }

        // RN-02: importe que se retiene y que se reintegra si se cancela hoy.
        public static void CalcularCancelacion_704ILR(BE_Reserva_704ILR reserva_704ILR,
            out decimal retenido_704ILR, out decimal reembolsable_704ILR)
        {
            retenido_704ILR = 0m;
            reembolsable_704ILR = 0m;
            if (reserva_704ILR == null) return;

            decimal pagado_704ILR = DAL_Pago_704ILR.TotalPagado_704ILR(reserva_704ILR.Id_704ILR);
            if (pagado_704ILR <= 0) return;

            int diasAntelacion_704ILR = (reserva_704ILR.FechaEvento_704ILR.Date - DateTime.Today).Days;
            if (diasAntelacion_704ILR >= DiasCancelacionSinPenalidad_704ILR)
            {
                reembolsable_704ILR = pagado_704ILR;
            }
            else
            {
                retenido_704ILR = decimal.Round(pagado_704ILR * PorcentajeRetencion_704ILR / 100m, 2);
                reembolsable_704ILR = pagado_704ILR - retenido_704ILR;
            }
        }

        // Cancela la reserva aplicando la RN-02 y dejando todo asentado. No pasa por
        // Actualizar porque una reserva con fecha ya pasada tambien se puede cancelar.
        public static ReservaResult_704ILR Cancelar_704ILR(int reservaId_704ILR,
            out decimal retenido_704ILR, out decimal reembolsable_704ILR)
        {
            retenido_704ILR = 0m;
            reembolsable_704ILR = 0m;

            BE_Reserva_704ILR antes_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (antes_704ILR == null) return ReservaResult_704ILR.NotFound;
            if (!PuedeModificar_704ILR(antes_704ILR)) return ReservaResult_704ILR.NoModificable;

            CalcularCancelacion_704ILR(antes_704ILR, out retenido_704ILR, out reembolsable_704ILR);

            BE_Reserva_704ILR cancelada_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            cancelada_704ILR.Estado_704ILR = EstadoReserva_704ILR.CANCELADA;
            cancelada_704ILR.VenceEl_704ILR = null;

            CaretakerReserva_704ILR.GuardarVersion_704ILR(antes_704ILR);
            cancelada_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(cancelada_704ILR);
            DAL_Reserva_704ILR.Update_704ILR(cancelada_704ILR);
            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            RegistradorDeCambios_704ILR.RegistrarCambios_704ILR("Reserva", reservaId_704ILR,
                antes_704ILR, cancelada_704ILR, CamposAuditados_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Cancelacion de reserva",
                CriticidadBitacora_704ILR.Advertencia,
                $"Reserva #{reservaId_704ILR} cancelada. Retenido {retenido_704ILR:0.00}, " +
                $"reintegro {reembolsable_704ILR:0.00} (RN-02).");
            return ReservaResult_704ILR.Success;
        }

        // Campos auditados por el control de cambios (T06b).
        private static readonly string[] CamposAuditados_704ILR =
            { "ClienteId", "SalonId", "FechaEvento", "Estado", "Monto" };

        public static ReservaResult_704ILR Crear_704ILR(BE_Reserva_704ILR reserva_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            var validacion_704ILR = Validar_704ILR(reserva_704ILR);
            if (validacion_704ILR != ReservaResult_704ILR.Success) return validacion_704ILR;

            // RN-01: la vigencia se fija al dar de alta, segun el estado inicial.
            reserva_704ILR.VenceEl_704ILR =
                CalcularVencimiento_704ILR(reserva_704ILR.Estado_704ILR, DateTime.Now);

            // DV horizontal: se calcula sobre los campos de negocio antes de persistir.
            reserva_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(reserva_704ILR);
            nuevoId_704ILR = DAL_Reserva_704ILR.Insert_704ILR(reserva_704ILR);
            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Alta de reserva", CriticidadBitacora_704ILR.Info,
                $"Reserva #{nuevoId_704ILR} - cliente #{reserva_704ILR.ClienteId_704ILR}, monto {reserva_704ILR.Monto_704ILR:0.00}");
            return ReservaResult_704ILR.Success;
        }

        // Una reserva cancelada es un estado terminal: no admite mas ediciones.
        // (La fecha pasada no se contempla aca a proposito: Validar ya rechaza
        // guardar con fecha anterior a hoy, y bloquear por la fecha VIEJA
        // impediria reprogramar un evento vencido, que si es una operacion valida.)
        public static bool PuedeModificar_704ILR(BE_Reserva_704ILR reserva_704ILR)
            => reserva_704ILR != null && reserva_704ILR.Estado_704ILR != EstadoReserva_704ILR.CANCELADA;

        public static ReservaResult_704ILR Actualizar_704ILR(BE_Reserva_704ILR reserva_704ILR)
        {
            BE_Reserva_704ILR antes_704ILR = reserva_704ILR.Id_704ILR > 0 ? DAL_Reserva_704ILR.GetById_704ILR(reserva_704ILR.Id_704ILR) : null;
            if (antes_704ILR == null) return ReservaResult_704ILR.NotFound;

            // Se evalua sobre el estado PERSISTIDO: lo que el usuario mando en el
            // formulario no puede habilitar la edicion de una reserva ya cancelada.
            if (!PuedeModificar_704ILR(antes_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Modificacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR} cancelada: no admite modificaciones.");
                return ReservaResult_704ILR.NoModificable;
            }

            // RN-01: no se puede confirmar una cotizacion o pendiente cuyo plazo expiro.
            // Se evalua sobre lo PERSISTIDO: el formulario no puede saltear el vencimiento.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                antes_704ILR.Estado_704ILR != EstadoReserva_704ILR.CONFIRMADA &&
                antes_704ILR.EstaVencida_704ILR)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Confirmacion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR} vencida el " +
                    $"{antes_704ILR.VenceEl_704ILR:yyyy-MM-dd HH:mm}: hay que renovarla (RN-01).");
                return ReservaResult_704ILR.Vencida;
            }

            var validacion_704ILR = Validar_704ILR(reserva_704ILR);
            if (validacion_704ILR != ReservaResult_704ILR.Success) return validacion_704ILR;

            // RN-01: si cambia el estado se recalcula la vigencia; si no, se conserva.
            reserva_704ILR.VenceEl_704ILR =
                reserva_704ILR.Estado_704ILR != antes_704ILR.Estado_704ILR
                    ? CalcularVencimiento_704ILR(reserva_704ILR.Estado_704ILR, DateTime.Now)
                    : antes_704ILR.VenceEl_704ILR;

            // Memento: antes de pisar el estado actual se guarda una version
            // completa (reserva + servicios) para poder volver atras.
            CaretakerReserva_704ILR.GuardarVersion_704ILR(antes_704ILR);

            // Recalcular DV horizontal con los nuevos valores antes de persistir.
            reserva_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(reserva_704ILR);
            DAL_Reserva_704ILR.Update_704ILR(reserva_704ILR);
            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            // Control de cambios: registra campo por campo lo que cambio.
            int cambios_704ILR = RegistradorDeCambios_704ILR.RegistrarCambios_704ILR("Reserva", reserva_704ILR.Id_704ILR, antes_704ILR, reserva_704ILR, CamposAuditados_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Modificacion de reserva", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reserva_704ILR.Id_704ILR} - {cambios_704ILR} campo(s) modificado(s)");
            return ReservaResult_704ILR.Success;
        }

        // Restaura la reserva al estado de una version previa (patron Memento).
        // El estado vigente se versiona antes de pisarlo, de modo que la propia
        // restauracion tambien se puede deshacer.
        public static ReservaResult_704ILR RestaurarVersion_704ILR(int reservaId_704ILR, int mementoId_704ILR)
        {
            BE_Reserva_704ILR actual_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (actual_704ILR == null) return ReservaResult_704ILR.NotFound;

            BE_ReservaMemento_704ILR memento_704ILR = CaretakerReserva_704ILR.GetVersion_704ILR(mementoId_704ILR);
            if (memento_704ILR == null || memento_704ILR.ReservaId_704ILR != reservaId_704ILR) return ReservaResult_704ILR.NotFound;

            BE_Reserva_704ILR restaurada_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            restaurada_704ILR.RestaurarDesde_704ILR(memento_704ILR);

            // La fecha del evento de esa version puede haber quedado en el pasado:
            // se admite (es un estado historico valido), pero el resto de las
            // reglas (cliente/salon existentes, anti-solapamiento) sigue vigente.
            var validacion_704ILR = Validar_704ILR(restaurada_704ILR, permitirFechaPasada_704ILR: true);
            if (validacion_704ILR != ReservaResult_704ILR.Success) return validacion_704ILR;

            CaretakerReserva_704ILR.GuardarVersion_704ILR(actual_704ILR);

            restaurada_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(restaurada_704ILR);
            DAL_Reserva_704ILR.Update_704ILR(restaurada_704ILR);
            BLL_ReservaServicio_704ILR.Guardar_704ILR(reservaId_704ILR, memento_704ILR.Servicios_704ILR);
            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            // El control de cambios registra la restauracion como una modificacion
            // mas, campo por campo (queda trazado en el historial de la reserva).
            int cambios_704ILR = RegistradorDeCambios_704ILR.RegistrarCambios_704ILR("Reserva", reservaId_704ILR, actual_704ILR, restaurada_704ILR, CamposAuditados_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion de version", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reservaId_704ILR} restaurada a la version #{mementoId_704ILR} ({cambios_704ILR} campo(s) repuestos)");
            return ReservaResult_704ILR.Success;
        }

        private static ReservaResult_704ILR Validar_704ILR(BE_Reserva_704ILR reserva_704ILR, bool permitirFechaPasada_704ILR = false)
        {
            if (reserva_704ILR == null || reserva_704ILR.ClienteId_704ILR <= 0 || !DAL_Cliente_704ILR.Exists_704ILR(reserva_704ILR.ClienteId_704ILR))
                return ReservaResult_704ILR.InvalidCliente;

            if (reserva_704ILR.SalonId_704ILR <= 0 || !DAL_Salon_704ILR.Exists_704ILR(reserva_704ILR.SalonId_704ILR))
                return ReservaResult_704ILR.InvalidSalon;

            // Una reserva nueva no puede agendarse en el pasado (al restaurar una
            // version historica esta regla se relaja).
            if (reserva_704ILR.FechaEvento_704ILR == default ||
                (!permitirFechaPasada_704ILR && reserva_704ILR.FechaEvento_704ILR.Date < DateTime.Today))
                return ReservaResult_704ILR.InvalidFecha;

            if (reserva_704ILR.Monto_704ILR < 0)
                return ReservaResult_704ILR.InvalidMonto;

            // Anti-solapamiento: el salon se compromete solo al CONFIRMAR. Una
            // cotizacion o una reserva pendiente no bloquean (puede haber varias
            // para la misma fecha); recien al confirmar se verifica que no haya
            // otra reserva firme ese dia. Se excluye la propia reserva.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                DAL_Reserva_704ILR.SalonOcupado_704ILR(reserva_704ILR.SalonId_704ILR, reserva_704ILR.FechaEvento_704ILR, reserva_704ILR.Id_704ILR))
                return ReservaResult_704ILR.SalonOcupado;

            return ReservaResult_704ILR.Success;
        }
    }
}
