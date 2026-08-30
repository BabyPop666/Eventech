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
    public class frmVersionesReserva_704ILR : FormBase_704ILR, IObservadorIdioma_704ILR
    {
        private readonly int _reservaId_704ILR;

        private Label _lblTitle_704ILR;
        private DataGridView _grid_704ILR;
        private Label _lblVacio_704ILR;
        private AppButton_704ILR _btnRestaurar_704ILR;
        private DataGridViewTextBoxColumn _colFecha_704ILR, _colUsuario_704ILR, _colCliente_704ILR, _colSalon_704ILR, _colFechaEvento_704ILR, _colInvitados_704ILR, _colEstado_704ILR, _colMonto_704ILR;

        public frmVersionesReserva_704ILR(int reservaId_704ILR)
        {
            _reservaId_704ILR = reservaId_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => CargarVersiones_704ILR();
            GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            FormClosed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            Text = "EvenTech";
            ClientSize = new Size(860, 480);
            BackColor = Theme_704ILR.BgContent_704ILR;

            // ---------------- Barra de titulo ----------------
            var pnlTop_704ILR = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTop_704ILR);

            _lblTitle_704ILR = new Label
            {
                Font = Theme_704ILR.FontH2_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(_lblTitle_704ILR);

            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => Close(), danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Right;

            pnlTop_704ILR.Controls.Add(_lblTitle_704ILR);
            pnlTop_704ILR.Controls.Add(btnClose_704ILR);

            // ---------------- Contenido (tarjeta con grilla + acciones) ----------------
            var pnlContent_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };

            var layout_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // boton restaurar

            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                BehindColor_704ILR = Theme_704ILR.BgContent_704ILR,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR),
                Padding = new Padding(Theme_704ILR.SpaceSm_704ILR)
            };

            _grid_704ILR = new DataGridView
            {
                Name = "grid",
                Dock = DockStyle.Fill,
                BackgroundColor = Theme_704ILR.Surface_704ILR
            };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);

            _colFecha_704ILR = new DataGridViewTextBoxColumn
            {
                Name = "Fecha",
                DataPropertyName = "Fecha_704ILR",
                FillWeight = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            };
            _colUsuario_704ILR     = new DataGridViewTextBoxColumn { Name = "Usuario",       DataPropertyName = "Usuario_704ILR",       FillWeight = 45 };
            _colCliente_704ILR     = new DataGridViewTextBoxColumn { Name = "ClienteNombre", DataPropertyName = "ClienteNombre_704ILR", FillWeight = 65 };
            _colSalon_704ILR       = new DataGridViewTextBoxColumn { Name = "SalonNombre",   DataPropertyName = "SalonNombre_704ILR",   FillWeight = 55 };
            _colFechaEvento_704ILR = new DataGridViewTextBoxColumn
            {
                Name = "FechaEvento",
                DataPropertyName = "FechaEvento_704ILR",
                FillWeight = 50,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            };
            // El memento conserva la cantidad de invitados de cada version (es parte del
            // estado de negocio y la RN-06 depende de el): la pantalla tiene que mostrarla.
            _colInvitados_704ILR = new DataGridViewTextBoxColumn
            {
                Name = "Invitados",
                DataPropertyName = "CantidadInvitados_704ILR",
                FillWeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            _colEstado_704ILR = new DataGridViewTextBoxColumn { Name = "Estado", DataPropertyName = "Estado_704ILR", FillWeight = 45 };
            _colMonto_704ILR  = new DataGridViewTextBoxColumn
            {
                Name = "Monto",
                DataPropertyName = "Monto_704ILR",
                FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            _grid_704ILR.Columns.AddRange(_colFecha_704ILR, _colUsuario_704ILR, _colCliente_704ILR, _colSalon_704ILR, _colFechaEvento_704ILR, _colInvitados_704ILR, _colEstado_704ILR, _colMonto_704ILR);
            _grid_704ILR.CellFormatting += Grid_CellFormatting_704ILR;

            // Estado vacio: centrado sobre la grilla, visible solo si no hay filas.
            _lblVacio_704ILR = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme_704ILR.FontBody_704ILR,
                ForeColor = Theme_704ILR.TextMuted_704ILR,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme_704ILR.Surface_704ILR,
                Visible = false
            };

            card_704ILR.Controls.Add(_lblVacio_704ILR);
            card_704ILR.Controls.Add(_grid_704ILR);

            var acciones_704ILR = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            _btnRestaurar_704ILR = Ui_704ILR.Primary_704ILR("Restaurar seleccionada");
            _btnRestaurar_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnRestaurar_704ILR.Size = new Size(220, 38);
            _btnRestaurar_704ILR.Click += (s_704ILR, e_704ILR) => Restaurar_704ILR();
            acciones_704ILR.Controls.Add(_btnRestaurar_704ILR);

            layout_704ILR.Controls.Add(card_704ILR, 0, 0);
            layout_704ILR.Controls.Add(acciones_704ILR, 0, 1);

            pnlContent_704ILR.Controls.Add(layout_704ILR);

            Controls.Add(pnlContent_704ILR);
            Controls.Add(pnlTop_704ILR);
        }

        private void CargarVersiones_704ILR()
        {
            try
            {
                List<BE_ReservaMemento_704ILR> data_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(_reservaId_704ILR);
                _grid_704ILR.DataSource = data_704ILR;
                ActualizarEstadoVacio_704ILR(data_704ILR == null || data_704ILR.Count == 0);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Cargar versiones de reserva");
                ActualizarEstadoVacio_704ILR(true);
                MessageBox.Show(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, Tr_704ILR.T_704ILR("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Restaurar_704ILR()
        {
            if (!(_grid_704ILR.CurrentRow?.DataBoundItem is BE_ReservaMemento_704ILR memento_704ILR)) return;

            var confirma_704ILR = MessageBox.Show(
                T_704ILR("VER_CONFIRMA", "Restaurar la reserva al estado de la version seleccionada? El estado actual se guardara como una nueva version."),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirma_704ILR != DialogResult.Yes) return;

            try
            {
                ReservaResult_704ILR result_704ILR = BLL_Reserva_704ILR.RestaurarVersion_704ILR(_reservaId_704ILR, memento_704ILR.Id_704ILR);
                if (result_704ILR != ReservaResult_704ILR.Success)
                {
                    MessageBox.Show(MensajeError_704ILR(result_704ILR), Tr_704ILR.T_704ILR("MSG_ERROR"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show(T_704ILR("MSG_VER_OK", "Version restaurada."), "EvenTech",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Restaurar version de reserva");
                MessageBox.Show(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, Tr_704ILR.T_704ILR("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Traduce el valor de la columna Estado (el enum se muestra segun el idioma).
        private void Grid_CellFormatting_704ILR(object sender_704ILR, DataGridViewCellFormattingEventArgs e_704ILR)
        {
            if (e_704ILR.RowIndex < 0 || e_704ILR.ColumnIndex < 0 || e_704ILR.ColumnIndex >= _grid_704ILR.Columns.Count) return;
            if (_grid_704ILR.Columns[e_704ILR.ColumnIndex].Name != "Estado") return;
            if (e_704ILR.Value is EstadoReserva_704ILR est_704ILR) { e_704ILR.Value = Tr_704ILR.Estado_704ILR(est_704ILR); e_704ILR.FormattingApplied = true; }
        }

        private void ActualizarEstadoVacio_704ILR(bool vacio_704ILR)
        {
            _lblVacio_704ILR.Visible = vacio_704ILR;
            _grid_704ILR.Visible = !vacio_704ILR;
            _btnRestaurar_704ILR.Enabled = !vacio_704ILR;
        }

        private static string MensajeError_704ILR(ReservaResult_704ILR r_704ILR)
        {
            switch (r_704ILR)
            {
                case ReservaResult_704ILR.InvalidCliente: return Tr_704ILR.T_704ILR("MSG_RES_CLIENTE");
                case ReservaResult_704ILR.InvalidSalon:   return Tr_704ILR.T_704ILR("MSG_RES_SALON");
                case ReservaResult_704ILR.InvalidFecha:   return Tr_704ILR.T_704ILR("MSG_RES_FECHA");
                case ReservaResult_704ILR.InvalidMonto:   return Tr_704ILR.T_704ILR("MSG_RES_MONTO");
                case ReservaResult_704ILR.SalonOcupado:   return Tr_704ILR.T_704ILR("MSG_RES_SALON_OCUPADO");
                case ReservaResult_704ILR.NotFound:       return Tr_704ILR.T_704ILR("MSG_RES_NOTFOUND");
                // Motivos que la restauracion puede devolver desde que rige la RN-05/RN-06:
                // sin estos casos el dialogo mostraria un error generico sabiendo la causa.
                case ReservaResult_704ILR.NoModificable:
                    return T_704ILR("MSG_RES_NO_MODIFICABLE", "La reserva esta cancelada: no admite modificaciones.");
                case ReservaResult_704ILR.TransicionInvalida:
                    return T_704ILR("MSG_RES_TRANSICION_GEN", "El cambio de estado solicitado no esta admitido.");
                case ReservaResult_704ILR.CapacidadInsuficiente:
                    return T_704ILR("MSG_RES_CAPACIDAD", "El salon no alcanza para la cantidad de invitados indicada.");
                case ReservaResult_704ILR.InvalidInvitados:
                    return T_704ILR("MSG_RES_INVITADOS", "La cantidad de invitados no puede ser negativa.");
                case ReservaResult_704ILR.Vencida:
                    return T_704ILR("MSG_RES_VENCIDA", "La operacion vencio: renovala antes de confirmarla.");
                default:                           return Tr_704ILR.T_704ILR("MSG_RES_ERROR");
            }
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        // Observador (patron Observer): re-traduce titulo, encabezados y acciones.
        public void ActualizarTextos_704ILR()
        {
            if (_lblTitle_704ILR != null) _lblTitle_704ILR.Text = T_704ILR("VER_TITULO", "Versiones de la reserva") + " #" + _reservaId_704ILR;
            if (_colFecha_704ILR != null)
            {
                _colFecha_704ILR.HeaderText       = Tr_704ILR.T_704ILR("COL_FECHA");
                _colUsuario_704ILR.HeaderText     = Tr_704ILR.T_704ILR("COL_USUARIO");
                _colCliente_704ILR.HeaderText     = Tr_704ILR.T_704ILR("COL_CLIENTE");
                _colSalon_704ILR.HeaderText       = Tr_704ILR.T_704ILR("COL_SALON");
                _colFechaEvento_704ILR.HeaderText = Tr_704ILR.T_704ILR("RES_LBL_FECHA");
                _colInvitados_704ILR.HeaderText   = T_704ILR("COL_INVITADOS", "Invitados");
                _colEstado_704ILR.HeaderText      = Tr_704ILR.T_704ILR("COL_ESTADO");
                _colMonto_704ILR.HeaderText       = Tr_704ILR.T_704ILR("COL_MONTO");
            }
            if (_lblVacio_704ILR != null) _lblVacio_704ILR.Text = T_704ILR("VER_VACIO", "Sin versiones guardadas. Se crea una automaticamente al modificar la reserva.");
            if (_btnRestaurar_704ILR != null) _btnRestaurar_704ILR.Text = T_704ILR("VER_RESTAURAR", "Restaurar seleccionada");
            _grid_704ILR?.Invalidate();
        }
    }
}
