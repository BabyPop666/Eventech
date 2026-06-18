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
    public class ucServicios : UserControl, IObservadorIdioma
    {
        private DataGridView _grid;
        private Label _lblCount, _lblError, _lblOk, _lblFormTitle;
        private TextBox _txtNombre, _txtDescripcion, _txtPrecio;
        private CheckBox _chkActivo;
        private AppButton _btnNuevo, _btnGuardar;
        private int _editId;

        public ucServicios()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { LimpiarForm(); SafeLoadData(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.BgContent };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);
            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4, RowCount = 2, BackColor = Theme.BgContent, Padding = new Padding(0, 0, 0, Theme.SpaceMd)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = Ui.H1("Gestion de Servicios");
            lblTitle.Tag = "T:SRV_TITULO"; lblTitle.Anchor = AnchorStyles.Left; lblTitle.Margin = new Padding(0, 0, Theme.SpaceLg, 0);

            _btnNuevo = Ui.Primary("Nuevo", Theme.IcoAdd);
            _btnNuevo.Tag = "T:BTN_NUEVA"; _btnNuevo.Size = new Size(120, 36); _btnNuevo.BehindColor = Theme.BgContent;
            _btnNuevo.Anchor = AnchorStyles.Left; _btnNuevo.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnNuevo.Click += (s, e) => LimpiarForm();

            _lblCount = Ui.Body(); _lblCount.ForeColor = Theme.TextMuted; _lblCount.Anchor = AnchorStyles.Left;

            _lblError = Ui.Body(); _lblError.Font = Theme.FontBodyBold; _lblError.ForeColor = Theme.Error;
            _lblError.Visible = false; _lblError.AutoSize = true; _lblError.MaximumSize = new Size(900, 0);
            _lblError.Anchor = AnchorStyles.Left; _lblError.Margin = new Padding(0, Theme.SpaceXs, 0, 0);

            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(_btnNuevo, 1, 0);
            header.Controls.Add(_lblCount, 2, 0);
            header.Controls.Add(_lblError, 0, 1);
            header.SetColumnSpan(_lblError, 4);
            return header;
        }

        private Control BuildBody()
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.BgContent, Margin = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Controls.Add(BuildGridCard(), 0, 0);
            body.Controls.Add(BuildFormCard(), 1, 0);
            return body;
        }

        private Control BuildGridCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.SpaceLg, 0), Padding = new Padding(Theme.SpaceSm) };
            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cNombre", HeaderText = "Nombre", DataPropertyName = "Nombre", FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDescripcion", HeaderText = "Descripcion", DataPropertyName = "Descripcion", FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrecio", HeaderText = "Precio", DataPropertyName = "Precio", FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "cActivo", HeaderText = "Activo", DataPropertyName = "Activo", FillWeight = 30, ReadOnly = true });
            _grid.SelectionChanged += Grid_SelectionChanged;
            card.Controls.Add(_grid);
            return card;
        }

        private Control BuildFormCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, MinimumSize = new Size(280, 0), Margin = new Padding(0), Padding = new Padding(Theme.SpaceLg) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblFormTitle = Ui.Title("Nuevo servicio");
            _lblFormTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, AutoScroll = true, BackColor = Color.Transparent, Margin = new Padding(0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _txtNombre = Ui.Input();
            _txtDescripcion = Ui.Input();
            _txtPrecio = Ui.Input();
            var fN = Field(_txtNombre, "COL_NOMBRE", "Nombre");
            var fD = Field(_txtDescripcion, "COL_DESCRIPCION", "Descripcion");
            var fP = Field(_txtPrecio, "COL_PRECIO", "Precio");

            _chkActivo = new CheckBox
            {
                Text = "Activo", Tag = "T:COL_ACTIVO", Font = Theme.FontSmall, ForeColor = Theme.TextOnLight,
                FlatStyle = FlatStyle.Standard, BackColor = Color.Transparent, AutoSize = true, Checked = true,
                Margin = new Padding(2, 4, 0, 0)
            };

            int row = 0;
            foreach (var f in new Control[] { fN, fD, fP, _chkActivo })
            {
                f.Dock = f is CheckBox ? DockStyle.Left : DockStyle.Fill;
                f.Margin = new Padding(0, 0, 0, Theme.SpaceMd);
                fields.Controls.Add(f, 0, row++);
            }

            var actions = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent, Margin = new Padding(0, Theme.SpaceSm, 0, 0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _btnGuardar = Ui.Primary("Guardar", Theme.IcoSave);
            _btnGuardar.Tag = "T:BTN_GUARDAR"; _btnGuardar.Dock = DockStyle.Fill; _btnGuardar.Margin = new Padding(0, 0, 0, Theme.SpaceSm);
            _btnGuardar.Click += (s, e) => Guardar();
            _lblOk = new Label { AutoSize = true, Font = Theme.FontBodyBold, ForeColor = Theme.Success, Visible = false, BackColor = Color.Transparent };
            actions.Controls.Add(_btnGuardar, 0, 0);
            actions.Controls.Add(_lblOk, 0, 1);

            layout.Controls.Add(_lblFormTitle, 0, 0);
            layout.Controls.Add(fields, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            card.Controls.Add(layout);
            return card;
        }

        private TableLayoutPanel Field(Control input, string tagKey, string defecto)
        {
            var f = Ui.Field(T(tagKey, defecto), input);
            ((Label)f.GetControlFromPosition(0, 0)).Tag = "T:" + tagKey;
            return f;
        }

        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_grid.Columns.Count >= 4)
            {
                _grid.Columns["cNombre"].HeaderText      = Tr.T("COL_NOMBRE");
                _grid.Columns["cDescripcion"].HeaderText = Tr.T("COL_DESCRIPCION");
                _grid.Columns["cPrecio"].HeaderText      = Tr.T("COL_PRECIO");
                _grid.Columns["cActivo"].HeaderText      = Tr.T("COL_ACTIVO");
            }
            _lblFormTitle.Text = _editId == 0 ? Tr.T("SRV_NUEVO") : Tr.T("SRV_FORM_EDITAR") + " #" + _editId;
            ActualizarCount();
        }

        private void ActualizarCount()
        {
            if (_grid.DataSource is List<BE_Servicio> data) _lblCount.Text = data.Count + " " + Tr.T("SRV_COUNT");
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                _grid.DataSource = BLL_Servicio.GetAll();
                ActualizarCount();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Servicios", "Cargar servicios");
                _lblError.Text = "Error: " + ex.GetType().Name + " - " + ex.Message;
                _lblError.Visible = true;
                _lblCount.Text = "";
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is BE_Servicio s) CargarEnForm(s);
        }

        private void CargarEnForm(BE_Servicio s)
        {
            _editId = s.Id;
            _lblOk.Visible = false;
            _lblFormTitle.Text = Tr.T("SRV_FORM_EDITAR") + " #" + _editId;
            _txtNombre.Text = s.Nombre;
            _txtDescripcion.Text = s.Descripcion;
            _txtPrecio.Text = s.Precio.ToString("0.##");
            _chkActivo.Checked = s.Activo;
        }

        private void LimpiarForm()
        {
            _editId = 0;
            _lblOk.Visible = false;
            _lblFormTitle.Text = Tr.T("SRV_NUEVO");
            _txtNombre.Text = _txtDescripcion.Text = "";
            _txtPrecio.Text = "0";
            _chkActivo.Checked = true;
            _grid.ClearSelection();
        }

        private void Guardar()
        {
            _lblError.Visible = false;
            _lblOk.Visible = false;

            if (!decimal.TryParse(_txtPrecio.Text, out decimal precio))
            {
                _lblError.Text = Tr.T("MSG_MONTO_INVALIDO");
                _lblError.Visible = true;
                return;
            }

            var s = new BE_Servicio
            {
                Id = _editId,
                Nombre = _txtNombre.Text.Trim(),
                Descripcion = _txtDescripcion.Text.Trim(),
                Precio = precio,
                Activo = _chkActivo.Checked
            };
            ServicioResult r = _editId == 0 ? BLL_Servicio.Crear(s, out _) : BLL_Servicio.Actualizar(s);
            if (r == ServicioResult.Success)
            {
                LimpiarForm();
                SafeLoadData();
                _lblOk.Text = Tr.T("MSG_SRV_OK");
                _lblOk.Visible = true;
            }
            else
            {
                _lblError.Text = MensajeError(r);
                _lblError.Visible = true;
            }
        }

        private static string MensajeError(ServicioResult r)
        {
            switch (r)
            {
                case ServicioResult.NombreInvalido:  return Tr.T("MSG_SRV_NOMBRE");
                case ServicioResult.NombreDuplicado: return Tr.T("MSG_SRV_DUP");
                case ServicioResult.PrecioInvalido:  return Tr.T("MSG_SRV_PRECIO");
                case ServicioResult.NotFound:        return Tr.T("MSG_RES_NOTFOUND");
                default:                             return "Error";
            }
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
