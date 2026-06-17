using System;
using System.Globalization;

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
    // del sistema). El campo Dvh guarda el digito verificador horizontal.
    public class BE_Reserva : IVerificable
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; }
        public int SalonId { get; set; }
        public string SalonNombre { get; set; }   // proyectado en lecturas (JOIN), no se persiste aca
        public DateTime FechaEvento { get; set; }
        public EstadoReserva Estado { get; set; }
        public decimal Monto { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Dvh { get; set; }            // digito verificador horizontal

        // Atributos de negocio que entran en el DV, en orden estable. La cultura
        // invariante evita que el formato de fecha/monto cambie el DV entre equipos.
        public string[] ObtenerCamposParaDV() => new[]
        {
            ClienteNombre ?? string.Empty,
            SalonId.ToString(CultureInfo.InvariantCulture),
            FechaEvento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Estado.ToString(),
            Monto.ToString("0.00", CultureInfo.InvariantCulture)
        };
    }
}
