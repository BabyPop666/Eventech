using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Vista principal post-login (shell). Borderless.
    //   - Sidebar (BgSidebar) con logo + saludo y items icono+texto con indicador
    //     de activo.
    //   - Topbar (BgTitleBar) con titulo de seccion, salir y botones de ventana.
    //   - Panel central (BgContent) que hospeda UserControls (Dock=Fill).
    //   - Pie (footer) con el selector de idioma + alta rapida, abajo a la derecha.
    // Layout por Dock + TableLayoutPanel (DPI-aware, sin coordenadas magicas).
    public class frmMain : FormBase, IObservadorIdioma
    {
        private Panel _pnlContent;
        private Label _lblPageTitle, _lblWelcome;
        private AppButton _btnLogout;
        private LangSelector _lang;

        private SideMenuItem _itInicio, _itReservas, _itClientes, _itServicios, _itPerfiles, _itAuditoria;
        private SideMenuItem _activo;
        private readonly List<SideMenuItem> _items = new List<SideMenuItem>();

        public frmMain()
        {
            BuildUi();
            bool bloqueado = SessionManager.IsSessionActive && SessionManager.GetInstance.SinPerfil;
            if (bloqueado) { foreach (var it in _items) it.Visible = false; }
            else AplicarPermisos();
            ActualizarTextos();              // si esta bloqueado, muestra el mensaje
            if (!bloqueado) Navegar(_itInicio);
            GestorDeIdioma.GetInstance.Suscribir(this);
        }

        // Control de acceso (T04): muestra/oculta secciones segun los permisos
        // efectivos del perfil del usuario. Sin perfil => acceso total.
        private void AplicarPermisos()
        {
            if (!SessionManager.IsSessionActive) return;
            var s = SessionManager.GetInstance;
            _itReservas.Visible  = s.TienePermiso("RESERVA_CREAR")
                                || s.TienePermiso("RESERVA_EDITAR")
                                || s.TienePermiso("RESERVA_HISTORIAL");
            _itAuditoria.Visible = s.TienePermiso("BITACORA_VER")
                                || s.TienePermiso("AUDIT_LOGIN_VER");
            // Inicio / Perfiles: siempre visibles (administracion).
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(1040, 680);
            BackColor = Theme.BgContent;
            MinimumSize = new Size(900, 600);

            // ---------------- Sidebar ----------------
            var pnlMenu = new Panel { Dock = DockStyle.Left, Width = 232, BackColor = Theme.BgSidebar };

            var pnlLogo = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Theme.BgSidebar };
            EnableDrag(pnlLogo);
            var lblLogo = new Label
            {
                Text = "EvenTech",
                Font = Theme.FontH1,
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 52,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            EnableDrag(lblLogo);
            _lblWelcome = new Label
            {
                Font = Theme.FontCaption,
                ForeColor = Theme.TextLight,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            EnableDrag(_lblWelcome);
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.SidebarHover };
            pnlLogo.Controls.Add(_lblWelcome);
            pnlLogo.Controls.Add(lblLogo);
            pnlLogo.Controls.Add(sep);

            _itInicio    = new SideMenuItem(Theme.IcoHome,     "MENU_INICIO",    (s, e) => Navegar(_itInicio));
            _itReservas  = new SideMenuItem(Theme.IcoCalendar, "MENU_RESERVAS",  (s, e) => Navegar(_itReservas));
            _itClientes  = new SideMenuItem(Theme.IcoContact,  "MENU_CLIENTES",  (s, e) => Navegar(_itClientes));
            _itServicios = new SideMenuItem(Theme.IcoServicio, "MENU_SERVICIOS", (s, e) => Navegar(_itServicios));
            _itPerfiles  = new SideMenuItem(Theme.IcoPeople,   "MENU_PERFILES",  (s, e) => Navegar(_itPerfiles));
            _itAuditoria = new SideMenuItem(Theme.IcoHistory,  "MENU_AUDITORIA", (s, e) => Navegar(_itAuditoria));
            _items.AddRange(new[] { _itInicio, _itReservas, _itClientes, _itServicios, _itPerfiles, _itAuditoria });

            // Dock=Top se apila en orden inverso al de agregado.
            // Idiomas salio del menu (se gestiona desde el globo del pie).
            pnlMenu.Controls.Add(_itAuditoria);
            pnlMenu.Controls.Add(_itPerfiles);
            pnlMenu.Controls.Add(_itServicios);
            pnlMenu.Controls.Add(_itClientes);
            pnlMenu.Controls.Add(_itReservas);
            pnlMenu.Controls.Add(_itInicio);
            pnlMenu.Controls.Add(pnlLogo);

            // ---------------- Topbar ----------------
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTop);

            var topGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // titulo
            for (int i = 0; i < 3; i++) topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            EnableDrag(topGrid);

            _lblPageTitle = new Label
            {
                Font = Theme.FontH2,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceXl, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(_lblPageTitle);

            _btnLogout = Ui.Primary(Tr.T("MENU_SALIR"), Theme.IcoLogout);
            _btnLogout.Size = new Size(150, 34);
            _btnLogout.Anchor = AnchorStyles.Left;
            _btnLogout.BehindColor = Theme.BgTitleBar;
            _btnLogout.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnLogout.Click += (s, e) => DoLogout();

            var btnMin = WindowButton(Theme.IcoMinimize, (s, e) => WindowState = FormWindowState.Minimized);
            btnMin.Dock = DockStyle.Fill;
            btnMin.Margin = new Padding(0);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Close(), danger: true);
            btnClose.Dock = DockStyle.Fill;
            btnClose.Margin = new Padding(0);

            topGrid.Controls.Add(_lblPageTitle, 0, 0);
            topGrid.Controls.Add(_btnLogout, 1, 0);
            topGrid.Controls.Add(btnMin, 2, 0);
            topGrid.Controls.Add(btnClose, 3, 0);
            pnlTop.Controls.Add(topGrid);

            // ---------------- Pie (footer) con selector de idioma a la derecha ----------------
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Theme.BgContent,
                Padding = new Padding(0, 0, Theme.SpaceXl, 0)
            };
            var footerSep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
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
            _lang = new LangSelector(dark: false, allowManage: true) { Anchor = AnchorStyles.Right };
            footerGrid.Controls.Add(_lang, 1, 0);
            footer.Controls.Add(footerGrid);
            footer.Controls.Add(footerSep);

            // ---------------- Contenido ----------------
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceMd)
            };

            // Orden de Add: menu (Left) primero en docking, luego topbar (Top),
            // footer (Bottom) y por ultimo el contenido (Fill). Topbar y footer
            // abarcan solo la columna de contenido (a la derecha del menu).
            Controls.Add(_pnlContent);
            Controls.Add(footer);
            Controls.Add(pnlTop);
            Controls.Add(pnlMenu);
        }

        // -------- Navegacion --------
        private void Navegar(SideMenuItem item)
        {
            SetActive(item);
            _pnlContent.Controls.Clear();

            Control vista;
            if (item == _itInicio)         vista = BuildInicio();
            else if (item == _itReservas)  vista = new ucReservas();
            else if (item == _itClientes)  vista = new ucClientes();
            else if (item == _itServicios) vista = new ucServicios();
            else if (item == _itPerfiles)  vista = new ucPerfiles();
            else                           vista = new ucAuditoriaHub();

            vista.Dock = DockStyle.Fill;
            _pnlContent.Controls.Add(vista);
        }

        private void SetActive(SideMenuItem item)
        {
            if (_activo != null) _activo.SetActive(false);
            _activo = item;
            if (item != null)
            {
                item.SetActive(true);
                _lblPageTitle.Text = Tr.T(item.Key);
            }
        }

        private Control BuildInicio()
        {
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgContent };

            var card = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 170,
                BehindColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceXl, Theme.SpaceXl, Theme.SpaceXl)
            };

            string usuario = SessionManager.IsSessionActive ? SessionManager.GetInstance.User.Username : "?";
            var lblWelcome = new Label
            {
                Text = Tr.T("MAIN_WELCOME"),
                Font = Theme.FontH1,
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(Theme.SpaceXl, Theme.SpaceLg),
                BackColor = Color.Transparent
            };
            var lblBody = new Label
            {
                Text = Tr.T("MAIN_SESSION") + " " + usuario + Environment.NewLine + Environment.NewLine +
                       Tr.T("MAIN_SUBTITLE"),
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Location = new Point(Theme.SpaceXl, 64),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblBody);
            card.Controls.Add(lblWelcome);

            host.Controls.Add(card);
            return host;
        }

        // Refresca el selector de idioma (lo usa ucIdiomas tras crear/editar idiomas).
        public void RefrescarIdiomas() => _lang?.Repopulate();

        // Observador (patron Observer): refresca textos sin recrear el form.
        public void ActualizarTextos()
        {
            foreach (var it in _items) it.Caption.Text = Tr.T(it.Key);
            if (_btnLogout != null) _btnLogout.Text = Tr.T("MENU_SALIR");
            if (_lblWelcome != null)
            {
                string usuario = SessionManager.IsSessionActive ? SessionManager.GetInstance.User.Username : "?";
                _lblWelcome.Text = T("MAIN_HELLO", "Bienvenido") + " " + usuario;
            }
            if (_activo != null) _lblPageTitle.Text = Tr.T(_activo.Key);

            if (SessionManager.IsSessionActive && SessionManager.GetInstance.SinPerfil)
                MostrarBloqueado();                       // usuario sin rol: pantalla bloqueada
            else if (_activo == _itInicio)
                Navegar(_itInicio);                       // recarga la portada traducida
        }

        // Pantalla para usuarios sin perfil asignado: mensaje + sin navegacion
        // (solo pueden cerrar sesion).
        private void MostrarBloqueado()
        {
            _activo = null;
            _lblPageTitle.Text = "EvenTech";
            _pnlContent.Controls.Clear();

            var card = new CardPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BehindColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl)
            };
            var icon = new Label
            {
                Text = Theme.IcoLock,
                Font = new Font("Segoe MDL2 Assets", 26F),
                ForeColor = Theme.Warning,
                AutoSize = true,
                Location = new Point(Theme.SpaceXl, Theme.SpaceXl),
                BackColor = Color.Transparent
            };
            var lblTit = new Label
            {
                Text = T("MAIN_SIN_ROL_TIT", "Acceso restringido"),
                Font = Theme.FontH1,
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(72, Theme.SpaceLg),
                BackColor = Color.Transparent
            };
            var lblMsg = new Label
            {
                Text = T("MAIN_SIN_ROL", "Tu cuenta todavia no tiene un perfil asignado. Contactate con un administrador para que te asigne uno."),
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Location = new Point(72, 60),
                Size = new Size(760, 50),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblMsg);
            card.Controls.Add(lblTit);
            card.Controls.Add(icon);
            _pnlContent.Controls.Add(card);
        }

        // Traduccion con fallback al texto por defecto si la clave no existe.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        private void DoLogout()
        {
            try { BLL_Login.Logout(); } catch { /* ignorar */ }
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorDeIdioma.GetInstance.Desuscribir(this);
            if (SessionManager.IsSessionActive)
            {
                try { BLL_Login.Logout(); } catch { }
            }
            base.OnFormClosing(e);
        }

        // ============================================================
        // Item de menu lateral: barra de acento + icono + texto, con
        // estados hover/activo. Encapsula su propio cromo (cohesion).
        // ============================================================
        private sealed class SideMenuItem : Panel
        {
            public readonly string Key;
            public readonly Label Caption;
            private readonly Panel _bar;
            private readonly Label _icon;
            private bool _active;

            public SideMenuItem(string glyph, string key, EventHandler onClick)
            {
                Key = key;
                Dock = DockStyle.Top;
                Height = 48;
                BackColor = Theme.BgSidebar;
                Cursor = Cursors.Hand;

                _bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.BgSidebar };
                _icon = new Label
                {
                    Text = glyph,
                    Font = Theme.FontIcon,
                    ForeColor = Theme.Accent,
                    Dock = DockStyle.Left,
                    Width = 50,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                Caption = new Label
                {
                    Font = Theme.FontMenu,
                    ForeColor = Theme.TextLight,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };

                Controls.Add(Caption);
                Controls.Add(_icon);
                Controls.Add(_bar);

                foreach (Control c in new Control[] { this, _icon, Caption })
                {
                    c.MouseEnter += (s, e) => Hover(true);
                    c.MouseLeave += (s, e) => Hover(false);
                    c.Click += onClick;
                }
            }

            private void Hover(bool on)
            {
                if (_active) return;
                BackColor = on ? Theme.SidebarHover : Theme.BgSidebar;
            }

            public void SetActive(bool active)
            {
                _active = active;
                _bar.BackColor = active ? Theme.Accent : Theme.BgSidebar;
                BackColor = active ? Theme.SidebarActive : Theme.BgSidebar;
                Caption.ForeColor = active ? Theme.TextOnDark : Theme.TextLight;
                _icon.ForeColor = Theme.Accent;
            }
        }
    }
}
