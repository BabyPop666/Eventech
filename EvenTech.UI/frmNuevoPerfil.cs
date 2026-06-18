using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Popup para dar de alta un perfil (nombre + descripcion). Reusa
    // BLL_Perfil.CrearPerfil. Devuelve DialogResult.OK y el Id en NuevoId.
    public class frmNuevoPerfil : FormBase
    {
        private TextBox _txtNombre, _txtDesc;
        private Label _lblMsg;

        public int NuevoId { get; private set; }

        public frmNuevoPerfil()
        {
            BuildUi();
            Shown += (s, e) => _txtNombre.Focus();
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(380, 252);
            BackColor = Theme.BgContent;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);
            var lblTitle = new Label
            {
                Text = T("PERF_NUEVO", "Nuevo perfil"),
                Font = Theme.FontH2,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }, danger: true);
            btnClose.Dock = DockStyle.Right;
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(btnClose);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceLg)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtNombre = Ui.Input(); _txtNombre.MaxLength = 80;
            var fNombre = Ui.Field(T("IDI_NOMBRE", "Nombre"), _txtNombre); fNombre.Dock = DockStyle.Fill;
            _txtDesc = Ui.Input(); _txtDesc.MaxLength = 250;
            var fDesc = Ui.Field(T("PERF_DESC", "Descripcion"), _txtDesc); fDesc.Dock = DockStyle.Fill;
            _lblMsg = new Label { Dock = DockStyle.Fill, Font = Theme.FontSmall, ForeColor = Theme.Error, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft };

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent, Margin = new Padding(0) };
            var btnCrear = Ui.Primary(T("PERF_CREAR", "Crear perfil"), Theme.IcoAdd); btnCrear.BehindColor = Theme.BgContent; btnCrear.Size = new Size(150, 38); btnCrear.Click += (s, e) => Crear();
            var btnCancelar = Ui.Secondary(T("BTN_CANCELAR", "Cancelar")); btnCancelar.BehindColor = Theme.BgContent; btnCancelar.Size = new Size(110, 38); btnCancelar.Margin = new Padding(Theme.SpaceSm, 0, 0, 0); btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            acciones.Controls.Add(btnCrear); acciones.Controls.Add(btnCancelar);

            body.Controls.Add(fNombre, 0, 0);
            body.Controls.Add(fDesc, 0, 1);
            body.Controls.Add(_lblMsg, 0, 2);
            body.Controls.Add(acciones, 0, 3);
            Controls.Add(body);
            Controls.Add(pnlTitle);
            AcceptButton = btnCrear;
        }

        private void Crear()
        {
            try
            {
                PerfilResult r = BLL_Perfil.CrearPerfil(_txtNombre.Text, _txtDesc.Text, out int id);
                if (r != PerfilResult.Success) { _lblMsg.Text = MensajeError(r); return; }
                NuevoId = id;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Crear perfil (popup)");
                _lblMsg.Text = Tr.T("MSG_ERROR_PREFIJO") + ex.Message;
            }
        }

        private static string MensajeError(PerfilResult r)
        {
            switch (r)
            {
                case PerfilResult.NombreInvalido:  return T("MSG_PERF_NOM_INV", "Ingrese el nombre del perfil.");
                case PerfilResult.NombreDuplicado: return T("MSG_PERF_DUP", "Ya existe un perfil con ese nombre.");
                default:                           return Tr.T("MSG_ERROR");
            }
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
