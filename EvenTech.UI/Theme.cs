using System.Drawing;

namespace EvenTech.UI
{
    // Paleta, tipografia, espaciado e iconos centralizados (design tokens).
    // Modificar aca para repintar toda la app. Mantiene la identidad de marca
    // (azul oscuro / dorado / silver) y agrega tokens semanticos para superficies,
    // estados y grillas, de modo que ninguna vista hardcodee colores o fuentes.
    internal static class Theme
    {
        // ===================== Superficies oscuras (marca) =====================
        public static readonly Color BgLogin       = Color.FromArgb(48, 63, 105);   // ventana login
        public static readonly Color BgTitleBar    = Color.FromArgb(36, 43, 73);    // barra titulo / topbar / header de grilla
        public static readonly Color BgInput       = Color.FromArgb(80, 96, 130);   // inputs sobre fondo oscuro (login)
        public static readonly Color BgSidebar     = Color.FromArgb(34, 33, 31);    // menu lateral
        public static readonly Color BgMenu        = Color.FromArgb(34, 33, 31);    // (alias retrocompat de BgSidebar)
        public static readonly Color SidebarHover  = Color.FromArgb(50, 49, 47);
        public static readonly Color SidebarActive = Color.FromArgb(45, 44, 42);

        // ===================== Superficies claras (contenido) =====================
        public static readonly Color BgContent  = Color.FromArgb(244, 245, 248);    // panel central
        public static readonly Color Surface    = Color.White;                      // tarjetas / paneles
        public static readonly Color SurfaceAlt = Color.FromArgb(246, 247, 249);    // zebra / paneles sutiles
        public static readonly Color Border     = Color.FromArgb(223, 227, 233);    // bordes de tarjeta / input

        // ===================== Acentos (dorado) =====================
        public static readonly Color Accent            = Color.FromArgb(185, 160, 91);
        public static readonly Color AccentButton      = Color.FromArgb(157, 112, 53);
        public static readonly Color AccentButtonHover = Color.FromArgb(184, 134, 67);
        public static readonly Color AccentButtonDown  = Color.FromArgb(126, 89, 41);

        // ===================== Texto =====================
        public static readonly Color TextOnDark  = Color.White;
        public static readonly Color TextLight   = Color.FromArgb(206, 212, 222);   // texto secundario sobre oscuro
        public static readonly Color TextOnLight = Color.FromArgb(33, 37, 41);
        public static readonly Color TextMuted   = Color.FromArgb(108, 117, 125);   // hints / captions sobre claro

        // ===================== Estados semanticos =====================
        public static readonly Color Success = Color.FromArgb(33, 136, 56);
        public static readonly Color Error   = Color.FromArgb(190, 49, 68);
        public static readonly Color Warning = Color.FromArgb(181, 132, 26);
        public static readonly Color Info    = Color.FromArgb(48, 63, 105);

        // ===================== Botones neutros (secundarios) =====================
        public static readonly Color Neutral      = Color.FromArgb(108, 117, 125);
        public static readonly Color NeutralHover = Color.FromArgb(127, 135, 143);
        public static readonly Color NeutralDown  = Color.FromArgb(84, 91, 98);

        // ===================== Grilla =====================
        public static readonly Color GridHeaderBg = Color.FromArgb(36, 43, 73);
        public static readonly Color GridHeaderFg = Color.White;
        public static readonly Color GridZebra    = Color.FromArgb(246, 247, 249);
        public static readonly Color GridSelectBg = Color.FromArgb(238, 230, 207);  // dorado claro (en vez del azul default)
        public static readonly Color GridSelectFg = Color.FromArgb(33, 37, 41);
        public static readonly Color GridLines    = Color.FromArgb(233, 236, 240);

        // ===================== Tipografia =====================
        // Ebrima viene con Windows; si falta, WinForms cae al sans-serif por defecto.
        private const string Family = "Ebrima";
        public static readonly Font FontDisplay  = new Font(Family, 22F, FontStyle.Bold);   // logo
        public static readonly Font FontH1       = new Font(Family, 18F, FontStyle.Bold);   // titulo de pagina
        public static readonly Font FontH2       = new Font(Family, 14F, FontStyle.Bold);   // titulo de seccion / tarjeta
        public static readonly Font FontTitle    = new Font(Family, 13F, FontStyle.Bold);
        public static readonly Font FontBody     = new Font(Family, 11F, FontStyle.Regular);
        public static readonly Font FontBodyBold = new Font(Family, 11F, FontStyle.Bold);
        public static readonly Font FontLabel    = new Font(Family, 11F, FontStyle.Regular); // (retrocompat)
        public static readonly Font FontInput    = new Font(Family, 11F, FontStyle.Regular);
        public static readonly Font FontButton   = new Font(Family, 10.5F, FontStyle.Bold);
        public static readonly Font FontSmall    = new Font(Family, 9.5F, FontStyle.Regular);
        public static readonly Font FontCaption  = new Font(Family, 9F, FontStyle.Regular);
        public static readonly Font FontMenu     = new Font(Family, 11.5F, FontStyle.Bold);
        public static readonly Font FontIcon     = new Font("Segoe MDL2 Assets", 13F);       // glifos de icono
        public static readonly Font FontWinCtl   = new Font("Segoe MDL2 Assets", 9F);        // botones de ventana

        // ===================== Espaciado (escala 4) =====================
        public const int SpaceXs  = 4;
        public const int SpaceSm  = 8;
        public const int SpaceMd  = 12;
        public const int SpaceLg  = 16;
        public const int SpaceXl  = 24;
        public const int SpaceXxl = 32;

        // ===================== Radios de esquina =====================
        public const int RadiusSm = 6;
        public const int RadiusMd = 10;

        // ===================== Iconos (Segoe MDL2 Assets, area de uso privado) =====================
        // Se construyen desde el codepoint para no depender de caracteres especiales en el .cs.
        private static string Glyph(int cp) => ((char)cp).ToString();
        public static readonly string IcoHome     = Glyph(0xE80F);
        public static readonly string IcoCalendar = Glyph(0xE787);
        public static readonly string IcoPeople   = Glyph(0xE716);
        public static readonly string IcoContact  = Glyph(0xE77B); // Contact (clientes)
        public static readonly string IcoServicio = Glyph(0xE719); // Shop (servicios)
        public static readonly string IcoGlobe    = Glyph(0xE774);
        public static readonly string IcoHistory  = Glyph(0xE81C);
        public static readonly string IcoLock     = Glyph(0xE72E);
        public static readonly string IcoClose    = Glyph(0xE8BB); // ChromeClose
        public static readonly string IcoMinimize = Glyph(0xE921); // ChromeMinimize
        public static readonly string IcoSearch   = Glyph(0xE721);
        public static readonly string IcoAdd      = Glyph(0xE710);
        public static readonly string IcoSave     = Glyph(0xE74E);
        public static readonly string IcoRefresh  = Glyph(0xE72C);
        public static readonly string IcoClear    = Glyph(0xE894); // Clear
        public static readonly string IcoWarning  = Glyph(0xE7BA);
        public static readonly string IcoLogout   = Glyph(0xF3B1); // SignOut
        public static readonly string IcoEye      = Glyph(0xE7B3); // RedEye (ver contrasena)
        public static readonly string IcoEyeOff   = Glyph(0xED1A); // Hide
        public static readonly string IcoUnlock   = Glyph(0xE785); // Unlock (desbloquear)
    }
}
