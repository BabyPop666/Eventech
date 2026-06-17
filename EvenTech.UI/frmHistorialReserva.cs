using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Ventana modal que muestra el historial de cambios (control de cambios) de
    // una reserva puntual, campo por campo y en orden cronologico.
    public class frmHistorialReserva : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private readonly int _reservaId;

        public frmHistorialReserva(int reservaId)
        {
            _reservaId = reservaId;
            BuildUi();
            Load += (s, e) => CargarHistorial();
        }

        private void BuildUi()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 420);
            BackColor = Theme.BgContent;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            pnlTop.MouseDown += Drag;
            var lblTitle = new Label
            {
                Text = "Historial de la reserva #" + _reservaId,
                Font = new Font("Ebrima", 12F, FontStyle.Bold),
                ForeColor = Theme.TextOnDark,
                AutoSize = true,
                Location = new Point(14, 11),
                BackColor = Color.Transparent
            };
            lblTitle.MouseDown += Drag;
            var btnClose = new Label
            {
                Text = "✕", ForeColor = Theme.TextOnDark, Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, Size = new Size(34, 28), Location = new Point(600, 8), Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(btnClose);

            var grid = new DataGridView
            {
                Name = "grid",
                Location = new Point(15, 58),
                Size = new Size(610, 348),
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
                Font = new Font("Ebrima", 9.5F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.BgTitleBar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Ebrima", 9.5F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 30;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha",    DataPropertyName = "Fecha",         FillWeight = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Usuario",  DataPropertyName = "Usuario",       FillWeight = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Campo",    DataPropertyName = "NombreCampo",   FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Anterior", DataPropertyName = "ValorAnterior", FillWeight = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nuevo",    DataPropertyName = "ValorNuevo",    FillWeight = 70 });

            Controls.Add(grid);
            Controls.Add(pnlTop);
        }

        private void CargarHistorial()
        {
            var grid = (DataGridView)Controls["grid"];
            try
            {
                List<BE_CambioEntry> data = RegistradorDeCambios.GetHistorial("Reserva", _reservaId);
                grid.DataSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar el historial: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
