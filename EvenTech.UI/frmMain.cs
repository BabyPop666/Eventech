using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Vista principal post-login.
    //   - Borderless 1000x650
    //   - Panel izquierdo (Theme.BgMenu) con botones de seccion
    //   - Topbar (Theme.BgTitleBar) con nombre de usuario + close/min
    //   - Panel central (Theme.BgContent) donde se cargan UserControls (Dock=Fill)
    public class frmMain : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private Panel _pnlContent;
        private Button _btnInicio, _btnAuditoria, _btnSalir;
        private Button _currentActive;

        public frmMain()
        {
            BuildUi();
            LoadInicio();
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 650);
            BackColor = Theme.BgContent;

            // --- Menu lateral ---
            var pnlMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Theme.BgMenu
            };

            var pnlLogo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Theme.BgMenu
            };
            pnlLogo.MouseDown += Drag;
            var lblLogo = new Label
            {
                Text = "EvenTech",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            lblLogo.MouseDown += Drag;
            pnlLogo.Controls.Add(lblLogo);

            _btnInicio    = MakeMenuButton("  Inicio",      (s, e) => { LoadInicio();     SetActive(_btnInicio); });
            _btnAuditoria = MakeMenuButton("  Auditoria",   (s, e) => { LoadAuditoria();  SetActive(_btnAuditoria); });
            _btnSalir     = MakeMenuButton("  Cerrar sesion",(s, e) => DoLogout());

            // El orden de Add importa porque cada uno se hace Dock=Top y se apila al reves.
            pnlMenu.Controls.Add(_btnSalir);
            pnlMenu.Controls.Add(_btnAuditoria);
            pnlMenu.Controls.Add(_btnInicio);
            pnlMenu.Controls.Add(pnlLogo);

            // --- Topbar ---
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgTitleBar
            };
            pnlTop.MouseDown += Drag;

            string usuario = SessionManager.IsSessionActive ? SessionManager.GetInstance.User.Username : "?";
            var lblUser = new Label
            {
                Text = "Usuario: " + usuario,
                Font = new Font("Ebrima", 11F, FontStyle.Regular),
                ForeColor = Theme.TextLight,
                AutoSize = true,
                Location = new Point(15, 15),
                BackColor = Color.Transparent
            };
            lblUser.MouseDown += Drag;

            var btnClose = new Label
            {
                Text = "✕",
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(35, 30),
                Location = new Point(957, 10),
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();

            var btnMin = new Label
            {
                Text = "—",
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(35, 30),
                Location = new Point(916, 10),
                Cursor = Cursors.Hand
            };
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            pnlTop.Controls.Add(lblUser);
            pnlTop.Controls.Add(btnMin);
            pnlTop.Controls.Add(btnClose);

            // --- Contenido central (donde se cargan UserControls) ---
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgContent,
                Padding = new Padding(15)
            };

            Controls.Add(_pnlContent);
            Controls.Add(pnlTop);
            Controls.Add(pnlMenu);

            SetActive(_btnInicio);
        }

        private Button MakeMenuButton(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 55,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontMenu,
                ForeColor = Theme.Accent,
                BackColor = Theme.BgMenu,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 49, 47);
            b.Click += onClick;
            return b;
        }

        private void SetActive(Button btn)
        {
            if (_currentActive != null)
            {
                _currentActive.BackColor = Theme.BgMenu;
                _currentActive.ForeColor = Theme.Accent;
            }
            if (btn != null && btn != _btnSalir)
            {
                btn.BackColor = Color.FromArgb(50, 49, 47);
                btn.ForeColor = Theme.TextOnDark;
                _currentActive = btn;
            }
        }

        private void LoadInicio()
        {
            _pnlContent.Controls.Clear();
            var lbl = new Label
            {
                Text = "Bienvenido a EvenTech",
                Font = new Font("Ebrima", 22F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(20, 30)
            };
            var lbl2 = new Label
            {
                Text = "Sesion iniciada por: " +
                       (SessionManager.IsSessionActive ? SessionManager.GetInstance.User.Username : "?") +
                       Environment.NewLine + Environment.NewLine +
                       "Usa el menu de la izquierda para ver el registro de auditoria.",
                Font = new Font("Ebrima", 12F),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(20, 90),
                MaximumSize = new Size(700, 0)
            };
            _pnlContent.Controls.Add(lbl);
            _pnlContent.Controls.Add(lbl2);
        }

        private void LoadAuditoria()
        {
            _pnlContent.Controls.Clear();
            var uc = new ucAuditoria { Dock = DockStyle.Fill };
            _pnlContent.Controls.Add(uc);
        }

        private void DoLogout()
        {
            try { BLL_Login.Logout(); } catch { /* ignorar */ }
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (SessionManager.IsSessionActive)
            {
                try { BLL_Login.Logout(); } catch { }
            }
            base.OnFormClosing(e);
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
