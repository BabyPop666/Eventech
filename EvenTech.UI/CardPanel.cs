using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Panel-tarjeta: fondo de superficie con esquinas redondeadas y borde sutil.
    // Sirve para agrupar contenido (listados, formularios, filtros) sobre el area
    // clara. BehindColor es el color del fondo detras de la tarjeta (las esquinas
    // redondeadas lo dejan ver); por defecto el fondo de contenido.
    internal class CardPanel : Panel
    {
        public int Radius { get; set; } = Theme.RadiusMd;
        public Color BorderColor { get; set; } = Theme.Border;
        public Color FillColor { get; set; } = Theme.Surface;
        public Color BehindColor { get; set; } = Theme.BgContent;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Padding = new Padding(Theme.SpaceLg);
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BehindColor);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = AppButton.Rounded(rect, Radius))
            using (var b = new SolidBrush(FillColor))
            using (var pen = new Pen(BorderColor))
            {
                g.FillPath(b, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
