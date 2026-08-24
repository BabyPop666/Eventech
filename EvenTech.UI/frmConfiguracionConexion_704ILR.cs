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
    public class frmConfiguracionConexion_704ILR : FormBase_704ILR
    {
        private ComboBox _cboServidor_704ILR;
        private TextBox _txtBase_704ILR;
        private Label _lblEstado_704ILR;
        private AppButton_704ILR _btnProbar_704ILR, _btnGuardar_704ILR;

        // true cuando se guardo una configuracion que conecta: el llamador puede
        // reintentar el arranque sin volver a preguntar.
        public bool Configurada_704ILR { get; private set; }

        public frmConfiguracionConexion_704ILR(string mensajeInicial_704ILR = null)
        {
            BuildUi_704ILR();
            if (!string.IsNullOrEmpty(mensajeInicial_704ILR)) Estado_704ILR(mensajeInicial_704ILR, error_704ILR: true);
        }

        private void BuildUi_704ILR()
        {
            Text = "EvenTech";
            ClientSize = new Size(560, 400);
            BackColor = Theme_704ILR.BgContent_704ILR;

            // ---------------- Barra de titulo ----------------
            var pnlTitle_704ILR = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTitle_704ILR);
            var lblTitle_704ILR = new Label
            {
                Text = T_704ILR("CONN_TITULO", "Configuracion de conexion"),
                Font = Theme_704ILR.FontH2_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(lblTitle_704ILR);
            var btnCerrar_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => { DialogResult = DialogResult.Cancel; Close(); }, danger_704ILR: true);
            btnCerrar_704ILR.Dock = DockStyle.Right;
            pnlTitle_704ILR.Controls.Add(lblTitle_704ILR);
            pnlTitle_704ILR.Controls.Add(btnCerrar_704ILR);

            // ---------------- Cuerpo ----------------
            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                BehindColor_704ILR = Theme_704ILR.BgContent_704ILR,
                Margin = new Padding(Theme_704ILR.SpaceLg_704ILR),
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR)
            };

            var layout_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // ayuda
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // servidor
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // base
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // estado
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // botones

            var lblAyuda_704ILR = new Label
            {
                Text = T_704ILR("CONN_AYUDA",
                    "No se pudo conectar a la base de datos. Indica donde esta la instancia de SQL Server " +
                    "y el nombre de la base. La configuracion se guarda cifrada en tu perfil de Windows."),
                Font = Theme_704ILR.FontBody_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 58,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };

            // Instancia: editable (se puede tipear una que la deteccion no encontro).
            _cboServidor_704ILR = new ComboBox
            {
                Font = Theme_704ILR.FontInput_704ILR,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Flat
            };
            foreach (string i_704ILR in BLL_Conexion_704ILR.GetInstancias_704ILR()) _cboServidor_704ILR.Items.Add(i_704ILR);
            _cboServidor_704ILR.Text = BLL_Conexion_704ILR.ServidorActual_704ILR;

            _txtBase_704ILR = Ui_704ILR.Input_704ILR();
            _txtBase_704ILR.Text = BLL_Conexion_704ILR.BaseDatosActual_704ILR;

            _lblEstado_704ILR = new Label
            {
                Font = Theme_704ILR.FontBody_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(2, Theme_704ILR.SpaceSm_704ILR, 0, 0)
            };

            // ---------------- Botones ----------------
            var botones_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            botones_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            botones_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            botones_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var btnSalir_704ILR = Ui_704ILR.Secondary_704ILR(T_704ILR("CONN_SALIR", "Salir"));
            btnSalir_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            btnSalir_704ILR.Size = new Size(110, 38);
            btnSalir_704ILR.Anchor = AnchorStyles.Left;
            btnSalir_704ILR.Click += (s_704ILR, e_704ILR) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnProbar_704ILR = Ui_704ILR.Secondary_704ILR(T_704ILR("CONN_PROBAR", "Probar"));
            _btnProbar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnProbar_704ILR.Size = new Size(130, 38);
            _btnProbar_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceSm_704ILR, 0);
            _btnProbar_704ILR.Click += (s_704ILR, e_704ILR) => Probar_704ILR();

            _btnGuardar_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("CONN_GUARDAR", "Guardar"), Theme_704ILR.IcoSave_704ILR);
            _btnGuardar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnGuardar_704ILR.Size = new Size(150, 38);
            _btnGuardar_704ILR.Click += (s_704ILR, e_704ILR) => Guardar_704ILR();

            botones_704ILR.Controls.Add(btnSalir_704ILR, 0, 0);
            botones_704ILR.Controls.Add(_btnProbar_704ILR, 1, 0);
            botones_704ILR.Controls.Add(_btnGuardar_704ILR, 2, 0);

            layout_704ILR.Controls.Add(lblAyuda_704ILR, 0, 0);
            layout_704ILR.Controls.Add(Ui_704ILR.Field_704ILR(T_704ILR("CONN_SERVIDOR", "Instancia de SQL Server"), _cboServidor_704ILR), 0, 1);
            layout_704ILR.Controls.Add(Ui_704ILR.Field_704ILR(T_704ILR("CONN_BASE", "Base de datos"), _txtBase_704ILR), 0, 2);
            layout_704ILR.Controls.Add(_lblEstado_704ILR, 0, 3);
            layout_704ILR.Controls.Add(botones_704ILR, 0, 4);

            card_704ILR.Controls.Add(layout_704ILR);

            var host_704ILR = new Panel { Dock = DockStyle.Fill, BackColor = Theme_704ILR.BgContent_704ILR, Padding = new Padding(Theme_704ILR.SpaceLg_704ILR) };
            host_704ILR.Controls.Add(card_704ILR);

            Controls.Add(host_704ILR);
            Controls.Add(pnlTitle_704ILR);
            AcceptButton = _btnGuardar_704ILR;
        }

        private void Probar_704ILR()
        {
            Estado_704ILR(T_704ILR("CONN_PROBANDO", "Probando conexion..."), error_704ILR: false);
            Application.DoEvents();      // feedback inmediato: la prueba bloquea hasta 5 s

            if (BLL_Conexion_704ILR.Probar_704ILR(_cboServidor_704ILR.Text, _txtBase_704ILR.Text, out string msg_704ILR))
                Estado_704ILR(T_704ILR("CONN_OK", "Conexion correcta."), error_704ILR: false, ok_704ILR: true);
            else
                Estado_704ILR(msg_704ILR, error_704ILR: true);
        }

        private void Guardar_704ILR()
        {
            Estado_704ILR(T_704ILR("CONN_PROBANDO", "Probando conexion..."), error_704ILR: false);
            Application.DoEvents();

            // Guardar solo si conecta: evita dejar la app apuntando a una
            // instancia inexistente y tener que reconfigurar a ciegas.
            if (!BLL_Conexion_704ILR.Guardar_704ILR(_cboServidor_704ILR.Text, _txtBase_704ILR.Text, out string msg_704ILR))
            {
                Estado_704ILR(msg_704ILR, error_704ILR: true);
                return;
            }

            Configurada_704ILR = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Estado_704ILR(string texto_704ILR, bool error_704ILR, bool ok_704ILR = false)
        {
            _lblEstado_704ILR.Text = texto_704ILR;
            _lblEstado_704ILR.ForeColor = error_704ILR ? Theme_704ILR.Error_704ILR : (ok_704ILR ? Theme_704ILR.Success_704ILR : Theme_704ILR.TextMuted_704ILR);
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
