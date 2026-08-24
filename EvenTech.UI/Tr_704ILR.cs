using System.Windows.Forms;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Helper de traduccion para la UI. Cada control que deba traducirse lleva en
    // su Tag la cadena "T:CLAVE"; AplicarTags recorre el arbol de controles y les
    // asigna el texto del idioma activo. Asi no hay que promover cada label a
    // campo: un solo metodo traduce toda la vista (patron Observer -> ActualizarTextos).
    internal static class Tr_704ILR
    {
        public static string T_704ILR(string clave_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Traducir_704ILR(clave_704ILR);

        // Traduccion de valores de enumeraciones mostrados al usuario (grillas/combos).
        // La clave se arma por convencion PREFIJO_VALOR para no acoplar el enum a la UI.
        public static string Estado_704ILR(EvenTech.BE.EstadoReserva_704ILR e_704ILR) => T_704ILR("EST_" + e_704ILR.ToString());
        public static string Criticidad_704ILR(EvenTech.BE.CriticidadBitacora_704ILR c_704ILR) => T_704ILR("CRIT_" + c_704ILR.ToString().ToUpperInvariant());
        public static string Accion_704ILR(string codigo_704ILR) => T_704ILR("ACC_" + codigo_704ILR);

        // Asigna el Text traducido a todos los controles cuyo Tag sea "T:CLAVE".
        public static void AplicarTags_704ILR(Control root_704ILR)
        {
            foreach (Control c_704ILR in root_704ILR.Controls)
            {
                if (c_704ILR.Tag is string tag_704ILR)
                {
                    // "T:CLAVE": traduce el Text del propio control.
                    if (tag_704ILR.StartsWith("T:"))
                        c_704ILR.Text = T_704ILR(tag_704ILR.Substring(2));
                    // "FIELD:CLAVE": traduce el caption (primer Label hijo) de un Ui.Field.
                    else if (tag_704ILR.StartsWith("FIELD:"))
                    {
                        string clave_704ILR = tag_704ILR.Substring(6);
                        foreach (Control hijo_704ILR in c_704ILR.Controls)
                            if (hijo_704ILR is Label lbl_704ILR) { lbl_704ILR.Text = T_704ILR(clave_704ILR); break; }
                    }
                }
                if (c_704ILR.HasChildren)
                    AplicarTags_704ILR(c_704ILR);
            }
        }
    }
}
