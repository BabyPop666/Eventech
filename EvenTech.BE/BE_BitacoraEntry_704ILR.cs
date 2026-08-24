using System;

namespace EvenTech.BE
{
    // Criticidad de un evento de bitacora.
    public enum CriticidadBitacora_704ILR : byte
    {
        Info = 1,
        Advertencia = 2,
        Error = 3
    }

    // Registro de la bitacora general del sistema: quien hizo que, cuando, en
    // que modulo y con que criticidad. A diferencia de la auditoria de login,
    // esta bitacora cubre cualquier operacion de negocio.
    public class BE_BitacoraEntry_704ILR
    {
        public int Id_704ILR { get; set; }
        public DateTime Fecha_704ILR { get; set; }
        public string Usuario_704ILR { get; set; }
        public string Modulo_704ILR { get; set; }
        public string Accion_704ILR { get; set; }
        public CriticidadBitacora_704ILR Criticidad_704ILR { get; set; }
        public string Detalle_704ILR { get; set; }
    }

    // Filtros opcionales para la busqueda combinada de la bitacora.
    // Cualquier propiedad en null/empty se ignora (no filtra por ese campo).
    public class BitacoraFiltros_704ILR
    {
        public string Usuario_704ILR { get; set; }
        public DateTime? FechaInicio_704ILR { get; set; }
        public DateTime? FechaFin_704ILR { get; set; }
        public string Modulo_704ILR { get; set; }
        public string Accion_704ILR { get; set; }
        public CriticidadBitacora_704ILR? Criticidad_704ILR { get; set; }
    }
}
