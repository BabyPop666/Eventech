using System;

namespace EvenTech.BE
{
    // Resultado de la consulta de disponibilidad (Proceso 1, paso 1): estado de
    // un salon para la fecha consultada y, si no sirve, la propuesta alternativa
    // (proxima fecha libre) para ofrecerle al cliente.
    public class BE_DisponibilidadSalon_704ILR
    {
        public int SalonId_704ILR { get; set; }
        public string SalonNombre_704ILR { get; set; }
        public int Capacidad_704ILR { get; set; }

        public DateTime FechaConsultada_704ILR { get; set; }

        // El salon no tiene una reserva CONFIRMADA para la fecha consultada.
        public bool Libre_704ILR { get; set; }

        // La capacidad del salon alcanza para los invitados estimados.
        public bool CapacidadSuficiente_704ILR { get; set; }

        // Disponible = se puede reservar tal cual se pidio.
        public bool Disponible_704ILR => Libre_704ILR && CapacidadSuficiente_704ILR;

        // Propuesta alternativa: primera fecha posterior sin reserva confirmada
        // (solo se calcula cuando el salon esta ocupado y la capacidad alcanza).
        public DateTime? ProximaFechaLibre_704ILR { get; set; }
    }
}
