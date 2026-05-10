using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Pantalla de login.
    //
    // Layout:
    //   - Borderless 414x641, BG azul (Theme.BgLogin), Opacity 0.95
    //   - Panel titulo arriba (Theme.BgTitleBar) con botones minimize y close
    //   - Logo de texto centrado
    //   - Label "Usuario" + TextBox (sin border, BG Theme.BgInput, FG Silver)
    //   - Label "Contrasena" + TextBox password (idem)
    //   - Checkbox "Recordar cuenta" (decorativo, no persistimos)
    //   - Boton "Ingresar" grande, naranja (Theme.AccentButton)
    //   - Link "¿No tenes cuenta? Crear" abajo
    public class frmLogin : Form
    {
        // Win32 drag para mover la ventana borderless tomandola del title bar.
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private TextBox _txtUser;
        private TextBox _txtPass;
        private Button _btnLogin;
        private Label _lblStatus;
        private Label _lblCrearCuenta;

        public frmLogin()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(414, 641);
            BackColor = Theme.BgLogin;
            Opacity = 0.95;
            KeyPreview = true;

            // --- Title bar ---
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgTitleBar
            };
            pnlTitle.MouseDown += DragWindow;

            var btnClose = new Label
            {
                Text = "✕",
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(35, 30),
                Location = new Point(371, 10),
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => { Application.Exit(); };

            var btnMin = new Label
            {
                Text = "—",
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(35, 30),
                Location = new Point(330, 10),
                Cursor = Cursors.Hand
            };
            btnMin.Click += (s, e) => { WindowState = FormWindowState.Minimized; };

            pnlTitle.Controls.Add(btnClose);
            pnlTitle.Controls.Add(btnMin);

            // --- Logo: texto dorado centrado ---
            var lblLogo = new Label
            {
                Text = "EvenTech",
                Font = new Font("Ebrima", 22F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 90),
                Size = new Size(414, 80),
                BackColor = Color.Transparent
            };

            // --- Campo Usuario ---
            var lblUser = new Label
            {
                Text = "Usuario",
                Font = Theme.FontLabel,
                ForeColor = Theme.TextLight,
                Location = new Point(28, 211),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _txtUser = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.FontInput,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextLight,
                Location = new Point(32, 245),
                Size = new Size(350, 22)
            };
            var pnlUserLine = new Panel
            {
                BackColor = Theme.Accent,
                Location = new Point(32, 270),
                Size = new Size(350, 1)
            };

            // --- Campo Contrasena ---
            var lblPass = new Label
            {
                Text = "Contraseña",
                Font = Theme.FontLabel,
                ForeColor = Theme.TextLight,
                Location = new Point(28, 294),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _txtPass = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.FontInput,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextLight,
                Location = new Point(32, 327),
                Size = new Size(350, 22),
                UseSystemPasswordChar = true
            };
            var pnlPassLine = new Panel
            {
                BackColor = Theme.Accent,
                Location = new Point(32, 352),
                Size = new Size(350, 1)
            };

            // --- Checkbox "Recordar cuenta" (decorativo) ---
            var chbRemember = new CheckBox
            {
                Text = "Recordar cuenta",
                Font = new Font("Ebrima", 11.25F, FontStyle.Regular),
                ForeColor = Color.FromArgb(231, 218, 139),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(32, 373)
            };

            // --- Boton Ingresar ---
            _btnLogin = new Button
            {
                Text = "Ingresar",
                Font = Theme.FontButton,
                ForeColor = Theme.TextOnDark,
                BackColor = Theme.AccentButton,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(32, 421),
                Size = new Size(350, 50),
                Cursor = Cursors.Hand
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 103, 242);
            _btnLogin.FlatAppearance.MouseDownBackColor = Color.DarkBlue;
            _btnLogin.Click += BtnLogin_Click;

            // --- Status (mensajes de error/exito) ---
            _lblStatus = new Label
            {
                Location = new Point(32, 481),
                Size = new Size(350, 50),
                ForeColor = Color.FromArgb(255, 180, 180),
                Font = new Font("Ebrima", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Text = ""
            };

            // --- Link "Crear cuenta" (auto-registro) ---
            _lblCrearCuenta = new Label
            {
                Text = "¿No tenes cuenta? Crear",
                Font = Theme.FontLabel,
                ForeColor = Theme.TextLight,
                AutoSize = true,
                Location = new Point(120, 580),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            _lblCrearCuenta.Click += LblCrearCuenta_Click;

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                BackColor = Theme.BgTitleBar
            };

            Controls.Add(pnlTitle);
            Controls.Add(lblLogo);
            Controls.Add(lblUser);
            Controls.Add(_txtUser);
            Controls.Add(pnlUserLine);
            Controls.Add(lblPass);
            Controls.Add(_txtPass);
            Controls.Add(pnlPassLine);
            Controls.Add(chbRemember);
            Controls.Add(_btnLogin);
            Controls.Add(_lblStatus);
            Controls.Add(_lblCrearCuenta);
            Controls.Add(pnlFooter);

            AcceptButton = _btnLogin;
            _txtUser.Focus();
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = _txtUser.Text.Trim();
            string plain = _txtPass.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(plain))
            {
                SetError("Completar usuario y contraseña.");
                return;
            }

            string hashed = Encrypt.HashValue(plain);
            _txtPass.Clear();

            LoginResult result;
            try
            {
                result = BLL_Login.Authenticate(username, hashed);
            }
            catch (Exception ex)
            {
                SetError("Error de conexion: " + ex.Message);
                return;
            }

            switch (result)
            {
                case LoginResult.Success:
                    // Ocultamos el login y mostramos frmMain modal. Cuando
                    // frmMain se cierra (logout), volvemos al login.
                    Hide();
                    using (var main = new frmMain())
                    {
                        main.ShowDialog();
                    }
                    // Reset visual y volver al estado inicial
                    _txtUser.Clear();
                    _txtPass.Clear();
                    _lblStatus.Text = "";
                    _txtUser.Focus();
                    Show();
                    break;
                case LoginResult.UserNotFound:
                    SetError("Usuario no encontrado.");
                    break;
                case LoginResult.IncorrectPassword:
                    SetError("Contraseña incorrecta.");
                    break;
            }
        }

        private void LblCrearCuenta_Click(object sender, EventArgs e)
        {
            using (var alta = new frmCrearCuenta())
            {
                alta.ShowDialog();
            }
            _txtUser.Focus();
        }

        private void SetError(string msg)
        {
            _lblStatus.ForeColor = Color.FromArgb(255, 180, 180);
            _lblStatus.Text = msg;
        }
    }
}
