using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Bitacora general con busqueda combinada. Observa el cambio de idioma.
    // Rediseño visual: titulo H1, tarjeta de filtros con campos compactos y
    // tarjeta con la grilla. Layout por TableLayoutPanel/FlowLayoutPanel + Dock,
    // tokens de Theme y helpers de Ui/UiGrid (sin coordenadas magicas).
    public class ucBitacora : UserControl, IObservadorIdioma
    {
        private TextBox _txtUsuario;
        private DateTimePicker _dtDesde, _dtHasta;
        private ComboBox _cboModulo, _cboCriticidad;
        private DataGridView _grid;
        private Label _lblCount, _lblError;
        private AppButton _btnBuscar, _btnLimpiar;

        public ucBitacora()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { CargarModulos(); SafeBuscar(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            // ---------------- Layout raiz ----------------
            // Filas: 0 titulo, 1 tarjeta de filtros, 2 labels de estado, 3 tarjeta de grilla (fill).
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // titulo
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); // filtros (2 filas con wrap)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // estado
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla

            // ---------------- Titulo ----------------
            var lblTitle = Ui.H1("Bitacora del Sistema");
            lblTitle.Tag = "T:BIT_TITULO";
            lblTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            // ---------------- Tarjeta de filtros ----------------
            var cardFiltros = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd),
                Padding = new Padding(Theme.SpaceLg, Theme.SpaceMd, Theme.SpaceLg, Theme.SpaceMd)
            };

            // Fila de filtros: cada control etiquetado con Ui.Field, apilados en horizontal.
            // Dock=Top + AutoSize => ocupa el ancho de la tarjeta y, si no entran, los
            // filtros/botones envuelven a una segunda fila (la tarjeta crece en alto).
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _txtUsuario = Ui.Input();
            _txtUsuario.Width = 150;

            _dtDesde = Ui.DatePicker();
            _dtDesde.Width = 130;
            _dtDesde.ShowCheckBox = true;
            _dtDesde.Checked = false;

            _dtHasta = Ui.DatePicker();
            _dtHasta.Width = 130;
            _dtHasta.ShowCheckBox = true;
            _dtHasta.Checked = false;

            _cboModulo = Ui.Combo();
            _cboModulo.Width = 160;

            _cboCriticidad = Ui.Combo();
            _cboCriticidad.Width = 140;
            _cboCriticidad.Items.Add(Tr.T("OPT_TODAS"));
            _cboCriticidad.Items.Add(CriticidadBitacora.Info);
            _cboCriticidad.Items.Add(CriticidadBitacora.Advertencia);
            _cboCriticidad.Items.Add(CriticidadBitacora.Error);
            _cboCriticidad.SelectedIndex = 0;
            Ui.DibujarEnum(_cboCriticidad, o => o is CriticidadBitacora cb ? Tr.Criticidad(cb) : o?.ToString());

            var fUsuario = Ui.Field("Usuario", _txtUsuario);
            fUsuario.Tag = "FIELD:COL_USUARIO";
            var fDesde = Ui.Field("Desde", _dtDesde);
            fDesde.Tag = "FIELD:BIT_DESDE";
            var fHasta = Ui.Field("Hasta", _dtHasta);
            fHasta.Tag = "FIELD:BIT_HASTA";
            var fModulo = Ui.Field("Modulo", _cboModulo);
            fModulo.Tag = "FIELD:COL_MODULO";
            var fCrit = Ui.Field("Criticidad", _cboCriticidad);
            fCrit.Tag = "FIELD:COL_CRITICIDAD";

            // Separacion horizontal entre campos.
            foreach (var f in new[] { fUsuario, fDesde, fHasta, fModulo, fCrit })
                f.Margin = new Padding(0, 0, Theme.SpaceLg, 0);

            // Botones de accion alineados con la fila de inputs (debajo del caption).
            _btnBuscar = Ui.Primary("Buscar", Theme.IcoSearch);
            _btnBuscar.Tag = "T:BTN_BUSCAR";
            _btnBuscar.Size = new Size(120, 32);
            _btnBuscar.Margin = new Padding(0, 18, Theme.SpaceSm, 0);
            _btnBuscar.Click += (s, e) => SafeBuscar();

            _btnLimpiar = Ui.Secondary("Limpiar", Theme.IcoClear);
            _btnLimpiar.Tag = "T:BTN_LIMPIAR";
            _btnLimpiar.Size = new Size(120, 32);
            _btnLimpiar.Margin = new Padding(0, 18, 0, 0);
            _btnLimpiar.Click += (s, e) => { LimpiarFiltros(); SafeBuscar(); };

            flow.Controls.Add(fUsuario);
            flow.Controls.Add(fDesde);
            flow.Controls.Add(fHasta);
            flow.Controls.Add(fModulo);
            flow.Controls.Add(fCrit);
            flow.Controls.Add(_btnBuscar);
            flow.Controls.Add(_btnLimpiar);

            cardFiltros.Controls.Add(flow);

            // ---------------- Labels de estado (conteo / error) ----------------
            var pnlEstado = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceSm)
            };

            _lblCount = new Label
            {
                AutoSize = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(2, 0),
                BackColor = Color.Transparent
            };
            _lblError = new Label
            {
                AutoSize = true,
                Font = Theme.FontBodyBold,
                ForeColor = Theme.Error,
                Location = new Point(2, 0),
                Visible = false,
                MaximumSize = new Size(900, 0),
                BackColor = Color.Transparent
            };
            pnlEstado.Controls.Add(_lblCount);
            pnlEstado.Controls.Add(_lblError);

            // ---------------- Tarjeta de la grilla ----------------
            var cardGrid = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(Theme.SpaceSm)
            };

            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            _grid.CellFormatting += Grid_CellFormatting;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",     HeaderText = "Id",         DataPropertyName = "Id",         FillWeight = 25 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",  HeaderText = "Fecha",      DataPropertyName = "Fecha",      FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario",HeaderText = "Usuario",    DataPropertyName = "Usuario",    FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cModulo", HeaderText = "Modulo",     DataPropertyName = "Modulo",     FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cAccion", HeaderText = "Accion",     DataPropertyName = "Accion",     FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCrit",   HeaderText = "Criticidad", DataPropertyName = "Criticidad", FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDetalle",HeaderText = "Detalle",    DataPropertyName = "Detalle",    FillWeight = 150 });

            cardGrid.Controls.Add(_grid);

            // ---------------- Ensamblado ----------------
            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(cardFiltros, 0, 1);
            root.Controls.Add(pnlEstado, 0, 2);
            root.Controls.Add(cardGrid, 0, 3);

            Controls.Add(root);
        }

        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            // Las etiquetas de los Ui.Field no se traducen por Tag (su caption es un
            // Label hijo): se re-traducen explicitamente desde el Tag "FIELD:CLAVE".
            ActualizarFieldLabels(this);

            if (_grid.Columns.Count >= 7)
            {
                _grid.Columns["cId"].HeaderText      = Tr.T("COL_ID");
                _grid.Columns["cFecha"].HeaderText   = Tr.T("COL_FECHA");
                _grid.Columns["cUsuario"].HeaderText = Tr.T("COL_USUARIO");
                _grid.Columns["cModulo"].HeaderText  = Tr.T("COL_MODULO");
                _grid.Columns["cAccion"].HeaderText  = Tr.T("COL_ACCION");
                _grid.Columns["cCrit"].HeaderText    = Tr.T("COL_CRITICIDAD");
                _grid.Columns["cDetalle"].HeaderText = Tr.T("COL_DETALLE");
            }
            // Placeholder "(Todas)"/"(Todos)" de los combos de filtro.
            if (_cboCriticidad.Items.Count > 0)
            {
                int sel = _cboCriticidad.SelectedIndex;
                _cboCriticidad.Items[0] = Tr.T("OPT_TODAS");
                _cboCriticidad.SelectedIndex = sel;
                _cboCriticidad.Invalidate();
            }
            if (_cboModulo.Items.Count > 0)
            {
                int sel = _cboModulo.SelectedIndex;
                _cboModulo.Items[0] = Tr.T("OPT_TODOS");
                _cboModulo.SelectedIndex = sel;
            }
            if (_grid.DataSource != null && _lblCount.Visible) _lblCount.Text = _grid.Rows.Count + " " + Tr.T("BIT_COUNT");
            _grid.Invalidate(); // re-traduce los valores de criticidad en las celdas
        }

        // Recorre el arbol buscando paneles Ui.Field con Tag "FIELD:CLAVE" y traduce
        // su primer Label (el caption del campo).
        private static void ActualizarFieldLabels(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c.Tag is string tag && tag.StartsWith("FIELD:"))
                {
                    string clave = tag.Substring(6);
                    foreach (Control hijo in c.Controls)
                        if (hijo is Label lbl) { lbl.Text = Tr.T(clave); break; }
                }
                if (c.HasChildren)
                    ActualizarFieldLabels(c);
            }
        }

        private void CargarModulos()
        {
            try
            {
                _cboModulo.Items.Clear();
                _cboModulo.Items.Add(Tr.T("OPT_TODOS"));
                foreach (var m in BLL_Bitacora.GetModulos()) _cboModulo.Items.Add(m);
                _cboModulo.SelectedIndex = 0;
            }
            catch { _cboModulo.SelectedIndex = -1; }
        }

        private void LimpiarFiltros()
        {
            _txtUsuario.Text = "";
            _dtDesde.Checked = false;
            _dtHasta.Checked = false;
            if (_cboModulo.Items.Count > 0) _cboModulo.SelectedIndex = 0;
            _cboCriticidad.SelectedIndex = 0;
        }

        private void SafeBuscar()
        {
            try
            {
                _lblError.Visible = false;
                _lblCount.Visible = true;

                var filtros = new BitacoraFiltros
                {
                    Usuario = string.IsNullOrWhiteSpace(_txtUsuario.Text) ? null : _txtUsuario.Text.Trim(),
                    FechaInicio = _dtDesde.Checked ? _dtDesde.Value : (DateTime?)null,
                    FechaFin = _dtHasta.Checked ? _dtHasta.Value : (DateTime?)null,
                    Modulo = (_cboModulo.SelectedIndex > 0) ? _cboModulo.SelectedItem.ToString() : null,
                    Criticidad = (_cboCriticidad.SelectedItem is CriticidadBitacora c) ? c : (CriticidadBitacora?)null
                };

                List<BE_BitacoraEntry> data = BLL_Bitacora.Buscar(filtros);
                _grid.DataSource = data;
                _lblCount.Text = data.Count + " " + Tr.T("BIT_COUNT");
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Bitacora", "Buscar");
                _lblCount.Visible = false;
                _lblError.Text = Tr.T("MSG_ERROR_PREFIJO") + ex.GetType().Name + " - " + ex.Message;
                _lblError.Visible = true;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
            if (_grid.Columns[e.ColumnIndex].DataPropertyName != "Criticidad") return;

            switch (e.Value?.ToString())
            {
                case "Error":       e.CellStyle.ForeColor = Theme.Error; e.CellStyle.Font = Theme.FontBodyBold; break;
                case "Advertencia": e.CellStyle.ForeColor = Theme.Warning; break;
                case "Info":        e.CellStyle.ForeColor = Theme.Success; break;
            }
            // Traduce el valor mostrado (el color se calculo arriba con el valor crudo).
            if (e.Value is CriticidadBitacora cb) { e.Value = Tr.Criticidad(cb); e.FormattingApplied = true; }
        }
    }
}
