using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Auditoria de login. Mismo estilo y filtros que la bitacora general (tarjeta
    // de filtros + grilla), para que ambas pestanas sean simetricas. El filtrado
    // es en memoria sobre los ultimos registros cargados. Observa el idioma.
    public class ucAuditoria : UserControl, IObservadorIdioma
    {
        private TextBox _txtUsuario;
        private DateTimePicker _dtDesde, _dtHasta;
        private ComboBox _cboAccion;
        private AppButton _btnBuscar, _btnLimpiar;
        private DataGridView _grid;
        private Label _lblCount, _lblError;
        private List<BE_LoginAuditEntry> _todos = new List<BE_LoginAuditEntry>();

        public ucAuditoria()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { SafeLoadData(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // titulo
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));   // filtros
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // estado
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // grilla

            var lblTitle = Ui.H1("Registro de Auditoria");
            lblTitle.Tag = "T:AUD_TITULO";
            lblTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            // ---- Tarjeta de filtros (misma estructura que ucBitacora) ----
            var cardFiltros = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd),
                Padding = new Padding(Theme.SpaceLg, Theme.SpaceMd, Theme.SpaceLg, Theme.SpaceMd)
            };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true,
                BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
            };

            _txtUsuario = Ui.Input(); _txtUsuario.Width = 150;
            _dtDesde = Ui.DatePicker(); _dtDesde.Width = 130; _dtDesde.ShowCheckBox = true; _dtDesde.Checked = false;
            _dtHasta = Ui.DatePicker(); _dtHasta.Width = 130; _dtHasta.ShowCheckBox = true; _dtHasta.Checked = false;
            _cboAccion = Ui.Combo(); _cboAccion.Width = 150;
            _cboAccion.Items.Add(Tr.T("OPT_TODAS"));
            _cboAccion.Items.Add("LOGIN_OK");
            _cboAccion.Items.Add("LOGIN_FAIL");
            _cboAccion.Items.Add("LOGOUT");
            _cboAccion.SelectedIndex = 0;

            var fUsuario = Ui.Field("Usuario", _txtUsuario); fUsuario.Tag = "FIELD:COL_USUARIO";
            var fDesde = Ui.Field("Desde", _dtDesde); fDesde.Tag = "FIELD:BIT_DESDE";
            var fHasta = Ui.Field("Hasta", _dtHasta); fHasta.Tag = "FIELD:BIT_HASTA";
            var fAccion = Ui.Field("Accion", _cboAccion); fAccion.Tag = "FIELD:COL_ACCION";
            foreach (var f in new[] { fUsuario, fDesde, fHasta, fAccion })
                f.Margin = new Padding(0, 0, Theme.SpaceLg, 0);

            _btnBuscar = Ui.Primary("Buscar", Theme.IcoSearch);
            _btnBuscar.Tag = "T:BTN_BUSCAR"; _btnBuscar.Size = new Size(120, 32); _btnBuscar.Margin = new Padding(0, 18, Theme.SpaceSm, 0);
            _btnBuscar.Click += (s, e) => SafeLoadData();
            _btnLimpiar = Ui.Secondary("Limpiar", Theme.IcoClear);
            _btnLimpiar.Tag = "T:BTN_LIMPIAR"; _btnLimpiar.Size = new Size(120, 32); _btnLimpiar.Margin = new Padding(0, 18, 0, 0);
            _btnLimpiar.Click += (s, e) => { LimpiarFiltros(); Aplicar(); };

            flow.Controls.Add(fUsuario); flow.Controls.Add(fDesde); flow.Controls.Add(fHasta); flow.Controls.Add(fAccion);
            flow.Controls.Add(_btnBuscar); flow.Controls.Add(_btnLimpiar);
            cardFiltros.Controls.Add(flow);

            // ---- Estado (conteo / error) ----
            var pnlEstado = new Panel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme.SpaceSm) };
            _lblCount = new Label { AutoSize = true, Font = Theme.FontSmall, ForeColor = Theme.TextMuted, Location = new Point(2, 0), BackColor = Color.Transparent };
            _lblError = new Label { AutoSize = true, Font = Theme.FontBodyBold, ForeColor = Theme.Error, Location = new Point(2, 0), Visible = false, MaximumSize = new Size(900, 0), BackColor = Color.Transparent };
            pnlEstado.Controls.Add(_lblCount); pnlEstado.Controls.Add(_lblError);

            // ---- Tarjeta de la grilla ----
            var cardGrid = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(Theme.SpaceSm) };
            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",      HeaderText = "Id",      DataPropertyName = "Id",          FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",   HeaderText = "Fecha",   DataPropertyName = "Timestamp",   FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario", HeaderText = "Usuario", DataPropertyName = "Username",    FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cAccion",  HeaderText = "Accion",  DataPropertyName = "Action",      FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMaquina", HeaderText = "Maquina", DataPropertyName = "MachineName", FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDetalle", HeaderText = "Detalle", DataPropertyName = "Details",     FillWeight = 120 });
            cardGrid.Controls.Add(_grid);

            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(cardFiltros, 0, 1);
            root.Controls.Add(pnlEstado, 0, 2);
            root.Controls.Add(cardGrid, 0, 3);
            Controls.Add(root);
        }

        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_grid.Columns.Count >= 6)
            {
                _grid.Columns["cId"].HeaderText      = Tr.T("COL_ID");
                _grid.Columns["cFecha"].HeaderText   = Tr.T("COL_FECHA");
                _grid.Columns["cUsuario"].HeaderText = Tr.T("COL_USUARIO");
                _grid.Columns["cAccion"].HeaderText  = Tr.T("COL_ACCION");
                _grid.Columns["cMaquina"].HeaderText = Tr.T("COL_MAQUINA");
                _grid.Columns["cDetalle"].HeaderText = Tr.T("COL_DETALLE");
            }
            if (_cboAccion != null && _cboAccion.Items.Count > 0)
            {
                int sel = _cboAccion.SelectedIndex;
                _cboAccion.Items[0] = Tr.T("OPT_TODAS");
                _cboAccion.SelectedIndex = sel;
            }
        }

        private void LimpiarFiltros()
        {
            _txtUsuario.Text = "";
            _dtDesde.Checked = false;
            _dtHasta.Checked = false;
            _cboAccion.SelectedIndex = 0;
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                _lblCount.Visible = true;
                _todos = BLL_LoginAudit.GetAll(500);
                Aplicar();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Auditoria", "Cargar auditoria de login");
                _lblCount.Visible = false;
                _lblError.Text = "Error: " + ex.GetType().Name + " - " + ex.Message;
                _lblError.Visible = true;
            }
        }

        // Aplica los filtros en memoria sobre los registros cargados.
        private void Aplicar()
        {
            IEnumerable<BE_LoginAuditEntry> q = _todos ?? new List<BE_LoginAuditEntry>();

            string u = _txtUsuario.Text.Trim();
            if (u.Length > 0)
                q = q.Where(x => (x.Username ?? "").IndexOf(u, StringComparison.OrdinalIgnoreCase) >= 0);
            if (_dtDesde.Checked)
                q = q.Where(x => x.Timestamp >= _dtDesde.Value.Date);
            if (_dtHasta.Checked)
                q = q.Where(x => x.Timestamp < _dtHasta.Value.Date.AddDays(1));
            if (_cboAccion.SelectedIndex > 0)
                q = q.Where(x => x.Action.ToString() == _cboAccion.SelectedItem.ToString());

            var data = q.ToList();
            _grid.DataSource = data;
            _lblCount.Text = data.Count + " " + Tr.T("AUD_COUNT");
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex >= _grid.Columns.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "cAccion") return;

            string val = e.Value?.ToString();
            if (val == "LOGIN_OK")
            {
                e.CellStyle.ForeColor = Theme.Success;
                e.CellStyle.Font = Theme.FontBodyBold;
            }
            else if (val == "LOGIN_FAIL")
            {
                e.CellStyle.ForeColor = Theme.Error;
                e.CellStyle.Font = Theme.FontBodyBold;
            }
            else if (val == "LOGOUT")
            {
                e.CellStyle.ForeColor = Theme.TextMuted;
            }
        }
    }
}
