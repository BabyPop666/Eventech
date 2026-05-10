using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // UserControl que muestra el registro de auditoria con refresco manual.
    //
    // Estrategia para evitar bugs de binding:
    //   - Columnas declaradas explicitas (AutoGenerateColumns=false). Asi
    //     controlamos nombres, anchos y formato; no dependemos de reflection.
    //   - LoadData() corre en el evento Load para que el handle del grid ya
    //     este creado cuando bindeamos.
    //   - El catch global escribe el error a un label, no a un MessageBox,
    //     para no ocultar la causa real.
    public class ucAuditoria : UserControl
    {
        private DataGridView _grid;
        private Label _lblCount;
        private Label _lblError;

        public ucAuditoria()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            Load += (s, e) => SafeLoadData();
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "Registro de Auditoria",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var btnRefresh = new Button
            {
                Text = "Refrescar",
                Font = Theme.FontButton,
                BackColor = Theme.AccentButton,
                ForeColor = Theme.TextOnDark,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 32),
                Location = new Point(10, 55),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => SafeLoadData();

            _lblCount = new Label
            {
                AutoSize = true,
                Font = new Font("Ebrima", 10F),
                ForeColor = Color.DimGray,
                Location = new Point(135, 62)
            };

            _lblError = new Label
            {
                AutoSize = true,
                Font = new Font("Ebrima", 10F, FontStyle.Bold),
                ForeColor = Color.Firebrick,
                Location = new Point(10, 92),
                Visible = false,
                MaximumSize = new Size(900, 0)
            };

            _grid = new DataGridView
            {
                Location = new Point(10, 115),
                Size = new Size(900, 450),
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
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _grid.ColumnHeadersHeight = 32;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            _grid.CellFormatting += Grid_CellFormatting;

            // Columnas explicitas. FillWeight le da peso relativo a Fill mode.
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",        HeaderText = "Id",       DataPropertyName = "Id",          FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha",     HeaderText = "Fecha",    DataPropertyName = "Timestamp",   FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUsuario",   HeaderText = "Usuario",  DataPropertyName = "Username",    FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAccion",    HeaderText = "Accion",   DataPropertyName = "Action",      FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaquina",   HeaderText = "Maquina",  DataPropertyName = "MachineName", FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetalle",   HeaderText = "Detalle",  DataPropertyName = "Details",     FillWeight = 120 });

            Controls.Add(lblTitle);
            Controls.Add(btnRefresh);
            Controls.Add(_lblCount);
            Controls.Add(_lblError);
            Controls.Add(_grid);
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                List<BE_LoginAuditEntry> data = BLL_LoginAudit.GetAll(500);
                _grid.DataSource = data;
                _lblCount.Text = $"{data.Count} registros";
            }
            catch (Exception ex)
            {
                _lblError.Text = "Error cargando auditoria: " + ex.GetType().Name + " - " + ex.Message;
                _lblError.Visible = true;
                _lblCount.Text = "";
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex >= _grid.Columns.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "colAccion") return;

            string val = e.Value?.ToString();
            if (val == "LOGIN_OK")
            {
                e.CellStyle.ForeColor = Color.SeaGreen;
                e.CellStyle.Font = new Font("Ebrima", 9.5F, FontStyle.Bold);
            }
            else if (val == "LOGIN_FAIL")
            {
                e.CellStyle.ForeColor = Color.Firebrick;
                e.CellStyle.Font = new Font("Ebrima", 9.5F, FontStyle.Bold);
            }
            else if (val == "LOGOUT")
            {
                e.CellStyle.ForeColor = Color.DimGray;
            }
        }
    }
}
