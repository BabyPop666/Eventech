using System;
using System.Collections.Generic;
using System.Globalization;
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
    public static class RegistradorDeCambios_704ILR
    {
        // Compara 'antes' contra 'despues' sobre los campos indicados (por nombre
        // de propiedad) y persiste una fila por cada diferencia detectada.
        // Devuelve la cantidad de campos modificados.
        public static int RegistrarCambios_704ILR(string entidad_704ILR, int entidadId_704ILR, object antes_704ILR, object despues_704ILR, params string[] campos_704ILR)
        {
            if (antes_704ILR == null || despues_704ILR == null) return 0;

            string usuario_704ILR = UsuarioActual_704ILR();
            DateTime ahora_704ILR = DateTime.Now;
            int cambios_704ILR = 0;
            Type tipo_704ILR = despues_704ILR.GetType();

            foreach (string campo_704ILR in campos_704ILR)
            {
                // 'campo' es el nombre LOGICO del dato (asi se persiste en
                // HistorialCambios y asi figuran las filas historicas); la
                // propiedad fisica lleva el sufijo de autoria, por eso se
                // resuelve probando primero el nombre logico y despues el
                // nombre sufijado.
                PropertyInfo pi_704ILR = tipo_704ILR.GetProperty(campo_704ILR, BindingFlags.Public | BindingFlags.Instance)
                    ?? tipo_704ILR.GetProperty(campo_704ILR + "_704ILR", BindingFlags.Public | BindingFlags.Instance);
                if (pi_704ILR == null) continue;

                string valorAntes_704ILR = Formatear_704ILR(pi_704ILR.GetValue(antes_704ILR));
                string valorDespues_704ILR = Formatear_704ILR(pi_704ILR.GetValue(despues_704ILR));

                if (!string.Equals(valorAntes_704ILR, valorDespues_704ILR, StringComparison.Ordinal))
                {
                    DAL_HistorialCambios_704ILR.Insert_704ILR(new BE_CambioEntry_704ILR
                    {
                        Entidad_704ILR = entidad_704ILR,
                        EntidadId_704ILR = entidadId_704ILR,
                        NombreCampo_704ILR = campo_704ILR,
                        ValorAnterior_704ILR = valorAntes_704ILR,
                        ValorNuevo_704ILR = valorDespues_704ILR,
                        Usuario_704ILR = usuario_704ILR,
                        Fecha_704ILR = ahora_704ILR
                    });
                    cambios_704ILR++;
                }
            }
            return cambios_704ILR;
        }

        public static List<BE_CambioEntry_704ILR> GetHistorial_704ILR(string entidad_704ILR, int entidadId_704ILR)
            => DAL_HistorialCambios_704ILR.GetByEntidad_704ILR(entidad_704ILR, entidadId_704ILR);

        // El valor formateado SE PERSISTE en HistorialCambios, asi que se escribe con
        // cultura invariante: si dependiera de la configuracion de la estacion, la
        // misma reserva quedaria historiada con "77000,00" o "77000.00" segun donde se
        // haya editado. Es el mismo criterio con el que se arma el digito verificador.
        // El formato para PANTALLA se resuelve aparte, en las grillas.
        private static string Formatear_704ILR(object valor_704ILR)
        {
            if (valor_704ILR == null) return null;
            if (valor_704ILR is DateTime dt_704ILR) return dt_704ILR.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            if (valor_704ILR is decimal dec_704ILR) return dec_704ILR.ToString("0.00", CultureInfo.InvariantCulture);
            return valor_704ILR.ToString();
        }

        private static string UsuarioActual_704ILR()
        {
            try
            {
                return SessionManager_704ILR.IsSessionActive_704ILR
                    ? SessionManager_704ILR.GetInstance_704ILR.User_704ILR.Username_704ILR
                    : "Sistema";
            }
            catch { return "Sistema"; }
        }
    }
}
