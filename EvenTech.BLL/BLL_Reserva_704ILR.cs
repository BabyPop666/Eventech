using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum ReservaResult_704ILR
    {
        Success_704ILR,
        InvalidCliente_704ILR,
        InvalidSalon_704ILR,
        InvalidFecha_704ILR,
        InvalidMonto_704ILR,
        SalonOcupado_704ILR,        // ya hay otra reserva activa para ese salon y fecha
        NoModificable_704ILR,       // la reserva esta cancelada: es un estado terminal
        Vencida_704ILR,             // se quiso confirmar una cotizacion/pendiente cuyo plazo expiro (RN-01)
        TransicionInvalida_704ILR,  // el cambio de estado no figura en la tabla de transiciones (RN-05)
        InvalidInvitados_704ILR,    // cantidad de invitados negativa
        CapacidadInsuficiente_704ILR, // el salon no aloja a los invitados de la reserva (RN-06)
        MontoInferiorPagado_704ILR,   // RN-04: el total quedaria por debajo de lo ya cobrado
        SinAdelanto_704ILR,           // RN-07: se quiso confirmar sin ningun pago registrado
        NotFound_704ILR
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

        // ---------------------------------------------------------------
        // RN-05 — Transiciones de estado admitidas.
        // El ciclo de vida de la operacion no es libre: COTIZACION puede avanzar a
        // cualquier estado; PENDIENTE solo confirma o cancela; CONFIRMADA solo
        // cancela (no se "desconfirma": el salon ya quedo comprometido y hay
        // cobros asociados); CANCELADA es terminal. Mantener el mismo estado
        // siempre es valido (guardar una reserva sin tocar su estado).
        // La tabla vive aca, en una unica funcion, para que el documento y el
        // codigo compartan una sola fuente de verdad.
        public static bool TransicionValida_704ILR(EstadoReserva_704ILR desde_704ILR, EstadoReserva_704ILR hacia_704ILR)
        {
            if (desde_704ILR == hacia_704ILR)
                return desde_704ILR != EstadoReserva_704ILR.CANCELADA;

            switch (desde_704ILR)
            {
                case EstadoReserva_704ILR.COTIZACION:
                    return hacia_704ILR == EstadoReserva_704ILR.PENDIENTE
                        || hacia_704ILR == EstadoReserva_704ILR.CONFIRMADA
                        || hacia_704ILR == EstadoReserva_704ILR.CANCELADA;

                case EstadoReserva_704ILR.PENDIENTE:
                    return hacia_704ILR == EstadoReserva_704ILR.CONFIRMADA
                        || hacia_704ILR == EstadoReserva_704ILR.CANCELADA;

                case EstadoReserva_704ILR.CONFIRMADA:
                    return hacia_704ILR == EstadoReserva_704ILR.CANCELADA;

                default:                       // CANCELADA: estado terminal
                    return false;
            }
        }

        // RN-06 — Capacidad del salon.
        // Al comprometer el salon (CONFIRMADA) tiene que poder alojar a los invitados
        // estimados de la reserva. En COTIZACION y PENDIENTE no se exige: el vendedor
        // todavia esta armando la propuesta y puede cambiar de salon o de cantidad.
        // La funcion responde para cualquier estado; quien decide CUANDO exigirla (y
        // que el dato no falte al confirmar) es Validar_704ILR.
        public static bool CapacidadSuficiente_704ILR(int salonId_704ILR, int cantidadInvitados_704ILR)
        {
            if (cantidadInvitados_704ILR <= 0) return true;   // sin dato no hay nada que comparar
            int capacidad_704ILR = DAL_Salon_704ILR.Capacidad_704ILR(salonId_704ILR);
            return capacidad_704ILR <= 0 || cantidadInvitados_704ILR <= capacidad_704ILR;
        }

        // RN-07 — Adelanto para confirmar.
        // Lo que distingue a una reserva CONFIRMADA de una PENDIENTE es que el cliente
        // ya puso dinero: recien ahi la operacion queda firme y compromete el salon.
        // Como el cobro necesita una reserva ya registrada, el orden es siempre
        // guardar -> cobrar -> confirmar, que es el que describe el proceso de negocio.
        public static bool TieneAdelanto_704ILR(int reservaId_704ILR)
            => reservaId_704ILR > 0 && DAL_Pago_704ILR.TotalPagado_704ILR(reservaId_704ILR) > 0m;

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
            if (r_704ILR == null) return ReservaResult_704ILR.NotFound_704ILR;
            if (!PuedeModificar_704ILR(r_704ILR)) return ReservaResult_704ILR.NoModificable_704ILR;

            r_704ILR.VenceEl_704ILR = CalcularVencimiento_704ILR(r_704ILR.Estado_704ILR, DateTime.Now);
            DAL_Reserva_704ILR.Update_704ILR(r_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Renovacion de vigencia", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reservaId_704ILR} renovada hasta {r_704ILR.VenceEl_704ILR:yyyy-MM-dd HH:mm}");
            return ReservaResult_704ILR.Success_704ILR;
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
            if (antes_704ILR == null) return ReservaResult_704ILR.NotFound_704ILR;
            if (!PuedeModificar_704ILR(antes_704ILR)) return ReservaResult_704ILR.NoModificable_704ILR;

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
            return ReservaResult_704ILR.Success_704ILR;
        }

        // Campos auditados por el control de cambios (T06b). Se persisten con el
        // nombre logico; RegistradorDeCambios resuelve por reflexion la propiedad
        // sufijada correspondiente.
        private static readonly string[] CamposAuditados_704ILR =
            { "ClienteId", "SalonId", "FechaEvento", "Estado", "Monto", "CantidadInvitados" };

        public static ReservaResult_704ILR Crear_704ILR(BE_Reserva_704ILR reserva_704ILR, out int nuevoId_704ILR)
            => Crear_704ILR(reserva_704ILR, null, out nuevoId_704ILR);

        // Alta de la operacion COMPLETA: la reserva y los servicios contratados que
        // componen su monto se escriben bajo una unica transaccion. Si algo falla en
        // el medio no queda una reserva con un total que sus lineas no explican.
        // Con 'servicios' en null se guarda solo la cabecera (alta sin servicios).
        public static ReservaResult_704ILR Crear_704ILR(BE_Reserva_704ILR reserva_704ILR,
            IList<BE_ReservaServicio_704ILR> servicios_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            if (reserva_704ILR == null) return ReservaResult_704ILR.InvalidCliente_704ILR;

            // RN-05: CANCELADA es un estado al que se LLEGA dando de baja una operacion
            // existente, no uno con el que se nace. Admitirlo en el alta dejaria una
            // reserva en estado terminal sin liquidacion de la RN-02, sin version previa
            // y sin asiento de cancelacion, y ya no se podria editar ni dar de baja.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CANCELADA)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Alta rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    "No se admite dar de alta una reserva directamente en estado CANCELADA (RN-05).");
                return ReservaResult_704ILR.TransicionInvalida_704ILR;
            }

            // RN-07: una reserva nace en COTIZACION o PENDIENTE. No puede nacer
            // CONFIRMADA porque para confirmar hace falta el adelanto cobrado, y un
            // cobro necesita una reserva ya registrada a la que imputarse: primero se
            // guarda la operacion, despues se cobra y recien entonces se confirma.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Alta rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    "No se admite dar de alta una reserva directamente CONFIRMADA: " +
                    "primero se registra la operacion y se cobra el adelanto (RN-07).");
                return ReservaResult_704ILR.SinAdelanto_704ILR;
            }

            var validacion_704ILR = Validar_704ILR(reserva_704ILR);
            if (validacion_704ILR != ReservaResult_704ILR.Success_704ILR) return validacion_704ILR;

            // RN-01: la vigencia se fija al dar de alta, segun el estado inicial.
            reserva_704ILR.VenceEl_704ILR =
                CalcularVencimiento_704ILR(reserva_704ILR.Estado_704ILR, DateTime.Now);

            // DV horizontal: se calcula sobre los campos de negocio antes de persistir.
            reserva_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(reserva_704ILR);

            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                SqlConnection conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (SqlTransaction tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    nuevoId_704ILR = DAL_Reserva_704ILR.Insert_704ILR(reserva_704ILR, conn_704ILR, tx_704ILR);
                    if (servicios_704ILR != null)
                        DAL_ReservaServicio_704ILR.ReplaceForReserva_704ILR(nuevoId_704ILR, servicios_704ILR, conn_704ILR, tx_704ILR);
                    tx_704ILR.Commit();
                }
            }

            // El DV vertical es un dato derivado del conjunto: se recalcula despues del
            // commit y, si fallara, se vuelve a calcular en el proximo arranque.
            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Alta de reserva", CriticidadBitacora_704ILR.Info,
                $"Reserva #{nuevoId_704ILR} - cliente #{reserva_704ILR.ClienteId_704ILR}, monto {reserva_704ILR.Monto_704ILR:0.00}");
            return ReservaResult_704ILR.Success_704ILR;
        }

        // Una reserva cancelada es un estado terminal: no admite mas ediciones.
        // (La fecha pasada no se contempla aca a proposito: Validar ya rechaza
        // guardar con fecha anterior a hoy, y bloquear por la fecha VIEJA
        // impediria reprogramar un evento vencido, que si es una operacion valida.)
        public static bool PuedeModificar_704ILR(BE_Reserva_704ILR reserva_704ILR)
            => reserva_704ILR != null && reserva_704ILR.Estado_704ILR != EstadoReserva_704ILR.CANCELADA;

        public static ReservaResult_704ILR Actualizar_704ILR(BE_Reserva_704ILR reserva_704ILR)
            => Actualizar_704ILR(reserva_704ILR, null);

        // Modificacion de la operacion COMPLETA (ver Crear_704ILR): la cabecera y sus
        // servicios contratados viajan en la misma transaccion.
        public static ReservaResult_704ILR Actualizar_704ILR(BE_Reserva_704ILR reserva_704ILR,
            IList<BE_ReservaServicio_704ILR> servicios_704ILR)
        {
            BE_Reserva_704ILR antes_704ILR = reserva_704ILR.Id_704ILR > 0 ? DAL_Reserva_704ILR.GetById_704ILR(reserva_704ILR.Id_704ILR) : null;
            if (antes_704ILR == null) return ReservaResult_704ILR.NotFound_704ILR;

            // Se evalua sobre el estado PERSISTIDO: lo que el usuario mando en el
            // formulario no puede habilitar la edicion de una reserva ya cancelada.
            if (!PuedeModificar_704ILR(antes_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Modificacion rechazada", CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR} cancelada: no admite modificaciones.");
                return ReservaResult_704ILR.NoModificable_704ILR;
            }

            // RN-05: el cambio de estado tiene que figurar en la tabla de transiciones.
            // Se evalua sobre el estado PERSISTIDO contra el pedido, antes que nada:
            // una CONFIRMADA no puede volver a COTIZACION ni a PENDIENTE.
            if (!TransicionValida_704ILR(antes_704ILR.Estado_704ILR, reserva_704ILR.Estado_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Transicion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR}: no se admite pasar de " +
                    $"{antes_704ILR.Estado_704ILR} a {reserva_704ILR.Estado_704ILR} (RN-05).");
                return ReservaResult_704ILR.TransicionInvalida_704ILR;
            }

            // Dar de baja no es una edicion mas: entrar a CANCELADA tiene que pasar
            // por Cancelar, que es quien liquida la RN-02 y deja asentado el
            // reintegro. Si se admitiera por aca se podria cancelar sin liquidacion.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CANCELADA)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Cancelacion rechazada por via incorrecta",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR}: la baja se registra por la via de " +
                    "cancelacion, que aplica la politica de reintegro (RN-02).");
                return ReservaResult_704ILR.TransicionInvalida_704ILR;
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
                return ReservaResult_704ILR.Vencida_704ILR;
            }

            var validacion_704ILR = Validar_704ILR(reserva_704ILR);
            if (validacion_704ILR != ReservaResult_704ILR.Success_704ILR) return validacion_704ILR;

            // RN-04: el total de la reserva es el tope de la cobranza. Una edicion que
            // lo achique por debajo de lo ya cobrado (por ejemplo, quitando servicios)
            // romperia el invariante sin registrar ningun pago: se rechaza aca, que es
            // el unico camino por el que el total puede bajar.
            if (reserva_704ILR.Monto_704ILR < DAL_Pago_704ILR.TotalPagado_704ILR(reserva_704ILR.Id_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Modificacion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR}: el total quedaria por debajo de lo ya cobrado (RN-04).");
                return ReservaResult_704ILR.MontoInferiorPagado_704ILR;
            }

            // RN-07: la reserva queda firme cuando el cliente puso el adelanto. Pasar a
            // CONFIRMADA sin ningun pago registrado comprometeria el salon sobre una
            // decision que el cliente todavia no respaldo con dinero.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                antes_704ILR.Estado_704ILR != EstadoReserva_704ILR.CONFIRMADA &&
                !TieneAdelanto_704ILR(reserva_704ILR.Id_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Confirmacion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reserva_704ILR.Id_704ILR}: no se registro el adelanto, " +
                    "no puede confirmarse (RN-07).");
                return ReservaResult_704ILR.SinAdelanto_704ILR;
            }

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

            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                SqlConnection conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (SqlTransaction tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    DAL_Reserva_704ILR.Update_704ILR(reserva_704ILR, conn_704ILR, tx_704ILR);
                    if (servicios_704ILR != null)
                        DAL_ReservaServicio_704ILR.ReplaceForReserva_704ILR(reserva_704ILR.Id_704ILR, servicios_704ILR, conn_704ILR, tx_704ILR);
                    tx_704ILR.Commit();
                }
            }

            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            // Control de cambios: registra campo por campo lo que cambio.
            int cambios_704ILR = RegistradorDeCambios_704ILR.RegistrarCambios_704ILR("Reserva", reserva_704ILR.Id_704ILR, antes_704ILR, reserva_704ILR, CamposAuditados_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Modificacion de reserva", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reserva_704ILR.Id_704ILR} - {cambios_704ILR} campo(s) modificado(s)");
            return ReservaResult_704ILR.Success_704ILR;
        }

        // Restaura la reserva al estado de una version previa (patron Memento).
        // El estado vigente se versiona antes de pisarlo, de modo que la propia
        // restauracion tambien se puede deshacer.
        //
        // Relacion con la RN-05: restaurar es una correccion ADMINISTRATIVA, no un
        // avance del ciclo comercial, y por eso no se le exige la tabla de
        // transiciones (deshacer una confirmacion erronea es su caso de uso tipico
        // y queda enteramente auditado: versiona, registra campo por campo y asienta
        // en bitacora). Los dos limites terminales de la RN-05 si se respetan: no se
        // restaura una reserva CANCELADA ni se restaura HACIA una version cancelada.
        public static ReservaResult_704ILR RestaurarVersion_704ILR(int reservaId_704ILR, int mementoId_704ILR)
        {
            BE_Reserva_704ILR actual_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (actual_704ILR == null) return ReservaResult_704ILR.NotFound_704ILR;

            // CANCELADA es terminal (RN-05): restaurar una version previa tambien es
            // una modificacion y reabriria la operacion por la puerta de atras. La
            // regla se aplica aca y no solo en la UI porque la restauracion persiste
            // en el acto, sin pasar por Actualizar.
            if (!PuedeModificar_704ILR(actual_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR} cancelada: no admite restaurar versiones (RN-05).");
                return ReservaResult_704ILR.NoModificable_704ILR;
            }

            BE_ReservaMemento_704ILR memento_704ILR = CaretakerReserva_704ILR.GetVersion_704ILR(mementoId_704ILR);
            if (memento_704ILR == null || memento_704ILR.ReservaId_704ILR != reservaId_704ILR) return ReservaResult_704ILR.NotFound_704ILR;

            // Tampoco se puede ENTRAR a CANCELADA restaurando: dar de baja una
            // operacion exige pasar por Cancelar, que es quien aplica la RN-02 y
            // deja asentado el reintegro. Restaurar salteando esa via dejaria una
            // reserva cancelada sin liquidacion.
            if (memento_704ILR.Estado_704ILR == EstadoReserva_704ILR.CANCELADA)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR}: la version #{mementoId_704ILR} esta CANCELADA; " +
                    "la baja se registra por la via de cancelacion (RN-02/RN-05).");
                return ReservaResult_704ILR.TransicionInvalida_704ILR;
            }

            // RN-01: la restauracion esta exceptuada de la TABLA de transiciones, no del
            // plazo de vigencia. Si la version que se repone deja la operacion CONFIRMADA
            // y la actual ya vencio, confirmar por esta via seria saltear el vencimiento
            // que Actualizar rechaza; hay que renovar primero, igual que en la edicion.
            if (memento_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                actual_704ILR.Estado_704ILR != EstadoReserva_704ILR.CONFIRMADA &&
                actual_704ILR.EstaVencida_704ILR)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR} vencida el " +
                    $"{actual_704ILR.VenceEl_704ILR:yyyy-MM-dd HH:mm}: hay que renovarla antes de " +
                    "restaurar una version confirmada (RN-01).");
                return ReservaResult_704ILR.Vencida_704ILR;
            }

            // RN-07: tampoco se llega a CONFIRMADA restaurando si no hay adelanto.
            if (memento_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                actual_704ILR.Estado_704ILR != EstadoReserva_704ILR.CONFIRMADA &&
                !TieneAdelanto_704ILR(reservaId_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR}: la version #{mementoId_704ILR} esta CONFIRMADA " +
                    "y no hay adelanto registrado (RN-07).");
                return ReservaResult_704ILR.SinAdelanto_704ILR;
            }

            BE_Reserva_704ILR restaurada_704ILR = DAL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            restaurada_704ILR.RestaurarDesde_704ILR(memento_704ILR);

            // RN-01: el memento no guarda el vencimiento (es un dato administrativo, no
            // parte del estado de negocio versionado), asi que hay que recalcularlo con
            // el mismo criterio que usa la edicion: si el estado cambio, el plazo se
            // cuenta de nuevo; si no cambio, se conserva el que tenia. Sin esto una
            // cotizacion restaurada quedaba sin plazo y una confirmada, con plazo.
            restaurada_704ILR.VenceEl_704ILR =
                restaurada_704ILR.Estado_704ILR != actual_704ILR.Estado_704ILR
                    ? CalcularVencimiento_704ILR(restaurada_704ILR.Estado_704ILR, DateTime.Now)
                    : actual_704ILR.VenceEl_704ILR;

            // La fecha del evento de esa version puede haber quedado en el pasado:
            // se admite (es un estado historico valido), pero el resto de las
            // reglas (cliente/salon existentes, anti-solapamiento) sigue vigente.
            var validacion_704ILR = Validar_704ILR(restaurada_704ILR, permitirFechaPasada_704ILR: true);
            if (validacion_704ILR != ReservaResult_704ILR.Success_704ILR) return validacion_704ILR;

            // RN-04: una version previa mas barata que lo ya cobrado tampoco se repone.
            if (restaurada_704ILR.Monto_704ILR < DAL_Pago_704ILR.TotalPagado_704ILR(reservaId_704ILR))
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion rechazada",
                    CriticidadBitacora_704ILR.Advertencia,
                    $"Reserva #{reservaId_704ILR}: la version #{mementoId_704ILR} dejaria el total " +
                    "por debajo de lo ya cobrado (RN-04).");
                return ReservaResult_704ILR.MontoInferiorPagado_704ILR;
            }

            CaretakerReserva_704ILR.GuardarVersion_704ILR(actual_704ILR);

            restaurada_704ILR.Dvh_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(restaurada_704ILR);

            // La cabecera repuesta y los servicios de esa version entran juntos, igual
            // que en el alta y en la edicion: el monto restaurado y las lineas que lo
            // componen no pueden quedar desfasados.
            using (var cn_704ILR = new DAL_DB_Connection_704ILR())
            {
                SqlConnection conn_704ILR = cn_704ILR.OpenConnection_704ILR();
                using (SqlTransaction tx_704ILR = conn_704ILR.BeginTransaction())
                {
                    DAL_Reserva_704ILR.Update_704ILR(restaurada_704ILR, conn_704ILR, tx_704ILR);
                    DAL_ReservaServicio_704ILR.ReplaceForReserva_704ILR(
                        reservaId_704ILR, memento_704ILR.Servicios_704ILR, conn_704ILR, tx_704ILR);
                    tx_704ILR.Commit();
                }
            }

            BLL_Integridad_704ILR.RecalcularDVVerticalReservas_704ILR();

            // El control de cambios registra la restauracion como una modificacion
            // mas, campo por campo (queda trazado en el historial de la reserva).
            int cambios_704ILR = RegistradorDeCambios_704ILR.RegistrarCambios_704ILR("Reserva", reservaId_704ILR, actual_704ILR, restaurada_704ILR, CamposAuditados_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Restauracion de version", CriticidadBitacora_704ILR.Info,
                $"Reserva #{reservaId_704ILR} restaurada a la version #{mementoId_704ILR} ({cambios_704ILR} campo(s) repuestos)");
            return ReservaResult_704ILR.Success_704ILR;
        }

        private static ReservaResult_704ILR Validar_704ILR(BE_Reserva_704ILR reserva_704ILR, bool permitirFechaPasada_704ILR = false)
        {
            if (reserva_704ILR == null || reserva_704ILR.ClienteId_704ILR <= 0 || !DAL_Cliente_704ILR.Exists_704ILR(reserva_704ILR.ClienteId_704ILR))
                return ReservaResult_704ILR.InvalidCliente_704ILR;

            if (reserva_704ILR.SalonId_704ILR <= 0 || !DAL_Salon_704ILR.Exists_704ILR(reserva_704ILR.SalonId_704ILR))
                return ReservaResult_704ILR.InvalidSalon_704ILR;

            // Una reserva nueva no puede agendarse en el pasado (al restaurar una
            // version historica esta regla se relaja).
            if (reserva_704ILR.FechaEvento_704ILR == default ||
                (!permitirFechaPasada_704ILR && reserva_704ILR.FechaEvento_704ILR.Date < DateTime.Today))
                return ReservaResult_704ILR.InvalidFecha_704ILR;

            if (reserva_704ILR.Monto_704ILR < 0)
                return ReservaResult_704ILR.InvalidMonto_704ILR;

            if (reserva_704ILR.CantidadInvitados_704ILR < 0)
                return ReservaResult_704ILR.InvalidInvitados_704ILR;

            // RN-06: al comprometer el salon hay que saber a cuanta gente hay que alojar,
            // y el salon tiene que poder hacerlo. En COTIZACION y PENDIENTE el dato puede
            // faltar: la propuesta todavia se esta componiendo.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA)
            {
                if (reserva_704ILR.CantidadInvitados_704ILR <= 0)
                    return ReservaResult_704ILR.InvalidInvitados_704ILR;
                if (!CapacidadSuficiente_704ILR(reserva_704ILR.SalonId_704ILR, reserva_704ILR.CantidadInvitados_704ILR))
                    return ReservaResult_704ILR.CapacidadInsuficiente_704ILR;
            }

            // Anti-solapamiento: el salon se compromete solo al CONFIRMAR. Una
            // cotizacion o una reserva pendiente no bloquean (puede haber varias
            // para la misma fecha); recien al confirmar se verifica que no haya
            // otra reserva firme ese dia. Se excluye la propia reserva.
            if (reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CONFIRMADA &&
                DAL_Reserva_704ILR.SalonOcupado_704ILR(reserva_704ILR.SalonId_704ILR, reserva_704ILR.FechaEvento_704ILR, reserva_704ILR.Id_704ILR))
                return ReservaResult_704ILR.SalonOcupado_704ILR;

            return ReservaResult_704ILR.Success_704ILR;
        }
    }
}
