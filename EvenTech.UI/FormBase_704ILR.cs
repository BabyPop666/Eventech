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
        // El sufijo de autoria nombra al metodo del lado de C#, pero el runtime busca
        // ese mismo nombre DENTRO de user32.dll si no se declara EntryPoint. Como la
        // exportacion nativa se llama ReleaseCapture / SendMessage, hay que fijarla:
        // sin EntryPoint la llamada lanza EntryPointNotFoundException al arrastrar.
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private static extern bool ReleaseCapture_704ILR();

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage_704ILR(IntPtr hWnd_704ILR, int Msg_704ILR, IntPtr wParam_704ILR, IntPtr lParam_704ILR);
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

        // Una ventana sin borde no trae el redimensionado del sistema. Activandolo,
        // el formulario responde el codigo de zona que corresponde a cada borde y
        // Windows la redimensiona como a cualquier otra. Lo usa frmMain, para que en
        // pantallas mas grandes la ventana pueda aprovechar el espacio.
        protected bool Redimensionable_704ILR { get; set; }

        private const int BordeRedimension_704ILR = 6;

        protected override void WndProc(ref Message m_704ILR)
        {
            const int WM_NCHITTEST_704ILR = 0x0084;
            const int HTCLIENT_704ILR = 1;
            const int HTLEFT_704ILR = 10, HTRIGHT_704ILR = 11, HTTOP_704ILR = 12;
            const int HTTOPLEFT_704ILR = 13, HTTOPRIGHT_704ILR = 14, HTBOTTOM_704ILR = 15;
            const int HTBOTTOMLEFT_704ILR = 16, HTBOTTOMRIGHT_704ILR = 17;

            base.WndProc(ref m_704ILR);

            if (!Redimensionable_704ILR || m_704ILR.Msg != WM_NCHITTEST_704ILR ||
                WindowState != FormWindowState.Normal || (int)m_704ILR.Result != HTCLIENT_704ILR)
                return;

            int lp_704ILR = m_704ILR.LParam.ToInt32();
            Point p_704ILR = PointToClient(new Point(unchecked((short)lp_704ILR),
                                                     unchecked((short)(lp_704ILR >> 16))));
            bool izq_704ILR = p_704ILR.X <= BordeRedimension_704ILR;
            bool der_704ILR = p_704ILR.X >= ClientSize.Width - BordeRedimension_704ILR;
            bool arr_704ILR = p_704ILR.Y <= BordeRedimension_704ILR;
            bool aba_704ILR = p_704ILR.Y >= ClientSize.Height - BordeRedimension_704ILR;

            int zona_704ILR =
                arr_704ILR && izq_704ILR ? HTTOPLEFT_704ILR :
                arr_704ILR && der_704ILR ? HTTOPRIGHT_704ILR :
                aba_704ILR && izq_704ILR ? HTBOTTOMLEFT_704ILR :
                aba_704ILR && der_704ILR ? HTBOTTOMRIGHT_704ILR :
                izq_704ILR ? HTLEFT_704ILR :
                der_704ILR ? HTRIGHT_704ILR :
                arr_704ILR ? HTTOP_704ILR :
                aba_704ILR ? HTBOTTOM_704ILR : HTCLIENT_704ILR;

            if (zona_704ILR != HTCLIENT_704ILR) m_704ILR.Result = (IntPtr)zona_704ILR;
        }

        // Permite arrastrar la ventana tomando el control indicado (barra de titulo).
        public void EnableDrag_704ILR(Control c_704ILR)
        {
            c_704ILR.MouseDown += (s_704ILR, e_704ILR) =>
            {
                if (e_704ILR.Button == MouseButtons.Left)
                {
                    ReleaseCapture_704ILR();
                    SendMessage_704ILR(Handle, WM_NCLBUTTONDOWN_704ILR,
                        new IntPtr(HT_CAPTION_704ILR), IntPtr.Zero);
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
