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
    public class frmMain_704ILR : FormBase_704ILR, IObservadorIdioma_704ILR
    {
        private Panel _pnlContent_704ILR;
        private Label _lblPageTitle_704ILR, _lblWelcome_704ILR;
        private AppButton_704ILR _btnLogout_704ILR;
        private LangSelector_704ILR _lang_704ILR;

        private SideMenuItem_704ILR _itInicio_704ILR, _itReservas_704ILR, _itClientes_704ILR, _itServicios_704ILR, _itPerfiles_704ILR, _itAuditoria_704ILR;
        private SideMenuItem_704ILR _activo_704ILR;
        private readonly List<SideMenuItem_704ILR> _items_704ILR = new List<SideMenuItem_704ILR>();

        public frmMain_704ILR()
        {
            BuildUi_704ILR();
            bool bloqueado_704ILR = SessionManager_704ILR.IsSessionActive_704ILR && SessionManager_704ILR.GetInstance_704ILR.SinPerfil_704ILR;
            if (bloqueado_704ILR)
            {
                foreach (var it_704ILR in _items_704ILR) it_704ILR.Visible = false;
                // Sin perfil no hay ningun permiso: el globo no debe ofrecer el ABM
                // de idiomas (se construye con allowManage:true por defecto).
                if (_lang_704ILR != null) _lang_704ILR.PermitirGestion_704ILR = false;
            }
            else AplicarPermisos_704ILR();
            ActualizarTextos_704ILR();              // si esta bloqueado, muestra el mensaje
            if (!bloqueado_704ILR) Navegar_704ILR(_itInicio_704ILR);
            GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            AvisarPermisosNoDisponibles_704ILR();
        }

        // Si los permisos del perfil no se pudieron resolver, la sesion quedo sin
        // ninguno (denegar por defecto). Se avisa para que el usuario entienda por
        // que no ve sus secciones y no lo confunda con una baja de permisos.
        private void AvisarPermisosNoDisponibles_704ILR()
        {
            if (!SessionManager_704ILR.IsSessionActive_704ILR || !SessionManager_704ILR.GetInstance_704ILR.PermisosNoDisponibles_704ILR) return;
            Shown += (s_704ILR, e_704ILR) => MessageBox.Show(this,
                T_704ILR("MAIN_PERMISOS_ERROR",
                  "No se pudieron cargar los permisos de tu perfil, asi que la sesion quedo sin acceso a las secciones. " +
                  "Volve a iniciar sesion; si el problema sigue, avisale a un administrador."),
                "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // Control de acceso (T04), primera capa: muestra/oculta cada seccion segun
        // los permisos efectivos del perfil. Toda seccion exige su permiso; la
        // unica sin restriccion es Inicio (portada de la sesion). La segunda capa
        // vive en Navegar(), que vuelve a exigir el permiso al abrir la vista.
        private void AplicarPermisos_704ILR()
        {
            if (!SessionManager_704ILR.IsSessionActive_704ILR) return;
            _itReservas_704ILR.Visible  = Permisos_704ILR.TieneAlguno_704ILR("RESERVA_CREAR", "RESERVA_EDITAR", "RESERVA_HISTORIAL");
            _itClientes_704ILR.Visible  = Permisos_704ILR.Tiene_704ILR("CLIENTES_GESTION");
            _itServicios_704ILR.Visible = Permisos_704ILR.Tiene_704ILR("SERVICIOS_GESTION");
            _itPerfiles_704ILR.Visible  = Permisos_704ILR.Tiene_704ILR("PERFILES_GESTION");
            _itAuditoria_704ILR.Visible = Permisos_704ILR.TieneAlguno_704ILR("BITACORA_VER", "AUDIT_LOGIN_VER");
            // La gestion de idiomas (ABM de traducciones) cuelga del globo del pie.
            if (_lang_704ILR != null) _lang_704ILR.PermitirGestion_704ILR = Permisos_704ILR.Tiene_704ILR("IDIOMAS_GESTION");
        }

        // Permisos que habilitan cada seccion del menu (fuente unica para la
        // primera y la segunda capa: evita que se desincronicen).
        private string[] PermisosDe_704ILR(SideMenuItem_704ILR item_704ILR)
        {
            if (item_704ILR == _itReservas_704ILR)  return new[] { "RESERVA_CREAR", "RESERVA_EDITAR", "RESERVA_HISTORIAL" };
            if (item_704ILR == _itClientes_704ILR)  return new[] { "CLIENTES_GESTION" };
            if (item_704ILR == _itServicios_704ILR) return new[] { "SERVICIOS_GESTION" };
            if (item_704ILR == _itPerfiles_704ILR)  return new[] { "PERFILES_GESTION" };
            if (item_704ILR == _itAuditoria_704ILR) return new[] { "BITACORA_VER", "AUDIT_LOGIN_VER" };
            return null;   // Inicio: sin restriccion
        }

        private void BuildUi_704ILR()
        {
            Text = "EvenTech";
            // Tamano por defecto acorde a la resolucion minima declarada en G05
            // (1366x768): con 1040x680 el area de trabajo quedaba en 760x550 y la
            // seccion de reservas no entraba (encabezados cortados y campos de la
            // ficha fuera de vista). La ventana ademas se puede maximizar y
            // redimensionar, asi que en pantallas mas grandes aprovecha el espacio.
            ClientSize = new Size(1355, 715);
            BackColor = Theme_704ILR.BgContent_704ILR;
            MinimumSize = new Size(1100, 680);
            Redimensionable_704ILR = true;

            // ---------------- Sidebar ----------------
            var pnlMenu_704ILR = new Panel { Dock = DockStyle.Left, Width = 232, BackColor = Theme_704ILR.BgSidebar_704ILR };

            var pnlLogo_704ILR = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Theme_704ILR.BgSidebar_704ILR };
            EnableDrag_704ILR(pnlLogo_704ILR);
            // Isologotipo del sistema en el extremo superior izquierdo, visible desde
            // cualquier seccion (G05). Si el recurso no estuviera disponible se cae al
            // rotulo de texto, de modo que la pantalla nunca queda sin identidad.
            Control lblLogo_704ILR;
            if (Theme_704ILR.Logo_704ILR != null)
            {
                lblLogo_704ILR = new PictureBox
                {
                    Image = Theme_704ILR.Logo_704ILR,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Top,
                    Height = 62,
                    BackColor = Color.Transparent
                };
            }
            else
            {
                lblLogo_704ILR = new Label
                {
                    Text = "EvenTech",
                    Font = Theme_704ILR.FontH1_704ILR,
                    ForeColor = Theme_704ILR.Accent_704ILR,
                    Dock = DockStyle.Top,
                    Height = 52,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
            }
            EnableDrag_704ILR(lblLogo_704ILR);
            _lblWelcome_704ILR = new Label
            {
                Font = Theme_704ILR.FontCaption_704ILR,
                ForeColor = Theme_704ILR.TextLight_704ILR,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(_lblWelcome_704ILR);
            var sep_704ILR = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme_704ILR.SidebarHover_704ILR };
            pnlLogo_704ILR.Controls.Add(_lblWelcome_704ILR);
            pnlLogo_704ILR.Controls.Add(lblLogo_704ILR);
            pnlLogo_704ILR.Controls.Add(sep_704ILR);

            _itInicio_704ILR    = new SideMenuItem_704ILR(Theme_704ILR.IcoHome_704ILR,     "MENU_INICIO",    (s_704ILR, e_704ILR) => Navegar_704ILR(_itInicio_704ILR));
            _itReservas_704ILR  = new SideMenuItem_704ILR(Theme_704ILR.IcoCalendar_704ILR, "MENU_RESERVAS",  (s_704ILR, e_704ILR) => Navegar_704ILR(_itReservas_704ILR));
            _itClientes_704ILR  = new SideMenuItem_704ILR(Theme_704ILR.IcoContact_704ILR,  "MENU_CLIENTES",  (s_704ILR, e_704ILR) => Navegar_704ILR(_itClientes_704ILR));
            _itServicios_704ILR = new SideMenuItem_704ILR(Theme_704ILR.IcoServicio_704ILR, "MENU_SERVICIOS", (s_704ILR, e_704ILR) => Navegar_704ILR(_itServicios_704ILR));
            _itPerfiles_704ILR  = new SideMenuItem_704ILR(Theme_704ILR.IcoPeople_704ILR,   "MENU_PERFILES",  (s_704ILR, e_704ILR) => Navegar_704ILR(_itPerfiles_704ILR));
            _itAuditoria_704ILR = new SideMenuItem_704ILR(Theme_704ILR.IcoHistory_704ILR,  "MENU_AUDITORIA", (s_704ILR, e_704ILR) => Navegar_704ILR(_itAuditoria_704ILR));
            _items_704ILR.AddRange(new[] { _itInicio_704ILR, _itReservas_704ILR, _itClientes_704ILR, _itServicios_704ILR, _itPerfiles_704ILR, _itAuditoria_704ILR });

            // Dock=Top se apila en orden inverso al de agregado.
            // Idiomas salio del menu (se gestiona desde el globo del pie).
            pnlMenu_704ILR.Controls.Add(_itAuditoria_704ILR);
            pnlMenu_704ILR.Controls.Add(_itPerfiles_704ILR);
            pnlMenu_704ILR.Controls.Add(_itServicios_704ILR);
            pnlMenu_704ILR.Controls.Add(_itClientes_704ILR);
            pnlMenu_704ILR.Controls.Add(_itReservas_704ILR);
            pnlMenu_704ILR.Controls.Add(_itInicio_704ILR);
            pnlMenu_704ILR.Controls.Add(pnlLogo_704ILR);

            // ---------------- Topbar ----------------
            var pnlTop_704ILR = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTop_704ILR);

            var topGrid_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            topGrid_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // titulo
            for (int i_704ILR = 0; i_704ILR < 4; i_704ILR++) topGrid_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topGrid_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            EnableDrag_704ILR(topGrid_704ILR);

            _lblPageTitle_704ILR = new Label
            {
                Font = Theme_704ILR.FontH2_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(_lblPageTitle_704ILR);

            _btnLogout_704ILR = Ui_704ILR.Primary_704ILR(Tr_704ILR.T_704ILR("MENU_SALIR"), Theme_704ILR.IcoLogout_704ILR);
            _btnLogout_704ILR.Size = new Size(150, 34);
            _btnLogout_704ILR.Anchor = AnchorStyles.Left;
            _btnLogout_704ILR.BehindColor_704ILR = Theme_704ILR.BgTitleBar_704ILR;
            _btnLogout_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnLogout_704ILR.Click += (s_704ILR, e_704ILR) => DoLogout_704ILR();

            var btnMin_704ILR = WindowButton_704ILR(Theme_704ILR.IcoMinimize_704ILR, (s_704ILR, e_704ILR) => WindowState = FormWindowState.Minimized);
            btnMin_704ILR.Dock = DockStyle.Fill;
            btnMin_704ILR.Margin = new Padding(0);
            var btnMax_704ILR = WindowButton_704ILR(Theme_704ILR.IcoMaximize_704ILR, (s_704ILR, e_704ILR) =>
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized);
            btnMax_704ILR.Dock = DockStyle.Fill;
            btnMax_704ILR.Margin = new Padding(0);
            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => Close(), danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Fill;
            btnClose_704ILR.Margin = new Padding(0);

            topGrid_704ILR.Controls.Add(_lblPageTitle_704ILR, 0, 0);
            topGrid_704ILR.Controls.Add(_btnLogout_704ILR, 1, 0);
            topGrid_704ILR.Controls.Add(btnMin_704ILR, 2, 0);
            topGrid_704ILR.Controls.Add(btnMax_704ILR, 3, 0);
            topGrid_704ILR.Controls.Add(btnClose_704ILR, 4, 0);
            pnlTop_704ILR.Controls.Add(topGrid_704ILR);

            // ---------------- Pie (footer) con selector de idioma a la derecha ----------------
            var footer_704ILR = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(0, 0, Theme_704ILR.SpaceXl_704ILR, 0)
            };
            var footerSep_704ILR = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme_704ILR.Border_704ILR };
            var footerGrid_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            footerGrid_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footerGrid_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerGrid_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _lang_704ILR = new LangSelector_704ILR(dark_704ILR: false, allowManage_704ILR: true) { Anchor = AnchorStyles.Right };
            footerGrid_704ILR.Controls.Add(_lang_704ILR, 1, 0);
            footer_704ILR.Controls.Add(footerGrid_704ILR);
            footer_704ILR.Controls.Add(footerSep_704ILR);

            // ---------------- Contenido ----------------
            _pnlContent_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceMd_704ILR)
            };

            // Orden de Add: menu (Left) primero en docking, luego topbar (Top),
            // footer (Bottom) y por ultimo el contenido (Fill). Topbar y footer
            // abarcan solo la columna de contenido (a la derecha del menu).
            Controls.Add(_pnlContent_704ILR);
            Controls.Add(footer_704ILR);
            Controls.Add(pnlTop_704ILR);
            Controls.Add(pnlMenu_704ILR);
        }

        // -------- Navegacion --------
        private void Navegar_704ILR(SideMenuItem_704ILR item_704ILR)
        {
            // Segunda capa del control de acceso: el permiso se vuelve a exigir
            // aca y no solo al armar el menu. Si el item quedo visible por error
            // o la vista se alcanza por otra via, la navegacion se corta igual.
            string[] requeridos_704ILR = PermisosDe_704ILR(item_704ILR);
            if (requeridos_704ILR != null &&
                !Permisos_704ILR.ExigirAlguno_704ILR(this, "abrir la seccion " + Tr_704ILR.T_704ILR(item_704ILR.Key_704ILR), requeridos_704ILR))
                return;

            SetActive_704ILR(item_704ILR);
            _pnlContent_704ILR.Controls.Clear();

            Control vista_704ILR;
            if (item_704ILR == _itInicio_704ILR)         vista_704ILR = BuildInicio_704ILR();
            else if (item_704ILR == _itReservas_704ILR)  vista_704ILR = new ucReservas_704ILR();
            else if (item_704ILR == _itClientes_704ILR)  vista_704ILR = new ucClientes_704ILR();
            else if (item_704ILR == _itServicios_704ILR) vista_704ILR = new ucServicios_704ILR();
            else if (item_704ILR == _itPerfiles_704ILR)  vista_704ILR = new ucPerfiles_704ILR();
            else                           vista_704ILR = new ucAuditoriaHub_704ILR();

            vista_704ILR.Dock = DockStyle.Fill;
            _pnlContent_704ILR.Controls.Add(vista_704ILR);
        }

        private void SetActive_704ILR(SideMenuItem_704ILR item_704ILR)
        {
            if (_activo_704ILR != null) _activo_704ILR.SetActive_704ILR(false);
            _activo_704ILR = item_704ILR;
            if (item_704ILR != null)
            {
                item_704ILR.SetActive_704ILR(true);
                _lblPageTitle_704ILR.Text = Tr_704ILR.T_704ILR(item_704ILR.Key_704ILR);
            }
        }

        private Control BuildInicio_704ILR()
        {
            var host_704ILR = new Panel { Dock = DockStyle.Fill, BackColor = Theme_704ILR.BgContent_704ILR };

            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Top,
                Height = 170,
                BehindColor_704ILR = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceXl_704ILR)
            };

            string usuario_704ILR = SessionManager_704ILR.IsSessionActive_704ILR ? SessionManager_704ILR.GetInstance_704ILR.User_704ILR.Username_704ILR : "?";
            var lblWelcome_704ILR = new Label
            {
                Text = Tr_704ILR.T_704ILR("MAIN_WELCOME"),
                Font = Theme_704ILR.FontH1_704ILR,
                ForeColor = Theme_704ILR.TextOnLight_704ILR,
                AutoSize = true,
                Location = new Point(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR),
                BackColor = Color.Transparent
            };
            var lblBody_704ILR = new Label
            {
                Text = Tr_704ILR.T_704ILR("MAIN_SESSION") + " " + usuario_704ILR + Environment.NewLine + Environment.NewLine +
                       Tr_704ILR.T_704ILR("MAIN_SUBTITLE"),
                Font = Theme_704ILR.FontBody_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Location = new Point(Theme_704ILR.SpaceXl_704ILR, 64),
                BackColor = Color.Transparent
            };
            card_704ILR.Controls.Add(lblBody_704ILR);
            card_704ILR.Controls.Add(lblWelcome_704ILR);

            host_704ILR.Controls.Add(card_704ILR);
            return host_704ILR;
        }

        // Refresca el selector de idioma (lo usa ucIdiomas tras crear/editar idiomas).
        public void RefrescarIdiomas_704ILR() => _lang_704ILR?.Repopulate_704ILR();

        // Observador (patron Observer): refresca textos sin recrear el form.
        public void ActualizarTextos_704ILR()
        {
            foreach (var it_704ILR in _items_704ILR) it_704ILR.Caption_704ILR.Text = Tr_704ILR.T_704ILR(it_704ILR.Key_704ILR);
            if (_btnLogout_704ILR != null) _btnLogout_704ILR.Text = Tr_704ILR.T_704ILR("MENU_SALIR");
            if (_lblWelcome_704ILR != null)
            {
                string usuario_704ILR = SessionManager_704ILR.IsSessionActive_704ILR ? SessionManager_704ILR.GetInstance_704ILR.User_704ILR.Username_704ILR : "?";
                _lblWelcome_704ILR.Text = T_704ILR("MAIN_HELLO", "Bienvenido") + " " + usuario_704ILR;
            }
            if (_activo_704ILR != null) _lblPageTitle_704ILR.Text = Tr_704ILR.T_704ILR(_activo_704ILR.Key_704ILR);

            if (SessionManager_704ILR.IsSessionActive_704ILR && SessionManager_704ILR.GetInstance_704ILR.SinPerfil_704ILR)
                MostrarBloqueado_704ILR();                       // usuario sin rol: pantalla bloqueada
            else if (_activo_704ILR == _itInicio_704ILR)
                Navegar_704ILR(_itInicio_704ILR);                       // recarga la portada traducida
        }

        // Pantalla para usuarios sin perfil asignado: mensaje + sin navegacion
        // (solo pueden cerrar sesion).
        private void MostrarBloqueado_704ILR()
        {
            _activo_704ILR = null;
            _lblPageTitle_704ILR.Text = "EvenTech";
            _pnlContent_704ILR.Controls.Clear();

            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Top,
                Height = 150,
                BehindColor_704ILR = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR)
            };
            var icon_704ILR = new Label
            {
                Text = Theme_704ILR.IcoLock_704ILR,
                Font = new Font("Segoe MDL2 Assets", 26F),
                ForeColor = Theme_704ILR.Warning_704ILR,
                AutoSize = true,
                Location = new Point(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceXl_704ILR),
                BackColor = Color.Transparent
            };
            var lblTit_704ILR = new Label
            {
                Text = T_704ILR("MAIN_SIN_ROL_TIT", "Acceso restringido"),
                Font = Theme_704ILR.FontH1_704ILR,
                ForeColor = Theme_704ILR.TextOnLight_704ILR,
                AutoSize = true,
                Location = new Point(72, Theme_704ILR.SpaceLg_704ILR),
                BackColor = Color.Transparent
            };
            var lblMsg_704ILR = new Label
            {
                Text = T_704ILR("MAIN_SIN_ROL", "Tu cuenta todavia no tiene un perfil asignado. Contactate con un administrador para que te asigne uno."),
                Font = Theme_704ILR.FontBody_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                AutoSize = false,
                Location = new Point(72, 60),
                Size = new Size(760, 50),
                BackColor = Color.Transparent
            };
            card_704ILR.Controls.Add(lblMsg_704ILR);
            card_704ILR.Controls.Add(lblTit_704ILR);
            card_704ILR.Controls.Add(icon_704ILR);
            _pnlContent_704ILR.Controls.Add(card_704ILR);
        }

        // Traduccion con fallback al texto por defecto si la clave no existe.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        private void DoLogout_704ILR()
        {
            try { BLL_Login_704ILR.Logout_704ILR(); } catch { /* ignorar */ }
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e_704ILR)
        {
            GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
            if (SessionManager_704ILR.IsSessionActive_704ILR)
            {
                try { BLL_Login_704ILR.Logout_704ILR(); } catch { }
            }
            base.OnFormClosing(e_704ILR);
        }

        // ============================================================
        // Item de menu lateral: barra de acento + icono + texto, con
        // estados hover/activo. Encapsula su propio cromo (cohesion).
        // ============================================================
        private sealed class SideMenuItem_704ILR : Panel
        {
            public readonly string Key_704ILR;
            public readonly Label Caption_704ILR;
            private readonly Panel _bar_704ILR;
            private readonly Label _icon_704ILR;
            private bool _active_704ILR;

            public SideMenuItem_704ILR(string glyph_704ILR, string key_704ILR, EventHandler onClick_704ILR)
            {
                Key_704ILR = key_704ILR;
                Dock = DockStyle.Top;
                Height = 48;
                BackColor = Theme_704ILR.BgSidebar_704ILR;
                Cursor = Cursors.Hand;

                _bar_704ILR = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme_704ILR.BgSidebar_704ILR };
                _icon_704ILR = new Label
                {
                    Text = glyph_704ILR,
                    Font = Theme_704ILR.FontIcon_704ILR,
                    ForeColor = Theme_704ILR.Accent_704ILR,
                    Dock = DockStyle.Left,
                    Width = 50,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                Caption_704ILR = new Label
                {
                    Font = Theme_704ILR.FontMenu_704ILR,
                    ForeColor = Theme_704ILR.TextLight_704ILR,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };

                Controls.Add(Caption_704ILR);
                Controls.Add(_icon_704ILR);
                Controls.Add(_bar_704ILR);

                foreach (Control c_704ILR in new Control[] { this, _icon_704ILR, Caption_704ILR })
                {
                    c_704ILR.MouseEnter += (s_704ILR, e_704ILR) => Hover_704ILR(true);
                    c_704ILR.MouseLeave += (s_704ILR, e_704ILR) => Hover_704ILR(false);
                    c_704ILR.Click += onClick_704ILR;
                }
            }

            private void Hover_704ILR(bool on_704ILR)
            {
                if (_active_704ILR) return;
                BackColor = on_704ILR ? Theme_704ILR.SidebarHover_704ILR : Theme_704ILR.BgSidebar_704ILR;
            }

            public void SetActive_704ILR(bool active_704ILR)
            {
                _active_704ILR = active_704ILR;
                _bar_704ILR.BackColor = active_704ILR ? Theme_704ILR.Accent_704ILR : Theme_704ILR.BgSidebar_704ILR;
                BackColor = active_704ILR ? Theme_704ILR.SidebarActive_704ILR : Theme_704ILR.BgSidebar_704ILR;
                Caption_704ILR.ForeColor = active_704ILR ? Theme_704ILR.TextOnDark_704ILR : Theme_704ILR.TextLight_704ILR;
                _icon_704ILR.ForeColor = Theme_704ILR.Accent_704ILR;
            }
        }
    }
}
