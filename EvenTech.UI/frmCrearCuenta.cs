using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Alta de usuario accesible desde el login (link "¿No tenes cuenta? Crear").
    // Misma paleta y estilo que frmLogin.
    public class frmCrearCuenta : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private TextBox _txtUser, _txtPass, _txtPass2;
        private Label _lblStatus;

        public frmCrearCuenta()
        {
            Text = "EvenTech - Crear cuenta";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(414, 500);
            BackColor = Theme.BgLogin;
            Opacity = 0.97;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.BgTitleBar };
            pnlTitle.MouseDown += Drag;

            var lblTitle = new Label
            {
                Text = "Crear cuenta",
                Font = new Font("Ebrima", 14F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(20, 14),
                BackColor = Color.Transparent
            };
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
            btnClose.Click += (s, e) => Close();
            pnlTitle.Controls.Add(btnClose);
            pnlTitle.Controls.Add(lblTitle);

            _txtUser = MakeInput("Usuario", 100);
            _txtPass = MakeInput("Contraseña", 180, true);
            _txtPass2 = MakeInput("Repetir contraseña", 260, true);

            var btnCrear = new Button
            {
                Text = "Crear",
                Font = Theme.FontButton,
                ForeColor = Theme.TextOnDark,
                BackColor = Theme.AccentButton,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(32, 360),
                Size = new Size(350, 50),
                Cursor = Cursors.Hand
            };
            btnCrear.FlatAppearance.BorderSize = 0;
            btnCrear.Click += BtnCrear_Click;

            _lblStatus = new Label
            {
                Location = new Point(32, 420),
                Size = new Size(350, 50),
                Font = new Font("Ebrima", 10F),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Text = ""
            };

            Controls.Add(pnlTitle);
            Controls.Add(btnCrear);
            Controls.Add(_lblStatus);
            AcceptButton = btnCrear;
        }

        private TextBox MakeInput(string labelText, int top, bool password = false)
        {
            var lbl = new Label
            {
                Text = labelText,
                Font = Theme.FontLabel,
                ForeColor = Theme.TextLight,
                Location = new Point(28, top),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.FontInput,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextLight,
                Location = new Point(32, top + 34),
                Size = new Size(350, 22),
                UseSystemPasswordChar = password
            };
            var line = new Panel
            {
                BackColor = Theme.Accent,
                Location = new Point(32, top + 59),
                Size = new Size(350, 1)
            };
            Controls.Add(lbl);
            Controls.Add(tb);
            Controls.Add(line);
            return tb;
        }

        private void BtnCrear_Click(object sender, EventArgs e)
        {
            string user = _txtUser.Text.Trim();
            string p1 = _txtPass.Text;
            string p2 = _txtPass2.Text;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(p1))
            {
                SetStatus("Completar todos los campos.", error: true);
                return;
            }
            if (p1 != p2)
            {
                SetStatus("Las contraseñas no coinciden.", error: true);
                return;
            }
            if (p1.Length < 4)
            {
                SetStatus("La contraseña debe tener al menos 4 caracteres.", error: true);
                return;
            }

            string hash = Encrypt.HashValue(p1);
            _txtPass.Clear();
            _txtPass2.Clear();

            try
            {
                var r = BLL_User.CreateUser(user, hash);
                switch (r)
                {
                    case CreateUserResult.Success:
                        SetStatus("Usuario creado. Ya podes iniciar sesion.", error: false);
                        _txtUser.Clear();
                        break;
                    case CreateUserResult.InvalidUsername:
                        SetStatus("Usuario invalido (3-50, letras/numeros/._-).", error: true);
                        break;
                    case CreateUserResult.UsernameAlreadyExists:
                        SetStatus("Ese usuario ya existe.", error: true);
                        break;
                    case CreateUserResult.InvalidPassword:
                        SetStatus("Contraseña invalida.", error: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, error: true);
            }
        }

        private void SetStatus(string msg, bool error)
        {
            _lblStatus.ForeColor = error ? Color.FromArgb(255, 180, 180) : Color.FromArgb(180, 255, 180);
            _lblStatus.Text = msg;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
