using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Gestion de clientes (Proceso 1): grilla + ficha de alta/edicion.
    // Mismo patron visual que ucReservas. Observa el cambio de idioma.
    public class ucClientes_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private DataGridView _grid_704ILR;
        private Label _lblCount_704ILR, _lblError_704ILR, _lblOk_704ILR, _lblFormTitle_704ILR;
        private TextBox _txtNombre_704ILR, _txtApellido_704ILR, _txtDni_704ILR, _txtEmail_704ILR, _txtTelefono_704ILR;
        private AppButton_704ILR _btnNuevo_704ILR, _btnGuardar_704ILR;
        private int _editId_704ILR;

        public ucClientes_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => { LimpiarForm_704ILR(); SafeLoadData_704ILR(); GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this); };
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            var root_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme_704ILR.BgContent_704ILR };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root_704ILR.Controls.Add(BuildHeader_704ILR(), 0, 0);
            root_704ILR.Controls.Add(BuildBody_704ILR(), 0, 1);
            Controls.Add(root_704ILR);
        }

        private Control BuildHeader_704ILR()
        {
            var header_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4, RowCount = 2, BackColor = Theme_704ILR.BgContent_704ILR, Padding = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Gestion de Clientes");
            lblTitle_704ILR.Tag = "T:CLI_TITULO"; lblTitle_704ILR.Anchor = AnchorStyles.Left; lblTitle_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0);

            _btnNuevo_704ILR = Ui_704ILR.Primary_704ILR("Nuevo", Theme_704ILR.IcoAdd_704ILR);
            _btnNuevo_704ILR.Tag = "T:BTN_NUEVA"; _btnNuevo_704ILR.Size = new Size(120, 36); _btnNuevo_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnNuevo_704ILR.Anchor = AnchorStyles.Left; _btnNuevo_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnNuevo_704ILR.Click += (s_704ILR, e_704ILR) => LimpiarForm_704ILR();

            _lblCount_704ILR = Ui_704ILR.Body_704ILR(); _lblCount_704ILR.ForeColor = Theme_704ILR.TextMuted_704ILR; _lblCount_704ILR.Anchor = AnchorStyles.Left;

            _lblError_704ILR = Ui_704ILR.Body_704ILR(); _lblError_704ILR.Font = Theme_704ILR.FontBodyBold_704ILR; _lblError_704ILR.ForeColor = Theme_704ILR.Error_704ILR;
            _lblError_704ILR.Visible = false; _lblError_704ILR.AutoSize = true; _lblError_704ILR.MaximumSize = new Size(900, 0);
            _lblError_704ILR.Anchor = AnchorStyles.Left; _lblError_704ILR.Margin = new Padding(0, Theme_704ILR.SpaceXs_704ILR, 0, 0);

            header_704ILR.Controls.Add(lblTitle_704ILR, 0, 0);
            header_704ILR.Controls.Add(_btnNuevo_704ILR, 1, 0);
            header_704ILR.Controls.Add(_lblCount_704ILR, 2, 0);
            header_704ILR.Controls.Add(_lblError_704ILR, 0, 1);
            header_704ILR.SetColumnSpan(_lblError_704ILR, 4);
            return header_704ILR;
        }

        private Control BuildBody_704ILR()
        {
            var body_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme_704ILR.BgContent_704ILR, Margin = new Padding(0) };
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body_704ILR.Controls.Add(BuildGridCard_704ILR(), 0, 0);
            body_704ILR.Controls.Add(BuildFormCard_704ILR(), 1, 0);
            return body_704ILR;
        }

        private Control BuildGridCard_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0), Padding = new Padding(Theme_704ILR.SpaceSm_704ILR) };
            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cNombre",   HeaderText = "Nombre",   DataPropertyName = "Nombre_704ILR",   FillWeight = 60 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cApellido", HeaderText = "Apellido", DataPropertyName = "Apellido_704ILR", FillWeight = 60 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDni",      HeaderText = "DNI",      DataPropertyName = "Dni_704ILR",      FillWeight = 45 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEmail",    HeaderText = "Email",    DataPropertyName = "Email_704ILR",    FillWeight = 90 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cTelefono", HeaderText = "Telefono", DataPropertyName = "Telefono_704ILR", FillWeight = 60 });
            _grid_704ILR.SelectionChanged += Grid_SelectionChanged_704ILR;
            card_704ILR.Controls.Add(_grid_704ILR);
            return card_704ILR;
        }

        private Control BuildFormCard_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, MinimumSize = new Size(280, 0), Margin = new Padding(0), Padding = new Padding(Theme_704ILR.SpaceLg_704ILR) };
            var layout_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblFormTitle_704ILR = Ui_704ILR.Title_704ILR("Nuevo cliente");
            _lblFormTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            var fields_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            fields_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i_704ILR = 0; i_704ILR < 5; i_704ILR++) fields_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtNombre_704ILR = Ui_704ILR.Input_704ILR(); var fN_704ILR = Field_704ILR(_txtNombre_704ILR, "COL_NOMBRE", "Nombre");
            _txtApellido_704ILR = Ui_704ILR.Input_704ILR(); var fA_704ILR = Field_704ILR(_txtApellido_704ILR, "COL_APELLIDO", "Apellido");
            _txtDni_704ILR = Ui_704ILR.Input_704ILR(); var fD_704ILR = Field_704ILR(_txtDni_704ILR, "COL_DNI", "DNI");
            _txtEmail_704ILR = Ui_704ILR.Input_704ILR(); var fE_704ILR = Field_704ILR(_txtEmail_704ILR, "COL_EMAIL", "Email");
            _txtTelefono_704ILR = Ui_704ILR.Input_704ILR(); var fT_704ILR = Field_704ILR(_txtTelefono_704ILR, "COL_TELEFONO", "Telefono");
            int row_704ILR = 0;
            foreach (var f_704ILR in new[] { fN_704ILR, fA_704ILR, fD_704ILR, fE_704ILR, fT_704ILR }) { f_704ILR.Dock = DockStyle.Fill; f_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR); fields_704ILR.Controls.Add(f_704ILR, 0, row_704ILR++); }

            var actions_704ILR = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent, Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0) };
            actions_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _btnGuardar_704ILR = Ui_704ILR.Primary_704ILR("Guardar", Theme_704ILR.IcoSave_704ILR);
            _btnGuardar_704ILR.Tag = "T:BTN_GUARDAR"; _btnGuardar_704ILR.Dock = DockStyle.Fill; _btnGuardar_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR);
            _btnGuardar_704ILR.Click += (s_704ILR, e_704ILR) => Guardar_704ILR();
            _lblOk_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.Success_704ILR, Visible = false, BackColor = Color.Transparent };
            actions_704ILR.Controls.Add(_btnGuardar_704ILR, 0, 0);
            actions_704ILR.Controls.Add(_lblOk_704ILR, 0, 1);

            layout_704ILR.Controls.Add(_lblFormTitle_704ILR, 0, 0);
            layout_704ILR.Controls.Add(fields_704ILR, 0, 1);
            layout_704ILR.Controls.Add(actions_704ILR, 0, 2);
            card_704ILR.Controls.Add(layout_704ILR);
            return card_704ILR;
        }

        private TableLayoutPanel Field_704ILR(Control input_704ILR, string tagKey_704ILR, string defecto_704ILR)
        {
            var f_704ILR = Ui_704ILR.Field_704ILR(T_704ILR(tagKey_704ILR, defecto_704ILR), input_704ILR);
            ((Label)f_704ILR.GetControlFromPosition(0, 0)).Tag = "T:" + tagKey_704ILR;
            return f_704ILR;
        }

        public void ActualizarTextos_704ILR()
        {
            Tr_704ILR.AplicarTags_704ILR(this);
            if (_grid_704ILR.Columns.Count >= 5)
            {
                _grid_704ILR.Columns["cNombre"].HeaderText   = Tr_704ILR.T_704ILR("COL_NOMBRE");
                _grid_704ILR.Columns["cApellido"].HeaderText = Tr_704ILR.T_704ILR("COL_APELLIDO");
                _grid_704ILR.Columns["cDni"].HeaderText      = Tr_704ILR.T_704ILR("COL_DNI");
                _grid_704ILR.Columns["cEmail"].HeaderText    = Tr_704ILR.T_704ILR("COL_EMAIL");
                _grid_704ILR.Columns["cTelefono"].HeaderText = Tr_704ILR.T_704ILR("COL_TELEFONO");
            }
            _lblFormTitle_704ILR.Text = _editId_704ILR == 0 ? Tr_704ILR.T_704ILR("CLI_NUEVO") : Tr_704ILR.T_704ILR("CLI_FORM_EDITAR") + " #" + _editId_704ILR;
            ActualizarCount_704ILR();
        }

        private void ActualizarCount_704ILR()
        {
            if (_grid_704ILR.DataSource is List<BE_Cliente_704ILR> data_704ILR) _lblCount_704ILR.Text = data_704ILR.Count + " " + Tr_704ILR.T_704ILR("CLI_COUNT");
        }

        private void SafeLoadData_704ILR()
        {
            try
            {
                _lblError_704ILR.Visible = false;
                _grid_704ILR.DataSource = BLL_Cliente_704ILR.GetAll_704ILR();
                ActualizarCount_704ILR();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Clientes", "Cargar clientes");
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.GetType().Name + " - " + ex_704ILR.Message;
                _lblError_704ILR.Visible = true;
                _lblCount_704ILR.Text = "";
            }
        }

        // Deja seleccionada en la grilla la fila del cliente indicado.
        private void SeleccionarCliente_704ILR(int clienteId_704ILR)
        {
            foreach (DataGridViewRow fila_704ILR in _grid_704ILR.Rows)
            {
                if (fila_704ILR.DataBoundItem is BE_Cliente_704ILR c_704ILR && c_704ILR.Id_704ILR == clienteId_704ILR)
                {
                    _grid_704ILR.CurrentCell = fila_704ILR.Cells[0];
                    break;
                }
            }
        }

        private void Grid_SelectionChanged_704ILR(object sender_704ILR, EventArgs e_704ILR)
        {
            if (_grid_704ILR.CurrentRow?.DataBoundItem is BE_Cliente_704ILR c_704ILR) CargarEnForm_704ILR(c_704ILR);
        }

        private void CargarEnForm_704ILR(BE_Cliente_704ILR c_704ILR)
        {
            _editId_704ILR = c_704ILR.Id_704ILR;
            _lblOk_704ILR.Visible = false;
            _lblFormTitle_704ILR.Text = Tr_704ILR.T_704ILR("CLI_FORM_EDITAR") + " #" + _editId_704ILR;
            _txtNombre_704ILR.Text = c_704ILR.Nombre_704ILR;
            _txtApellido_704ILR.Text = c_704ILR.Apellido_704ILR;
            _txtDni_704ILR.Text = c_704ILR.Dni_704ILR;
            _txtEmail_704ILR.Text = c_704ILR.Email_704ILR;
            _txtTelefono_704ILR.Text = c_704ILR.Telefono_704ILR;
        }

        private void LimpiarForm_704ILR()
        {
            _editId_704ILR = 0;
            _lblOk_704ILR.Visible = false;
            _lblFormTitle_704ILR.Text = Tr_704ILR.T_704ILR("CLI_NUEVO");
            _txtNombre_704ILR.Text = _txtApellido_704ILR.Text = _txtDni_704ILR.Text = _txtEmail_704ILR.Text = _txtTelefono_704ILR.Text = "";
            _grid_704ILR.ClearSelection();
        }

        private void Guardar_704ILR()
        {
            // Segunda capa del control de acceso (ver Permisos.cs).
            if (!Permisos_704ILR.Exigir_704ILR("CLIENTES_GESTION", FindForm(),
                    _editId_704ILR == 0 ? "crear un cliente" : "editar el cliente #" + _editId_704ILR))
                return;

            _lblError_704ILR.Visible = false;
            _lblOk_704ILR.Visible = false;
            var c_704ILR = new BE_Cliente_704ILR
            {
                Id_704ILR = _editId_704ILR,
                Nombre_704ILR = _txtNombre_704ILR.Text.Trim(),
                Apellido_704ILR = _txtApellido_704ILR.Text.Trim(),
                Dni_704ILR = _txtDni_704ILR.Text.Trim(),
                Email_704ILR = _txtEmail_704ILR.Text.Trim(),
                Telefono_704ILR = _txtTelefono_704ILR.Text.Trim()
            };
            bool esAlta_704ILR = _editId_704ILR == 0;
            int nuevoId_704ILR = 0;
            ClienteResult_704ILR r_704ILR = esAlta_704ILR ? BLL_Cliente_704ILR.Crear_704ILR(c_704ILR, out nuevoId_704ILR) : BLL_Cliente_704ILR.Actualizar_704ILR(c_704ILR);
            if (r_704ILR == ClienteResult_704ILR.Success_704ILR)
            {
                LimpiarForm_704ILR();
                SafeLoadData_704ILR();
                // CUN002, paso 5: el alta se confirma y el cliente recien creado queda
                // seleccionado en la grilla (la seleccion carga su ficha) para poder
                // seguir operando con el sin buscarlo.
                if (esAlta_704ILR) SeleccionarCliente_704ILR(nuevoId_704ILR);
                _lblOk_704ILR.Text = Tr_704ILR.T_704ILR(esAlta_704ILR ? "MSG_CLI_CREADO" : "MSG_CLI_OK");
                _lblOk_704ILR.Visible = true;
            }
            else
            {
                _lblError_704ILR.Text = MensajeError_704ILR(r_704ILR);
                _lblError_704ILR.Visible = true;
            }
        }

        private static string MensajeError_704ILR(ClienteResult_704ILR r_704ILR)
        {
            switch (r_704ILR)
            {
                case ClienteResult_704ILR.NombreInvalido_704ILR: return Tr_704ILR.T_704ILR("MSG_CLI_NOMBRE");
                case ClienteResult_704ILR.DniDuplicado_704ILR:   return Tr_704ILR.T_704ILR("MSG_CLI_DNI_DUP");
                case ClienteResult_704ILR.EmailInvalido_704ILR:  return Tr_704ILR.T_704ILR("MSG_CLI_EMAIL");
                case ClienteResult_704ILR.NotFound_704ILR:       return Tr_704ILR.T_704ILR("MSG_RES_NOTFOUND");
                default:                           return Tr_704ILR.T_704ILR("MSG_ERROR");
            }
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
