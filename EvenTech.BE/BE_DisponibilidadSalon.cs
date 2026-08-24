using System;

namespace EvenTech.BE
{
    // Resultado de la consulta de disponibilidad (Proceso 1, paso 1): estado de
    // un salon para la fecha consultada y, si no sirve, la propuesta alternativa
    // (proxima fecha libre) para ofrecerle al cliente.
    public class BE_DisponibilidadSalon
    {
        public int SalonId { get; set; }
        public string SalonNombre { get; set; }
        public int Capacidad { get; set; }

        public DateTime FechaConsultada { get; set; }

        // El salon no tiene una reserva CONFIRMADA para la fecha consultada.
        public bool Libre { get; set; }

        // La capacidad del salon alcanza para los invitados estimados.
        public bool CapacidadSuficiente { get; set; }

        // Disponible = se puede reservar tal cual se pidio.
        public bool Disponible => Libre && CapacidadSuficiente;

        // Propuesta alternativa: primera fecha posterior sin reserva confirmada
        // (solo se calcula cuando el salon esta ocupado y la capacidad alcanza).
        public DateTime? ProximaFechaLibre { get; set; }
    }
}
