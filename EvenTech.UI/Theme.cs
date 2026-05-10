using System.Drawing;

namespace EvenTech.UI
{
    // Paleta y tipografia centralizadas. Modificar aca para repintar la app.
    internal static class Theme
    {
        // Fondos
        public static readonly Color BgLogin       = Color.FromArgb(48, 63, 105);   // ventana login
        public static readonly Color BgTitleBar    = Color.FromArgb(36, 43, 73);    // panel titulo
        public static readonly Color BgInput       = Color.FromArgb(80, 96, 130);   // textbox
        public static readonly Color BgMenu        = Color.FromArgb(34, 33, 31);    // menu lateral frmMain
        public static readonly Color BgContent     = Color.FromArgb(245, 245, 245); // panel central frmMain

        // Acentos
        public static readonly Color Accent        = Color.FromArgb(185, 160, 91);  // dorado
        public static readonly Color AccentButton  = Color.FromArgb(157, 112, 53);  // boton "Ingresar"

        // Texto
        public static readonly Color TextLight     = Color.Silver;
        public static readonly Color TextOnDark    = Color.White;
        public static readonly Color TextOnLight   = Color.FromArgb(34, 33, 31);

        // Tipografia. Ebrima viene con Windows; si falta, fallback a sans-serif por defecto.
        public static readonly Font FontTitle      = new Font("Ebrima", 14F, FontStyle.Bold);
        public static readonly Font FontLabel      = new Font("Ebrima", 12F, FontStyle.Regular);
        public static readonly Font FontInput      = new Font("Ebrima", 12F, FontStyle.Regular);
        public static readonly Font FontButton     = new Font("Ebrima", 12F, FontStyle.Regular);
        public static readonly Font FontMenu       = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
    }
}
