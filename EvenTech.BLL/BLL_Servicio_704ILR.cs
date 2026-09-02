using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum ServicioResult_704ILR
    {
        Success_704ILR,
        NombreInvalido_704ILR,
        NombreDuplicado_704ILR,
        PrecioInvalido_704ILR,
        NotFound_704ILR
    }

    // Reglas de negocio del catalogo de servicios (Proceso 1).
    public static class BLL_Servicio_704ILR
    {
        public static List<BE_Servicio_704ILR> GetAll_704ILR() => DAL_Servicio_704ILR.GetAll_704ILR();

        public static List<BE_Servicio_704ILR> GetActivos_704ILR() => DAL_Servicio_704ILR.GetActivos_704ILR();

        public static ServicioResult_704ILR Crear_704ILR(BE_Servicio_704ILR s_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            var v_704ILR = Validar_704ILR(s_704ILR, 0);
            if (v_704ILR != ServicioResult_704ILR.Success_704ILR) return v_704ILR;

            nuevoId_704ILR = DAL_Servicio_704ILR.Insert_704ILR(s_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Servicios", "Alta de servicio", CriticidadBitacora_704ILR.Info,
                $"Servicio '{s_704ILR.Nombre_704ILR}' creado (#{nuevoId_704ILR})");
            return ServicioResult_704ILR.Success_704ILR;
        }

        public static ServicioResult_704ILR Actualizar_704ILR(BE_Servicio_704ILR s_704ILR)
        {
            if (s_704ILR == null || s_704ILR.Id_704ILR <= 0 || !DAL_Servicio_704ILR.Exists_704ILR(s_704ILR.Id_704ILR)) return ServicioResult_704ILR.NotFound_704ILR;
            var v_704ILR = Validar_704ILR(s_704ILR, s_704ILR.Id_704ILR);
            if (v_704ILR != ServicioResult_704ILR.Success_704ILR) return v_704ILR;

            DAL_Servicio_704ILR.Update_704ILR(s_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Servicios", "Modificacion de servicio", CriticidadBitacora_704ILR.Info,
                $"Servicio #{s_704ILR.Id_704ILR} actualizado");
            return ServicioResult_704ILR.Success_704ILR;
        }

        // Anchos de dbo.Servicios. La capa de datos manda los parametros con ese
        // tamano fijo, de modo que un texto mas largo se guardaria recortado sin aviso:
        // la regla se hace explicita aca (nombre demasiado largo = nombre invalido) y
        // la descripcion —dato accesorio— se recorta a lo que entra.
        private const int MaxNombre_704ILR = 80;
        private const int MaxDescripcion_704ILR = 250;

        private static ServicioResult_704ILR Validar_704ILR(BE_Servicio_704ILR s_704ILR, int idActual_704ILR)
        {
            if (s_704ILR == null || string.IsNullOrWhiteSpace(s_704ILR.Nombre_704ILR))
                return ServicioResult_704ILR.NombreInvalido_704ILR;
            if (s_704ILR.Nombre_704ILR.Trim().Length > MaxNombre_704ILR)
                return ServicioResult_704ILR.NombreInvalido_704ILR;
            if (s_704ILR.Descripcion_704ILR != null && s_704ILR.Descripcion_704ILR.Trim().Length > MaxDescripcion_704ILR)
                s_704ILR.Descripcion_704ILR = s_704ILR.Descripcion_704ILR.Trim().Substring(0, MaxDescripcion_704ILR);
            if (s_704ILR.Precio_704ILR < 0)
                return ServicioResult_704ILR.PrecioInvalido_704ILR;
            if (DAL_Servicio_704ILR.ExistsNombre_704ILR(s_704ILR.Nombre_704ILR.Trim(), idActual_704ILR))
                return ServicioResult_704ILR.NombreDuplicado_704ILR;
            return ServicioResult_704ILR.Success_704ILR;
        }
    }
}
