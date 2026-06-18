using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Servicios contratados por reserva (M:N). El monto de la reserva se deriva
    // de la suma de estos servicios (cantidad x precio).
    public static class BLL_ReservaServicio
    {
        public static List<BE_ReservaServicio> GetByReserva(int reservaId) =>
            DAL_ReservaServicio.GetByReserva(reservaId);

        public static void Guardar(int reservaId, IEnumerable<BE_ReservaServicio> items) =>
            DAL_ReservaServicio.ReplaceForReserva(reservaId, items ?? new List<BE_ReservaServicio>());

        // Total de una lista de servicios contratados (lo usa la UI y el alta de reserva).
        public static decimal Total(IEnumerable<BE_ReservaServicio> items) =>
            items == null ? 0m : items.Sum(i => i.Subtotal);
    }
}
