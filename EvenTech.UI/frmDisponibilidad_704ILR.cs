using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Consulta de disponibilidad (Proceso 1, paso 1). El vendedor carga la fecha
    // del evento y los invitados estimados; la grilla muestra que salones estan
    // libres, cuales no alcanzan en capacidad y, para los ocupados que si
    // alcanzan, la proxima fecha libre como propuesta alternativa. "Usar en la
    // reserva" devuelve salon + fecha para precargar la ficha.
    public class frmDisponibilidad_704ILR : FormBase_704ILR
    {
        private DateTimePicker _dtFecha_704ILR;
        private NumericUpDown _numCapacidad_704ILR;
        private DataGridView _grid_704ILR;
        private Label _lblResumen_704ILR;
        private AppButton_704ILR _btnUsar_704ILR;
        private List<BE_DisponibilidadSalon_704ILR> _resultado_704ILR = new List<BE_DisponibilidadSalon_704ILR>();

        // Seleccion confirmada con "Usar en la reserva" (valida si DialogResult = OK).
        public int SalonSeleccionado_704ILR { get; private set; }
        public DateTime FechaSeleccionada_704ILR { get; private set; }

        // Invitados con los que se hizo la consulta: vuelve a la ficha de la
        // reserva para que la cantidad estimada quede registrada en la operacion
        // (PN1: Capacidad_Requerida) y no se pierda al cerrar el dialogo.
        public int InvitadosConsultados_704ILR => (int)_numCapacidad_704ILR.Value;

        public frmDisponibilidad_704ILR(DateTime fechaInicial_704ILR, int invitadosIniciales_704ILR = 0)
        {
            BuildUi_704ILR();
            _dtFecha_704ILR.Value = fechaInicial_704ILR < _dtFecha_704ILR.MinDate ? _dtFecha_704ILR.MinDate : fechaInicial_704ILR;
            if (invitadosIniciales_704ILR > 0 && invitadosIniciales_704ILR <= _numCapacidad_704ILR.Maximum)
                _numCapacidad_704ILR.Value = invitadosIniciales_704ILR;
            Consultar_704ILR();
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
                Text = T_704ILR("DISP_TITULO", "Consulta de disponibilidad"),
                Font = Theme_704ILR.FontH2_704ILR, ForeColor = Theme_704ILR.TextOnDark_704ILR, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0), BackColor = Color.Transparent
            };
            EnableDrag_704ILR(lblTitle_704ILR);
            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => { DialogResult = DialogResult.Cancel; Close(); }, danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Right;
            pnlTitle_704ILR.Controls.Add(lblTitle_704ILR);
            pnlTitle_704ILR.Controls.Add(btnClose_704ILR);

            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // criterios
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // footer

            // --- Fila de criterios: fecha + invitados + consultar ---
            var criterios_704ILR = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR) };

            var lblFecha_704ILR = Ui_704ILR.FieldLabel_704ILR(T_704ILR("RES_LBL_FECHA", "Fecha del evento"));
            lblFecha_704ILR.Margin = new Padding(0, 9, Theme_704ILR.SpaceXs_704ILR, 0);
            _dtFecha_704ILR = Ui_704ILR.DatePicker_704ILR();
            _dtFecha_704ILR.MinDate = DateTime.Today;
            _dtFecha_704ILR.Width = 140;
            _dtFecha_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);

            var lblCap_704ILR = Ui_704ILR.FieldLabel_704ILR(T_704ILR("DISP_LBL_CAPACIDAD", "Invitados estimados"));
            lblCap_704ILR.Margin = new Padding(0, 9, Theme_704ILR.SpaceXs_704ILR, 0);
            _numCapacidad_704ILR = new NumericUpDown { Minimum = 0, Maximum = 100000, Width = 90, Font = Theme_704ILR.FontInput_704ILR, Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0), TextAlign = HorizontalAlignment.Right };

            var btnConsultar_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("BTN_CONSULTAR", "Consultar"), Theme_704ILR.IcoSearch_704ILR);
            btnConsultar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            btnConsultar_704ILR.Size = new Size(130, 30);
            btnConsultar_704ILR.Click += (s_704ILR, e_704ILR) => Consultar_704ILR();

            criterios_704ILR.Controls.Add(lblFecha_704ILR);
            criterios_704ILR.Controls.Add(_dtFecha_704ILR);
            criterios_704ILR.Controls.Add(lblCap_704ILR);
            criterios_704ILR.Controls.Add(_numCapacidad_704ILR);
            criterios_704ILR.Controls.Add(btnConsultar_704ILR);

            // --- Grilla de salones ---
            var card_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR), Padding = new Padding(Theme_704ILR.SpaceSm_704ILR) };
            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSalon",     HeaderText = T_704ILR("COL_SALON", "Salon"), FillWeight = 80 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCapacidad", HeaderText = T_704ILR("COL_CAPACIDAD", "Capacidad"), FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado",    HeaderText = T_704ILR("COL_ESTADO", "Estado"), FillWeight = 70 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPropuesta", HeaderText = T_704ILR("DISP_COL_PROPUESTA", "Proxima fecha libre"), FillWeight = 65 });
            _grid_704ILR.SelectionChanged += (s_704ILR, e_704ILR) => ActualizarBotonUsar_704ILR();
            _grid_704ILR.CellDoubleClick += (s_704ILR, e_704ILR) => { if (e_704ILR.RowIndex >= 0) Usar_704ILR(); };
            card_704ILR.Controls.Add(_grid_704ILR);

            // --- Footer: resumen + usar en la reserva ---
            var footer_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent };
            footer_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblResumen_704ILR = new Label { Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.TextOnLight_704ILR, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 6, 0, 0), BackColor = Color.Transparent, MaximumSize = new Size(520, 0) };
            _btnUsar_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("DISP_USAR", "Usar en la reserva"), Theme_704ILR.IcoSave_704ILR);
            _btnUsar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnUsar_704ILR.Size = new Size(190, 38);
            _btnUsar_704ILR.Anchor = AnchorStyles.Right;
            _btnUsar_704ILR.Click += (s_704ILR, e_704ILR) => Usar_704ILR();
            footer_704ILR.Controls.Add(_lblResumen_704ILR, 0, 0);
            footer_704ILR.Controls.Add(_btnUsar_704ILR, 1, 0);

            root_704ILR.Controls.Add(criterios_704ILR, 0, 0);
            root_704ILR.Controls.Add(card_704ILR, 0, 1);
            root_704ILR.Controls.Add(footer_704ILR, 0, 2);

            Controls.Add(root_704ILR);
            Controls.Add(pnlTitle_704ILR);
            AcceptButton = btnConsultar_704ILR;
        }

        private void Consultar_704ILR()
        {
            try
            {
                _resultado_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(_dtFecha_704ILR.Value.Date, (int)_numCapacidad_704ILR.Value);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Consultar disponibilidad");
                _lblResumen_704ILR.ForeColor = Theme_704ILR.Error_704ILR;
                _lblResumen_704ILR.Text = T_704ILR("MSG_RES_ERROR", "No se pudo completar la operacion.");
                return;
            }

            _grid_704ILR.Rows.Clear();
            foreach (var d_704ILR in _resultado_704ILR)
            {
                string estado_704ILR = d_704ILR.Disponible_704ILR
                    ? T_704ILR("DISP_EST_DISPONIBLE", "Disponible")
                    : !d_704ILR.CapacidadSuficiente_704ILR
                        ? T_704ILR("DISP_EST_CAPACIDAD", "Capacidad insuficiente")
                        : T_704ILR("DISP_EST_OCUPADO", "Ocupado");
                string propuesta_704ILR = d_704ILR.ProximaFechaLibre_704ILR.HasValue ? d_704ILR.ProximaFechaLibre_704ILR.Value.ToString("yyyy-MM-dd") : "";

                int i_704ILR = _grid_704ILR.Rows.Add(d_704ILR.SalonNombre_704ILR, d_704ILR.Capacidad_704ILR, estado_704ILR, propuesta_704ILR);
                _grid_704ILR.Rows[i_704ILR].Tag = d_704ILR;
                _grid_704ILR.Rows[i_704ILR].Cells["cEstado"].Style.ForeColor =
                    d_704ILR.Disponible_704ILR ? Theme_704ILR.Success_704ILR : !d_704ILR.CapacidadSuficiente_704ILR ? Theme_704ILR.TextMuted_704ILR : Theme_704ILR.Error_704ILR;
            }

            int disponibles_704ILR = _resultado_704ILR.Count(d_704ILR => d_704ILR.Disponible_704ILR);
            if (disponibles_704ILR > 0)
            {
                _lblResumen_704ILR.ForeColor = Theme_704ILR.Success_704ILR;
                _lblResumen_704ILR.Text = string.Format(T_704ILR("DISP_RESUMEN_OK", "{0} salon(es) disponible(s) para la fecha consultada."), disponibles_704ILR);
            }
            else
            {
                _lblResumen_704ILR.ForeColor = Theme_704ILR.Warning_704ILR;
                _lblResumen_704ILR.Text = T_704ILR("DISP_RESUMEN_ALTERNATIVAS", "Ningun salon disponible para esa fecha: se proponen fechas alternativas.");
            }
            ActualizarBotonUsar_704ILR();

            // La consulta es parte del proceso de venta: queda en la bitacora.
            BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Disponibilidad consultada", CriticidadBitacora_704ILR.Info,
                "Fecha " + _dtFecha_704ILR.Value.ToString("yyyy-MM-dd") + " | Invitados " + (int)_numCapacidad_704ILR.Value +
                " | Disponibles: " + disponibles_704ILR + "/" + _resultado_704ILR.Count);
        }

        // "Usar" toma el salon seleccionado: si esta disponible usa la fecha
        // consultada; si esta ocupado pero tiene propuesta, usa la fecha
        // alternativa (asi se concreta el "ofrecer otras propuestas" del proceso).
        private void Usar_704ILR()
        {
            if (!(_grid_704ILR.CurrentRow?.Tag is BE_DisponibilidadSalon_704ILR d_704ILR))
            {
                Aviso_704ILR(T_704ILR("DISP_SELECCIONE", "Seleccione un salon de la grilla."));
                return;
            }
            if (!d_704ILR.CapacidadSuficiente_704ILR)
            {
                Aviso_704ILR(T_704ILR("DISP_EST_CAPACIDAD", "Capacidad insuficiente"));
                return;
            }
            if (!d_704ILR.Disponible_704ILR && !d_704ILR.ProximaFechaLibre_704ILR.HasValue)
            {
                Aviso_704ILR(T_704ILR("DISP_SIN_PROPUESTA", "El salon no tiene fechas libres en el horizonte consultado."));
                return;
            }

            SalonSeleccionado_704ILR = d_704ILR.SalonId_704ILR;
            FechaSeleccionada_704ILR = d_704ILR.Disponible_704ILR ? d_704ILR.FechaConsultada_704ILR : d_704ILR.ProximaFechaLibre_704ILR.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ActualizarBotonUsar_704ILR()
        {
            _btnUsar_704ILR.Enabled = _grid_704ILR.CurrentRow?.Tag is BE_DisponibilidadSalon_704ILR d_704ILR &&
                               d_704ILR.CapacidadSuficiente_704ILR && (d_704ILR.Disponible_704ILR || d_704ILR.ProximaFechaLibre_704ILR.HasValue);
        }

        private void Aviso_704ILR(string msg_704ILR) =>
            MessageBox.Show(this, msg_704ILR, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
