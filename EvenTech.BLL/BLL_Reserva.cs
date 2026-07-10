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
        SalonOcupado,        // ya hay otra reserva activa para ese salon y fecha
        NotFound,
        NoEditable,          // la reserva esta CANCELADA (estado terminal)
        MontoMenorQuePagado  // el nuevo monto es menor que lo ya cobrado
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

        // ---- Alta / edicion de la cabecera (sin tocar servicios) ----

        public static ReservaResult Crear(BE_Reserva reserva, out int nuevoId)
        {
            nuevoId = 0;
            var validacion = Validar(reserva, null);
            if (validacion != ReservaResult.Success) return validacion;

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

            var validacion = ValidarActualizacion(reserva, antes);
            if (validacion != ReservaResult.Success) return validacion;

            reserva.Dvh = ValidadorDeIntegridad.CalcularDVH(reserva);
            DAL_Reserva.Update(reserva);
            BLL_Integridad.RecalcularDVVerticalReservas();

            RegistrarCambiosYBitacora(reserva, antes);
            return ReservaResult.Success;
        }

        // ---- Alta / edicion de la reserva JUNTO con sus servicios (atomico) ----
        // La UI usa estos: cabecera + servicios se guardan en una sola transaccion.

        public static ReservaResult CrearConServicios(BE_Reserva reserva, List<BE_ReservaServicio> servicios, out int nuevoId)
        {
            nuevoId = 0;
            var validacion = Validar(reserva, null);
            if (validacion != ReservaResult.Success) return validacion;

            reserva.Dvh = ValidadorDeIntegridad.CalcularDVH(reserva);
            nuevoId = DAL_Reserva.GuardarConServicios(reserva, servicios, esAlta: true);
            BLL_Integridad.RecalcularDVVerticalReservas();

            BLL_Bitacora.Registrar("Reservas", "Alta de reserva", CriticidadBitacora.Info,
                $"Reserva #{nuevoId} - cliente #{reserva.ClienteId}, monto {reserva.Monto:0.00}");
            return ReservaResult.Success;
        }

        public static ReservaResult ActualizarConServicios(BE_Reserva reserva, List<BE_ReservaServicio> servicios)
        {
            BE_Reserva antes = reserva.Id > 0 ? DAL_Reserva.GetById(reserva.Id) : null;
            if (antes == null) return ReservaResult.NotFound;

            var validacion = ValidarActualizacion(reserva, antes);
            if (validacion != ReservaResult.Success) return validacion;

            reserva.Dvh = ValidadorDeIntegridad.CalcularDVH(reserva);
            DAL_Reserva.GuardarConServicios(reserva, servicios, esAlta: false);
            BLL_Integridad.RecalcularDVVerticalReservas();

            RegistrarCambiosYBitacora(reserva, antes);
            return ReservaResult.Success;
        }

        // ---- Validaciones ----

        private static ReservaResult ValidarActualizacion(BE_Reserva reserva, BE_Reserva antes)
        {
            // Una reserva CANCELADA es un estado terminal: no se edita (ni se
            // "revive" a CONFIRMADA con un clic). Para volver a operar, se crea otra.
            if (antes.Estado == EstadoReserva.CANCELADA)
                return ReservaResult.NoEditable;

            var v = Validar(reserva, antes);
            if (v != ReservaResult.Success) return v;

            // No se puede bajar el monto por debajo de lo ya cobrado (dejaria saldo
            // negativo). El monto se deriva de los servicios: quitar servicios tras
            // cobrar queda bloqueado hasta anular pagos.
            if (reserva.Monto < DAL_Pago.TotalPagado(reserva.Id))
                return ReservaResult.MontoMenorQuePagado;

            return ReservaResult.Success;
        }

        // 'antes' == null indica alta. En edicion, la restriccion de "fecha no
        // pasada" solo aplica si la fecha CAMBIA: asi se puede editar/cancelar una
        // reserva cuyo evento ya paso sin verse forzado a alterar su fecha.
        private static ReservaResult Validar(BE_Reserva reserva, BE_Reserva antes)
        {
            if (reserva == null || reserva.ClienteId <= 0 || !DAL_Cliente.Exists(reserva.ClienteId))
                return ReservaResult.InvalidCliente;

            if (reserva.SalonId <= 0 || !DAL_Salon.Exists(reserva.SalonId))
                return ReservaResult.InvalidSalon;

            bool fechaCambia = antes == null || antes.FechaEvento.Date != reserva.FechaEvento.Date;
            if (reserva.FechaEvento == default ||
                (fechaCambia && reserva.FechaEvento.Date < DateTime.Today))
                return ReservaResult.InvalidFecha;

            if (reserva.Monto < 0)
                return ReservaResult.InvalidMonto;

            // Anti-solapamiento: el salon se compromete solo al CONFIRMAR. Se excluye
            // la propia reserva. (La base ademas lo garantiza con un indice unico
            // filtrado, como red de seguridad ante confirmaciones concurrentes.)
            if (reserva.Estado == EstadoReserva.CONFIRMADA &&
                DAL_Reserva.SalonOcupado(reserva.SalonId, reserva.FechaEvento, reserva.Id))
                return ReservaResult.SalonOcupado;

            return ReservaResult.Success;
        }

        private static void RegistrarCambiosYBitacora(BE_Reserva reserva, BE_Reserva antes)
        {
            int cambios = RegistradorDeCambios.RegistrarCambios("Reserva", reserva.Id, antes, reserva, CamposAuditados);
            BLL_Bitacora.Registrar("Reservas", "Modificacion de reserva", CriticidadBitacora.Info,
                $"Reserva #{reserva.Id} - {cambios} campo(s) modificado(s)");
        }
    }
}
