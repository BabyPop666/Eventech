using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Servicios contratados por reserva (M:N). El monto de la reserva se deriva
    // de la suma de estos servicios (cantidad x precio).
    public static class BLL_ReservaServicio_704ILR
    {
        public static List<BE_ReservaServicio_704ILR> GetByReserva_704ILR(int reservaId_704ILR) =>
            DAL_ReservaServicio_704ILR.GetByReserva_704ILR(reservaId_704ILR);

        public static void Guardar_704ILR(int reservaId_704ILR, IEnumerable<BE_ReservaServicio_704ILR> items_704ILR) =>
            DAL_ReservaServicio_704ILR.ReplaceForReserva_704ILR(reservaId_704ILR, items_704ILR ?? new List<BE_ReservaServicio_704ILR>());

        // Total de una lista de servicios contratados (lo usa la UI y el alta de reserva).
        public static decimal Total_704ILR(IEnumerable<BE_ReservaServicio_704ILR> items_704ILR) =>
            items_704ILR == null ? 0m : items_704ILR.Sum(i_704ILR => i_704ILR.Subtotal_704ILR);
    }
}
