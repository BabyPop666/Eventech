using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Catalogo de servicios (Proceso 1): grilla + ficha de alta/edicion.
    // Mismo patron visual que ucClientes/ucReservas. Observa el cambio de idioma.
    public class ucServicios_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private DataGridView _grid_704ILR;
        private Label _lblCount_704ILR, _lblError_704ILR, _lblOk_704ILR, _lblFormTitle_704ILR;
        private TextBox _txtNombre_704ILR, _txtDescripcion_704ILR, _txtPrecio_704ILR;
        private CheckBox _chkActivo_704ILR;
        private AppButton_704ILR _btnNuevo_704ILR, _btnGuardar_704ILR;
        private int _editId_704ILR;

        public ucServicios_704ILR()
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

            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Gestion de Servicios");
            lblTitle_704ILR.Tag = "T:SRV_TITULO"; lblTitle_704ILR.Anchor = AnchorStyles.Left; lblTitle_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0);

            _btnNuevo_704ILR = Ui_704ILR.Primary_704ILR("Nuevo", Theme_704ILR.IcoAdd_704ILR);
            // Servicio es masculino: BTN_NUEVO (ver ucClientes).
            _btnNuevo_704ILR.Tag = "T:BTN_NUEVO"; _btnNuevo_704ILR.Size = new Size(120, 36); _btnNuevo_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
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
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cNombre", HeaderText = "Nombre", DataPropertyName = "Nombre_704ILR", FillWeight = 55 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDescripcion", HeaderText = "Descripcion", DataPropertyName = "Descripcion_704ILR", FillWeight = 90 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrecio", HeaderText = "Precio", DataPropertyName = "Precio_704ILR", FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid_704ILR.Columns.Add(new DataGridViewCheckBoxColumn { Name = "cActivo", HeaderText = "Activo", DataPropertyName = "Activo_704ILR", FillWeight = 30, ReadOnly = true });
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

            _lblFormTitle_704ILR = Ui_704ILR.Title_704ILR("Nuevo servicio");
            _lblFormTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            var fields_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            fields_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i_704ILR = 0; i_704ILR < 4; i_704ILR++) fields_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // MaxLength = ancho real de las columnas de dbo.Servicios: sin el tope, un
            // texto mas largo llegaba recortado a la base y sin aviso (la validacion de
            // negocio rechaza el nombre excedido, esto lo evita antes de intentarlo).
            _txtNombre_704ILR = Ui_704ILR.Input_704ILR();
            _txtNombre_704ILR.MaxLength = 80;
            _txtDescripcion_704ILR = Ui_704ILR.Input_704ILR();
            _txtDescripcion_704ILR.MaxLength = 250;
            _txtPrecio_704ILR = Ui_704ILR.Input_704ILR();
            var fN_704ILR = Field_704ILR(_txtNombre_704ILR, "COL_NOMBRE", "Nombre");
            var fD_704ILR = Field_704ILR(_txtDescripcion_704ILR, "COL_DESCRIPCION", "Descripcion");
            var fP_704ILR = Field_704ILR(_txtPrecio_704ILR, "COL_PRECIO", "Precio");

            _chkActivo_704ILR = new CheckBox
            {
                Text = "Activo", Tag = "T:COL_ACTIVO", Font = Theme_704ILR.FontSmall_704ILR, ForeColor = Theme_704ILR.TextOnLight_704ILR,
                FlatStyle = FlatStyle.Standard, BackColor = Color.Transparent, AutoSize = true, Checked = true,
                Margin = new Padding(2, 4, 0, 0)
            };

            int row_704ILR = 0;
            foreach (var f_704ILR in new Control[] { fN_704ILR, fD_704ILR, fP_704ILR, _chkActivo_704ILR })
            {
                f_704ILR.Dock = f_704ILR is CheckBox ? DockStyle.Left : DockStyle.Fill;
                f_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);
                fields_704ILR.Controls.Add(f_704ILR, 0, row_704ILR++);
            }

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
            if (_grid_704ILR.Columns.Count >= 4)
            {
                _grid_704ILR.Columns["cNombre"].HeaderText      = Tr_704ILR.T_704ILR("COL_NOMBRE");
                _grid_704ILR.Columns["cDescripcion"].HeaderText = Tr_704ILR.T_704ILR("COL_DESCRIPCION");
                _grid_704ILR.Columns["cPrecio"].HeaderText      = Tr_704ILR.T_704ILR("COL_PRECIO");
                _grid_704ILR.Columns["cActivo"].HeaderText      = Tr_704ILR.T_704ILR("COL_ACTIVO");
            }
            _lblFormTitle_704ILR.Text = _editId_704ILR == 0 ? Tr_704ILR.T_704ILR("SRV_NUEVO") : Tr_704ILR.T_704ILR("SRV_FORM_EDITAR") + " #" + _editId_704ILR;
            ActualizarCount_704ILR();
        }

        private void ActualizarCount_704ILR()
        {
            if (_grid_704ILR.DataSource is List<BE_Servicio_704ILR> data_704ILR) _lblCount_704ILR.Text = data_704ILR.Count + " " + Tr_704ILR.T_704ILR("SRV_COUNT");
        }

        private void SafeLoadData_704ILR()
        {
            try
            {
                _lblError_704ILR.Visible = false;
                _grid_704ILR.DataSource = BLL_Servicio_704ILR.GetAll_704ILR();
                ActualizarCount_704ILR();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Servicios", "Cargar servicios");
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.GetType().Name + " - " + ex_704ILR.Message;
                _lblError_704ILR.Visible = true;
                _lblCount_704ILR.Text = "";
            }
        }

        private void Grid_SelectionChanged_704ILR(object sender_704ILR, EventArgs e_704ILR)
        {
            if (_grid_704ILR.CurrentRow?.DataBoundItem is BE_Servicio_704ILR s_704ILR) CargarEnForm_704ILR(s_704ILR);
        }

        private void CargarEnForm_704ILR(BE_Servicio_704ILR s_704ILR)
        {
            _editId_704ILR = s_704ILR.Id_704ILR;
            _lblOk_704ILR.Visible = false;
            _lblFormTitle_704ILR.Text = Tr_704ILR.T_704ILR("SRV_FORM_EDITAR") + " #" + _editId_704ILR;
            _txtNombre_704ILR.Text = s_704ILR.Nombre_704ILR;
            _txtDescripcion_704ILR.Text = s_704ILR.Descripcion_704ILR;
            _txtPrecio_704ILR.Text = s_704ILR.Precio_704ILR.ToString("0.##");
            _chkActivo_704ILR.Checked = s_704ILR.Activo_704ILR;
        }

        private void LimpiarForm_704ILR()
        {
            _editId_704ILR = 0;
            _lblOk_704ILR.Visible = false;
            _lblFormTitle_704ILR.Text = Tr_704ILR.T_704ILR("SRV_NUEVO");
            _txtNombre_704ILR.Text = _txtDescripcion_704ILR.Text = "";
            _txtPrecio_704ILR.Text = "0";
            _chkActivo_704ILR.Checked = true;
            _grid_704ILR.ClearSelection();
        }

        private void Guardar_704ILR()
        {
            // Segunda capa del control de acceso (ver Permisos.cs).
            if (!Permisos_704ILR.Exigir_704ILR("SERVICIOS_GESTION", FindForm(),
                    _editId_704ILR == 0 ? "crear un servicio" : "editar el servicio #" + _editId_704ILR))
                return;

            _lblError_704ILR.Visible = false;
            _lblOk_704ILR.Visible = false;

            if (!decimal.TryParse(_txtPrecio_704ILR.Text, out decimal precio_704ILR))
            {
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_MONTO_INVALIDO");
                _lblError_704ILR.Visible = true;
                return;
            }

            var s_704ILR = new BE_Servicio_704ILR
            {
                Id_704ILR = _editId_704ILR,
                Nombre_704ILR = _txtNombre_704ILR.Text.Trim(),
                Descripcion_704ILR = _txtDescripcion_704ILR.Text.Trim(),
                Precio_704ILR = precio_704ILR,
                Activo_704ILR = _chkActivo_704ILR.Checked
            };
            ServicioResult_704ILR r_704ILR = _editId_704ILR == 0 ? BLL_Servicio_704ILR.Crear_704ILR(s_704ILR, out _) : BLL_Servicio_704ILR.Actualizar_704ILR(s_704ILR);
            if (r_704ILR == ServicioResult_704ILR.Success_704ILR)
            {
                LimpiarForm_704ILR();
                SafeLoadData_704ILR();
                _lblOk_704ILR.Text = Tr_704ILR.T_704ILR("MSG_SRV_OK");
                _lblOk_704ILR.Visible = true;
            }
            else
            {
                _lblError_704ILR.Text = MensajeError_704ILR(r_704ILR);
                _lblError_704ILR.Visible = true;
            }
        }

        private static string MensajeError_704ILR(ServicioResult_704ILR r_704ILR)
        {
            switch (r_704ILR)
            {
                case ServicioResult_704ILR.NombreInvalido_704ILR:  return Tr_704ILR.T_704ILR("MSG_SRV_NOMBRE");
                case ServicioResult_704ILR.NombreDuplicado_704ILR: return Tr_704ILR.T_704ILR("MSG_SRV_DUP");
                case ServicioResult_704ILR.PrecioInvalido_704ILR:  return Tr_704ILR.T_704ILR("MSG_SRV_PRECIO");
                case ServicioResult_704ILR.NotFound_704ILR:        return Tr_704ILR.T_704ILR("MSG_RES_NOTFOUND");
                default:                             return Tr_704ILR.T_704ILR("MSG_ERROR");
            }
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
