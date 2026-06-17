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
    public class frmHistorialReserva : FormBase, IObservadorIdioma
    {
        private readonly int _reservaId;

        private Label _lblTitle;
        private DataGridView _grid;
        private Label _lblVacio;
        private DataGridViewTextBoxColumn _colFecha, _colUsuario, _colCampo, _colAnterior, _colNuevo;

        public frmHistorialReserva(int reservaId)
        {
            _reservaId = reservaId;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => CargarHistorial();
            GestorDeIdioma.GetInstance.Suscribir(this);
            FormClosed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            Text = "EvenTech";
            ClientSize = new Size(680, 460);
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

            // ---------------- Contenido (tarjeta con grilla) ----------------
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceLg)
            };

            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                BehindColor = Theme.BgContent,
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
                FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            };
            _colUsuario  = new DataGridViewTextBoxColumn { Name = "Usuario",       DataPropertyName = "Usuario",       FillWeight = 55 };
            _colCampo    = new DataGridViewTextBoxColumn { Name = "NombreCampo",   DataPropertyName = "NombreCampo",   FillWeight = 60 };
            _colAnterior = new DataGridViewTextBoxColumn { Name = "ValorAnterior", DataPropertyName = "ValorAnterior", FillWeight = 70 };
            _colNuevo    = new DataGridViewTextBoxColumn { Name = "ValorNuevo",    DataPropertyName = "ValorNuevo",    FillWeight = 70 };
            _grid.Columns.AddRange(_colFecha, _colUsuario, _colCampo, _colAnterior, _colNuevo);

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

            // El label de estado vacio se agrega despues del grid para quedar al
            // frente cuando se muestra (oculta la grilla sin filas).
            card.Controls.Add(_lblVacio);
            card.Controls.Add(_grid);

            pnlContent.Controls.Add(card);

            Controls.Add(pnlContent);
            Controls.Add(pnlTop);
        }

        private void CargarHistorial()
        {
            try
            {
                List<BE_CambioEntry> data = RegistradorDeCambios.GetHistorial("Reserva", _reservaId);
                _grid.DataSource = data;
                ActualizarEstadoVacio(data == null || data.Count == 0);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "HistorialReserva", "Cargar historial de cambios");
                ActualizarEstadoVacio(true);
                MessageBox.Show("No se pudo cargar el historial: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Alterna el estado vacio: muestra el mensaje y oculta la grilla cuando no
        // hay registros de cambios para la reserva.
        private void ActualizarEstadoVacio(bool vacio)
        {
            _lblVacio.Visible = vacio;
            _grid.Visible = !vacio;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // Observador (patron Observer): re-traduce titulo, encabezados y estado vacio.
        public void ActualizarTextos()
        {
            if (_lblTitle != null) _lblTitle.Text = Tr.T("HIST_TITULO") + " #" + _reservaId;
            if (_colFecha != null)
            {
                _colFecha.HeaderText    = Tr.T("COL_FECHA");
                _colUsuario.HeaderText  = Tr.T("COL_USUARIO");
                _colCampo.HeaderText    = Tr.T("COL_CAMPO");
                _colAnterior.HeaderText = Tr.T("COL_ANTERIOR");
                _colNuevo.HeaderText    = Tr.T("COL_NUEVO");
            }
            if (_lblVacio != null) _lblVacio.Text = T("HIST_VACIO", "Sin cambios registrados.");
        }
    }
}
