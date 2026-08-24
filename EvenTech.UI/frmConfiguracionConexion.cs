using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Configuracion de la conexion a la base. Se abre cuando el arranque no logra
    // conectar (antes del login) y permite elegir instancia y base, probar la
    // conexion y guardarla cifrada.
    //
    // Corre ANTES de que se carguen las traducciones desde la base, asi que todos
    // los textos usan T(clave, defecto): si el diccionario todavia no esta, se ve
    // el texto por defecto en vez de la clave cruda.
    public class frmConfiguracionConexion : FormBase
    {
        private ComboBox _cboServidor;
        private TextBox _txtBase;
        private Label _lblEstado;
        private AppButton _btnProbar, _btnGuardar;

        // true cuando se guardo una configuracion que conecta: el llamador puede
        // reintentar el arranque sin volver a preguntar.
        public bool Configurada { get; private set; }

        public frmConfiguracionConexion(string mensajeInicial = null)
        {
            BuildUi();
            if (!string.IsNullOrEmpty(mensajeInicial)) Estado(mensajeInicial, error: true);
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(560, 400);
            BackColor = Theme.BgContent;

            // ---------------- Barra de titulo ----------------
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);
            var lblTitle = new Label
            {
                Text = T("CONN_TITULO", "Configuracion de conexion"),
                Font = Theme.FontH2,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnCerrar = WindowButton(Theme.IcoClose, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }, danger: true);
            btnCerrar.Dock = DockStyle.Right;
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(btnCerrar);

            // ---------------- Cuerpo ----------------
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                BehindColor = Theme.BgContent,
                Margin = new Padding(Theme.SpaceLg),
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceLg)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // ayuda
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // servidor
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // base
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // estado
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // botones

            var lblAyuda = new Label
            {
                Text = T("CONN_AYUDA",
                    "No se pudo conectar a la base de datos. Indica donde esta la instancia de SQL Server " +
                    "y el nombre de la base. La configuracion se guarda cifrada en tu perfil de Windows."),
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 58,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd)
            };

            // Instancia: editable (se puede tipear una que la deteccion no encontro).
            _cboServidor = new ComboBox
            {
                Font = Theme.FontInput,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Flat
            };
            foreach (string i in BLL_Conexion.GetInstancias()) _cboServidor.Items.Add(i);
            _cboServidor.Text = BLL_Conexion.ServidorActual;

            _txtBase = Ui.Input();
            _txtBase.Text = BLL_Conexion.BaseDatosActual;

            _lblEstado = new Label
            {
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(2, Theme.SpaceSm, 0, 0)
            };

            // ---------------- Botones ----------------
            var botones = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var btnSalir = Ui.Secondary(T("CONN_SALIR", "Salir"));
            btnSalir.BehindColor = Theme.BgContent;
            btnSalir.Size = new Size(110, 38);
            btnSalir.Anchor = AnchorStyles.Left;
            btnSalir.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnProbar = Ui.Secondary(T("CONN_PROBAR", "Probar"));
            _btnProbar.BehindColor = Theme.BgContent;
            _btnProbar.Size = new Size(130, 38);
            _btnProbar.Margin = new Padding(0, 0, Theme.SpaceSm, 0);
            _btnProbar.Click += (s, e) => Probar();

            _btnGuardar = Ui.Primary(T("CONN_GUARDAR", "Guardar"), Theme.IcoSave);
            _btnGuardar.BehindColor = Theme.BgContent;
            _btnGuardar.Size = new Size(150, 38);
            _btnGuardar.Click += (s, e) => Guardar();

            botones.Controls.Add(btnSalir, 0, 0);
            botones.Controls.Add(_btnProbar, 1, 0);
            botones.Controls.Add(_btnGuardar, 2, 0);

            layout.Controls.Add(lblAyuda, 0, 0);
            layout.Controls.Add(Ui.Field(T("CONN_SERVIDOR", "Instancia de SQL Server"), _cboServidor), 0, 1);
            layout.Controls.Add(Ui.Field(T("CONN_BASE", "Base de datos"), _txtBase), 0, 2);
            layout.Controls.Add(_lblEstado, 0, 3);
            layout.Controls.Add(botones, 0, 4);

            card.Controls.Add(layout);

            var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgContent, Padding = new Padding(Theme.SpaceLg) };
            host.Controls.Add(card);

            Controls.Add(host);
            Controls.Add(pnlTitle);
            AcceptButton = _btnGuardar;
        }

        private void Probar()
        {
            Estado(T("CONN_PROBANDO", "Probando conexion..."), error: false);
            Application.DoEvents();      // feedback inmediato: la prueba bloquea hasta 5 s

            if (BLL_Conexion.Probar(_cboServidor.Text, _txtBase.Text, out string msg))
                Estado(T("CONN_OK", "Conexion correcta."), error: false, ok: true);
            else
                Estado(msg, error: true);
        }

        private void Guardar()
        {
            Estado(T("CONN_PROBANDO", "Probando conexion..."), error: false);
            Application.DoEvents();

            // Guardar solo si conecta: evita dejar la app apuntando a una
            // instancia inexistente y tener que reconfigurar a ciegas.
            if (!BLL_Conexion.Guardar(_cboServidor.Text, _txtBase.Text, out string msg))
            {
                Estado(msg, error: true);
                return;
            }

            Configurada = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Estado(string texto, bool error, bool ok = false)
        {
            _lblEstado.Text = texto;
            _lblEstado.ForeColor = error ? Theme.Error : (ok ? Theme.Success : Theme.TextMuted);
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
