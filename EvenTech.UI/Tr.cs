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
                if (c.Tag is string tag)
                {
                    // "T:CLAVE": traduce el Text del propio control.
                    if (tag.StartsWith("T:"))
                        c.Text = T(tag.Substring(2));
                    // "FIELD:CLAVE": traduce el caption (primer Label hijo) de un Ui.Field.
                    else if (tag.StartsWith("FIELD:"))
                    {
                        string clave = tag.Substring(6);
                        foreach (Control hijo in c.Controls)
                            if (hijo is Label lbl) { lbl.Text = T(clave); break; }
                    }
                }
                if (c.HasChildren)
                    AplicarTags(c);
            }
        }
    }
}
