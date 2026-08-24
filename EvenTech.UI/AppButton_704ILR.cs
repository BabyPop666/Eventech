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
    internal class AppButton_704ILR : Button
    {
        private bool _hover_704ILR, _down_704ILR;

        public int Radius_704ILR { get; set; } = Theme_704ILR.RadiusSm_704ILR;
        public Color BaseColor_704ILR { get; set; } = Theme_704ILR.AccentButton_704ILR;
        public Color HoverColor_704ILR { get; set; } = Theme_704ILR.AccentButtonHover_704ILR;
        public Color DownColor_704ILR { get; set; } = Theme_704ILR.AccentButtonDown_704ILR;
        public Color BehindColor_704ILR { get; set; } = Theme_704ILR.Surface_704ILR;
        // Texto auxiliar opcional con la fuente de iconos (Segoe MDL2) a la izquierda.
        public string Glyph_704ILR { get; set; }

        public AppButton_704ILR()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ForeColor = Theme_704ILR.TextOnDark_704ILR;
            Font = Theme_704ILR.FontButton_704ILR;
            Cursor = Cursors.Hand;
            Size = new Size(120, 38);
        }

        protected override void OnMouseEnter(EventArgs e_704ILR) { _hover_704ILR = true; Invalidate(); base.OnMouseEnter(e_704ILR); }
        protected override void OnMouseLeave(EventArgs e_704ILR) { _hover_704ILR = false; _down_704ILR = false; Invalidate(); base.OnMouseLeave(e_704ILR); }
        protected override void OnMouseDown(MouseEventArgs e_704ILR) { if (e_704ILR.Button == MouseButtons.Left) { _down_704ILR = true; Invalidate(); } base.OnMouseDown(e_704ILR); }
        protected override void OnMouseUp(MouseEventArgs e_704ILR) { _down_704ILR = false; Invalidate(); base.OnMouseUp(e_704ILR); }

        protected override void OnPaint(PaintEventArgs e_704ILR)
        {
            var g_704ILR = e_704ILR.Graphics;
            g_704ILR.SmoothingMode = SmoothingMode.AntiAlias;
            g_704ILR.Clear(BehindColor_704ILR);

            Color fill_704ILR = !Enabled
                ? Color.FromArgb(206, 206, 206)
                : _down_704ILR ? DownColor_704ILR : _hover_704ILR ? HoverColor_704ILR : BaseColor_704ILR;

            var rect_704ILR = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path_704ILR = Rounded_704ILR(rect_704ILR, Radius_704ILR))
            using (var b_704ILR = new SolidBrush(fill_704ILR))
                g_704ILR.FillPath(b_704ILR, path_704ILR);

            bool hasGlyph_704ILR = !string.IsNullOrEmpty(Glyph_704ILR);
            bool hasText_704ILR = !string.IsNullOrEmpty(Text);
            if (hasGlyph_704ILR && hasText_704ILR)
            {
                // Glifo a la izquierda + texto; ambos centrados verticalmente.
                var glyphRect_704ILR = new Rectangle(rect_704ILR.X + 14, rect_704ILR.Y, 22, rect_704ILR.Height);
                TextRenderer.DrawText(g_704ILR, Glyph_704ILR, Theme_704ILR.FontIcon_704ILR, glyphRect_704ILR, ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                var textRect_704ILR = new Rectangle(rect_704ILR.X + 30, rect_704ILR.Y, rect_704ILR.Width - 30, rect_704ILR.Height);
                TextRenderer.DrawText(g_704ILR, Text, Font, textRect_704ILR, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else if (hasGlyph_704ILR)
            {
                // Boton solo-icono: glifo centrado.
                TextRenderer.DrawText(g_704ILR, Glyph_704ILR, Theme_704ILR.FontIcon_704ILR, rect_704ILR, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g_704ILR, Text, Font, rect_704ILR, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        // Camino con esquinas redondeadas reutilizable (tambien lo usa CardPanel).
        internal static GraphicsPath Rounded_704ILR(Rectangle r_704ILR, int radius_704ILR)
        {
            int d_704ILR = radius_704ILR * 2;
            var p_704ILR = new GraphicsPath();
            if (d_704ILR <= 0 || r_704ILR.Width <= 0 || r_704ILR.Height <= 0) { p_704ILR.AddRectangle(r_704ILR); return p_704ILR; }
            if (d_704ILR > r_704ILR.Width) d_704ILR = r_704ILR.Width;
            if (d_704ILR > r_704ILR.Height) d_704ILR = r_704ILR.Height;
            p_704ILR.AddArc(r_704ILR.X, r_704ILR.Y, d_704ILR, d_704ILR, 180, 90);
            p_704ILR.AddArc(r_704ILR.Right - d_704ILR, r_704ILR.Y, d_704ILR, d_704ILR, 270, 90);
            p_704ILR.AddArc(r_704ILR.Right - d_704ILR, r_704ILR.Bottom - d_704ILR, d_704ILR, d_704ILR, 0, 90);
            p_704ILR.AddArc(r_704ILR.X, r_704ILR.Bottom - d_704ILR, d_704ILR, d_704ILR, 90, 90);
            p_704ILR.CloseFigure();
            return p_704ILR;
        }
    }
}
