using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // UserControl de gestion de reservas: grilla + panel de alta/edicion.
    //   - Columnas explicitas (AutoGenerateColumns=false), igual que la auditoria.
    //   - El panel derecho carga la fila seleccionada para editar, o queda vacio
    //     para dar de alta una nueva reserva.
    //   - Los errores van a un label, no a MessageBox, para no ocultar la causa.
    public class ucReservas : UserControl
    {
        private DataGridView _grid;
        private Label _lblCount, _lblError, _lblFormTitle;
        private TextBox _txtCliente, _txtMonto;
        private ComboBox _cboSalon, _cboEstado;
        private DateTimePicker _dtFecha;
        private Button _btnNuevo, _btnGuardar;

        private int _editId; // 0 = alta, >0 = edicion

        public ucReservas()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            Load += (s, e) => { CargarSalones(); LimpiarForm(); SafeLoadData(); };
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "Gestion de Reservas",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _btnNuevo = MakeButton("Nueva", 10, 50, Theme.AccentButton);
            _btnNuevo.Click += (s, e) => LimpiarForm();

            _lblCount = new Label
            {
                AutoSize = true,
                Font = new Font("Ebrima", 10F),
                ForeColor = Color.DimGray,
                Location = new Point(95, 58)
            };

            _lblError = new Label
            {
                AutoSize = true,
                Font = new Font("Ebrima", 10F, FontStyle.Bold),
                ForeColor = Color.Firebrick,
                Location = new Point(10, 88),
                Visible = false,
                MaximumSize = new Size(560, 0)
            };

            _grid = new DataGridView
            {
                Location = new Point(10, 110),
                Size = new Size(560, 460),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
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
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Ebrima", 9.5F)
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.BgTitleBar;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Ebrima", 10F, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 32;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id",      DataPropertyName = "Id",            FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cliente", DataPropertyName = "ClienteNombre", FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Salon",   DataPropertyName = "SalonNombre",   FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha",   DataPropertyName = "FechaEvento",   FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado",  DataPropertyName = "Estado",        FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Monto",   DataPropertyName = "Monto",         FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.SelectionChanged += Grid_SelectionChanged;

            Controls.Add(lblTitle);
            Controls.Add(_btnNuevo);
            Controls.Add(_lblCount);
            Controls.Add(_lblError);
            Controls.Add(_grid);

            BuildForm();
        }

        // Panel derecho de alta/edicion.
        private void BuildForm()
        {
            var pnl = new Panel
            {
                Location = new Point(585, 110),
                Size = new Size(330, 460),
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(15)
            };

            _lblFormTitle = new Label
            {
                Text = "Nueva reserva",
                Font = new Font("Ebrima", 13F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(15, 12)
            };

            _txtCliente = MakeInput("Cliente", 55, out var lblCli);
            _cboSalon = new ComboBox
            {
                Location = new Point(15, 128),
                Size = new Size(295, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Ebrima", 11F)
            };
            var lblSal = MakeFieldLabel("Salon", 105);

            var lblFecha = MakeFieldLabel("Fecha del evento", 168);
            _dtFecha = new DateTimePicker
            {
                Location = new Point(15, 191),
                Size = new Size(295, 28),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Ebrima", 11F),
                MinDate = DateTime.Today
            };

            var lblEstado = MakeFieldLabel("Estado", 231);
            _cboEstado = new ComboBox
            {
                Location = new Point(15, 254),
                Size = new Size(295, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Ebrima", 11F)
            };
            _cboEstado.Items.AddRange(new object[] { EstadoReserva.PENDIENTE, EstadoReserva.CONFIRMADA, EstadoReserva.CANCELADA });

            _txtMonto = MakeInput("Monto", 294, out var lblMonto);

            _btnGuardar = MakeButton("Guardar", 15, 370, Theme.AccentButton);
            _btnGuardar.Size = new Size(295, 38);
            _btnGuardar.Click += (s, e) => Guardar();

            pnl.Controls.Add(_lblFormTitle);
            pnl.Controls.Add(lblCli);    pnl.Controls.Add(_txtCliente);
            pnl.Controls.Add(lblSal);    pnl.Controls.Add(_cboSalon);
            pnl.Controls.Add(lblFecha);  pnl.Controls.Add(_dtFecha);
            pnl.Controls.Add(lblEstado); pnl.Controls.Add(_cboEstado);
            pnl.Controls.Add(lblMonto);  pnl.Controls.Add(_txtMonto);
            pnl.Controls.Add(_btnGuardar);

            Controls.Add(pnl);
        }

        private TextBox MakeInput(string label, int y, out Label lbl)
        {
            lbl = MakeFieldLabel(label, y - 23);
            return new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(295, 28),
                Font = new Font("Ebrima", 11F)
            };
        }

        private Label MakeFieldLabel(string text, int y) => new Label
        {
            Text = text,
            Font = new Font("Ebrima", 9.5F),
            ForeColor = Color.DimGray,
            AutoSize = true,
            Location = new Point(15, y)
        };

        private Button MakeButton(string text, int x, int y, Color back)
        {
            var b = new Button
            {
                Text = text,
                Font = Theme.FontButton,
                BackColor = back,
                ForeColor = Theme.TextOnDark,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 32),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void CargarSalones()
        {
            try
            {
                _cboSalon.DataSource = BLL_Salon.GetAll();
                _cboSalon.DisplayMember = "Nombre";
                _cboSalon.ValueMember = "Id";
                _cboSalon.SelectedIndex = _cboSalon.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex) { ShowError("Error cargando salones: " + ex.Message); }
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                List<BE_Reserva> data = BLL_Reserva.GetAll();
                _grid.DataSource = data;
                _lblCount.Text = $"{data.Count} reservas";
            }
            catch (Exception ex)
            {
                ShowError("Error cargando reservas: " + ex.GetType().Name + " - " + ex.Message);
                _lblCount.Text = "";
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is BE_Reserva r) CargarEnForm(r);
        }

        private void CargarEnForm(BE_Reserva r)
        {
            _editId = r.Id;
            _lblFormTitle.Text = "Editar reserva #" + r.Id;
            _txtCliente.Text = r.ClienteNombre;
            _cboSalon.SelectedValue = r.SalonId;
            _dtFecha.Value = r.FechaEvento < _dtFecha.MinDate ? _dtFecha.MinDate : r.FechaEvento;
            _cboEstado.SelectedItem = r.Estado;
            _txtMonto.Text = r.Monto.ToString("0.##");
        }

        private void LimpiarForm()
        {
            _editId = 0;
            _lblFormTitle.Text = "Nueva reserva";
            _txtCliente.Text = "";
            if (_cboSalon.Items.Count > 0) _cboSalon.SelectedIndex = 0;
            _dtFecha.Value = DateTime.Today;
            _cboEstado.SelectedItem = EstadoReserva.PENDIENTE;
            _txtMonto.Text = "0";
            _grid.ClearSelection();
        }

        private void Guardar()
        {
            _lblError.Visible = false;

            if (!decimal.TryParse(_txtMonto.Text, out decimal monto))
            {
                ShowError("El monto no es un numero valido.");
                return;
            }

            var reserva = new BE_Reserva
            {
                Id = _editId,
                ClienteNombre = _txtCliente.Text.Trim(),
                SalonId = _cboSalon.SelectedValue is int sid ? sid : 0,
                FechaEvento = _dtFecha.Value.Date,
                Estado = _cboEstado.SelectedItem is EstadoReserva es ? es : EstadoReserva.PENDIENTE,
                Monto = monto
            };

            ReservaResult result = _editId == 0
                ? BLL_Reserva.Crear(reserva, out _)
                : BLL_Reserva.Actualizar(reserva);

            if (result == ReservaResult.Success)
            {
                LimpiarForm();
                SafeLoadData();
            }
            else
            {
                ShowError(MensajeError(result));
            }
        }

        private static string MensajeError(ReservaResult r)
        {
            switch (r)
            {
                case ReservaResult.InvalidCliente: return "Ingrese el nombre del cliente.";
                case ReservaResult.InvalidSalon:   return "Seleccione un salon valido.";
                case ReservaResult.InvalidFecha:   return "La fecha del evento no puede ser anterior a hoy.";
                case ReservaResult.InvalidMonto:   return "El monto no puede ser negativo.";
                case ReservaResult.NotFound:       return "La reserva ya no existe.";
                default:                           return "No se pudo guardar la reserva.";
            }
        }

        private void ShowError(string msg)
        {
            _lblError.Text = msg;
            _lblError.Visible = true;
        }
    }
}
