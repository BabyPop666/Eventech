using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Panel-tarjeta: fondo de superficie con esquinas redondeadas y borde sutil.
    // Sirve para agrupar contenido (listados, formularios, filtros) sobre el area
    // clara. BehindColor es el color del fondo detras de la tarjeta (las esquinas
    // redondeadas lo dejan ver); por defecto el fondo de contenido.
    internal class CardPanel_704ILR : Panel
    {
        public int Radius_704ILR { get; set; } = Theme_704ILR.RadiusMd_704ILR;
        public Color BorderColor_704ILR { get; set; } = Theme_704ILR.Border_704ILR;
        public Color FillColor_704ILR { get; set; } = Theme_704ILR.Surface_704ILR;
        public Color BehindColor_704ILR { get; set; } = Theme_704ILR.BgContent_704ILR;

        public CardPanel_704ILR()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Padding = new Padding(Theme_704ILR.SpaceLg_704ILR);
            BackColor = Theme_704ILR.Surface_704ILR;
        }

        protected override void OnPaint(PaintEventArgs e_704ILR)
        {
            var g_704ILR = e_704ILR.Graphics;
            g_704ILR.SmoothingMode = SmoothingMode.AntiAlias;
            g_704ILR.Clear(BehindColor_704ILR);
            var rect_704ILR = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path_704ILR = AppButton_704ILR.Rounded_704ILR(rect_704ILR, Radius_704ILR))
            using (var b_704ILR = new SolidBrush(FillColor_704ILR))
            using (var pen_704ILR = new Pen(BorderColor_704ILR))
            {
                g_704ILR.FillPath(b_704ILR, path_704ILR);
                g_704ILR.DrawPath(pen_704ILR, path_704ILR);
            }
        }
    }
}
