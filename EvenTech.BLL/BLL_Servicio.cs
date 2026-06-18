using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum ServicioResult
    {
        Success,
        NombreInvalido,
        NombreDuplicado,
        PrecioInvalido,
        NotFound
    }

    // Reglas de negocio del catalogo de servicios (Proceso 1).
    public static class BLL_Servicio
    {
        public static List<BE_Servicio> GetAll() => DAL_Servicio.GetAll();

        public static List<BE_Servicio> GetActivos() => DAL_Servicio.GetActivos();

        public static ServicioResult Crear(BE_Servicio s, out int nuevoId)
        {
            nuevoId = 0;
            var v = Validar(s, 0);
            if (v != ServicioResult.Success) return v;

            nuevoId = DAL_Servicio.Insert(s);
            BLL_Bitacora.Registrar("Servicios", "Alta de servicio", CriticidadBitacora.Info,
                $"Servicio '{s.Nombre}' creado (#{nuevoId})");
            return ServicioResult.Success;
        }

        public static ServicioResult Actualizar(BE_Servicio s)
        {
            if (s == null || s.Id <= 0 || !DAL_Servicio.Exists(s.Id)) return ServicioResult.NotFound;
            var v = Validar(s, s.Id);
            if (v != ServicioResult.Success) return v;

            DAL_Servicio.Update(s);
            BLL_Bitacora.Registrar("Servicios", "Modificacion de servicio", CriticidadBitacora.Info,
                $"Servicio #{s.Id} actualizado");
            return ServicioResult.Success;
        }

        private static ServicioResult Validar(BE_Servicio s, int idActual)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Nombre))
                return ServicioResult.NombreInvalido;
            if (s.Precio < 0)
                return ServicioResult.PrecioInvalido;
            if (DAL_Servicio.ExistsNombre(s.Nombre.Trim(), idActual))
                return ServicioResult.NombreDuplicado;
            return ServicioResult.Success;
        }
    }
}
