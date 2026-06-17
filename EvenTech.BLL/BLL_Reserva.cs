using System;
using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum ReservaResult
    {
        Success,
        InvalidCliente,
        InvalidSalon,
        InvalidFecha,
        InvalidMonto,
        NotFound
    }

    // Reglas de negocio de reservas: validacion de datos y estados antes de
    // delegar al DAL. Las validaciones viven aca (no en la UI ni en el DAL)
    // para mantener cohesion y permitir reuso desde otros frentes.
    public static class BLL_Reserva
    {
        public static List<BE_Reserva> GetAll() => DAL_Reserva.GetAll();

        public static BE_Reserva GetById(int id) => DAL_Reserva.GetById(id);

        public static ReservaResult Crear(BE_Reserva reserva, out int nuevoId)
        {
            nuevoId = 0;
            var validacion = Validar(reserva);
            if (validacion != ReservaResult.Success) return validacion;

            nuevoId = DAL_Reserva.Insert(reserva);
            return ReservaResult.Success;
        }

        public static ReservaResult Actualizar(BE_Reserva reserva)
        {
            if (reserva.Id <= 0 || DAL_Reserva.GetById(reserva.Id) == null)
                return ReservaResult.NotFound;

            var validacion = Validar(reserva);
            if (validacion != ReservaResult.Success) return validacion;

            DAL_Reserva.Update(reserva);
            return ReservaResult.Success;
        }

        private static ReservaResult Validar(BE_Reserva reserva)
        {
            if (reserva == null || string.IsNullOrWhiteSpace(reserva.ClienteNombre))
                return ReservaResult.InvalidCliente;

            if (reserva.SalonId <= 0 || !DAL_Salon.Exists(reserva.SalonId))
                return ReservaResult.InvalidSalon;

            // Una reserva nueva no puede agendarse en el pasado.
            if (reserva.FechaEvento == default || reserva.FechaEvento.Date < DateTime.Today)
                return ReservaResult.InvalidFecha;

            if (reserva.Monto < 0)
                return ReservaResult.InvalidMonto;

            return ReservaResult.Success;
        }
    }
}
