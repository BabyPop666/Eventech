using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Orquesta la carga de idiomas/traducciones desde la base hacia el
    // GestorDeIdioma (que vive en Services y no accede a datos). Se invoca una
    // vez al iniciar la aplicacion.
    public static class BLL_Idioma
    {
        public static void Inicializar()
        {
            var gestor = GestorDeIdioma.GetInstance;
            List<BE_Idioma> idiomas = DAL_Idioma.GetIdiomas();
            gestor.CargarIdiomas(idiomas);

            foreach (var idioma in idiomas)
                gestor.CargarTraducciones(idioma.Codigo, DAL_Idioma.GetTraducciones(idioma.Id));
        }

        public static List<BE_Idioma> GetIdiomas() => DAL_Idioma.GetIdiomas();
    }
}
