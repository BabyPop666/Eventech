using System.Collections.Generic;
using System.Linq;
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

        // Guarda los textos editados de un idioma y recarga el gestor en caliente.
        public static void GuardarTraducciones_704ILR(int idiomaId_704ILR, IDictionary<string, string> textos_704ILR)
        {
            foreach (var kv_704ILR in textos_704ILR)
                DAL_Idioma_704ILR.UpsertTraduccion_704ILR(idiomaId_704ILR, kv_704ILR.Key, kv_704ILR.Value);

            Inicializar_704ILR();
            BLL_Bitacora_704ILR.Registrar_704ILR("Idiomas", "Edicion de traducciones", CriticidadBitacora_704ILR.Info,
                $"Se actualizaron las traducciones del idioma #{idiomaId_704ILR}");
        }
    }
}
