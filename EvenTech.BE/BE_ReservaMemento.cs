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
    public class BE_ReservaMemento
    {
        public int Id { get; }                 // id persistido del memento (0 hasta guardarse)
        public int ReservaId { get; }
        public int ClienteId { get; }
        public int SalonId { get; }
        public DateTime FechaEvento { get; }
        public EstadoReserva Estado { get; }
        public decimal Monto { get; }
        public string Usuario { get; }         // quien provoco el cambio que genero esta version
        public DateTime Fecha { get; }         // cuando se tomo la foto
        public string ClienteNombre { get; }   // proyectado en lecturas (JOIN), solo para mostrar
        public string SalonNombre { get; }     // proyectado en lecturas (JOIN), solo para mostrar
        public IReadOnlyList<BE_ReservaServicio> Servicios { get; }

        public BE_ReservaMemento(int id, int reservaId, int clienteId, int salonId,
            DateTime fechaEvento, EstadoReserva estado, decimal monto,
            string usuario, DateTime fecha,
            string clienteNombre = null, string salonNombre = null,
            IReadOnlyList<BE_ReservaServicio> servicios = null)
        {
            Id = id;
            ReservaId = reservaId;
            ClienteId = clienteId;
            SalonId = salonId;
            FechaEvento = fechaEvento;
            Estado = estado;
            Monto = monto;
            Usuario = usuario;
            Fecha = fecha;
            ClienteNombre = clienteNombre;
            SalonNombre = salonNombre;
            Servicios = servicios ?? new List<BE_ReservaServicio>();
        }
    }
}
