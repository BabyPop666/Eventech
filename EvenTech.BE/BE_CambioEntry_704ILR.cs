using System;

namespace EvenTech.BE
{
    // Registro de control de cambios (auditoria fina) sobre una entidad de
    // negocio. Cada fila representa la modificacion de UN campo: guarda el valor
    // anterior y el nuevo, permitiendo reconstruir el estado previo campo a campo.
    public class BE_CambioEntry_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Entidad_704ILR { get; set; }     // ej. "Reserva"
        public int EntidadId_704ILR { get; set; }       // PK de la entidad modificada
        public string NombreCampo_704ILR { get; set; }
        public string ValorAnterior_704ILR { get; set; }
        public string ValorNuevo_704ILR { get; set; }
        public string Usuario_704ILR { get; set; }
        public DateTime Fecha_704ILR { get; set; }
    }
}
