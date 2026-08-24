using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Ventana modal que muestra el historial de cambios (control de cambios) de
    // una reserva puntual, campo por campo y en orden cronologico.
    // Borderless heredando de FormBase (cromo compartido), con barra de titulo de
    // marca y la grilla agrupada en una tarjeta. Implementa el Observer de idioma
    // para re-traducir titulo, encabezados y estado vacio sin recrear la vista.
    public class frmHistorialReserva_704ILR : FormBase_704ILR, IObservadorIdioma_704ILR
    {
        private readonly int _reservaId_704ILR;

        private Label _lblTitle_704ILR;
        private DataGridView _grid_704ILR;
        private Label _lblVacio_704ILR;
        private DataGridViewTextBoxColumn _colFecha_704ILR, _colUsuario_704ILR, _colCampo_704ILR, _colAnterior_704ILR, _colNuevo_704ILR;

        public frmHistorialReserva_704ILR(int reservaId_704ILR)
        {
            _reservaId_704ILR = reservaId_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => CargarHistorial_704ILR();
            GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            FormClosed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            Text = "EvenTech";
            ClientSize = new Size(680, 460);
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

            // ---------------- Contenido (tarjeta con grilla) ----------------
            var pnlContent_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };

            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                BehindColor_704ILR = Theme_704ILR.BgContent_704ILR,
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
                FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            };
            _colUsuario_704ILR  = new DataGridViewTextBoxColumn { Name = "Usuario",       DataPropertyName = "Usuario_704ILR",       FillWeight = 55 };
            _colCampo_704ILR    = new DataGridViewTextBoxColumn { Name = "NombreCampo",   DataPropertyName = "NombreCampo_704ILR",   FillWeight = 60 };
            _colAnterior_704ILR = new DataGridViewTextBoxColumn { Name = "ValorAnterior", DataPropertyName = "ValorAnterior_704ILR", FillWeight = 70 };
            _colNuevo_704ILR    = new DataGridViewTextBoxColumn { Name = "ValorNuevo",    DataPropertyName = "ValorNuevo_704ILR",    FillWeight = 70 };
            _grid_704ILR.Columns.AddRange(_colFecha_704ILR, _colUsuario_704ILR, _colCampo_704ILR, _colAnterior_704ILR, _colNuevo_704ILR);

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

            // El label de estado vacio se agrega despues del grid para quedar al
            // frente cuando se muestra (oculta la grilla sin filas).
            card_704ILR.Controls.Add(_lblVacio_704ILR);
            card_704ILR.Controls.Add(_grid_704ILR);

            pnlContent_704ILR.Controls.Add(card_704ILR);

            Controls.Add(pnlContent_704ILR);
            Controls.Add(pnlTop_704ILR);
        }

        private void CargarHistorial_704ILR()
        {
            try
            {
                List<BE_CambioEntry_704ILR> data_704ILR = RegistradorDeCambios_704ILR.GetHistorial_704ILR("Reserva", _reservaId_704ILR);
                _grid_704ILR.DataSource = data_704ILR;
                ActualizarEstadoVacio_704ILR(data_704ILR == null || data_704ILR.Count == 0);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "HistorialReserva", "Cargar historial de cambios");
                ActualizarEstadoVacio_704ILR(true);
                MessageBox.Show(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, Tr_704ILR.T_704ILR("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Alterna el estado vacio: muestra el mensaje y oculta la grilla cuando no
        // hay registros de cambios para la reserva.
        private void ActualizarEstadoVacio_704ILR(bool vacio_704ILR)
        {
            _lblVacio_704ILR.Visible = vacio_704ILR;
            _grid_704ILR.Visible = !vacio_704ILR;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        // Observador (patron Observer): re-traduce titulo, encabezados y estado vacio.
        public void ActualizarTextos_704ILR()
        {
            if (_lblTitle_704ILR != null) _lblTitle_704ILR.Text = Tr_704ILR.T_704ILR("HIST_TITULO") + " #" + _reservaId_704ILR;
            if (_colFecha_704ILR != null)
            {
                _colFecha_704ILR.HeaderText    = Tr_704ILR.T_704ILR("COL_FECHA");
                _colUsuario_704ILR.HeaderText  = Tr_704ILR.T_704ILR("COL_USUARIO");
                _colCampo_704ILR.HeaderText    = Tr_704ILR.T_704ILR("COL_CAMPO");
                _colAnterior_704ILR.HeaderText = Tr_704ILR.T_704ILR("COL_ANTERIOR");
                _colNuevo_704ILR.HeaderText    = Tr_704ILR.T_704ILR("COL_NUEVO");
            }
            if (_lblVacio_704ILR != null) _lblVacio_704ILR.Text = T_704ILR("HIST_VACIO", "Sin cambios registrados.");
        }
    }
}
