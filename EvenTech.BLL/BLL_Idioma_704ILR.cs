using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum IdiomaResult_704ILR
    {
        Success_704ILR,
        CodigoInvalido_704ILR,
        NombreInvalido_704ILR,
        CodigoDuplicado_704ILR
    }

    // Orquesta la carga de idiomas/traducciones desde la base hacia el
    // GestorDeIdioma (que vive en Services y no accede a datos). Se invoca una
    // vez al iniciar la aplicacion y cada vez que el admin modifica idiomas.
    public static class BLL_Idioma_704ILR
    {
        private const string CodigoPorDefecto_704ILR = "ES";

        public static void Inicializar_704ILR()
        {
            var gestor_704ILR = GestorDeIdioma_704ILR.GetInstance_704ILR;
            List<BE_Idioma_704ILR> idiomas_704ILR = DAL_Idioma_704ILR.GetIdiomas_704ILR();
            gestor_704ILR.CargarIdiomas_704ILR(idiomas_704ILR);

            foreach (var idioma_704ILR in idiomas_704ILR)
                gestor_704ILR.CargarTraducciones_704ILR(idioma_704ILR.Codigo_704ILR, DAL_Idioma_704ILR.GetTraducciones_704ILR(idioma_704ILR.Id_704ILR));
        }

        public static List<BE_Idioma_704ILR> GetIdiomas_704ILR() => DAL_Idioma_704ILR.GetIdiomas_704ILR();

        public static Dictionary<string, string> GetTraducciones_704ILR(int idiomaId_704ILR) => DAL_Idioma_704ILR.GetTraducciones_704ILR(idiomaId_704ILR);

        // Alta de un idioma nuevo desde la interfaz. Inicializa sus leyendas
        // copiando las claves del idioma por defecto como punto de partida (el
        // admin luego puede editar cada texto). Recarga el gestor al terminar.
        public static IdiomaResult_704ILR CrearIdioma_704ILR(string codigo_704ILR, string nombre_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            if (string.IsNullOrWhiteSpace(codigo_704ILR) || codigo_704ILR.Trim().Length > 5)
                return IdiomaResult_704ILR.CodigoInvalido_704ILR;
            if (string.IsNullOrWhiteSpace(nombre_704ILR))
                return IdiomaResult_704ILR.NombreInvalido_704ILR;

            codigo_704ILR = codigo_704ILR.Trim().ToUpperInvariant();
            if (DAL_Idioma_704ILR.ExistsCodigo_704ILR(codigo_704ILR))
                return IdiomaResult_704ILR.CodigoDuplicado_704ILR;

            nuevoId_704ILR = DAL_Idioma_704ILR.InsertIdioma_704ILR(codigo_704ILR, nombre_704ILR.Trim());

            // Copiar las claves del idioma por defecto como base inicial.
            var idiomas_704ILR = DAL_Idioma_704ILR.GetIdiomas_704ILR();
            var baseIdioma_704ILR = idiomas_704ILR.FirstOrDefault(i_704ILR => i_704ILR.Codigo_704ILR == CodigoPorDefecto_704ILR) ?? idiomas_704ILR.FirstOrDefault();
            if (baseIdioma_704ILR != null)
            {
                foreach (var kv_704ILR in DAL_Idioma_704ILR.GetTraducciones_704ILR(baseIdioma_704ILR.Id_704ILR))
                    DAL_Idioma_704ILR.UpsertTraduccion_704ILR(nuevoId_704ILR, kv_704ILR.Key, kv_704ILR.Value);
            }

            Inicializar_704ILR();
            BLL_Bitacora_704ILR.Registrar_704ILR("Idiomas", "Alta de idioma", CriticidadBitacora_704ILR.Info,
                $"Idioma '{nombre_704ILR}' ({codigo_704ILR}) creado");
            return IdiomaResult_704ILR.Success_704ILR;
        }

        // Marcadores {n} de una plantilla ("{0}", "{1:N2}"); las llaves escapadas
        // "{{" se quitan antes de buscar.
        private static readonly Regex Marcador_704ILR = new Regex(@"\{(\d+)", RegexOptions.Compiled);

        // Un texto editado solo puede usar los marcadores {n} que ya tiene el texto
        // de fabrica de la misma clave (el del idioma por defecto) y sus llaves
        // tienen que cerrar: string.Format con una plantilla rota lanza en la
        // pantalla que la usa, no en el editor.
        public static bool PlantillaValida_704ILR(string textoFabrica_704ILR, string textoNuevo_704ILR)
        {
            if (string.IsNullOrEmpty(textoNuevo_704ILR)) return true;

            var permitidos_704ILR = new HashSet<int>();
            foreach (Match m_704ILR in Marcador_704ILR.Matches((textoFabrica_704ILR ?? "").Replace("{{", "")))
                permitidos_704ILR.Add(int.Parse(m_704ILR.Groups[1].Value));
            foreach (Match m_704ILR in Marcador_704ILR.Matches(textoNuevo_704ILR.Replace("{{", "")))
                if (!permitidos_704ILR.Contains(int.Parse(m_704ILR.Groups[1].Value))) return false;

            int cantidad_704ILR = permitidos_704ILR.Count == 0 ? 0 : permitidos_704ILR.Max() + 1;
            try { string.Format(textoNuevo_704ILR, new object[cantidad_704ILR]); return true; }
            catch (FormatException) { return false; }
        }

        // Primera clave del lote cuyo texto no respeta la plantilla de fabrica, o
        // null si todas son validas.
        public static string PrimeraPlantillaInvalida_704ILR(IDictionary<string, string> textos_704ILR)
        {
            var idiomas_704ILR = DAL_Idioma_704ILR.GetIdiomas_704ILR();
            var baseIdioma_704ILR = idiomas_704ILR.FirstOrDefault(i_704ILR => i_704ILR.Codigo_704ILR == CodigoPorDefecto_704ILR);
            var fabrica_704ILR = baseIdioma_704ILR == null
                ? new Dictionary<string, string>()
                : DAL_Idioma_704ILR.GetTraducciones_704ILR(baseIdioma_704ILR.Id_704ILR);

            foreach (var kv_704ILR in textos_704ILR)
            {
                fabrica_704ILR.TryGetValue(kv_704ILR.Key, out string textoFabrica_704ILR);
                if (!PlantillaValida_704ILR(textoFabrica_704ILR, kv_704ILR.Value)) return kv_704ILR.Key;
            }
            return null;
        }

        // Guarda los textos editados de un idioma y recarga el gestor en caliente.
        // Un lote con una plantilla invalida se rechaza entero, sin guardar nada.
        public static void GuardarTraducciones_704ILR(int idiomaId_704ILR, IDictionary<string, string> textos_704ILR)
        {
            string claveInvalida_704ILR = PrimeraPlantillaInvalida_704ILR(textos_704ILR);
            if (claveInvalida_704ILR != null)
                throw new ArgumentException("La traduccion de '" + claveInvalida_704ILR + "' tiene llaves sin cerrar o marcadores que la clave no admite.");

            foreach (var kv_704ILR in textos_704ILR)
                DAL_Idioma_704ILR.UpsertTraduccion_704ILR(idiomaId_704ILR, kv_704ILR.Key, kv_704ILR.Value);

            Inicializar_704ILR();
            BLL_Bitacora_704ILR.Registrar_704ILR("Idiomas", "Edicion de traducciones", CriticidadBitacora_704ILR.Info,
                $"Se actualizaron las traducciones del idioma #{idiomaId_704ILR}");
        }
    }
}
