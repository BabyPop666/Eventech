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
    public class ucBitacora_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private TextBox _txtUsuario_704ILR;
        private DateTimePicker _dtDesde_704ILR, _dtHasta_704ILR;
        private ComboBox _cboModulo_704ILR, _cboCriticidad_704ILR;
        private DataGridView _grid_704ILR;
        private Label _lblCount_704ILR, _lblError_704ILR;
        private AppButton_704ILR _btnBuscar_704ILR, _btnLimpiar_704ILR;

        public ucBitacora_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => { CargarModulos_704ILR(); SafeBuscar_704ILR(); GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this); };
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            // ---------------- Layout raiz ----------------
            // Filas: 0 titulo, 1 tarjeta de filtros, 2 labels de estado, 3 tarjeta de grilla (fill).
            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // titulo
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); // filtros (2 filas con wrap)
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // estado
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla

            // ---------------- Titulo ----------------
            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Bitacora del Sistema");
            lblTitle_704ILR.Tag = "T:BIT_TITULO";
            lblTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            // ---------------- Tarjeta de filtros ----------------
            var cardFiltros_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR),
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceMd_704ILR, Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceMd_704ILR)
            };

            // Fila de filtros: cada control etiquetado con Ui.Field, apilados en horizontal.
            // Dock=Top + AutoSize => ocupa el ancho de la tarjeta y, si no entran, los
            // filtros/botones envuelven a una segunda fila (la tarjeta crece en alto).
            var flow_704ILR = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _txtUsuario_704ILR = Ui_704ILR.Input_704ILR();
            _txtUsuario_704ILR.Width = 150;

            _dtDesde_704ILR = Ui_704ILR.DatePicker_704ILR();
            _dtDesde_704ILR.Width = 130;
            _dtDesde_704ILR.ShowCheckBox = true;
            _dtDesde_704ILR.Checked = false;

            _dtHasta_704ILR = Ui_704ILR.DatePicker_704ILR();
            _dtHasta_704ILR.Width = 130;
            _dtHasta_704ILR.ShowCheckBox = true;
            _dtHasta_704ILR.Checked = false;

            _cboModulo_704ILR = Ui_704ILR.Combo_704ILR();
            _cboModulo_704ILR.Width = 160;

            _cboCriticidad_704ILR = Ui_704ILR.Combo_704ILR();
            _cboCriticidad_704ILR.Width = 140;
            _cboCriticidad_704ILR.Items.Add(Tr_704ILR.T_704ILR("OPT_TODAS"));
            _cboCriticidad_704ILR.Items.Add(CriticidadBitacora_704ILR.Info);
            _cboCriticidad_704ILR.Items.Add(CriticidadBitacora_704ILR.Advertencia);
            _cboCriticidad_704ILR.Items.Add(CriticidadBitacora_704ILR.Error);
            _cboCriticidad_704ILR.SelectedIndex = 0;
            Ui_704ILR.DibujarEnum_704ILR(_cboCriticidad_704ILR, o_704ILR => o_704ILR is CriticidadBitacora_704ILR cb_704ILR ? Tr_704ILR.Criticidad_704ILR(cb_704ILR) : o_704ILR?.ToString());

            var fUsuario_704ILR = Ui_704ILR.Field_704ILR("Usuario", _txtUsuario_704ILR);
            fUsuario_704ILR.Tag = "FIELD:COL_USUARIO";
            var fDesde_704ILR = Ui_704ILR.Field_704ILR("Desde", _dtDesde_704ILR);
            fDesde_704ILR.Tag = "FIELD:BIT_DESDE";
            var fHasta_704ILR = Ui_704ILR.Field_704ILR("Hasta", _dtHasta_704ILR);
            fHasta_704ILR.Tag = "FIELD:BIT_HASTA";
            var fModulo_704ILR = Ui_704ILR.Field_704ILR("Modulo", _cboModulo_704ILR);
            fModulo_704ILR.Tag = "FIELD:COL_MODULO";
            var fCrit_704ILR = Ui_704ILR.Field_704ILR("Criticidad", _cboCriticidad_704ILR);
            fCrit_704ILR.Tag = "FIELD:COL_CRITICIDAD";

            // Separacion horizontal entre campos.
            foreach (var f_704ILR in new[] { fUsuario_704ILR, fDesde_704ILR, fHasta_704ILR, fModulo_704ILR, fCrit_704ILR })
                f_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0);

            // Botones de accion alineados con la fila de inputs (debajo del caption).
            _btnBuscar_704ILR = Ui_704ILR.Primary_704ILR("Buscar", Theme_704ILR.IcoSearch_704ILR);
            _btnBuscar_704ILR.Tag = "T:BTN_BUSCAR";
            _btnBuscar_704ILR.Size = new Size(120, 32);
            _btnBuscar_704ILR.Margin = new Padding(0, 18, Theme_704ILR.SpaceSm_704ILR, 0);
            _btnBuscar_704ILR.Click += (s_704ILR, e_704ILR) => SafeBuscar_704ILR();

            _btnLimpiar_704ILR = Ui_704ILR.Secondary_704ILR("Limpiar", Theme_704ILR.IcoClear_704ILR);
            _btnLimpiar_704ILR.Tag = "T:BTN_LIMPIAR";
            _btnLimpiar_704ILR.Size = new Size(120, 32);
            _btnLimpiar_704ILR.Margin = new Padding(0, 18, 0, 0);
            _btnLimpiar_704ILR.Click += (s_704ILR, e_704ILR) => { LimpiarFiltros_704ILR(); SafeBuscar_704ILR(); };

            flow_704ILR.Controls.Add(fUsuario_704ILR);
            flow_704ILR.Controls.Add(fDesde_704ILR);
            flow_704ILR.Controls.Add(fHasta_704ILR);
            flow_704ILR.Controls.Add(fModulo_704ILR);
            flow_704ILR.Controls.Add(fCrit_704ILR);
            flow_704ILR.Controls.Add(_btnBuscar_704ILR);
            flow_704ILR.Controls.Add(_btnLimpiar_704ILR);

            cardFiltros_704ILR.Controls.Add(flow_704ILR);

            // ---------------- Labels de estado (conteo / error) ----------------
            var pnlEstado_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR)
            };

            _lblCount_704ILR = new Label
            {
                AutoSize = true,
                Font = Theme_704ILR.FontSmall_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                Location = new Point(2, 0),
                BackColor = Color.Transparent
            };
            _lblError_704ILR = new Label
            {
                AutoSize = true,
                Font = Theme_704ILR.FontBodyBold_704ILR,
                ForeColor = Theme_704ILR.Error_704ILR,
                Location = new Point(2, 0),
                Visible = false,
                MaximumSize = new Size(900, 0),
                BackColor = Color.Transparent
            };
            pnlEstado_704ILR.Controls.Add(_lblCount_704ILR);
            pnlEstado_704ILR.Controls.Add(_lblError_704ILR);

            // ---------------- Tarjeta de la grilla ----------------
            var cardGrid_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(Theme_704ILR.SpaceSm_704ILR)
            };

            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);
            _grid_704ILR.CellFormatting += Grid_CellFormatting_704ILR;

            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",     HeaderText = "Id",         DataPropertyName = "Id_704ILR",         FillWeight = 25 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",  HeaderText = "Fecha",      DataPropertyName = "Fecha_704ILR",      FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario",HeaderText = "Usuario",    DataPropertyName = "Usuario_704ILR",    FillWeight = 60 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cModulo", HeaderText = "Modulo",     DataPropertyName = "Modulo_704ILR",     FillWeight = 55 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cAccion", HeaderText = "Accion",     DataPropertyName = "Accion_704ILR",     FillWeight = 90 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCrit",   HeaderText = "Criticidad", DataPropertyName = "Criticidad_704ILR", FillWeight = 55 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDetalle",HeaderText = "Detalle",    DataPropertyName = "Detalle_704ILR",    FillWeight = 150 });

            cardGrid_704ILR.Controls.Add(_grid_704ILR);

            // ---------------- Ensamblado ----------------
            root_704ILR.Controls.Add(lblTitle_704ILR, 0, 0);
            root_704ILR.Controls.Add(cardFiltros_704ILR, 0, 1);
            root_704ILR.Controls.Add(pnlEstado_704ILR, 0, 2);
            root_704ILR.Controls.Add(cardGrid_704ILR, 0, 3);

            Controls.Add(root_704ILR);
        }

        public void ActualizarTextos_704ILR()
        {
            Tr_704ILR.AplicarTags_704ILR(this);
            // Las etiquetas de los Ui.Field no se traducen por Tag (su caption es un
            // Label hijo): se re-traducen explicitamente desde el Tag "FIELD:CLAVE".
            ActualizarFieldLabels_704ILR(this);

            if (_grid_704ILR.Columns.Count >= 7)
            {
                _grid_704ILR.Columns["cId"].HeaderText      = Tr_704ILR.T_704ILR("COL_ID");
                _grid_704ILR.Columns["cFecha"].HeaderText   = Tr_704ILR.T_704ILR("COL_FECHA");
                _grid_704ILR.Columns["cUsuario"].HeaderText = Tr_704ILR.T_704ILR("COL_USUARIO");
                _grid_704ILR.Columns["cModulo"].HeaderText  = Tr_704ILR.T_704ILR("COL_MODULO");
                _grid_704ILR.Columns["cAccion"].HeaderText  = Tr_704ILR.T_704ILR("COL_ACCION");
                _grid_704ILR.Columns["cCrit"].HeaderText    = Tr_704ILR.T_704ILR("COL_CRITICIDAD");
                _grid_704ILR.Columns["cDetalle"].HeaderText = Tr_704ILR.T_704ILR("COL_DETALLE");
            }
            // Placeholder "(Todas)"/"(Todos)" de los combos de filtro.
            if (_cboCriticidad_704ILR.Items.Count > 0)
            {
                int sel_704ILR = _cboCriticidad_704ILR.SelectedIndex;
                _cboCriticidad_704ILR.Items[0] = Tr_704ILR.T_704ILR("OPT_TODAS");
                _cboCriticidad_704ILR.SelectedIndex = sel_704ILR;
                _cboCriticidad_704ILR.Invalidate();
            }
            if (_cboModulo_704ILR.Items.Count > 0)
            {
                int sel_704ILR = _cboModulo_704ILR.SelectedIndex;
                _cboModulo_704ILR.Items[0] = Tr_704ILR.T_704ILR("OPT_TODOS");
                _cboModulo_704ILR.SelectedIndex = sel_704ILR;
            }
            if (_grid_704ILR.DataSource != null && _lblCount_704ILR.Visible) _lblCount_704ILR.Text = _grid_704ILR.Rows.Count + " " + Tr_704ILR.T_704ILR("BIT_COUNT");
            _grid_704ILR.Invalidate(); // re-traduce los valores de criticidad en las celdas
        }

        // Recorre el arbol buscando paneles Ui.Field con Tag "FIELD:CLAVE" y traduce
        // su primer Label (el caption del campo).
        private static void ActualizarFieldLabels_704ILR(Control root_704ILR)
        {
            foreach (Control c_704ILR in root_704ILR.Controls)
            {
                if (c_704ILR.Tag is string tag_704ILR && tag_704ILR.StartsWith("FIELD:"))
                {
                    string clave_704ILR = tag_704ILR.Substring(6);
                    foreach (Control hijo_704ILR in c_704ILR.Controls)
                        if (hijo_704ILR is Label lbl_704ILR) { lbl_704ILR.Text = Tr_704ILR.T_704ILR(clave_704ILR); break; }
                }
                if (c_704ILR.HasChildren)
                    ActualizarFieldLabels_704ILR(c_704ILR);
            }
        }

        private void CargarModulos_704ILR()
        {
            try
            {
                _cboModulo_704ILR.Items.Clear();
                _cboModulo_704ILR.Items.Add(Tr_704ILR.T_704ILR("OPT_TODOS"));
                foreach (var m_704ILR in BLL_Bitacora_704ILR.GetModulos_704ILR()) _cboModulo_704ILR.Items.Add(m_704ILR);
                _cboModulo_704ILR.SelectedIndex = 0;
            }
            catch { _cboModulo_704ILR.SelectedIndex = -1; }
        }

        private void LimpiarFiltros_704ILR()
        {
            _txtUsuario_704ILR.Text = "";
            _dtDesde_704ILR.Checked = false;
            _dtHasta_704ILR.Checked = false;
            if (_cboModulo_704ILR.Items.Count > 0) _cboModulo_704ILR.SelectedIndex = 0;
            _cboCriticidad_704ILR.SelectedIndex = 0;
        }

        private void SafeBuscar_704ILR()
        {
            // Segunda capa: la vista carga sola al abrirse, asi que el permiso se
            // exige aca y no solo donde se decide mostrarla.
            if (!Permisos_704ILR.Tiene_704ILR("BITACORA_VER"))
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

                var filtros_704ILR = new BitacoraFiltros_704ILR
                {
                    Usuario_704ILR = string.IsNullOrWhiteSpace(_txtUsuario_704ILR.Text) ? null : _txtUsuario_704ILR.Text.Trim(),
                    FechaInicio_704ILR = _dtDesde_704ILR.Checked ? _dtDesde_704ILR.Value : (DateTime?)null,
                    FechaFin_704ILR = _dtHasta_704ILR.Checked ? _dtHasta_704ILR.Value : (DateTime?)null,
                    Modulo_704ILR = (_cboModulo_704ILR.SelectedIndex > 0) ? _cboModulo_704ILR.SelectedItem.ToString() : null,
                    Criticidad_704ILR = (_cboCriticidad_704ILR.SelectedItem is CriticidadBitacora_704ILR c_704ILR) ? c_704ILR : (CriticidadBitacora_704ILR?)null
                };

                List<BE_BitacoraEntry_704ILR> data_704ILR = BLL_Bitacora_704ILR.Buscar_704ILR(filtros_704ILR);
                _grid_704ILR.DataSource = data_704ILR;
                _lblCount_704ILR.Text = data_704ILR.Count + " " + Tr_704ILR.T_704ILR("BIT_COUNT");
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Bitacora", "Buscar");
                _lblCount_704ILR.Visible = false;
                _lblError_704ILR.Text = Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.GetType().Name + " - " + ex_704ILR.Message;
                _lblError_704ILR.Visible = true;
            }
        }

        private void Grid_CellFormatting_704ILR(object sender_704ILR, DataGridViewCellFormattingEventArgs e_704ILR)
        {
            if (e_704ILR.RowIndex < 0 || e_704ILR.ColumnIndex < 0 || e_704ILR.ColumnIndex >= _grid_704ILR.Columns.Count) return;
            if (_grid_704ILR.Columns[e_704ILR.ColumnIndex].DataPropertyName != "Criticidad_704ILR") return;

            switch (e_704ILR.Value?.ToString())
            {
                case "Error":       e_704ILR.CellStyle.ForeColor = Theme_704ILR.Error_704ILR; e_704ILR.CellStyle.Font = Theme_704ILR.FontBodyBold_704ILR; break;
                case "Advertencia": e_704ILR.CellStyle.ForeColor = Theme_704ILR.Warning_704ILR; break;
                case "Info":        e_704ILR.CellStyle.ForeColor = Theme_704ILR.Success_704ILR; break;
            }
            // Traduce el valor mostrado (el color se calculo arriba con el valor crudo).
            if (e_704ILR.Value is CriticidadBitacora_704ILR cb_704ILR) { e_704ILR.Value = Tr_704ILR.Criticidad_704ILR(cb_704ILR); e_704ILR.FormattingApplied = true; }
        }
    }
}
