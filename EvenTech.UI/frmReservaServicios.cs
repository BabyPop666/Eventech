using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EvenTech.BE;

namespace EvenTech.UI
{
    // Dialogo para cargar los servicios contratados de una reserva. Trabaja sobre
    // una copia en memoria; al Aceptar, expone la lista editada en Items para que
    // la ficha de reserva la persista junto con el monto (suma de subtotales).
    public class frmReservaServicios : FormBase
    {
        private readonly List<BE_Servicio> _disponibles;
        private DataGridView _grid;
        private ComboBox _cboServicio;
        private NumericUpDown _numCantidad;
        private Label _lblTotal;

        public List<BE_ReservaServicio> Items { get; private set; }

        public frmReservaServicios(IEnumerable<BE_ReservaServicio> actuales, List<BE_Servicio> disponibles)
        {
            _disponibles = disponibles ?? new List<BE_Servicio>();
            Items = actuales == null
                ? new List<BE_ReservaServicio>()
                : actuales.Select(Clone).ToList();
            BuildUi();
            Refrescar();
        }

        private static BE_ReservaServicio Clone(BE_ReservaServicio x) => new BE_ReservaServicio
        {
            Id = x.Id, ReservaId = x.ReservaId, ServicioId = x.ServicioId,
            ServicioNombre = x.ServicioNombre, Cantidad = x.Cantidad, PrecioUnitario = x.PrecioUnitario
        };

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(680, 480);
            BackColor = Theme.BgContent;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);
            var lblTitle = new Label
            {
                Text = T("RES_SERVICIOS", "Servicios de la reserva"),
                Font = Theme.FontH2, ForeColor = Theme.TextOnDark, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Theme.SpaceLg, 0, 0, 0), BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }, danger: true);
            btnClose.Dock = DockStyle.Right;
            pnlTitle.Controls.Add(lblTitle);
            pnlTitle.Controls.Add(btnClose);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceLg)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // alta
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // footer

            // --- Fila de alta: servicio + cantidad + agregar ---
            var alta = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme.SpaceMd) };
            _cboServicio = Ui.Combo(); _cboServicio.Width = 300; _cboServicio.Margin = new Padding(0, 0, Theme.SpaceSm, 0);
            foreach (var s in _disponibles) _cboServicio.Items.Add(s);
            if (_cboServicio.Items.Count > 0) _cboServicio.SelectedIndex = 0;
            _numCantidad = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1, Width = 70, Font = Theme.FontInput, Margin = new Padding(0, 0, Theme.SpaceSm, 0) };
            var btnAgregar = Ui.Primary(T("BTN_AGREGAR", "Agregar"), Theme.IcoAdd); btnAgregar.BehindColor = Theme.BgContent; btnAgregar.Size = new Size(120, 30); btnAgregar.Click += (s, e) => Agregar();
            var btnQuitar = Ui.Secondary(T("BTN_QUITAR", "Quitar"), Theme.IcoClear); btnQuitar.BehindColor = Theme.BgContent; btnQuitar.Size = new Size(110, 30); btnQuitar.Margin = new Padding(Theme.SpaceSm, 0, 0, 0); btnQuitar.Click += (s, e) => Quitar();
            alta.Controls.Add(_cboServicio); alta.Controls.Add(_numCantidad); alta.Controls.Add(btnAgregar); alta.Controls.Add(btnQuitar);

            // --- Grilla ---
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme.SpaceMd), Padding = new Padding(Theme.SpaceSm) };
            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            // La cantidad se edita en la grilla (asi se puede BAJAR sin quitar/re-agregar,
            // que perderia el precio congelado). El resto es de solo lectura.
            _grid.ReadOnly = false;
            _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cServicio", HeaderText = T("COL_SERVICIO", "Servicio"), FillWeight = 100, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCantidad", HeaderText = T("COL_CANTIDAD", "Cantidad"), FillWeight = 35, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPrecio", HeaderText = T("COL_PRECIO", "Precio"), FillWeight = 45, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSubtotal", HeaderText = T("COL_SUBTOTAL", "Subtotal"), FillWeight = 50, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            // Sin ordenamiento por encabezado: el indice de fila mapea 1:1 con Items,
            // asi "Quitar" borra la linea correcta y la edicion actualiza la correcta.
            foreach (DataGridViewColumn col in _grid.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
            _grid.CellEndEdit += Grid_CellEndEdit;
            card.Controls.Add(_grid);

            // --- Footer: total + aceptar ---
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblTotal = new Label { Font = Theme.FontH2, ForeColor = Theme.TextOnLight, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 6, 0, 0), BackColor = Color.Transparent };
            var btnAceptar = Ui.Primary(T("BTN_ACEPTAR", "Aceptar"), Theme.IcoSave); btnAceptar.BehindColor = Theme.BgContent; btnAceptar.Size = new Size(140, 38); btnAceptar.Anchor = AnchorStyles.Right;
            btnAceptar.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            footer.Controls.Add(_lblTotal, 0, 0);
            footer.Controls.Add(btnAceptar, 1, 0);

            root.Controls.Add(alta, 0, 0);
            root.Controls.Add(card, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
            Controls.Add(pnlTitle);
            AcceptButton = btnAceptar;
        }

        private void Agregar()
        {
            if (!(_cboServicio.SelectedItem is BE_Servicio s)) return;
            int cant = (int)_numCantidad.Value;
            var existente = Items.FirstOrDefault(i => i.ServicioId == s.Id);
            if (existente != null)
                existente.Cantidad += cant;
            else
                Items.Add(new BE_ReservaServicio { ServicioId = s.Id, ServicioNombre = s.Nombre, Cantidad = cant, PrecioUnitario = s.Precio });
            Refrescar();
        }

        private void Quitar()
        {
            if (_grid.CurrentRow == null) return;
            int idx = _grid.CurrentRow.Index;
            if (idx >= 0 && idx < Items.Count) { Items.RemoveAt(idx); Refrescar(); }
        }

        // Edicion de la cantidad en la grilla: actualiza el item conservando su
        // PrecioUnitario congelado y recalcula subtotal/total. Clampa a 1..9999.
        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= Items.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "cCantidad") return;

            int cant = Items[e.RowIndex].Cantidad;
            var val = _grid.Rows[e.RowIndex].Cells["cCantidad"].Value;
            if (val == null || !int.TryParse(val.ToString(), out cant)) cant = Items[e.RowIndex].Cantidad;
            if (cant < 1) cant = 1;
            if (cant > 9999) cant = 9999;

            Items[e.RowIndex].Cantidad = cant;
            _grid.Rows[e.RowIndex].Cells["cCantidad"].Value = cant;
            _grid.Rows[e.RowIndex].Cells["cSubtotal"].Value = Items[e.RowIndex].Subtotal;
            _lblTotal.Text = T("LBL_TOTAL", "Total") + ": " + Items.Sum(i => i.Subtotal).ToString("N2");
        }

        private void Refrescar()
        {
            _grid.Rows.Clear();
            foreach (var it in Items)
                _grid.Rows.Add(it.ServicioNombre, it.Cantidad, it.PrecioUnitario, it.Subtotal);
            _lblTotal.Text = T("LBL_TOTAL", "Total") + ": " + Items.Sum(i => i.Subtotal).ToString("N2");
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
