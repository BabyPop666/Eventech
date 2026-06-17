using System;

namespace EvenTech.BE
{
    public enum EstadoReserva
    {
        PENDIENTE,
        CONFIRMADA,
        CANCELADA
    }

    // Entidad central del dominio: reserva de un evento sobre un salon.
    // Es la entidad de negocio sensible elegida para control de cambios y
    // digitos verificadores (su monto/fecha/estado no deben alterarse por fuera
    // del sistema). El campo Dvh queda reservado para el modulo de integridad.
    public class BE_Reserva
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; }
        public int SalonId { get; set; }
        public string SalonNombre { get; set; }   // proyectado en lecturas (JOIN), no se persiste aca
        public DateTime FechaEvento { get; set; }
        public EstadoReserva Estado { get; set; }
        public decimal Monto { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Dvh { get; set; }            // digito verificador horizontal (se calcula en el modulo de integridad)
    }
}
