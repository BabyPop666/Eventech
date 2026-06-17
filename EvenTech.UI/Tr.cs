using System.Windows.Forms;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Helper de traduccion para la UI. Cada control que deba traducirse lleva en
    // su Tag la cadena "T:CLAVE"; AplicarTags recorre el arbol de controles y les
    // asigna el texto del idioma activo. Asi no hay que promover cada label a
    // campo: un solo metodo traduce toda la vista (patron Observer -> ActualizarTextos).
    internal static class Tr
    {
        public static string T(string clave) => GestorDeIdioma.GetInstance.Traducir(clave);

        // Asigna el Text traducido a todos los controles cuyo Tag sea "T:CLAVE".
        public static void AplicarTags(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c.Tag is string tag && tag.StartsWith("T:"))
                    c.Text = T(tag.Substring(2));
                if (c.HasChildren)
                    AplicarTags(c);
            }
        }
    }
}
