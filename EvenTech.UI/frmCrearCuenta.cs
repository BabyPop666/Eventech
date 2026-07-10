using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Alta de usuario accesible desde el login (link "¿No tenes cuenta? Crear").
    // Borderless con la identidad de marca de frmLogin (azul oscuro + dorado).
    // Layout por TableLayoutPanel/Dock (DPI-aware, sin coordenadas magicas) e
    // implementa el patron Observer para re-traducir labels al cambiar de idioma.
    public class frmCrearCuenta : FormBase, IObservadorIdioma
    {
        private TextBox _txtUser, _txtPass, _txtPass2;
        private Label _lblUser, _lblPass, _lblPass2, _lblTitle, _lblStatus;
        private AppButton _btnCrear;

        public frmCrearCuenta()
        {
            BuildUi();
            ActualizarTextos();
            GestorDeIdioma.GetInstance.Suscribir(this);
            FormClosed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
            Shown += (s, e) => _txtUser.Focus();
        }

        private void BuildUi()
        {
            Text = "EvenTech - " + Tr.T("CC_TITULO");
            ClientSize = new Size(420, 660); // misma altura que el login para cubrirlo por completo
            BackColor = Theme.BgLogin;
            KeyPreview = true;

            // ---------------- Barra de titulo ----------------
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);

            _lblTitle = new Label
            {
                Font = Theme.FontH2,
                ForeColor = Theme.Accent,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };

            // Dock=Right: reserva su lugar a la derecha sin que el titulo (Dock=Fill)
            // lo tape (antes quedaba oculto e impedia cerrar la ventana).
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Close(), danger: true);
            btnClose.Dock = DockStyle.Right;

            pnlTitle.Controls.Add(_lblTitle);
            pnlTitle.Controls.Add(btnClose);

            // ---------------- Cuerpo ----------------
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgLogin,
                Padding = new Padding(44, 26, 44, 22)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int[] heights = { 64, 60, 60, 60, 8, 50, 42, /*fill*/ 0 };
            for (int i = 0; i < heights.Length; i++)
                tbl.RowStyles.Add(i == 7
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

            // Campos oscuros estilo login; se capturan los caption Label para re-traducir.
            var userField  = Ui.DarkField("Usuario", false, out _txtUser,  out _lblUser);
            var passField  = Ui.DarkField("Contraseña", true, out _txtPass,  out _lblPass);
            var pass2Field = Ui.DarkField("Repetir contraseña", true, out _txtPass2, out _lblPass2);

            _btnCrear = Ui.Primary("Crear cuenta");
            _btnCrear.Dock = DockStyle.Fill;
            _btnCrear.BehindColor = Theme.BgLogin;
            _btnCrear.Margin = new Padding(0);
            _btnCrear.Click += BtnCrear_Click;

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(255, 170, 170),
                Font = Theme.FontSmall,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            tbl.Controls.Add(lblLogo, 0, 0);
            tbl.Controls.Add(userField, 0, 1);
            tbl.Controls.Add(passField, 0, 2);
            tbl.Controls.Add(pass2Field, 0, 3);
            tbl.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 4); // spacer
            tbl.Controls.Add(_btnCrear, 0, 5);
            tbl.Controls.Add(_lblStatus, 0, 6);
            tbl.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 7); // fill

            body.Controls.Add(tbl);

            Controls.Add(body);
            Controls.Add(pnlTitle);

            AcceptButton = _btnCrear;
        }

        private void BtnCrear_Click(object sender, EventArgs e)
        {
            string user = _txtUser.Text.Trim();
            string p1 = _txtPass.Text;
            string p2 = _txtPass2.Text;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(p1))
            {
                SetStatus(T("CC_MSG_COMPLETAR", "Completar todos los campos."), error: true);
                return;
            }
            if (p1 != p2)
            {
                SetStatus(T("CC_MSG_NO_COINCIDEN", "Las contraseñas no coinciden."), error: true);
                return;
            }
            if (p1.Length < 4)
            {
                SetStatus(T("CC_MSG_PASS_CORTA", "La contraseña debe tener al menos 4 caracteres."), error: true);
                return;
            }

            try
            {
                // La contrasena en claro se hashea (salteada, PBKDF2) dentro de la BLL.
                var r = BLL_User.CreateUser(user, p1);
                _txtPass.Clear();
                _txtPass2.Clear();
                switch (r)
                {
                    case CreateUserResult.Success:
                        SetStatus(T("CC_MSG_OK", "Usuario creado. Ya podes iniciar sesion."), error: false);
                        _txtUser.Clear();
                        break;
                    case CreateUserResult.InvalidUsername:
                        SetStatus(T("CC_MSG_USER_INVALIDO", "Usuario invalido (3-50, letras/numeros/._-)."), error: true);
                        break;
                    case CreateUserResult.UsernameAlreadyExists:
                        SetStatus(T("CC_MSG_USER_EXISTE", "Ese usuario ya existe."), error: true);
                        break;
                    case CreateUserResult.InvalidPassword:
                        SetStatus(T("CC_MSG_PASS_INVALIDA", "Contraseña invalida."), error: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "CrearCuenta", "Alta de usuario");
                SetStatus(T("CC_MSG_ERROR", "Error:") + " " + ex.Message, error: true);
            }
        }

        private void SetStatus(string msg, bool error)
        {
            _lblStatus.ForeColor = error ? Color.FromArgb(255, 170, 170) : Color.FromArgb(170, 235, 170);
            _lblStatus.Text = msg;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // Observador (patron Observer): re-traduce titulo, captions y boton.
        public void ActualizarTextos()
        {
            if (_lblTitle != null) _lblTitle.Text = Tr.T("CC_TITULO");
            if (_lblUser != null)  _lblUser.Text  = Tr.T("CC_USER");
            if (_lblPass != null)  _lblPass.Text  = Tr.T("CC_PASS");
            if (_lblPass2 != null) _lblPass2.Text = Tr.T("CC_PASS2");
            if (_btnCrear != null) _btnCrear.Text = Tr.T("CC_CREAR");
        }
    }
}
