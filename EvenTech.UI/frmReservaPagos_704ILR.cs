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
    public class frmReservaPagos_704ILR : FormBase_704ILR
    {
        private readonly int _reservaId_704ILR;
        private readonly decimal _montoReserva_704ILR;
        private DataGridView _grid_704ILR;
        private ComboBox _cboMetodo_704ILR;
        private NumericUpDown _numMonto_704ILR;
        private TextBox _txtObs_704ILR;
        private Label _lblResumen_704ILR;

        public frmReservaPagos_704ILR(int reservaId_704ILR, decimal montoReserva_704ILR)
        {
            _reservaId_704ILR = reservaId_704ILR;
            _montoReserva_704ILR = montoReserva_704ILR;
            BuildUi_704ILR();
            Refrescar_704ILR();
        }

        private void BuildUi_704ILR()
        {
            Text = "EvenTech";
            ClientSize = new Size(760, 500);
            BackColor = Theme_704ILR.BgContent_704ILR;

            var pnlTitle_704ILR = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTitle_704ILR);
            var lblTitle_704ILR = new Label
            {
                Text = T_704ILR("RES_PAGOS", "Pagos de la reserva"),
                Font = Theme_704ILR.FontH2_704ILR, ForeColor = Theme_704ILR.TextOnDark_704ILR, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0), BackColor = Color.Transparent
            };
            EnableDrag_704ILR(lblTitle_704ILR);
            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => { DialogResult = DialogResult.OK; Close(); }, danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Right;
            pnlTitle_704ILR.Controls.Add(lblTitle_704ILR);
            pnlTitle_704ILR.Controls.Add(btnClose_704ILR);

            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // alta
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // footer

            // --- Fila de alta: metodo + monto + observacion + registrar/quitar ---
            var alta_704ILR = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR) };
            _cboMetodo_704ILR = Ui_704ILR.Combo_704ILR(); _cboMetodo_704ILR.Width = 170; _cboMetodo_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceSm_704ILR, 0);
            foreach (var m_704ILR in BLL_Pago_704ILR.GetMetodos_704ILR()) _cboMetodo_704ILR.Items.Add(m_704ILR);
            if (_cboMetodo_704ILR.Items.Count > 0) _cboMetodo_704ILR.SelectedIndex = 0;
            _numMonto_704ILR = new NumericUpDown { Minimum = 0, Maximum = 99999999, DecimalPlaces = 2, Increment = 1000, Width = 120, Font = Theme_704ILR.FontInput_704ILR, Margin = new Padding(0, 0, Theme_704ILR.SpaceSm_704ILR, 0), TextAlign = HorizontalAlignment.Right };
            _txtObs_704ILR = Ui_704ILR.Input_704ILR(); _txtObs_704ILR.Width = 150; _txtObs_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceSm_704ILR, 0);
            var btnRegistrar_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("BTN_REGISTRAR", "Registrar"), Theme_704ILR.IcoAdd_704ILR); btnRegistrar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR; btnRegistrar_704ILR.Size = new Size(130, 30); btnRegistrar_704ILR.Click += (s_704ILR, e_704ILR) => Registrar_704ILR();
            var btnQuitar_704ILR = Ui_704ILR.Secondary_704ILR(T_704ILR("BTN_QUITAR", "Quitar"), Theme_704ILR.IcoClear_704ILR); btnQuitar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR; btnQuitar_704ILR.Size = new Size(100, 30); btnQuitar_704ILR.Margin = new Padding(Theme_704ILR.SpaceSm_704ILR, 0, 0, 0); btnQuitar_704ILR.Click += (s_704ILR, e_704ILR) => Quitar_704ILR();
            // Anular un pago es una operacion sensible: se oculta a quien no la tiene.
            btnQuitar_704ILR.Visible = Permisos_704ILR.Tiene_704ILR("PAGOS_ANULAR");
            alta_704ILR.Controls.Add(_cboMetodo_704ILR); alta_704ILR.Controls.Add(_numMonto_704ILR); alta_704ILR.Controls.Add(_txtObs_704ILR); alta_704ILR.Controls.Add(btnRegistrar_704ILR); alta_704ILR.Controls.Add(btnQuitar_704ILR);

            // --- Grilla ---
            var card_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR), Padding = new Padding(Theme_704ILR.SpaceSm_704ILR) };
            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha", HeaderText = T_704ILR("COL_FECHA", "Fecha"), FillWeight = 55 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMetodo", HeaderText = T_704ILR("COL_METODO", "Metodo"), FillWeight = 60 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMonto", HeaderText = T_704ILR("COL_MONTO", "Monto"), FillWeight = 50, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cObs", HeaderText = T_704ILR("COL_OBSERVACION", "Observacion"), FillWeight = 90 });
            card_704ILR.Controls.Add(_grid_704ILR);

            // --- Footer: resumen (total/pagado/saldo) + cerrar ---
            var footer_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent };
            footer_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblResumen_704ILR = new Label { Font = Theme_704ILR.FontH2_704ILR, ForeColor = Theme_704ILR.TextOnLight_704ILR, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 6, 0, 0), BackColor = Color.Transparent };
            var btnCerrar_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("BTN_CERRAR", "Cerrar"), Theme_704ILR.IcoSave_704ILR); btnCerrar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR; btnCerrar_704ILR.Size = new Size(130, 38); btnCerrar_704ILR.Anchor = AnchorStyles.Right;
            btnCerrar_704ILR.Click += (s_704ILR, e_704ILR) => { DialogResult = DialogResult.OK; Close(); };
            footer_704ILR.Controls.Add(_lblResumen_704ILR, 0, 0);
            footer_704ILR.Controls.Add(btnCerrar_704ILR, 1, 0);

            root_704ILR.Controls.Add(alta_704ILR, 0, 0);
            root_704ILR.Controls.Add(card_704ILR, 0, 1);
            root_704ILR.Controls.Add(footer_704ILR, 0, 2);

            Controls.Add(root_704ILR);
            Controls.Add(pnlTitle_704ILR);
            AcceptButton = btnCerrar_704ILR;
        }

        private void Registrar_704ILR()
        {
            // Registrar un cobro mueve el saldo de la reserva: es una escritura y
            // exige su permiso, igual que la anulacion.
            if (!Permisos_704ILR.Exigir_704ILR("PAGOS_REGISTRAR", this, "registrar un pago en la reserva #" + _reservaId_704ILR)) return;
            if (!(_cboMetodo_704ILR.SelectedItem is BE_MetodoPago_704ILR m_704ILR)) return;
            var pago_704ILR = new BE_Pago_704ILR
            {
                ReservaId_704ILR = _reservaId_704ILR,
                MetodoPagoId_704ILR = m_704ILR.Id_704ILR,
                Monto_704ILR = _numMonto_704ILR.Value,
                Observacion_704ILR = string.IsNullOrWhiteSpace(_txtObs_704ILR.Text) ? null : _txtObs_704ILR.Text.Trim()
            };
            int id_704ILR;
            var res_704ILR = BLL_Pago_704ILR.Registrar_704ILR(pago_704ILR, out id_704ILR);
            switch (res_704ILR)
            {
                case PagoResult_704ILR.MontoInvalido:
                    Aviso_704ILR(T_704ILR("MSG_PAGO_MONTO", "Ingrese un monto valido.")); return;
                case PagoResult_704ILR.MetodoInvalido:
                    Aviso_704ILR(T_704ILR("MSG_PAGO_METODO", "Seleccione un metodo de pago.")); return;
                case PagoResult_704ILR.ExcedeSaldo:
                    Aviso_704ILR(T_704ILR("MSG_PAGO_EXCEDE", "El pago supera el saldo pendiente.")); return;
                case PagoResult_704ILR.ReservaInvalida:
                    Aviso_704ILR(T_704ILR("MSG_PAGO_RESERVA", "Reserva invalida.")); return;
                case PagoResult_704ILR.ReservaCancelada:
                    Aviso_704ILR(T_704ILR("MSG_RES_NO_MODIFICABLE", "La reserva esta cancelada: no admite modificaciones.")); return;
            }
            _numMonto_704ILR.Value = 0;
            _txtObs_704ILR.Clear();
            Refrescar_704ILR();
        }

        // Anulacion de un pago. Es destructiva e irreversible (no hay versionado de
        // pagos como si lo hay de reservas), asi que exige permiso propio y una
        // confirmacion explicita que nombra el importe que se va a anular.
        private void Quitar_704ILR()
        {
            if (_grid_704ILR.CurrentRow == null) return;
            if (!(_grid_704ILR.CurrentRow.Tag is int pagoId_704ILR)) return;

            if (!Permisos_704ILR.Exigir_704ILR("PAGOS_ANULAR", this, "anular el pago #" + pagoId_704ILR + " de la reserva #" + _reservaId_704ILR))
                return;

            decimal monto_704ILR = _grid_704ILR.CurrentRow.Cells["cMonto"].Value is decimal m_704ILR ? m_704ILR : 0m;
            var confirma_704ILR = MessageBox.Show(this,
                string.Format(T_704ILR("MSG_PAGO_ANULAR_CONF",
                    "Anular el pago de {0}? La operacion no se puede deshacer."), monto_704ILR.ToString("N2")),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirma_704ILR != DialogResult.Yes) return;

            BLL_Pago_704ILR.Eliminar_704ILR(pagoId_704ILR, _reservaId_704ILR);
            Refrescar_704ILR();
        }

        private void Refrescar_704ILR()
        {
            _grid_704ILR.Rows.Clear();
            var pagos_704ILR = BLL_Pago_704ILR.GetByReserva_704ILR(_reservaId_704ILR);
            foreach (var p_704ILR in pagos_704ILR)
            {
                int i_704ILR = _grid_704ILR.Rows.Add(p_704ILR.Fecha_704ILR.ToString("yyyy-MM-dd HH:mm"), p_704ILR.MetodoNombre_704ILR, p_704ILR.Monto_704ILR, p_704ILR.Observacion_704ILR ?? "");
                _grid_704ILR.Rows[i_704ILR].Tag = p_704ILR.Id_704ILR;
            }
            decimal pagado_704ILR = pagos_704ILR.Sum(p_704ILR => p_704ILR.Monto_704ILR);
            decimal saldo_704ILR = _montoReserva_704ILR - pagado_704ILR;
            _lblResumen_704ILR.Text =
                T_704ILR("LBL_TOTAL", "Total") + ": " + _montoReserva_704ILR.ToString("N2") + "    " +
                T_704ILR("LBL_PAGADO", "Pagado") + ": " + pagado_704ILR.ToString("N2") + "    " +
                T_704ILR("LBL_SALDO", "Saldo") + ": " + saldo_704ILR.ToString("N2");
            _lblResumen_704ILR.ForeColor = saldo_704ILR <= 0 ? Theme_704ILR.Success_704ILR : Theme_704ILR.TextOnLight_704ILR;
        }

        private void Aviso_704ILR(string msg_704ILR) =>
            MessageBox.Show(msg_704ILR, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
