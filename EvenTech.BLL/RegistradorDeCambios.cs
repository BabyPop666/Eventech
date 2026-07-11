using System;
using System.Collections.Generic;
using System.Reflection;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Control de cambios generico (T06b). Compara el estado anterior y posterior
    // de una entidad campo por campo y genera un registro de HistorialCambios por
    // cada campo que cambio. Centralizar esta logica evita repetir comparaciones
    // en cada formulario (cohesion y reuso).
    public static class RegistradorDeCambios
    {
        // Compara 'antes' contra 'despues' sobre los campos indicados (por nombre
        // de propiedad) y persiste una fila por cada diferencia detectada.
        // Devuelve la cantidad de campos modificados.
        public static int RegistrarCambios(string entidad, int entidadId, object antes, object despues, params string[] campos)
        {
            if (antes == null || despues == null) return 0;

            string usuario = UsuarioActual();
            DateTime ahora = DateTime.Now;
            int cambios = 0;
            Type tipo = despues.GetType();

            foreach (string campo in campos)
            {
                PropertyInfo pi = tipo.GetProperty(campo, BindingFlags.Public | BindingFlags.Instance);
                if (pi == null) continue;

                string valorAntes = Formatear(pi.GetValue(antes));
                string valorDespues = Formatear(pi.GetValue(despues));

                if (!string.Equals(valorAntes, valorDespues, StringComparison.Ordinal))
                {
                    DAL_HistorialCambios.Insert(new BE_CambioEntry
                    {
                        Entidad = entidad,
                        EntidadId = entidadId,
                        NombreCampo = campo,
                        ValorAnterior = valorAntes,
                        ValorNuevo = valorDespues,
                        Usuario = usuario,
                        Fecha = ahora
                    });
                    cambios++;
                }
            }
            return cambios;
        }

        public static List<BE_CambioEntry> GetHistorial(string entidad, int entidadId)
            => DAL_HistorialCambios.GetByEntidad(entidad, entidadId);

        private static string Formatear(object valor)
        {
            if (valor == null) return null;
            if (valor is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm");
            if (valor is decimal dec) return dec.ToString("0.00");
            return valor.ToString();
        }

        private static string UsuarioActual()
        {
            try
            {
                return SessionManager.IsSessionActive
                    ? SessionManager.GetInstance.User.Username
                    : "Sistema";
            }
            catch { return "Sistema"; }
        }
    }
}
