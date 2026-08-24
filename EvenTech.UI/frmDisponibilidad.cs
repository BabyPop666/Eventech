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
    public class frmDisponibilidad : FormBase
    {
        private DateTimePicker _dtFecha;
        private NumericUpDown _numCapacidad;
        private DataGridView _grid;
        private Label _lblResumen;
        private AppButton _btnUsar;
        private List<BE_DisponibilidadSalon> _resultado = new List<BE_DisponibilidadSalon>();

        // Seleccion confirmada con "Usar en la reserva" (valida si DialogResult = OK).
        public int SalonSeleccionado { get; private set; }
        public DateTime FechaSeleccionada { get; private set; }

        public frmDisponibilidad(DateTime fechaInicial)
        {
            BuildUi();
            _dtFecha.Value = fechaInicial < _dtFecha.MinDate ? _dtFecha.MinDate : fechaInicial;
            Consultar();
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
                Text = T("DISP_TITULO", "Consulta de disponibilidad"),
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
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // criterios
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // footer

            // --- Fila de criterios: fecha + invitados + consultar ---
            var criterios = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Theme.SpaceMd) };

            var lblFecha = Ui.FieldLabel(T("RES_LBL_FECHA", "Fecha del evento"));
            lblFecha.Margin = new Padding(0, 9, Theme.SpaceXs, 0);
            _dtFecha = Ui.DatePicker();
            _dtFecha.MinDate = DateTime.Today;
            _dtFecha.Width = 140;
            _dtFecha.Margin = new Padding(0, 0, Theme.SpaceMd, 0);

            var lblCap = Ui.FieldLabel(T("DISP_LBL_CAPACIDAD", "Invitados estimados"));
            lblCap.Margin = new Padding(0, 9, Theme.SpaceXs, 0);
            _numCapacidad = new NumericUpDown { Minimum = 0, Maximum = 100000, Width = 90, Font = Theme.FontInput, Margin = new Padding(0, 0, Theme.SpaceMd, 0), TextAlign = HorizontalAlignment.Right };

            var btnConsultar = Ui.Primary(T("BTN_CONSULTAR", "Consultar"), Theme.IcoSearch);
            btnConsultar.BehindColor = Theme.BgContent;
            btnConsultar.Size = new Size(130, 30);
            btnConsultar.Click += (s, e) => Consultar();

            criterios.Controls.Add(lblFecha);
            criterios.Controls.Add(_dtFecha);
            criterios.Controls.Add(lblCap);
            criterios.Controls.Add(_numCapacidad);
            criterios.Controls.Add(btnConsultar);

            // --- Grilla de salones ---
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme.SpaceMd), Padding = new Padding(Theme.SpaceSm) };
            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSalon",     HeaderText = T("COL_SALON", "Salon"), FillWeight = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCapacidad", HeaderText = T("COL_CAPACIDAD", "Capacidad"), FillWeight = 45, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado",    HeaderText = T("COL_ESTADO", "Estado"), FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cPropuesta", HeaderText = T("DISP_COL_PROPUESTA", "Proxima fecha libre"), FillWeight = 65 });
            _grid.SelectionChanged += (s, e) => ActualizarBotonUsar();
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Usar(); };
            card.Controls.Add(_grid);

            // --- Footer: resumen + usar en la reserva ---
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = true, BackColor = Color.Transparent };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lblResumen = new Label { Font = Theme.FontBodyBold, ForeColor = Theme.TextOnLight, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 6, 0, 0), BackColor = Color.Transparent, MaximumSize = new Size(520, 0) };
            _btnUsar = Ui.Primary(T("DISP_USAR", "Usar en la reserva"), Theme.IcoSave);
            _btnUsar.BehindColor = Theme.BgContent;
            _btnUsar.Size = new Size(190, 38);
            _btnUsar.Anchor = AnchorStyles.Right;
            _btnUsar.Click += (s, e) => Usar();
            footer.Controls.Add(_lblResumen, 0, 0);
            footer.Controls.Add(_btnUsar, 1, 0);

            root.Controls.Add(criterios, 0, 0);
            root.Controls.Add(card, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
            Controls.Add(pnlTitle);
            AcceptButton = btnConsultar;
        }

        private void Consultar()
        {
            try
            {
                _resultado = BLL_Disponibilidad.Consultar(_dtFecha.Value.Date, (int)_numCapacidad.Value);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Consultar disponibilidad");
                _lblResumen.ForeColor = Theme.Error;
                _lblResumen.Text = T("MSG_RES_ERROR", "No se pudo completar la operacion.");
                return;
            }

            _grid.Rows.Clear();
            foreach (var d in _resultado)
            {
                string estado = d.Disponible
                    ? T("DISP_EST_DISPONIBLE", "Disponible")
                    : !d.CapacidadSuficiente
                        ? T("DISP_EST_CAPACIDAD", "Capacidad insuficiente")
                        : T("DISP_EST_OCUPADO", "Ocupado");
                string propuesta = d.ProximaFechaLibre.HasValue ? d.ProximaFechaLibre.Value.ToString("yyyy-MM-dd") : "";

                int i = _grid.Rows.Add(d.SalonNombre, d.Capacidad, estado, propuesta);
                _grid.Rows[i].Tag = d;
                _grid.Rows[i].Cells["cEstado"].Style.ForeColor =
                    d.Disponible ? Theme.Success : !d.CapacidadSuficiente ? Theme.TextMuted : Theme.Error;
            }

            int disponibles = _resultado.Count(d => d.Disponible);
            if (disponibles > 0)
            {
                _lblResumen.ForeColor = Theme.Success;
                _lblResumen.Text = string.Format(T("DISP_RESUMEN_OK", "{0} salon(es) disponible(s) para la fecha consultada."), disponibles);
            }
            else
            {
                _lblResumen.ForeColor = Theme.Warning;
                _lblResumen.Text = T("DISP_RESUMEN_ALTERNATIVAS", "Ningun salon disponible para esa fecha: se proponen fechas alternativas.");
            }
            ActualizarBotonUsar();

            // La consulta es parte del proceso de venta: queda en la bitacora.
            BLL_Bitacora.Registrar("Reservas", "Disponibilidad consultada", CriticidadBitacora.Info,
                "Fecha " + _dtFecha.Value.ToString("yyyy-MM-dd") + " | Invitados " + (int)_numCapacidad.Value +
                " | Disponibles: " + disponibles + "/" + _resultado.Count);
        }

        // "Usar" toma el salon seleccionado: si esta disponible usa la fecha
        // consultada; si esta ocupado pero tiene propuesta, usa la fecha
        // alternativa (asi se concreta el "ofrecer otras propuestas" del proceso).
        private void Usar()
        {
            if (!(_grid.CurrentRow?.Tag is BE_DisponibilidadSalon d))
            {
                Aviso(T("DISP_SELECCIONE", "Seleccione un salon de la grilla."));
                return;
            }
            if (!d.CapacidadSuficiente)
            {
                Aviso(T("DISP_EST_CAPACIDAD", "Capacidad insuficiente"));
                return;
            }
            if (!d.Disponible && !d.ProximaFechaLibre.HasValue)
            {
                Aviso(T("DISP_SIN_PROPUESTA", "El salon no tiene fechas libres en el horizonte consultado."));
                return;
            }

            SalonSeleccionado = d.SalonId;
            FechaSeleccionada = d.Disponible ? d.FechaConsultada : d.ProximaFechaLibre.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ActualizarBotonUsar()
        {
            _btnUsar.Enabled = _grid.CurrentRow?.Tag is BE_DisponibilidadSalon d &&
                               d.CapacidadSuficiente && (d.Disponible || d.ProximaFechaLibre.HasValue);
        }

        private void Aviso(string msg) =>
            MessageBox.Show(this, msg, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
