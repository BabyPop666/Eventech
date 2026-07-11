using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvenTech.BE
{
    public enum EstadoReserva
    {
        COTIZACION,   // presupuesto: no compromete el salon (puede haber varios por fecha)
        PENDIENTE,    // reserva tentativa (p.ej. esperando senia): tampoco bloquea el salon
        CONFIRMADA,   // reserva firme: bloquea el salon para esa fecha (anti-solapamiento)
        CANCELADA
    }

    // Entidad central del dominio: reserva de un evento sobre un salon.
    // Es la entidad de negocio sensible elegida para control de cambios y
    // digitos verificadores (su monto/fecha/estado no deben alterarse por fuera
    // del sistema). El campo Dvh guarda el digito verificador horizontal.
    public class BE_Reserva : IVerificable
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; }  // proyectado en lecturas (JOIN), no se persiste aca
        public int SalonId { get; set; }
        public string SalonNombre { get; set; }    // proyectado en lecturas (JOIN), no se persiste aca
        public DateTime FechaEvento { get; set; }
        public EstadoReserva Estado { get; set; }
        public decimal Monto { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Dvh { get; set; }            // digito verificador horizontal

        // --- Patron Memento (rol Originator) ---

        // Crea la foto del estado actual. Los servicios se reciben de afuera
        // porque la entidad no accede a la DAL (los carga el Caretaker).
        public BE_ReservaMemento CrearMemento(string usuario, IReadOnlyList<BE_ReservaServicio> servicios) =>
            new BE_ReservaMemento(0, Id, ClienteId, SalonId, FechaEvento, Estado, Monto,
                usuario, DateTime.Now, ClienteNombre, SalonNombre, servicios);

        // Repone el estado de negocio guardado en el memento. Solo el Originator
        // conoce que campos componen su estado interno.
        public void RestaurarDesde(BE_ReservaMemento memento)
        {
            if (memento == null) return;
            ClienteId = memento.ClienteId;
            SalonId = memento.SalonId;
            FechaEvento = memento.FechaEvento;
            Estado = memento.Estado;
            Monto = memento.Monto;
        }

        // Atributos de negocio que entran en el DV, en orden estable. La cultura
        // invariante evita que el formato de fecha/monto cambie el DV entre equipos.
        public string[] ObtenerCamposParaDV() => new[]
        {
            ClienteId.ToString(CultureInfo.InvariantCulture),
            SalonId.ToString(CultureInfo.InvariantCulture),
            FechaEvento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Estado.ToString(),
            Monto.ToString("0.00", CultureInfo.InvariantCulture)
        };
    }
}
