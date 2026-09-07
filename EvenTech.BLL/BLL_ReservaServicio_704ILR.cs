using System;
using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Servicios contratados por reserva (M:N). El monto de la reserva se deriva
    // de la suma de estos servicios (cantidad x precio); quien lo fija al guardar
    // es BLL_Reserva, a partir de Total_704ILR.
    public static class BLL_ReservaServicio_704ILR
    {
        public static List<BE_ReservaServicio_704ILR> GetByReserva_704ILR(int reservaId_704ILR) =>
            DAL_ReservaServicio_704ILR.GetByReserva_704ILR(reservaId_704ILR);

        // Reemplaza las lineas de una reserva sin tocar su cabecera. Las lineas se
        // validan igual que en el alta/modificacion de la reserva.
        public static void Guardar_704ILR(int reservaId_704ILR, IEnumerable<BE_ReservaServicio_704ILR> items_704ILR)
        {
            if (!ValidarLineas_704ILR(items_704ILR))
                throw new ArgumentException("Hay lineas con cantidad no positiva o precio negativo.", nameof(items_704ILR));
            DAL_ReservaServicio_704ILR.ReplaceForReserva_704ILR(reservaId_704ILR, items_704ILR ?? new List<BE_ReservaServicio_704ILR>());
        }

        // Total de una lista de servicios contratados (lo usa la UI y el alta de reserva).
        public static decimal Total_704ILR(IEnumerable<BE_ReservaServicio_704ILR> items_704ILR) =>
            items_704ILR == null ? 0m : items_704ILR.Sum(i_704ILR => i_704ILR.Subtotal_704ILR);

        // Toda linea tiene que tener cantidad positiva y precio no negativo: una
        // linea en cero o negativa no describe un servicio contratado y desfigura
        // el total. Una lista vacia o nula es valida (reserva sin servicios).
        public static bool ValidarLineas_704ILR(IEnumerable<BE_ReservaServicio_704ILR> items_704ILR) =>
            items_704ILR == null ||
            items_704ILR.All(i_704ILR => i_704ILR != null && i_704ILR.Cantidad_704ILR > 0 && i_704ILR.PrecioUnitario_704ILR >= 0m);

        // Compara dos composiciones por (servicio, cantidad, precio), sin mirar el
        // Id de linea: al guardar se recrean las filas y un Id nuevo no significa
        // que la composicion haya cambiado.
        public static bool MismasLineas_704ILR(IEnumerable<BE_ReservaServicio_704ILR> a_704ILR,
            IEnumerable<BE_ReservaServicio_704ILR> b_704ILR)
        {
            var la_704ILR = Canonica_704ILR(a_704ILR);
            var lb_704ILR = Canonica_704ILR(b_704ILR);
            if (la_704ILR.Count != lb_704ILR.Count) return false;
            for (int i_704ILR = 0; i_704ILR < la_704ILR.Count; i_704ILR++)
                if (!la_704ILR[i_704ILR].Equals(lb_704ILR[i_704ILR])) return false;
            return true;
        }

        private static List<(int servicio_704ILR, int cantidad_704ILR, decimal precio_704ILR)> Canonica_704ILR(
            IEnumerable<BE_ReservaServicio_704ILR> items_704ILR) =>
            (items_704ILR ?? Enumerable.Empty<BE_ReservaServicio_704ILR>())
                .Where(i_704ILR => i_704ILR != null)
                .Select(i_704ILR => (i_704ILR.ServicioId_704ILR, i_704ILR.Cantidad_704ILR, i_704ILR.PrecioUnitario_704ILR))
                .OrderBy(t_704ILR => t_704ILR.Item1).ThenBy(t_704ILR => t_704ILR.Item2).ThenBy(t_704ILR => t_704ILR.Item3)
                .ToList();
    }
}
