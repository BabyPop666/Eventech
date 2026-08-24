using System;
using System.Collections.Generic;

namespace EvenTech.BE
{
    // Memento (patron Memento): foto inmutable del estado de negocio de una
    // reserva en un momento dado, incluyendo sus servicios contratados (el monto
    // se deriva de ellos, asi que restaurar uno sin el otro dejaria datos
    // inconsistentes). Solo el Originator (BE_Reserva) crea mementos y sabe
    // restaurarse desde uno; el Caretaker (CaretakerReserva en la BLL) los
    // guarda y devuelve sin modificarlos.
    public class BE_ReservaMemento_704ILR
    {
        public int Id_704ILR { get; }                 // id persistido del memento (0 hasta guardarse)
        public int ReservaId_704ILR { get; }
        public int ClienteId_704ILR { get; }
        public int SalonId_704ILR { get; }
        public DateTime FechaEvento_704ILR { get; }
        public EstadoReserva_704ILR Estado_704ILR { get; }
        public decimal Monto_704ILR { get; }
        public string Usuario_704ILR { get; }         // quien provoco el cambio que genero esta version
        public DateTime Fecha_704ILR { get; }         // cuando se tomo la foto
        public string ClienteNombre_704ILR { get; }   // proyectado en lecturas (JOIN), solo para mostrar
        public string SalonNombre_704ILR { get; }     // proyectado en lecturas (JOIN), solo para mostrar
        public IReadOnlyList<BE_ReservaServicio_704ILR> Servicios_704ILR { get; }

        public BE_ReservaMemento_704ILR(int id_704ILR, int reservaId_704ILR, int clienteId_704ILR, int salonId_704ILR,
            DateTime fechaEvento_704ILR, EstadoReserva_704ILR estado_704ILR, decimal monto_704ILR,
            string usuario_704ILR, DateTime fecha_704ILR,
            string clienteNombre_704ILR = null, string salonNombre_704ILR = null,
            IReadOnlyList<BE_ReservaServicio_704ILR> servicios_704ILR = null)
        {
            Id_704ILR = id_704ILR;
            ReservaId_704ILR = reservaId_704ILR;
            ClienteId_704ILR = clienteId_704ILR;
            SalonId_704ILR = salonId_704ILR;
            FechaEvento_704ILR = fechaEvento_704ILR;
            Estado_704ILR = estado_704ILR;
            Monto_704ILR = monto_704ILR;
            Usuario_704ILR = usuario_704ILR;
            Fecha_704ILR = fecha_704ILR;
            ClienteNombre_704ILR = clienteNombre_704ILR;
            SalonNombre_704ILR = salonNombre_704ILR;
            Servicios_704ILR = servicios_704ILR ?? new List<BE_ReservaServicio_704ILR>();
        }
    }
}
