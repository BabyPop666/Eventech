using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Dialogo para registrar/anular pagos de una reserva (Proceso 1, paso 5).
    // A diferencia de los servicios, los pagos persisten en el acto: cada alta o
    // baja impacta la base y se recalcula el saldo (tope = total de la reserva).
    public class frmReservaPagos : FormBase
    {
        private readonly int _reservaId;
        private readonly decimal _montoReserva;
        private DataGridView _grid;
        private ComboBox _cboMetodo;
        private NumericUpDown _numMonto;
        private TextBox _txtObs;
        private Label _lblResumen;

        public frmReservaPagos(int reservaId, decimal montoReserva)
        {
            _reservaId = reservaId;
            _montoReserva = montoReserva;
            BuildUi();
            Refrescar();
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(760, 500);
            BackColor = Theme.BgContent;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);
            var lblTitle = new Label
            {
                Text = T("RES_PAGOS", "Pagos de la reserva"),
                Font = Theme.FontH2, ForeColor = Theme.TextOnDark, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Theme.SpaceLg, 0, 0, 0), BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => { DialogResult = DialogResult.OK; Close(); }, danger: true);
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

            // --- Fila de alta: metodo + monto + observacion + registrar/quitar ---
            var alta = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme.SpaceMd) };
            _cboMetodo = Ui.Combo(); _cboMetodo.Width = 170; _cboMetodo.Margin = new Padding(0, 0, Theme.SpaceSm, 0);
            foreach (var m in BLL_Pago.GetMetodos()) _cboMetodo.Items.Add(m);
            if (_cboMetodo.Items.Count > 0) _cboMetodo.SelectedIndex = 0;
            _numMonto = new NumericUpDown { Minimum = 0, Maximum = 99999999, DecimalPlaces = 2, Increment = 1000, Width = 120, Font = Theme.FontInput, Margin = new Padding(0, 0, Theme.SpaceSm, 0), TextAlign = HorizontalAlignment.Right };
            _txtObs = Ui.Input(); _txtObs.Width = 150; _txtObs.Margin = new Padding(0, 0, Theme.SpaceSm, 0);
            var btnRegistrar = Ui.Primary(T("BTN_REGISTRAR", "Registrar"), Theme.IcoAdd); btnRegistrar.BehindColor = Theme.BgContent; btnRegistrar.Size = new Size(130, 30); btnRegistrar.Click += (s, e) => Registrar();
            var btnQuitar = Ui.Secondary(T("BTN_QUITAR", "Quitar"), Theme.IcoClear); btnQuitar.BehindColor = Theme.BgContent; btnQuitar.Size = new Size(100, 30); btnQuitar.Margin = new Padding(Theme.SpaceSm, 0, 0, 0); btnQuitar.Click += (s, e) => Quitar();
            // Anular un pago es una operacion sensible: se oculta a quien no la tiene.
            btnQuitar.Visible = Permisos.Tiene("PAGOS_ANULAR");
            alta.Controls.Add(_cboMetodo); alta.Controls.Add(_numMonto); alta.Controls.Add(_txtObs); alta.Controls.Add(btnRegistrar); alta.Controls.Add(btnQuitar);

            // --- Grilla ---
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme.SpaceMd), Padding = new Padding(Theme.SpaceSm) };
            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha", HeaderText = T("COL_FECHA", "Fecha"), FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMetodo", HeaderText = T("COL_METODO", "Metodo"), FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMonto", HeaderText = T("COL_MONTO", "Monto"), FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cObs", HeaderText = T("COL_OBSERVACION", "Observacion"), FillWeight = 90 });
            card.Controls.Add(_grid);

            // --- Footer: resumen (total/pagado/saldo) + cerrar ---
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblResumen = new Label { Font = Theme.FontH2, ForeColor = Theme.TextOnLight, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 6, 0, 0), BackColor = Color.Transparent };
            var btnCerrar = Ui.Primary(T("BTN_CERRAR", "Cerrar"), Theme.IcoSave); btnCerrar.BehindColor = Theme.BgContent; btnCerrar.Size = new Size(130, 38); btnCerrar.Anchor = AnchorStyles.Right;
            btnCerrar.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            footer.Controls.Add(_lblResumen, 0, 0);
            footer.Controls.Add(btnCerrar, 1, 0);

            root.Controls.Add(alta, 0, 0);
            root.Controls.Add(card, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
            Controls.Add(pnlTitle);
            AcceptButton = btnCerrar;
        }

        private void Registrar()
        {
            // Registrar un cobro mueve el saldo de la reserva: es una escritura y
            // exige su permiso, igual que la anulacion.
            if (!Permisos.Exigir("PAGOS_REGISTRAR", this, "registrar un pago en la reserva #" + _reservaId)) return;
            if (!(_cboMetodo.SelectedItem is BE_MetodoPago m)) return;
            var pago = new BE_Pago
            {
                ReservaId = _reservaId,
                MetodoPagoId = m.Id,
                Monto = _numMonto.Value,
                Observacion = string.IsNullOrWhiteSpace(_txtObs.Text) ? null : _txtObs.Text.Trim()
            };
            int id;
            var res = BLL_Pago.Registrar(pago, out id);
            switch (res)
            {
                case PagoResult.MontoInvalido:
                    Aviso(T("MSG_PAGO_MONTO", "Ingrese un monto valido.")); return;
                case PagoResult.MetodoInvalido:
                    Aviso(T("MSG_PAGO_METODO", "Seleccione un metodo de pago.")); return;
                case PagoResult.ExcedeSaldo:
                    Aviso(T("MSG_PAGO_EXCEDE", "El pago supera el saldo pendiente.")); return;
                case PagoResult.ReservaInvalida:
                    Aviso(T("MSG_PAGO_RESERVA", "Reserva invalida.")); return;
                case PagoResult.ReservaCancelada:
                    Aviso(T("MSG_RES_NO_MODIFICABLE", "La reserva esta cancelada: no admite modificaciones.")); return;
            }
            _numMonto.Value = 0;
            _txtObs.Clear();
            Refrescar();
        }

        // Anulacion de un pago. Es destructiva e irreversible (no hay versionado de
        // pagos como si lo hay de reservas), asi que exige permiso propio y una
        // confirmacion explicita que nombra el importe que se va a anular.
        private void Quitar()
        {
            if (_grid.CurrentRow == null) return;
            if (!(_grid.CurrentRow.Tag is int pagoId)) return;

            if (!Permisos.Exigir("PAGOS_ANULAR", this, "anular el pago #" + pagoId + " de la reserva #" + _reservaId))
                return;

            decimal monto = _grid.CurrentRow.Cells["cMonto"].Value is decimal m ? m : 0m;
            var confirma = MessageBox.Show(this,
                string.Format(T("MSG_PAGO_ANULAR_CONF",
                    "Anular el pago de {0}? La operacion no se puede deshacer."), monto.ToString("N2")),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirma != DialogResult.Yes) return;

            BLL_Pago.Eliminar(pagoId, _reservaId);
            Refrescar();
        }

        private void Refrescar()
        {
            _grid.Rows.Clear();
            var pagos = BLL_Pago.GetByReserva(_reservaId);
            foreach (var p in pagos)
            {
                int i = _grid.Rows.Add(p.Fecha.ToString("yyyy-MM-dd HH:mm"), p.MetodoNombre, p.Monto, p.Observacion ?? "");
                _grid.Rows[i].Tag = p.Id;
            }
            decimal pagado = pagos.Sum(p => p.Monto);
            decimal saldo = _montoReserva - pagado;
            _lblResumen.Text =
                T("LBL_TOTAL", "Total") + ": " + _montoReserva.ToString("N2") + "    " +
                T("LBL_PAGADO", "Pagado") + ": " + pagado.ToString("N2") + "    " +
                T("LBL_SALDO", "Saldo") + ": " + saldo.ToString("N2");
            _lblResumen.ForeColor = saldo <= 0 ? Theme.Success : Theme.TextOnLight;
        }

        private void Aviso(string msg) =>
            MessageBox.Show(msg, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
