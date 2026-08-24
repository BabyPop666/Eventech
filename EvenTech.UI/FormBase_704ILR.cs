using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Form base sin bordes con utilidades de cromo compartidas: arrastre por la
    // barra de titulo (Win32) y botones de ventana (minimizar/cerrar). Evita
    // duplicar el P/Invoke y el armado de la barra en cada formulario.
    public class FormBase_704ILR : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture_704ILR();
        [DllImport("user32.dll")] private static extern int SendMessage_704ILR(IntPtr hWnd_704ILR, int Msg_704ILR, int wParam_704ILR, int lParam_704ILR);
        private const int WM_NCLBUTTONDOWN_704ILR = 0xA1;
        private const int HT_CAPTION_704ILR = 0x2;

        protected FormBase_704ILR()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            Font = Theme_704ILR.FontBody_704ILR;
            BackColor = Theme_704ILR.BgContent_704ILR;
            DoubleBuffered = true;
        }

        // Permite arrastrar la ventana tomando el control indicado (barra de titulo).
        public void EnableDrag_704ILR(Control c_704ILR)
        {
            c_704ILR.MouseDown += (s_704ILR, e_704ILR) =>
            {
                if (e_704ILR.Button == MouseButtons.Left)
                {
                    ReleaseCapture_704ILR();
                    SendMessage_704ILR(Handle, WM_NCLBUTTONDOWN_704ILR, HT_CAPTION_704ILR, 0);
                }
            };
        }

        // Boton de ventana (minimizar / cerrar) con glifo Segoe MDL2. Si es de
        // cierre (danger), el hover se pinta rojo; si no, gris oscuro.
        protected Label WindowButton_704ILR(string glyph_704ILR, EventHandler onClick_704ILR, bool danger_704ILR = false)
        {
            var l_704ILR = new Label
            {
                Text = glyph_704ILR,
                Font = Theme_704ILR.FontWinCtl_704ILR,
                ForeColor = Theme_704ILR.TextLight_704ILR,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 30),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            l_704ILR.MouseEnter += (s_704ILR, e_704ILR) => { l_704ILR.BackColor = danger_704ILR ? Theme_704ILR.Error_704ILR : Theme_704ILR.SidebarHover_704ILR; l_704ILR.ForeColor = Color.White; };
            l_704ILR.MouseLeave += (s_704ILR, e_704ILR) => { l_704ILR.BackColor = Color.Transparent; l_704ILR.ForeColor = Theme_704ILR.TextLight_704ILR; };
            l_704ILR.Click += onClick_704ILR;
            return l_704ILR;
        }
    }
}
