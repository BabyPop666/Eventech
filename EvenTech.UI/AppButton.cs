using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Boton plano con esquinas redondeadas y estados hover/pressed, pintado con
    // antialiasing. Centraliza el estilo de los botones de la app (reuso, cohesion).
    //
    // BehindColor = color que se ve detras del boton (en las esquinas redondeadas).
    // Por defecto Surface (blanco), porque la mayoria de los botones viven sobre
    // tarjetas. Si el boton esta sobre el fondo de contenido o sobre un panel
    // oscuro, ajustar BehindColor al color real de ese fondo.
    internal class AppButton : Button
    {
        private bool _hover, _down;

        public int Radius { get; set; } = Theme.RadiusSm;
        public Color BaseColor { get; set; } = Theme.AccentButton;
        public Color HoverColor { get; set; } = Theme.AccentButtonHover;
        public Color DownColor { get; set; } = Theme.AccentButtonDown;
        public Color BehindColor { get; set; } = Theme.Surface;
        // Texto auxiliar opcional con la fuente de iconos (Segoe MDL2) a la izquierda.
        public string Glyph { get; set; }

        public AppButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ForeColor = Theme.TextOnDark;
            Font = Theme.FontButton;
            Cursor = Cursors.Hand;
            Size = new Size(120, 38);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BehindColor);

            Color fill = !Enabled
                ? Color.FromArgb(206, 206, 206)
                : _down ? DownColor : _hover ? HoverColor : BaseColor;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Rounded(rect, Radius))
            using (var b = new SolidBrush(fill))
                g.FillPath(b, path);

            bool hasGlyph = !string.IsNullOrEmpty(Glyph);
            bool hasText = !string.IsNullOrEmpty(Text);
            if (hasGlyph && hasText)
            {
                // Glifo a la izquierda + texto; ambos centrados verticalmente.
                var glyphRect = new Rectangle(rect.X + 14, rect.Y, 22, rect.Height);
                TextRenderer.DrawText(g, Glyph, Theme.FontIcon, glyphRect, ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                var textRect = new Rectangle(rect.X + 30, rect.Y, rect.Width - 30, rect.Height);
                TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (hasGlyph)
            {
                // Boton solo-icono: glifo centrado.
                TextRenderer.DrawText(g, Glyph, Theme.FontIcon, rect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        // Camino con esquinas redondeadas reutilizable (tambien lo usa CardPanel).
        internal static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            if (d <= 0 || r.Width <= 0 || r.Height <= 0) { p.AddRectangle(r); return p; }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
