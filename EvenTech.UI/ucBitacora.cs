using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Bitacora general con busqueda combinada (usuario + rango de fechas +
    // modulo + criticidad). Cada filtro es opcional.
    public class ucBitacora : UserControl
    {
        private TextBox _txtUsuario;
        private DateTimePicker _dtDesde, _dtHasta;
        private ComboBox _cboModulo, _cboCriticidad;
        private DataGridView _grid;
        private Label _lblCount, _lblError;

        public ucBitacora()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            Load += (s, e) => { CargarModulos(); SafeBuscar(); };
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "Bitacora del Sistema",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            // --- fila de filtros ---
            var lblU = MiniLabel("Usuario", 10, 50);
            _txtUsuario = new TextBox { Location = new Point(10, 70), Size = new Size(130, 26), Font = new Font("Ebrima", 10F) };

            var lblD = MiniLabel("Desde", 150, 50);
            _dtDesde = new DateTimePicker { Location = new Point(150, 70), Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Font = new Font("Ebrima", 10F), ShowCheckBox = true, Checked = false };

            var lblH = MiniLabel("Hasta", 270, 50);
            _dtHasta = new DateTimePicker { Location = new Point(270, 70), Size = new Size(110, 26), Format = DateTimePickerFormat.Short, Font = new Font("Ebrima", 10F), ShowCheckBox = true, Checked = false };

            var lblM = MiniLabel("Modulo", 390, 50);
            _cboModulo = new ComboBox { Location = new Point(390, 70), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Ebrima", 10F) };

            var lblC = MiniLabel("Criticidad", 540, 50);
            _cboCriticidad = new ComboBox { Location = new Point(540, 70), Size = new Size(120, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Ebrima", 10F) };
            _cboCriticidad.Items.Add("(Todas)");
            _cboCriticidad.Items.Add(CriticidadBitacora.Info);
            _cboCriticidad.Items.Add(CriticidadBitacora.Advertencia);
            _cboCriticidad.Items.Add(CriticidadBitacora.Error);
            _cboCriticidad.SelectedIndex = 0;

            var btnBuscar = new Button
            {
                Text = "Buscar", Font = Theme.FontButton, BackColor = Theme.AccentButton,
                ForeColor = Theme.TextOnDark, FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 30), Location = new Point(675, 68), Cursor = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => SafeBuscar();

            var btnLimpiar = new Button
            {
                Text = "Limpiar", Font = Theme.FontButton, BackColor = Color.Gray,
                ForeColor = Theme.TextOnDark, FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30), Location = new Point(772, 68), Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => { LimpiarFiltros(); SafeBuscar(); };

            _lblCount = new Label { AutoSize = true, Font = new Font("Ebrima", 10F), ForeColor = Color.DimGray, Location = new Point(12, 108) };
            _lblError = new Label { AutoSize = true, Font = new Font("Ebrima", 10F, FontStyle.Bold), ForeColor = Color.Firebrick, Location = new Point(12, 108), Visible = false, MaximumSize = new Size(900, 0) };

            _grid = new DataGridView
            {
                Location = new Point(10, 132),
                Size = new Size(905, 430),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                Font = new Font("Ebrima", 9.5F)
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.BgTitleBar;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Ebrima", 10F, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 32;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            _grid.CellFormatting += Grid_CellFormatting;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id",         DataPropertyName = "Id",         FillWeight = 25 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha",      DataPropertyName = "Fecha",      FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Usuario",    DataPropertyName = "Usuario",    FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Modulo",     DataPropertyName = "Modulo",     FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Accion",     DataPropertyName = "Accion",     FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Criticidad", DataPropertyName = "Criticidad", FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Detalle",    DataPropertyName = "Detalle",    FillWeight = 150 });

            Controls.Add(lblTitle);
            Controls.Add(lblU); Controls.Add(_txtUsuario);
            Controls.Add(lblD); Controls.Add(_dtDesde);
            Controls.Add(lblH); Controls.Add(_dtHasta);
            Controls.Add(lblM); Controls.Add(_cboModulo);
            Controls.Add(lblC); Controls.Add(_cboCriticidad);
            Controls.Add(btnBuscar); Controls.Add(btnLimpiar);
            Controls.Add(_lblCount); Controls.Add(_lblError);
            Controls.Add(_grid);
        }

        private Label MiniLabel(string text, int x, int y) => new Label
        {
            Text = text, Font = new Font("Ebrima", 9F), ForeColor = Color.DimGray,
            AutoSize = true, Location = new Point(x, y)
        };

        private void CargarModulos()
        {
            try
            {
                _cboModulo.Items.Clear();
                _cboModulo.Items.Add("(Todos)");
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
                _lblCount.Text = $"{data.Count} registros";
            }
            catch (Exception ex)
            {
                _lblCount.Visible = false;
                _lblError.Text = "Error cargando bitacora: " + ex.GetType().Name + " - " + ex.Message;
                _lblError.Visible = true;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
            if (_grid.Columns[e.ColumnIndex].DataPropertyName != "Criticidad") return;

            switch (e.Value?.ToString())
            {
                case "Error":       e.CellStyle.ForeColor = Color.Firebrick; e.CellStyle.Font = new Font("Ebrima", 9.5F, FontStyle.Bold); break;
                case "Advertencia": e.CellStyle.ForeColor = Color.DarkGoldenrod; break;
                case "Info":        e.CellStyle.ForeColor = Color.SeaGreen; break;
            }
        }
    }
}
