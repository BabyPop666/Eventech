using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum IdiomaResult
    {
        Success,
        CodigoInvalido,
        NombreInvalido,
        CodigoDuplicado
    }

    // Orquesta la carga de idiomas/traducciones desde la base hacia el
    // GestorDeIdioma (que vive en Services y no accede a datos). Se invoca una
    // vez al iniciar la aplicacion y cada vez que el admin modifica idiomas.
    public static class BLL_Idioma
    {
        private const string CodigoPorDefecto = "ES";

        public static void Inicializar()
        {
            var gestor = GestorDeIdioma.GetInstance;
            List<BE_Idioma> idiomas = DAL_Idioma.GetIdiomas();
            gestor.CargarIdiomas(idiomas);

            foreach (var idioma in idiomas)
                gestor.CargarTraducciones(idioma.Codigo, DAL_Idioma.GetTraducciones(idioma.Id));
        }

        public static List<BE_Idioma> GetIdiomas() => DAL_Idioma.GetIdiomas();

        public static Dictionary<string, string> GetTraducciones(int idiomaId) => DAL_Idioma.GetTraducciones(idiomaId);

        // Alta de un idioma nuevo desde la interfaz. Inicializa sus leyendas
        // copiando las claves del idioma por defecto como punto de partida (el
        // admin luego puede editar cada texto). Recarga el gestor al terminar.
        public static IdiomaResult CrearIdioma(string codigo, string nombre, out int nuevoId)
        {
            nuevoId = 0;
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Trim().Length > 5)
                return IdiomaResult.CodigoInvalido;
            if (string.IsNullOrWhiteSpace(nombre))
                return IdiomaResult.NombreInvalido;

            codigo = codigo.Trim().ToUpperInvariant();
            if (DAL_Idioma.ExistsCodigo(codigo))
                return IdiomaResult.CodigoDuplicado;

            nuevoId = DAL_Idioma.InsertIdioma(codigo, nombre.Trim());

            // Copiar las claves del idioma por defecto como base inicial.
            var idiomas = DAL_Idioma.GetIdiomas();
            var baseIdioma = idiomas.FirstOrDefault(i => i.Codigo == CodigoPorDefecto) ?? idiomas.FirstOrDefault();
            if (baseIdioma != null)
            {
                foreach (var kv in DAL_Idioma.GetTraducciones(baseIdioma.Id))
                    DAL_Idioma.UpsertTraduccion(nuevoId, kv.Key, kv.Value);
            }

            Inicializar();
            BLL_Bitacora.Registrar("Idiomas", "Alta de idioma", CriticidadBitacora.Info,
                $"Idioma '{nombre}' ({codigo}) creado");
            return IdiomaResult.Success;
        }

        // Guarda los textos editados de un idioma y recarga el gestor en caliente.
        public static void GuardarTraducciones(int idiomaId, IDictionary<string, string> textos)
        {
            foreach (var kv in textos)
                DAL_Idioma.UpsertTraduccion(idiomaId, kv.Key, kv.Value);

            Inicializar();
            BLL_Bitacora.Registrar("Idiomas", "Edicion de traducciones", CriticidadBitacora.Info,
                $"Se actualizaron las traducciones del idioma #{idiomaId}");
        }
    }
}
