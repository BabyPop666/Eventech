using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum ServicioResult_704ILR
    {
        Success,
        NombreInvalido,
        NombreDuplicado,
        PrecioInvalido,
        NotFound
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
            if (v_704ILR != ServicioResult_704ILR.Success) return v_704ILR;

            nuevoId_704ILR = DAL_Servicio_704ILR.Insert_704ILR(s_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Servicios", "Alta de servicio", CriticidadBitacora_704ILR.Info,
                $"Servicio '{s_704ILR.Nombre_704ILR}' creado (#{nuevoId_704ILR})");
            return ServicioResult_704ILR.Success;
        }

        public static ServicioResult_704ILR Actualizar_704ILR(BE_Servicio_704ILR s_704ILR)
        {
            if (s_704ILR == null || s_704ILR.Id_704ILR <= 0 || !DAL_Servicio_704ILR.Exists_704ILR(s_704ILR.Id_704ILR)) return ServicioResult_704ILR.NotFound;
            var v_704ILR = Validar_704ILR(s_704ILR, s_704ILR.Id_704ILR);
            if (v_704ILR != ServicioResult_704ILR.Success) return v_704ILR;

            DAL_Servicio_704ILR.Update_704ILR(s_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Servicios", "Modificacion de servicio", CriticidadBitacora_704ILR.Info,
                $"Servicio #{s_704ILR.Id_704ILR} actualizado");
            return ServicioResult_704ILR.Success;
        }

        private static ServicioResult_704ILR Validar_704ILR(BE_Servicio_704ILR s_704ILR, int idActual_704ILR)
        {
            if (s_704ILR == null || string.IsNullOrWhiteSpace(s_704ILR.Nombre_704ILR))
                return ServicioResult_704ILR.NombreInvalido;
            if (s_704ILR.Precio_704ILR < 0)
                return ServicioResult_704ILR.PrecioInvalido;
            if (DAL_Servicio_704ILR.ExistsNombre_704ILR(s_704ILR.Nombre_704ILR.Trim(), idActual_704ILR))
                return ServicioResult_704ILR.NombreDuplicado;
            return ServicioResult_704ILR.Success;
        }
    }
}
