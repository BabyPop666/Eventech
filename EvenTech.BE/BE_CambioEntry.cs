using System;

namespace EvenTech.BE
{
    // Registro de control de cambios (auditoria fina) sobre una entidad de
    // negocio. Cada fila representa la modificacion de UN campo: guarda el valor
    // anterior y el nuevo, permitiendo reconstruir el estado previo campo a campo.
    public class BE_CambioEntry
    {
        public int Id { get; set; }
        public string Entidad { get; set; }     // ej. "Reserva"
        public int EntidadId { get; set; }       // PK de la entidad modificada
        public string NombreCampo { get; set; }
        public string ValorAnterior { get; set; }
        public string ValorNuevo { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha { get; set; }
    }
}
