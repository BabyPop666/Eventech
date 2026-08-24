using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvenTech.BE
{
    public enum EstadoReserva_704ILR
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
    public class BE_Reserva_704ILR : IVerificable_704ILR
    {
        public int Id_704ILR { get; set; }
        public int ClienteId_704ILR { get; set; }
        public string ClienteNombre_704ILR { get; set; }  // proyectado en lecturas (JOIN), no se persiste aca
        public int SalonId_704ILR { get; set; }
        public string SalonNombre_704ILR { get; set; }    // proyectado en lecturas (JOIN), no se persiste aca
        public DateTime FechaEvento_704ILR { get; set; }
        public EstadoReserva_704ILR Estado_704ILR { get; set; }
        public decimal Monto_704ILR { get; set; }
        public DateTime CreatedAt_704ILR { get; set; }
        public string Dvh_704ILR { get; set; }            // digito verificador horizontal

        // --- Patron Memento (rol Originator) ---

        // Crea la foto del estado actual. Los servicios se reciben de afuera
        // porque la entidad no accede a la DAL (los carga el Caretaker).
        public BE_ReservaMemento_704ILR CrearMemento_704ILR(string usuario_704ILR, IReadOnlyList<BE_ReservaServicio_704ILR> servicios_704ILR) =>
            new BE_ReservaMemento_704ILR(0, Id_704ILR, ClienteId_704ILR, SalonId_704ILR, FechaEvento_704ILR, Estado_704ILR, Monto_704ILR,
                usuario_704ILR, DateTime.Now, ClienteNombre_704ILR, SalonNombre_704ILR, servicios_704ILR);

        // Repone el estado de negocio guardado en el memento. Solo el Originator
        // conoce que campos componen su estado interno.
        public void RestaurarDesde_704ILR(BE_ReservaMemento_704ILR memento_704ILR)
        {
            if (memento_704ILR == null) return;
            ClienteId_704ILR = memento_704ILR.ClienteId_704ILR;
            SalonId_704ILR = memento_704ILR.SalonId_704ILR;
            FechaEvento_704ILR = memento_704ILR.FechaEvento_704ILR;
            Estado_704ILR = memento_704ILR.Estado_704ILR;
            Monto_704ILR = memento_704ILR.Monto_704ILR;
        }

        // Atributos de negocio que entran en el DV, en orden estable. La cultura
        // invariante evita que el formato de fecha/monto cambie el DV entre equipos.
        public string[] ObtenerCamposParaDV_704ILR() => new[]
        {
            ClienteId_704ILR.ToString(CultureInfo.InvariantCulture),
            SalonId_704ILR.ToString(CultureInfo.InvariantCulture),
            FechaEvento_704ILR.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Estado_704ILR.ToString(),
            Monto_704ILR.ToString("0.00", CultureInfo.InvariantCulture)
        };
    }
}
