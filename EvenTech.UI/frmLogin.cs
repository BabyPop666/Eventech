using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Pantalla de login. Borderless con identidad de marca (azul oscuro + dorado).
    // Layout 100% por TableLayoutPanel/Dock (DPI-aware, sin coordenadas magicas).
    // El loop principal vive aca: al validar credenciales abre frmMain modal y al
    // volver del logout queda esperando otro login. El boton cerrar termina la app.
    // El selector de idioma (+ alta rapida) vive en el pie, abajo a la derecha.
    public class frmLogin : FormBase, IObservadorIdioma
    {
        private TextBox _txtUser, _txtPass;
        private Label _lblUser, _lblPass, _lblTagline, _lblStatus, _lblCrearCuenta;
        private CheckBox _chkRemember;
        private AppButton _btnLogin;

        public frmLogin()
        {
            BuildUi();
            ActualizarTextos();
            GestorDeIdioma.GetInstance.Suscribir(this);
            FormClosed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);

            // "Recordar cuenta": precarga el usuario guardado (nunca la contrasena).
            LoginPrefs.Load();
            if (LoginPrefs.Remember)
            {
                _txtUser.Text = LoginPrefs.Username;
                _chkRemember.Checked = true;
            }
            Shown += (s, e) =>
            {
                if (string.IsNullOrEmpty(_txtUser.Text)) _txtUser.Focus();
                else _txtPass.Focus();
            };
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(420, 660);
            BackColor = Theme.BgLogin;
            KeyPreview = true;

            // ---------------- Barra de titulo (minimizar / cerrar) ----------------
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);

            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Application.Exit(), danger: true);
            btnClose.Dock = DockStyle.Right;
            var btnMin = WindowButton(Theme.IcoMinimize, (s, e) => WindowState = FormWindowState.Minimized);
            btnMin.Dock = DockStyle.Right;
            pnlTitle.Controls.Add(btnMin);
            pnlTitle.Controls.Add(btnClose);

            // ---------------- Pie: link "crear" (izq) + selector de idioma (der) ----------------
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Theme.BgLogin,
                Padding = new Padding(Theme.SpaceXl, 0, Theme.SpaceLg, Theme.SpaceSm)
            };
            var footerGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lblCrearCuenta = new Label
            {
                Font = Theme.FontBody,
                ForeColor = Theme.TextLight,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            _lblCrearCuenta.Click += LblCrearCuenta_Click;
            _lblCrearCuenta.MouseEnter += (s, e) => _lblCrearCuenta.ForeColor = Theme.Accent;
            _lblCrearCuenta.MouseLeave += (s, e) => _lblCrearCuenta.ForeColor = Theme.TextLight;

            var lang = new LangSelector(dark: true, allowManage: false) { Anchor = AnchorStyles.Right };

            footerGrid.Controls.Add(_lblCrearCuenta, 0, 0);
            footerGrid.Controls.Add(lang, 1, 0);
            footer.Controls.Add(footerGrid);

            // ---------------- Cuerpo ----------------
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgLogin,
                Padding = new Padding(44, 26, 44, 12)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int[] heights = { 64, 22, 22, 60, 60, 30, 8, 50, 42, /*fill*/ 0 };
            for (int i = 0; i < heights.Length; i++)
                tbl.RowStyles.Add(i == 9
                    ? new RowStyle(SizeType.Percent, 100)
                    : new RowStyle(SizeType.Absolute, heights[i]));

            var lblLogo = new Label
            {
                Text = "EvenTech",
                Font = Theme.FontDisplay,
                ForeColor = Theme.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _lblTagline = new Label
            {
                Font = Theme.FontSmall,
                ForeColor = Theme.TextLight,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var userField = Ui.DarkField("Usuario", false, out _txtUser, out _lblUser);
            var passField = Ui.DarkField("Contraseña", true, out _txtPass, out _lblPass);

            // BackColor solido (no Transparent) + FlatStyle.Standard: evita el
            // artefacto por el que el check "desaparecia" al marcarlo sobre el panel.
            _chkRemember = new CheckBox
            {
                Text = "Recordar cuenta",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextLight,
                FlatStyle = FlatStyle.Standard,
                BackColor = Theme.BgLogin,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnLogin = Ui.Primary("Ingresar");
            _btnLogin.Dock = DockStyle.Fill;
            _btnLogin.BehindColor = Theme.BgLogin;
            _btnLogin.Margin = new Padding(0);
            _btnLogin.Click += BtnLogin_Click;

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(255, 170, 170),
                Font = Theme.FontSmall,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            tbl.Controls.Add(lblLogo, 0, 0);
            tbl.Controls.Add(_lblTagline, 0, 1);
            tbl.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 2); // spacer
            tbl.Controls.Add(userField, 0, 3);
            tbl.Controls.Add(passField, 0, 4);
            tbl.Controls.Add(_chkRemember, 0, 5);
            tbl.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 6); // spacer
            tbl.Controls.Add(_btnLogin, 0, 7);
            tbl.Controls.Add(_lblStatus, 0, 8);
            tbl.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 9); // fill

            body.Controls.Add(tbl);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(pnlTitle);

            AcceptButton = _btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = _txtUser.Text.Trim();
            string plain = _txtPass.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(plain))
            {
                SetError(T("LOGIN_COMPLETAR", "Completar usuario y contraseña."));
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
                BLL_Bitacora.RegistrarExcepcion(ex, "Login", "Autenticacion");
                SetError(T("LOGIN_ERR_CONEXION", "Error de conexión:") + " " + ex.Message);
                return;
            }

            switch (result)
            {
                case LoginResult.Success:
                    LoginPrefs.Save(_chkRemember.Checked, username);
                    Hide();
                    using (var main = new frmMain()) main.ShowDialog();
                    _txtUser.Clear();
                    _txtPass.Clear();
                    _lblStatus.Text = "";
                    _txtUser.Focus();
                    Show();
                    break;
                case LoginResult.UserNotFound:
                    SetError(T("LOGIN_ERR_USUARIO", "Usuario no encontrado."));
                    break;
                case LoginResult.IncorrectPassword:
                    SetError(T("LOGIN_ERR_PASS", "Contraseña incorrecta."));
                    break;
            }
        }

        private void LblCrearCuenta_Click(object sender, EventArgs e)
        {
            using (var alta = new frmCrearCuenta()) alta.ShowDialog();
            _txtUser.Focus();
        }

        private void SetError(string msg)
        {
            _lblStatus.ForeColor = Color.FromArgb(255, 170, 170);
            _lblStatus.Text = msg;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // Observador del patron Observer: traduce las leyendas del login.
        public void ActualizarTextos()
        {
            if (_lblUser != null)        _lblUser.Text        = Tr.T("LOGIN_USER");
            if (_lblPass != null)        _lblPass.Text        = Tr.T("LOGIN_PASS");
            if (_btnLogin != null)       _btnLogin.Text       = Tr.T("LOGIN_ENTER");
            if (_lblCrearCuenta != null) _lblCrearCuenta.Text = Tr.T("LOGIN_CREATE");
            if (_lblTagline != null)     _lblTagline.Text     = T("LOGIN_TAGLINE", "Gestión de eventos y reservas");
            if (_chkRemember != null)    _chkRemember.Text    = T("LOGIN_REMEMBER", "Recordar cuenta");
        }
    }
}
