using System;

namespace EvenTech.BE
{
    // Criticidad de un evento de bitacora.
    public enum CriticidadBitacora : byte
    {
        Info = 1,
        Advertencia = 2,
        Error = 3
    }

    // Registro de la bitacora general del sistema: quien hizo que, cuando, en
    // que modulo y con que criticidad. A diferencia de la auditoria de login,
    // esta bitacora cubre cualquier operacion de negocio.
    public class BE_BitacoraEntry
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public CriticidadBitacora Criticidad { get; set; }
        public string Detalle { get; set; }
    }

    // Filtros opcionales para la busqueda combinada de la bitacora.
    // Cualquier propiedad en null/empty se ignora (no filtra por ese campo).
    public class BitacoraFiltros
    {
        public string Usuario { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public CriticidadBitacora? Criticidad { get; set; }
    }
}
