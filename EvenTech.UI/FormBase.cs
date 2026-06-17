using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Form base sin bordes con utilidades de cromo compartidas: arrastre por la
    // barra de titulo (Win32) y botones de ventana (minimizar/cerrar). Evita
    // duplicar el P/Invoke y el armado de la barra en cada formulario.
    public class FormBase : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        protected FormBase()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            Font = Theme.FontBody;
            BackColor = Theme.BgContent;
            DoubleBuffered = true;
        }

        // Permite arrastrar la ventana tomando el control indicado (barra de titulo).
        public void EnableDrag(Control c)
        {
            c.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
        }

        // Boton de ventana (minimizar / cerrar) con glifo Segoe MDL2. Si es de
        // cierre (danger), el hover se pinta rojo; si no, gris oscuro.
        protected Label WindowButton(string glyph, EventHandler onClick, bool danger = false)
        {
            var l = new Label
            {
                Text = glyph,
                Font = Theme.FontWinCtl,
                ForeColor = Theme.TextLight,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 30),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            l.MouseEnter += (s, e) => { l.BackColor = danger ? Theme.Error : Theme.SidebarHover; l.ForeColor = Color.White; };
            l.MouseLeave += (s, e) => { l.BackColor = Color.Transparent; l.ForeColor = Theme.TextLight; };
            l.Click += onClick;
            return l;
        }
    }
}
