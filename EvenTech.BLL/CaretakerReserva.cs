using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Caretaker (patron Memento): guarda y devuelve las versiones (mementos) de
    // una reserva sin interpretar su contenido. La decision de que entra en la
    // foto y como se repone es exclusiva del Originator (BE_Reserva).
    public static class CaretakerReserva
    {
        // Toma la foto del estado actual de la reserva (incluye sus servicios
        // contratados, porque el monto se deriva de ellos) y la persiste.
        public static void GuardarVersion(BE_Reserva reserva)
        {
            List<BE_ReservaServicio> servicios = DAL_ReservaServicio.GetByReserva(reserva.Id);
            BE_ReservaMemento memento = reserva.CrearMemento(UsuarioActual(), servicios);
            DAL_ReservaMemento.Insert(memento);
        }

        public static List<BE_ReservaMemento> GetVersiones(int reservaId) =>
            DAL_ReservaMemento.GetByReserva(reservaId);

        public static BE_ReservaMemento GetVersion(int mementoId) =>
            DAL_ReservaMemento.GetById(mementoId);

        private static string UsuarioActual()
        {
            try
            {
                return SessionManager.IsSessionActive
                    ? SessionManager.GetInstance.User.Username
                    : "Sistema";
            }
            catch { return "Sistema"; }
        }
    }
}
