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
    public class ucAuditoria_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private TextBox _txtUsuario_704ILR;
        private DateTimePicker _dtDesde_704ILR, _dtHasta_704ILR;
        private ComboBox _cboAccion_704ILR;
        private AppButton_704ILR _btnBuscar_704ILR, _btnLimpiar_704ILR;
        private DataGridView _grid_704ILR;
        private Label _lblCount_704ILR, _lblError_704ILR;
        private List<BE_LoginAuditEntry_704ILR> _todos_704ILR = new List<BE_LoginAuditEntry_704ILR>();

        public ucAuditoria_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => { SafeLoadData_704ILR(); GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this); };
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // titulo
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));   // filtros
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // estado
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // grilla

            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Registro de Auditoria");
            lblTitle_704ILR.Tag = "T:AUD_TITULO";
            lblTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            // ---- Tarjeta de filtros (misma estructura que ucBitacora) ----
            var cardFiltros_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR),
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceMd_704ILR, Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceMd_704ILR)
            };
            var flow_704ILR = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true,
                BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
            };

            _txtUsuario_704ILR = Ui_704ILR.Input_704ILR(); _txtUsuario_704ILR.Width = 150;
            _dtDesde_704ILR = Ui_704ILR.DatePicker_704ILR(); _dtDesde_704ILR.Width = 130; _dtDesde_704ILR.ShowCheckBox = true; _dtDesde_704ILR.Checked = false;
            _dtHasta_704ILR = Ui_704ILR.DatePicker_704ILR(); _dtHasta_704ILR.Width = 130; _dtHasta_704ILR.ShowCheckBox = true; _dtHasta_704ILR.Checked = false;
            _cboAccion_704ILR = Ui_704ILR.Combo_704ILR(); _cboAccion_704ILR.Width = 150;
            _cboAccion_704ILR.Items.Add(Tr_704ILR.T_704ILR("OPT_TODAS"));
            _cboAccion_704ILR.Items.Add(new AccionItem_704ILR("LOGIN_OK"));
            _cboAccion_704ILR.Items.Add(new AccionItem_704ILR("LOGIN_FAIL"));
            _cboAccion_704ILR.Items.Add(new AccionItem_704ILR("LOGOUT"));
            _cboAccion_704ILR.SelectedIndex = 0;
            Ui_704ILR.DibujarEnum_704ILR(_cboAccion_704ILR, o_704ILR => o_704ILR?.ToString());

            var fUsuario_704ILR = Ui_704ILR.Field_704ILR("Usuario", _txtUsuario_704ILR); fUsuario_704ILR.Tag = "FIELD:COL_USUARIO";
            var fDesde_704ILR = Ui_704ILR.Field_704ILR("Desde", _dtDesde_704ILR); fDesde_704ILR.Tag = "FIELD:BIT_DESDE";
            var fHasta_704ILR = Ui_704ILR.Field_704ILR("Hasta", _dtHasta_704ILR); fHasta_704ILR.Tag = "FIELD:BIT_HASTA";
            var fAccion_704ILR = Ui_704ILR.Field_704ILR("Accion", _cboAccion_704ILR); fAccion_704ILR.Tag = "FIELD:COL_ACCION";
            foreach (var f_704ILR in new[] { fUsuario_704ILR, fDesde_704ILR, fHasta_704ILR, fAccion_704ILR })
                f_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0);

            _btnBuscar_704ILR = Ui_704ILR.Primary_704ILR("Buscar", Theme_704ILR.IcoSearch_704ILR);
            _btnBuscar_704ILR.Tag = "T:BTN_BUSCAR"; _btnBuscar_704ILR.Size = new Size(120, 32); _btnBuscar_704ILR.Margin = new Padding(0, 18, Theme_704ILR.SpaceSm_704ILR, 0);
            _btnBuscar_704ILR.Click += (s_704ILR, e_704ILR) => SafeLoadData_704ILR();
            _btnLimpiar_704ILR = Ui_704ILR.Secondary_704ILR("Limpiar", Theme_704ILR.IcoClear_704ILR);
            _btnLimpiar_704ILR.Tag = "T:BTN_LIMPIAR"; _btnLimpiar_704ILR.Size = new Size(120, 32); _btnLimpiar_704ILR.Margin = new Padding(0, 18, 0, 0);
            _btnLimpiar_704ILR.Click += (s_704ILR, e_704ILR) => { LimpiarFiltros_704ILR(); Aplicar_704ILR(); };

            flow_704ILR.Controls.Add(fUsuario_704ILR); flow_704ILR.Controls.Add(fDesde_704ILR); flow_704ILR.Controls.Add(fHasta_704ILR); flow_704ILR.Controls.Add(fAccion_704ILR);
            flow_704ILR.Controls.Add(_btnBuscar_704ILR); flow_704ILR.Controls.Add(_btnLimpiar_704ILR);
            cardFiltros_704ILR.Controls.Add(flow_704ILR);

            // ---- Estado (conteo / error) ----
            var pnlEstado_704ILR = new Panel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR) };
            _lblCount_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontSmall_704ILR, ForeColor = Theme_704ILR.TextMuted_704ILR, Location = new Point(2, 0), BackColor = Color.Transparent };
            _lblError_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.Error_704ILR, Location = new Point(2, 0), Visible = false, MaximumSize = new Size(900, 0), BackColor = Color.Transparent };
            pnlEstado_704ILR.Controls.Add(_lblCount_704ILR); pnlEstado_704ILR.Controls.Add(_lblError_704ILR);

            // ---- Tarjeta de la grilla ----
            var cardGrid_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(Theme_704ILR.SpaceSm_704ILR) };
            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);
            _grid_704ILR.CellFormatting += Grid_CellFormatting_704ILR;
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",      HeaderText = "Id",      DataPropertyName = "Id_704ILR",          FillWeight = 30 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",   HeaderText = "Fecha",   DataPropertyName = "Timestamp_704ILR",   FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario", HeaderText = "Usuario", DataPropertyName = "Username_704ILR",    FillWeight = 70 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cAccion",  HeaderText = "Accion",  DataPropertyName = "Action_704ILR",      FillWeight = 60 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMaquina", HeaderText = "Maquina", DataPropertyName = "MachineName_704ILR", FillWeight = 70 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDetalle", HeaderText = "Detalle", DataPropertyName = "Details_704ILR",     FillWeight = 120 });
            cardGrid_704ILR.Controls.Add(_grid_704ILR);

            root_704ILR.Controls.Add(lblTitle_704ILR, 0, 0);
            root_704ILR.Controls.Add(cardFiltros_704ILR, 0, 1);
            root_704ILR.Controls.Add(pnlEstado_704ILR, 0, 2);
            root_704ILR.Controls.Add(cardGrid_704ILR, 0, 3);
            Controls.Add(root_704ILR);
        }

        public void ActualizarTextos_704ILR()
        {
            Tr_704ILR.AplicarTags_704ILR(this);
            if (_grid_704ILR.Columns.Count >= 6)
            {
                _grid_704ILR.Columns["cId"].HeaderText      = Tr_704ILR.T_704ILR("COL_ID");
                _grid_704ILR.Columns["cFecha"].HeaderText   = Tr_704ILR.T_704ILR("COL_FECHA");
                _grid_704ILR.Columns["cUsuario"].HeaderText = Tr_704ILR.T_704ILR("COL_USUARIO");
                _grid_704ILR.Columns["cAccion"].HeaderText  = Tr_704ILR.T_704ILR("COL_ACCION");
                _grid_704ILR.Columns["cMaquina"].HeaderText = Tr_704ILR.T_704ILR("COL_MAQUINA");
                _grid_704ILR.Columns["cDetalle"].HeaderText = Tr_704ILR.T_704ILR("COL_DETALLE");
            }
            if (_cboAccion_704ILR != null && _cboAccion_704ILR.Items.Count > 0)
            {
                int sel_704ILR = _cboAccion_704ILR.SelectedIndex;
                _cboAccion_704ILR.Items[0] = Tr_704ILR.T_704ILR("OPT_TODAS");
                _cboAccion_704ILR.SelectedIndex = sel_704ILR;
                _cboAccion_704ILR.Invalidate();
            }
            if (_grid_704ILR.DataSource != null && _lblCount_704ILR.Visible) _lblCount_704ILR.Text = _grid_704ILR.Rows.Count + " " + Tr_704ILR.T_704ILR("AUD_COUNT");
            _grid_704ILR.Invalidate(); // re-traduce los valores de accion en las celdas
        }

        private void LimpiarFiltros_704ILR()
        {
            _txtUsuario_704ILR.Text = "";
            _dtDesde_704ILR.Checked = false;
            _dtHasta_704ILR.Checked = false;
            _cboAccion_704ILR.SelectedIndex = 0;
        }

        private void SafeLoadData_704ILR()
        {
            // Segunda capa: la vista carga sola al abrirse, asi que el permiso se
            // exige aca y no solo donde se decide mostrarla.
            if (!Permisos_704ILR.Tiene_704ILR("AUDIT_LOGIN_VER"))
            {
                _lblCount_704ILR.Visible = false;
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_SIN_PERMISO");
                _lblError_704ILR.Visible = true;
                return;
            }

            try
            {
                _lblError_704ILR.Visible = false;
                _lblCount_704ILR.Visible = true;
                _todos_704ILR = BLL_LoginAudit_704ILR.GetAll_704ILR(500);
                Aplicar_704ILR();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Auditoria", "Cargar auditoria de login");
                _lblCount_704ILR.Visible = false;
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.GetType().Name + " - " + ex_704ILR.Message;
                _lblError_704ILR.Visible = true;
            }
        }

        // Aplica los filtros en memoria sobre los registros cargados.
        private void Aplicar_704ILR()
        {
            IEnumerable<BE_LoginAuditEntry_704ILR> q_704ILR = _todos_704ILR ?? new List<BE_LoginAuditEntry_704ILR>();

            string u_704ILR = _txtUsuario_704ILR.Text.Trim();
            if (u_704ILR.Length > 0)
                q_704ILR = q_704ILR.Where(x_704ILR => (x_704ILR.Username_704ILR ?? "").IndexOf(u_704ILR, StringComparison.OrdinalIgnoreCase) >= 0);
            if (_dtDesde_704ILR.Checked)
                q_704ILR = q_704ILR.Where(x_704ILR => x_704ILR.Timestamp_704ILR >= _dtDesde_704ILR.Value.Date);
            if (_dtHasta_704ILR.Checked)
                q_704ILR = q_704ILR.Where(x_704ILR => x_704ILR.Timestamp_704ILR < _dtHasta_704ILR.Value.Date.AddDays(1));
            if (_cboAccion_704ILR.SelectedItem is AccionItem_704ILR ai_704ILR)
                q_704ILR = q_704ILR.Where(x_704ILR => x_704ILR.Action_704ILR.ToString() == ai_704ILR.Code_704ILR);

            var data_704ILR = q_704ILR.ToList();
            _grid_704ILR.DataSource = data_704ILR;
            _lblCount_704ILR.Text = data_704ILR.Count + " " + Tr_704ILR.T_704ILR("AUD_COUNT");
        }

        private void Grid_CellFormatting_704ILR(object sender_704ILR, DataGridViewCellFormattingEventArgs e_704ILR)
        {
            if (e_704ILR.RowIndex < 0 || e_704ILR.ColumnIndex < 0) return;
            if (e_704ILR.ColumnIndex >= _grid_704ILR.Columns.Count) return;
            if (_grid_704ILR.Columns[e_704ILR.ColumnIndex].Name != "cAccion") return;

            string val_704ILR = e_704ILR.Value?.ToString();
            if (val_704ILR == "LOGIN_OK")
            {
                e_704ILR.CellStyle.ForeColor = Theme_704ILR.Success_704ILR;
                e_704ILR.CellStyle.Font = Theme_704ILR.FontBodyBold_704ILR;
            }
            else if (val_704ILR == "LOGIN_FAIL")
            {
                e_704ILR.CellStyle.ForeColor = Theme_704ILR.Error_704ILR;
                e_704ILR.CellStyle.Font = Theme_704ILR.FontBodyBold_704ILR;
            }
            else if (val_704ILR == "LOGOUT")
            {
                e_704ILR.CellStyle.ForeColor = Theme_704ILR.TextMuted_704ILR;
            }
            // Traduce el valor mostrado (el color se calculo arriba con el codigo crudo).
            if (val_704ILR == "LOGIN_OK" || val_704ILR == "LOGIN_FAIL" || val_704ILR == "LOGOUT") { e_704ILR.Value = Tr_704ILR.Accion_704ILR(val_704ILR); e_704ILR.FormattingApplied = true; }
        }

        // Item del combo de acciones: guarda el codigo (para filtrar) y muestra el
        // texto traducido (ToString se re-evalua al repintar / cambiar idioma).
        private sealed class AccionItem_704ILR
        {
            public string Code_704ILR { get; }
            public AccionItem_704ILR(string code_704ILR) { Code_704ILR = code_704ILR; }
            public override string ToString() => Tr_704ILR.Accion_704ILR(Code_704ILR);
        }
    }
}
