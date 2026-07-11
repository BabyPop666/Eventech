using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Ventana modal con las versiones (mementos) de una reserva: cada fila es la
    // foto completa que se guardo automaticamente antes de cada modificacion.
    // Permite restaurar cualquiera (patron Memento: la UI solo habla con el
    // Caretaker/BLL; nunca interpreta el contenido de la foto).
    // Devuelve DialogResult.OK si se restauro una version, para que la pantalla
    // de reservas recargue la grilla.
    public class frmVersionesReserva : FormBase, IObservadorIdioma
    {
        private readonly int _reservaId;

        private Label _lblTitle;
        private DataGridView _grid;
        private Label _lblVacio;
        private AppButton _btnRestaurar;
        private DataGridViewTextBoxColumn _colFecha, _colUsuario, _colCliente, _colSalon, _colFechaEvento, _colEstado, _colMonto;

        public frmVersionesReserva(int reservaId)
        {
            _reservaId = reservaId;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => CargarVersiones();
            GestorDeIdioma.GetInstance.Suscribir(this);
            FormClosed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(860, 480);
            BackColor = Theme.BgContent;

            // ---------------- Barra de titulo ----------------
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTop);

            _lblTitle = new Label
            {
                Font = Theme.FontH2,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(_lblTitle);

            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Close(), danger: true);
            btnClose.Dock = DockStyle.Right;

            pnlTop.Controls.Add(_lblTitle);
            pnlTop.Controls.Add(btnClose);

            // ---------------- Contenido (tarjeta con grilla + acciones) ----------------
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceLg)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // boton restaurar

            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                BehindColor = Theme.BgContent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd),
                Padding = new Padding(Theme.SpaceSm)
            };

            _grid = new DataGridView
            {
                Name = "grid",
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface
            };
            UiGrid.Style(_grid);

            _colFecha = new DataGridViewTextBoxColumn
            {
                Name = "Fecha",
                DataPropertyName = "Fecha",
                FillWeight = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            };
            _colUsuario     = new DataGridViewTextBoxColumn { Name = "Usuario",       DataPropertyName = "Usuario",       FillWeight = 45 };
            _colCliente     = new DataGridViewTextBoxColumn { Name = "ClienteNombre", DataPropertyName = "ClienteNombre", FillWeight = 65 };
            _colSalon       = new DataGridViewTextBoxColumn { Name = "SalonNombre",   DataPropertyName = "SalonNombre",   FillWeight = 55 };
            _colFechaEvento = new DataGridViewTextBoxColumn
            {
                Name = "FechaEvento",
                DataPropertyName = "FechaEvento",
                FillWeight = 50,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            };
            _colEstado = new DataGridViewTextBoxColumn { Name = "Estado", DataPropertyName = "Estado", FillWeight = 45 };
            _colMonto  = new DataGridViewTextBoxColumn
            {
                Name = "Monto",
                DataPropertyName = "Monto",
                FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            _grid.Columns.AddRange(_colFecha, _colUsuario, _colCliente, _colSalon, _colFechaEvento, _colEstado, _colMonto);
            _grid.CellFormatting += Grid_CellFormatting;

            // Estado vacio: centrado sobre la grilla, visible solo si no hay filas.
            _lblVacio = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.Surface,
                Visible = false
            };

            card.Controls.Add(_lblVacio);
            card.Controls.Add(_grid);

            var acciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            _btnRestaurar = Ui.Primary("Restaurar seleccionada");
            _btnRestaurar.BehindColor = Theme.BgContent;
            _btnRestaurar.Size = new Size(220, 38);
            _btnRestaurar.Click += (s, e) => Restaurar();
            acciones.Controls.Add(_btnRestaurar);

            layout.Controls.Add(card, 0, 0);
            layout.Controls.Add(acciones, 0, 1);

            pnlContent.Controls.Add(layout);

            Controls.Add(pnlContent);
            Controls.Add(pnlTop);
        }

        private void CargarVersiones()
        {
            try
            {
                List<BE_ReservaMemento> data = CaretakerReserva.GetVersiones(_reservaId);
                _grid.DataSource = data;
                ActualizarEstadoVacio(data == null || data.Count == 0);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar versiones de reserva");
                ActualizarEstadoVacio(true);
                MessageBox.Show(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, Tr.T("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Restaurar()
        {
            if (!(_grid.CurrentRow?.DataBoundItem is BE_ReservaMemento memento)) return;

            var confirma = MessageBox.Show(
                T("VER_CONFIRMA", "Restaurar la reserva al estado de la version seleccionada? El estado actual se guardara como una nueva version."),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirma != DialogResult.Yes) return;

            try
            {
                ReservaResult result = BLL_Reserva.RestaurarVersion(_reservaId, memento.Id);
                if (result != ReservaResult.Success)
                {
                    MessageBox.Show(MensajeError(result), Tr.T("MSG_ERROR"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(T("MSG_VER_OK", "Version restaurada."), "EvenTech",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Restaurar version de reserva");
                MessageBox.Show(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, Tr.T("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Traduce el valor de la columna Estado (el enum se muestra segun el idioma).
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
            if (_grid.Columns[e.ColumnIndex].Name != "Estado") return;
            if (e.Value is EstadoReserva est) { e.Value = Tr.Estado(est); e.FormattingApplied = true; }
        }

        private void ActualizarEstadoVacio(bool vacio)
        {
            _lblVacio.Visible = vacio;
            _grid.Visible = !vacio;
            _btnRestaurar.Enabled = !vacio;
        }

        private static string MensajeError(ReservaResult r)
        {
            switch (r)
            {
                case ReservaResult.InvalidCliente: return Tr.T("MSG_RES_CLIENTE");
                case ReservaResult.InvalidSalon:   return Tr.T("MSG_RES_SALON");
                case ReservaResult.InvalidFecha:   return Tr.T("MSG_RES_FECHA");
                case ReservaResult.InvalidMonto:   return Tr.T("MSG_RES_MONTO");
                case ReservaResult.SalonOcupado:   return Tr.T("MSG_RES_SALON_OCUPADO");
                case ReservaResult.NotFound:       return Tr.T("MSG_RES_NOTFOUND");
                default:                           return Tr.T("MSG_RES_ERROR");
            }
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // Observador (patron Observer): re-traduce titulo, encabezados y acciones.
        public void ActualizarTextos()
        {
            if (_lblTitle != null) _lblTitle.Text = T("VER_TITULO", "Versiones de la reserva") + " #" + _reservaId;
            if (_colFecha != null)
            {
                _colFecha.HeaderText       = Tr.T("COL_FECHA");
                _colUsuario.HeaderText     = Tr.T("COL_USUARIO");
                _colCliente.HeaderText     = Tr.T("COL_CLIENTE");
                _colSalon.HeaderText       = Tr.T("COL_SALON");
                _colFechaEvento.HeaderText = Tr.T("RES_LBL_FECHA");
                _colEstado.HeaderText      = Tr.T("COL_ESTADO");
                _colMonto.HeaderText       = Tr.T("COL_MONTO");
            }
            if (_lblVacio != null) _lblVacio.Text = T("VER_VACIO", "Sin versiones guardadas. Se crea una automaticamente al modificar la reserva.");
            if (_btnRestaurar != null) _btnRestaurar.Text = T("VER_RESTAURAR", "Restaurar seleccionada");
            _grid?.Invalidate();
        }
    }
}
