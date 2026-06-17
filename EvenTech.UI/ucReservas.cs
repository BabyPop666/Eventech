using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // UserControl de gestion de reservas: grilla + ficha de alta/edicion.
    // Layout por TableLayoutPanel/Dock (DPI-aware, sin coordenadas magicas):
    // fila 0 = barra de titulo, fila 1 = cuerpo en dos columnas (grilla / ficha).
    // Observa el cambio de idioma (patron Observer) para traducir sus textos.
    public class ucReservas : UserControl, IObservadorIdioma
    {
        private DataGridView _grid;
        private Label _lblCount, _lblError, _lblFormTitle;
        private TextBox _txtCliente, _txtMonto;
        private ComboBox _cboSalon, _cboEstado;
        private DateTimePicker _dtFecha;
        private AppButton _btnNuevo, _btnGuardar, _btnHistorial;

        private int _editId; // 0 = alta, >0 = edicion

        public ucReservas()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { CargarSalones(); LimpiarForm(); SafeLoadData(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            // ---------------- Estructura raiz ----------------
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.BgContent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // barra de titulo
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // cuerpo

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);

            Controls.Add(root);
        }

        // Barra superior: titulo de pagina, boton "Nueva", conteo y error.
        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Theme.BgContent,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, Theme.SpaceMd)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // titulo
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // boton nueva
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // conteo
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // relleno
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = Ui.H1("Gestion de Reservas");
            lblTitle.Tag = "T:RES_TITULO";
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.Margin = new Padding(0, 0, Theme.SpaceLg, 0);

            _btnNuevo = Ui.Primary("Nueva", Theme.IcoAdd);
            _btnNuevo.Tag = "T:BTN_NUEVA";
            _btnNuevo.Size = new Size(120, 36);
            _btnNuevo.BehindColor = Theme.BgContent; // vive sobre el area de contenido
            _btnNuevo.Anchor = AnchorStyles.Left;
            _btnNuevo.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnNuevo.Click += (s, e) => LimpiarForm();

            _lblCount = Ui.Body();
            _lblCount.ForeColor = Theme.TextMuted;
            _lblCount.Anchor = AnchorStyles.Left;
            _lblCount.Margin = new Padding(0, 0, 0, 0);

            _lblError = Ui.Body();
            _lblError.Font = Theme.FontBodyBold;
            _lblError.ForeColor = Theme.Error;
            _lblError.Visible = false;
            _lblError.AutoSize = true;
            _lblError.MaximumSize = new Size(900, 0);
            _lblError.Anchor = AnchorStyles.Left;
            _lblError.Margin = new Padding(0, Theme.SpaceXs, 0, 0);

            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(_btnNuevo, 1, 0);
            header.Controls.Add(_lblCount, 2, 0);
            // El error ocupa toda la fila inferior (debajo del titulo y acciones).
            header.Controls.Add(_lblError, 0, 1);
            header.SetColumnSpan(_lblError, 4);

            return header;
        }

        // Cuerpo: dos columnas (grilla a la izquierda, ficha a la derecha).
        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.BgContent,
                Margin = new Padding(0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            body.Controls.Add(BuildGridCard(), 0, 0);
            body.Controls.Add(BuildFormCard(), 1, 0);

            return body;
        }

        // Tarjeta con la grilla de reservas.
        private Control BuildGridCard()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme.SpaceLg, 0),
                Padding = new Padding(Theme.SpaceSm)
            };

            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",      HeaderText = "Id",      DataPropertyName = "Id",            FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCliente", HeaderText = "Cliente", DataPropertyName = "ClienteNombre", FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSalon",   HeaderText = "Salon",   DataPropertyName = "SalonNombre",   FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",   HeaderText = "Fecha",   DataPropertyName = "FechaEvento",   FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado",  HeaderText = "Estado",  DataPropertyName = "Estado",        FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMonto",   HeaderText = "Monto",   DataPropertyName = "Monto",         FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.SelectionChanged += Grid_SelectionChanged;

            card.Controls.Add(_grid);
            return card;
        }

        // Tarjeta con la ficha de alta/edicion.
        private Control BuildFormCard()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(300, 0),
                Margin = new Padding(0),
                Padding = new Padding(Theme.SpaceLg)
            };

            // Layout interno de la ficha: titulo, campos (scrollables) y botones.
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // titulo ficha
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // campos
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // botones

            _lblFormTitle = Ui.Title("Nueva reserva");
            _lblFormTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            // Pila vertical de campos etiquetados (caption arriba, input abajo).
            // TableLayoutPanel: cada campo Dock=Fill -> ocupa todo el ancho de la
            // ficha y se ajusta solo al redimensionar (sin calculos manuales).
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // empuja los campos hacia arriba

            _txtCliente = Ui.Input();
            var fldCliente = Ui.Field("Cliente", _txtCliente);
            ((Label)fldCliente.GetControlFromPosition(0, 0)).Tag = "T:COL_CLIENTE";

            _cboSalon = Ui.Combo();
            var fldSalon = Ui.Field("Salon", _cboSalon);
            ((Label)fldSalon.GetControlFromPosition(0, 0)).Tag = "T:COL_SALON";

            _dtFecha = Ui.DatePicker();
            _dtFecha.MinDate = DateTime.Today;
            var fldFecha = Ui.Field("Fecha", _dtFecha);
            ((Label)fldFecha.GetControlFromPosition(0, 0)).Tag = "T:RES_LBL_FECHA";

            _cboEstado = Ui.Combo();
            _cboEstado.Items.AddRange(new object[] { EstadoReserva.PENDIENTE, EstadoReserva.CONFIRMADA, EstadoReserva.CANCELADA });
            var fldEstado = Ui.Field("Estado", _cboEstado);
            ((Label)fldEstado.GetControlFromPosition(0, 0)).Tag = "T:COL_ESTADO";

            _txtMonto = Ui.Input();
            var fldMonto = Ui.Field("Monto", _txtMonto);
            ((Label)fldMonto.GetControlFromPosition(0, 0)).Tag = "T:COL_MONTO";

            int row = 0;
            foreach (var fld in new[] { fldCliente, fldSalon, fldFecha, fldEstado, fldMonto })
            {
                fld.Dock = DockStyle.Fill;
                fld.Margin = new Padding(0, 0, 0, Theme.SpaceMd);
                fields.Controls.Add(fld, 0, row++);
            }

            // Botones de accion apilados al pie de la ficha.
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, Theme.SpaceSm, 0, 0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            _btnGuardar = Ui.Primary("Guardar", Theme.IcoSave);
            _btnGuardar.Tag = "T:BTN_GUARDAR";
            _btnGuardar.Dock = DockStyle.Fill;
            _btnGuardar.Margin = new Padding(0, 0, 0, Theme.SpaceSm);
            _btnGuardar.Click += (s, e) => Guardar();

            _btnHistorial = Ui.Secondary("Ver historial de cambios");
            _btnHistorial.Tag = "T:RES_HISTORIAL";
            _btnHistorial.Dock = DockStyle.Fill;
            _btnHistorial.Margin = new Padding(0);
            _btnHistorial.Click += (s, e) => VerHistorial();

            actions.Controls.Add(_btnGuardar, 0, 0);
            actions.Controls.Add(_btnHistorial, 0, 1);

            layout.Controls.Add(_lblFormTitle, 0, 0);
            layout.Controls.Add(fields, 0, 1);
            layout.Controls.Add(actions, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        // Observer: re-traduce textos estaticos, encabezados de grilla y etiquetas dinamicas.
        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_grid.Columns.Count >= 6)
            {
                _grid.Columns["cId"].HeaderText      = Tr.T("COL_ID");
                _grid.Columns["cCliente"].HeaderText = Tr.T("COL_CLIENTE");
                _grid.Columns["cSalon"].HeaderText   = Tr.T("COL_SALON");
                _grid.Columns["cFecha"].HeaderText   = Tr.T("COL_FECHA");
                _grid.Columns["cEstado"].HeaderText  = Tr.T("COL_ESTADO");
                _grid.Columns["cMonto"].HeaderText   = Tr.T("COL_MONTO");
            }
            ActualizarTituloForm();
            ActualizarCount();
        }

        private void ActualizarTituloForm()
        {
            _lblFormTitle.Text = _editId == 0
                ? Tr.T("RES_FORM_NUEVA")
                : Tr.T("RES_FORM_EDITAR") + " #" + _editId;
        }

        private void ActualizarCount()
        {
            if (_grid.DataSource is List<BE_Reserva> data)
                _lblCount.Text = data.Count + " " + Tr.T("RES_COUNT");
        }

        private void CargarSalones()
        {
            try
            {
                _cboSalon.DataSource = BLL_Salon.GetAll();
                _cboSalon.DisplayMember = "Nombre";
                _cboSalon.ValueMember = "Id";
                _cboSalon.SelectedIndex = _cboSalon.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar salones");
                ShowError("Error cargando salones: " + ex.Message);
            }
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                List<BE_Reserva> data = BLL_Reserva.GetAll();
                _grid.DataSource = data;
                ActualizarCount();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar reservas");
                ShowError("Error cargando reservas: " + ex.GetType().Name + " - " + ex.Message);
                _lblCount.Text = "";
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is BE_Reserva r) CargarEnForm(r);
        }

        private void CargarEnForm(BE_Reserva r)
        {
            _editId = r.Id;
            ActualizarTituloForm();
            _txtCliente.Text = r.ClienteNombre;
            _cboSalon.SelectedValue = r.SalonId;
            _dtFecha.Value = r.FechaEvento < _dtFecha.MinDate ? _dtFecha.MinDate : r.FechaEvento;
            _cboEstado.SelectedItem = r.Estado;
            _txtMonto.Text = r.Monto.ToString("0.##");
        }

        private void LimpiarForm()
        {
            _editId = 0;
            ActualizarTituloForm();
            _txtCliente.Text = "";
            if (_cboSalon.Items.Count > 0) _cboSalon.SelectedIndex = 0;
            _dtFecha.Value = DateTime.Today;
            _cboEstado.SelectedItem = EstadoReserva.PENDIENTE;
            _txtMonto.Text = "0";
            _grid.ClearSelection();
        }

        private void Guardar()
        {
            _lblError.Visible = false;

            if (!decimal.TryParse(_txtMonto.Text, out decimal monto))
            {
                ShowError(Tr.T("MSG_MONTO_INVALIDO"));
                return;
            }

            var reserva = new BE_Reserva
            {
                Id = _editId,
                ClienteNombre = _txtCliente.Text.Trim(),
                SalonId = _cboSalon.SelectedValue is int sid ? sid : 0,
                FechaEvento = _dtFecha.Value.Date,
                Estado = _cboEstado.SelectedItem is EstadoReserva es ? es : EstadoReserva.PENDIENTE,
                Monto = monto
            };

            ReservaResult result = _editId == 0
                ? BLL_Reserva.Crear(reserva, out _)
                : BLL_Reserva.Actualizar(reserva);

            if (result == ReservaResult.Success)
            {
                LimpiarForm();
                SafeLoadData();
            }
            else
            {
                ShowError(MensajeError(result));
            }
        }

        private static string MensajeError(ReservaResult r)
        {
            switch (r)
            {
                case ReservaResult.InvalidCliente: return Tr.T("MSG_RES_CLIENTE");
                case ReservaResult.InvalidSalon:   return Tr.T("MSG_RES_SALON");
                case ReservaResult.InvalidFecha:   return Tr.T("MSG_RES_FECHA");
                case ReservaResult.InvalidMonto:   return Tr.T("MSG_RES_MONTO");
                case ReservaResult.NotFound:       return Tr.T("MSG_RES_NOTFOUND");
                default:                           return Tr.T("MSG_RES_ERROR");
            }
        }

        private void VerHistorial()
        {
            if (_editId == 0)
            {
                ShowError(Tr.T("MSG_RES_SELECCIONE"));
                return;
            }
            using (var frm = new frmHistorialReserva(_editId))
            {
                frm.ShowDialog(FindForm());
            }
        }

        private void ShowError(string msg)
        {
            _lblError.Text = msg;
            _lblError.Visible = true;
        }
    }
}
