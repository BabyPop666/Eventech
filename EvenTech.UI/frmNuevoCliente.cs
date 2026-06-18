using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Popup para dar de alta un cliente (Proceso 1). Reusable desde la ficha de
    // reservas ("Nuevo cliente") y desde la seccion Clientes. Devuelve OK + NuevoId.
    public class frmNuevoCliente : FormBase
    {
        private TextBox _txtNombre, _txtApellido, _txtDni, _txtEmail, _txtTelefono;
        private Label _lblMsg;

        public int NuevoId { get; private set; }

        public frmNuevoCliente()
        {
            BuildUi();
            Shown += (s, e) => _txtNombre.Focus();
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(470, 320);
            BackColor = Theme.BgContent;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);
            var lblTitle = new Label
            {
                Text = T("CLI_NUEVO", "Nuevo cliente"),
                Font = Theme.FontH2, ForeColor = Theme.TextOnDark, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Theme.SpaceLg, 0, 0, 0), BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }, danger: true);
            btnClose.Dock = DockStyle.Right;
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(btnClose);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceLg)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtNombre = Ui.Input(); _txtNombre.MaxLength = 60;
            _txtApellido = Ui.Input(); _txtApellido.MaxLength = 60;
            _txtDni = Ui.Input(); _txtDni.MaxLength = 20;
            _txtEmail = Ui.Input(); _txtEmail.MaxLength = 120;
            _txtTelefono = Ui.Input(); _txtTelefono.MaxLength = 30;

            var fNombre = Ui.Field(T("COL_NOMBRE", "Nombre"), _txtNombre); fNombre.Dock = DockStyle.Fill; fNombre.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            var fApellido = Ui.Field(T("COL_APELLIDO", "Apellido"), _txtApellido); fApellido.Dock = DockStyle.Fill;
            var fDni = Ui.Field(T("COL_DNI", "DNI"), _txtDni); fDni.Dock = DockStyle.Fill; fDni.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            var fEmail = Ui.Field(T("COL_EMAIL", "Email"), _txtEmail); fEmail.Dock = DockStyle.Fill;
            var fTel = Ui.Field(T("COL_TELEFONO", "Telefono"), _txtTelefono); fTel.Dock = DockStyle.Fill; fTel.Margin = new Padding(0, 0, Theme.SpaceMd, 0);

            _lblMsg = new Label { Dock = DockStyle.Fill, Font = Theme.FontSmall, ForeColor = Theme.Error, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft };

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent, Margin = new Padding(0) };
            var btnCrear = Ui.Primary(T("CLI_NUEVO", "Nuevo cliente"), Theme.IcoSave); btnCrear.Text = T("BTN_GUARDAR", "Guardar"); btnCrear.BehindColor = Theme.BgContent; btnCrear.Size = new Size(140, 38); btnCrear.Click += (s, e) => Crear();
            var btnCancelar = Ui.Secondary(T("BTN_CANCELAR", "Cancelar")); btnCancelar.BehindColor = Theme.BgContent; btnCancelar.Size = new Size(110, 38); btnCancelar.Margin = new Padding(Theme.SpaceSm, 0, 0, 0); btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            acciones.Controls.Add(btnCrear); acciones.Controls.Add(btnCancelar);

            body.Controls.Add(fNombre, 0, 0); body.Controls.Add(fApellido, 1, 0);
            body.Controls.Add(fDni, 0, 1); body.Controls.Add(fEmail, 1, 1);
            body.Controls.Add(fTel, 0, 2);
            body.Controls.Add(_lblMsg, 0, 3); body.SetColumnSpan(_lblMsg, 2);
            body.Controls.Add(acciones, 0, 4); body.SetColumnSpan(acciones, 2);

            Controls.Add(body);
            Controls.Add(pnlTitle);
            AcceptButton = btnCrear;
        }

        private void Crear()
        {
            try
            {
                var c = new BE_Cliente
                {
                    Nombre = _txtNombre.Text.Trim(),
                    Apellido = _txtApellido.Text.Trim(),
                    Dni = _txtDni.Text.Trim(),
                    Email = _txtEmail.Text.Trim(),
                    Telefono = _txtTelefono.Text.Trim()
                };
                ClienteResult r = BLL_Cliente.Crear(c, out int id);
                if (r != ClienteResult.Success) { _lblMsg.Text = MensajeError(r); return; }
                NuevoId = id;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Clientes", "Crear cliente (popup)");
                _lblMsg.Text = "Error: " + ex.Message;
            }
        }

        private static string MensajeError(ClienteResult r)
        {
            switch (r)
            {
                case ClienteResult.NombreInvalido: return T("MSG_CLI_NOMBRE", "Ingrese el nombre del cliente.");
                case ClienteResult.DniDuplicado:   return T("MSG_CLI_DNI_DUP", "Ya existe un cliente con ese DNI.");
                case ClienteResult.EmailInvalido:  return T("MSG_CLI_EMAIL", "El email no es valido.");
                default:                           return "Error";
            }
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
