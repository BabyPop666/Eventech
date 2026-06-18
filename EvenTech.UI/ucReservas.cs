using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // UserControl de gestion de reservas: grilla + ficha de alta/edicion.
    // Layout por TableLayoutPanel/Dock (DPI-aware, sin coordenadas magicas):
    // fila 0 = barra de titulo, fila 1 = cuerpo en dos columnas (grilla / ficha).
    // Observa el cambio de idioma (patron Observer) para traducir sus textos.
    public class ucReservas : UserControl, IObservadorIdioma
    {
        private DataGridView _grid;
        private Label _lblCount, _lblError, _lblFormTitle;
        private TextBox _txtMonto;   // solo lectura: total = suma de los servicios contratados
        private ComboBox _cboCliente, _cboSalon, _cboEstado;
        private DateTimePicker _dtFecha;
        private AppButton _btnNuevo, _btnGuardar, _btnHistorial, _btnNuevoCliente, _btnServicios, _btnPagos, _btnComprobante, _btnEmail;
        private List<BE_ReservaServicio> _serviciosReserva = new List<BE_ReservaServicio>();

        private int _editId; // 0 = alta, >0 = edicion

        public ucReservas()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { CargarClientes(); CargarSalones(); LimpiarForm(); SafeLoadData(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            // ---------------- Estructura raiz ----------------
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.BgContent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // barra de titulo
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // cuerpo

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);

            Controls.Add(root);
        }

        // Barra superior: titulo de pagina, boton "Nueva", conteo y error.
        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Theme.BgContent,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, Theme.SpaceMd)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // titulo
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // boton nueva
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // conteo
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // relleno
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle = Ui.H1("Gestion de Reservas");
            lblTitle.Tag = "T:RES_TITULO";
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.Margin = new Padding(0, 0, Theme.SpaceLg, 0);

            _btnNuevo = Ui.Primary("Nueva", Theme.IcoAdd);
            _btnNuevo.Tag = "T:BTN_NUEVA";
            _btnNuevo.Size = new Size(120, 36);
            _btnNuevo.BehindColor = Theme.BgContent; // vive sobre el area de contenido
            _btnNuevo.Anchor = AnchorStyles.Left;
            _btnNuevo.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnNuevo.Click += (s, e) => LimpiarForm();

            _lblCount = Ui.Body();
            _lblCount.ForeColor = Theme.TextMuted;
            _lblCount.Anchor = AnchorStyles.Left;
            _lblCount.Margin = new Padding(0, 0, 0, 0);

            _lblError = Ui.Body();
            _lblError.Font = Theme.FontBodyBold;
            _lblError.ForeColor = Theme.Error;
            _lblError.Visible = false;
            _lblError.AutoSize = true;
            _lblError.MaximumSize = new Size(900, 0);
            _lblError.Anchor = AnchorStyles.Left;
            _lblError.Margin = new Padding(0, Theme.SpaceXs, 0, 0);

            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(_btnNuevo, 1, 0);
            header.Controls.Add(_lblCount, 2, 0);
            // El error ocupa toda la fila inferior (debajo del titulo y acciones).
            header.Controls.Add(_lblError, 0, 1);
            header.SetColumnSpan(_lblError, 4);

            return header;
        }

        // Cuerpo: dos columnas (grilla a la izquierda, ficha a la derecha).
        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.BgContent,
                Margin = new Padding(0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            body.Controls.Add(BuildGridCard(), 0, 0);
            body.Controls.Add(BuildFormCard(), 1, 0);

            return body;
        }

        // Tarjeta con la grilla de reservas.
        private Control BuildGridCard()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme.SpaceLg, 0),
                Padding = new Padding(Theme.SpaceSm)
            };

            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",      HeaderText = "Id",      DataPropertyName = "Id",            FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCliente", HeaderText = "Cliente", DataPropertyName = "ClienteNombre", FillWeight = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSalon",   HeaderText = "Salon",   DataPropertyName = "SalonNombre",   FillWeight = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",   HeaderText = "Fecha",   DataPropertyName = "FechaEvento",   FillWeight = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado",  HeaderText = "Estado",  DataPropertyName = "Estado",        FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMonto",   HeaderText = "Monto",   DataPropertyName = "Monto",         FillWeight = 55, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid.SelectionChanged += Grid_SelectionChanged;

            card.Controls.Add(_grid);
            return card;
        }

        // Tarjeta con la ficha de alta/edicion.
        private Control BuildFormCard()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(300, 0),
                Margin = new Padding(0),
                Padding = new Padding(Theme.SpaceLg)
            };

            // Layout interno de la ficha: titulo, campos (scrollables) y botones.
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // titulo ficha
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // campos
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // botones

            _lblFormTitle = Ui.Title("Nueva reserva");
            _lblFormTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            // Pila vertical de campos etiquetados (caption arriba, input abajo).
            // TableLayoutPanel: cada campo Dock=Fill -> ocupa todo el ancho de la
            // ficha y se ajusta solo al redimensionar (sin calculos manuales).
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // empuja los campos hacia arriba

            // Cliente: combo para elegir uno existente + boton de alta rapida.
            _cboCliente = Ui.Combo();
            _cboCliente.Dock = DockStyle.Fill;
            _cboCliente.Margin = new Padding(0, 0, Theme.SpaceXs, 0);
            _btnNuevoCliente = Ui.Secondary("", Theme.IcoAdd);
            _btnNuevoCliente.Dock = DockStyle.Fill;
            _btnNuevoCliente.Margin = new Padding(0);
            _btnNuevoCliente.Click += (s, e) => NuevoCliente();
            var clientePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            clientePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            clientePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            clientePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            clientePanel.Controls.Add(_cboCliente, 0, 0);
            clientePanel.Controls.Add(_btnNuevoCliente, 1, 0);
            var fldCliente = Ui.Field("Cliente", clientePanel);
            ((Label)fldCliente.GetControlFromPosition(0, 0)).Tag = "T:COL_CLIENTE";

            _cboSalon = Ui.Combo();
            var fldSalon = Ui.Field("Salon", _cboSalon);
            ((Label)fldSalon.GetControlFromPosition(0, 0)).Tag = "T:COL_SALON";

            _dtFecha = Ui.DatePicker();
            _dtFecha.MinDate = DateTime.Today;
            var fldFecha = Ui.Field("Fecha", _dtFecha);
            ((Label)fldFecha.GetControlFromPosition(0, 0)).Tag = "T:RES_LBL_FECHA";

            _cboEstado = Ui.Combo();
            _cboEstado.Items.AddRange(new object[] { EstadoReserva.COTIZACION, EstadoReserva.PENDIENTE, EstadoReserva.CONFIRMADA, EstadoReserva.CANCELADA });
            var fldEstado = Ui.Field("Estado", _cboEstado);
            ((Label)fldEstado.GetControlFromPosition(0, 0)).Tag = "T:COL_ESTADO";

            // Servicios contratados: boton que abre el dialogo de carga.
            _btnServicios = Ui.Secondary("Servicios", Theme.IcoServicio);
            _btnServicios.Click += (s, e) => EditarServicios();
            var fldServicios = Ui.Field("Servicios", _btnServicios);
            ((Label)fldServicios.GetControlFromPosition(0, 0)).Tag = "T:MENU_SERVICIOS";

            // Monto = total (suma de servicios), de solo lectura.
            _txtMonto = Ui.Input();
            _txtMonto.ReadOnly = true;
            _txtMonto.BackColor = Theme.SurfaceAlt;
            var fldMonto = Ui.Field("Monto", _txtMonto);
            ((Label)fldMonto.GetControlFromPosition(0, 0)).Tag = "T:COL_MONTO";

            int row = 0;
            foreach (var fld in new[] { fldCliente, fldSalon, fldFecha, fldEstado, fldServicios, fldMonto })
            {
                fld.Dock = DockStyle.Fill;
                fld.Margin = new Padding(0, 0, 0, Theme.SpaceMd);
                fields.Controls.Add(fld, 0, row++);
            }

            // Botones de accion apilados al pie de la ficha.
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0, Theme.SpaceSm, 0, 0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            _btnGuardar = Ui.Primary("Guardar", Theme.IcoSave);
            _btnGuardar.Tag = "T:BTN_GUARDAR";
            _btnGuardar.Dock = DockStyle.Fill;
            _btnGuardar.Margin = new Padding(0, 0, 0, Theme.SpaceSm);
            _btnGuardar.Click += (s, e) => Guardar();

            // Fila inferior: historial + pagos lado a lado.
            var secondary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            secondary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secondary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secondary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _btnHistorial = Ui.Secondary("Ver historial de cambios");
            _btnHistorial.Tag = "T:RES_HISTORIAL";
            _btnHistorial.Dock = DockStyle.Fill;
            _btnHistorial.Margin = new Padding(0, 0, Theme.SpaceXs, 0);
            _btnHistorial.Click += (s, e) => VerHistorial();

            _btnPagos = Ui.Secondary("Pagos", Theme.IcoPago);
            _btnPagos.Tag = "T:RES_PAGOS_BTN";
            _btnPagos.Dock = DockStyle.Fill;
            _btnPagos.Margin = new Padding(Theme.SpaceXs, 0, 0, 0);
            _btnPagos.Click += (s, e) => EditarPagos();

            secondary.Controls.Add(_btnHistorial, 0, 0);
            secondary.Controls.Add(_btnPagos, 1, 0);

            // Fila documental: comprobante + email lado a lado.
            var docRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0, Theme.SpaceSm, 0, 0) };
            docRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            docRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            docRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _btnComprobante = Ui.Secondary("Comprobante", Theme.IcoDocumento);
            _btnComprobante.Tag = "T:RES_COMPROBANTE_BTN";
            _btnComprobante.Dock = DockStyle.Fill;
            _btnComprobante.Margin = new Padding(0, 0, Theme.SpaceXs, 0);
            _btnComprobante.Click += (s, e) => GenerarComprobante();

            _btnEmail = Ui.Secondary("Email", Theme.IcoEmail);
            _btnEmail.Tag = "T:RES_EMAIL_BTN";
            _btnEmail.Dock = DockStyle.Fill;
            _btnEmail.Margin = new Padding(Theme.SpaceXs, 0, 0, 0);
            _btnEmail.Click += (s, e) => EnviarEmail();

            docRow.Controls.Add(_btnComprobante, 0, 0);
            docRow.Controls.Add(_btnEmail, 1, 0);

            actions.Controls.Add(_btnGuardar, 0, 0);
            actions.Controls.Add(secondary, 0, 1);
            actions.Controls.Add(docRow, 0, 2);

            layout.Controls.Add(_lblFormTitle, 0, 0);
            layout.Controls.Add(fields, 0, 1);
            layout.Controls.Add(actions, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        // Observer: re-traduce textos estaticos, encabezados de grilla y etiquetas dinamicas.
        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_grid.Columns.Count >= 6)
            {
                _grid.Columns["cId"].HeaderText      = Tr.T("COL_ID");
                _grid.Columns["cCliente"].HeaderText = Tr.T("COL_CLIENTE");
                _grid.Columns["cSalon"].HeaderText   = Tr.T("COL_SALON");
                _grid.Columns["cFecha"].HeaderText   = Tr.T("COL_FECHA");
                _grid.Columns["cEstado"].HeaderText  = Tr.T("COL_ESTADO");
                _grid.Columns["cMonto"].HeaderText   = Tr.T("COL_MONTO");
            }
            ActualizarTituloForm();
            ActualizarCount();
            ActualizarMonto();
        }

        private void ActualizarTituloForm()
        {
            _lblFormTitle.Text = _editId == 0
                ? Tr.T("RES_FORM_NUEVA")
                : Tr.T("RES_FORM_EDITAR") + " #" + _editId;
        }

        private void ActualizarCount()
        {
            if (_grid.DataSource is List<BE_Reserva> data)
                _lblCount.Text = data.Count + " " + Tr.T("RES_COUNT");
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
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar salones");
                ShowError("Error cargando salones: " + ex.Message);
            }
        }

        private void CargarClientes()
        {
            try
            {
                _cboCliente.DataSource = BLL_Cliente.GetAll();
                _cboCliente.DisplayMember = "NombreCompleto";
                _cboCliente.ValueMember = "Id";
                _cboCliente.SelectedIndex = _cboCliente.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar clientes");
                ShowError("Error cargando clientes: " + ex.Message);
            }
        }

        // Alta rapida de cliente desde la ficha (Proceso 1: "si es nuevo, registrarlo").
        private void NuevoCliente()
        {
            using (var dlg = new frmNuevoCliente())
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    CargarClientes();
                    _cboCliente.SelectedValue = dlg.NuevoId;
                }
            }
        }

        private void SafeLoadData()
        {
            try
            {
                _lblError.Visible = false;
                List<BE_Reserva> data = BLL_Reserva.GetAll();
                _grid.DataSource = data;
                ActualizarCount();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar reservas");
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
            ActualizarTituloForm();
            _cboCliente.SelectedValue = r.ClienteId;
            _cboSalon.SelectedValue = r.SalonId;
            _dtFecha.Value = r.FechaEvento < _dtFecha.MinDate ? _dtFecha.MinDate : r.FechaEvento;
            _cboEstado.SelectedItem = r.Estado;
            try { _serviciosReserva = BLL_ReservaServicio.GetByReserva(r.Id); }
            catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Cargar servicios de reserva"); _serviciosReserva = new List<BE_ReservaServicio>(); }
            ActualizarMonto();
        }

        private void LimpiarForm()
        {
            _editId = 0;
            ActualizarTituloForm();
            if (_cboCliente.Items.Count > 0) _cboCliente.SelectedIndex = 0;
            if (_cboSalon.Items.Count > 0) _cboSalon.SelectedIndex = 0;
            _dtFecha.Value = DateTime.Today;
            _cboEstado.SelectedItem = EstadoReserva.COTIZACION;
            _serviciosReserva = new List<BE_ReservaServicio>();
            ActualizarMonto();
            _grid.ClearSelection();
        }

        // Refleja el total (suma de servicios) en el campo Monto y el conteo en el boton.
        private void ActualizarMonto()
        {
            _txtMonto.Text = BLL_ReservaServicio.Total(_serviciosReserva).ToString("0.##");
            if (_btnServicios != null)
                _btnServicios.Text = Tr.T("MENU_SERVICIOS") + " (" + _serviciosReserva.Count + ")";
        }

        private void EditarServicios()
        {
            using (var dlg = new frmReservaServicios(_serviciosReserva, BLL_Servicio.GetActivos()))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _serviciosReserva = dlg.Items;
                    ActualizarMonto();
                }
            }
        }

        // Abre el dialogo de pagos. Requiere una reserva guardada (los pagos se
        // registran contra su Id y su Monto = total ya persistido).
        private void EditarPagos()
        {
            if (_editId == 0)
            {
                ShowError(Tr.T("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            using (var dlg = new frmReservaPagos(_editId, BLL_Pago.MontoReserva(_editId)))
                dlg.ShowDialog(FindForm());
        }

        // Genera el comprobante/presupuesto HTML de la reserva, lo guarda donde el
        // usuario elija y lo abre en el navegador para imprimir (Proceso 1, paso 6).
        private void GenerarComprobante()
        {
            if (_editId == 0)
            {
                ShowError(Tr.T("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            try
            {
                string html = ComprobanteService.GenerarHtml(_editId);
                if (html == null) { ShowError(Tr.T("MSG_RES_NOTFOUND")); return; }

                using (var dlg = new SaveFileDialog
                {
                    Title = Tr.T("RES_COMPROBANTE_BTN"),
                    Filter = "HTML (*.html)|*.html",
                    FileName = "Comprobante_Reserva_" + _editId + ".html",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                })
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                    System.IO.File.WriteAllText(dlg.FileName, html, System.Text.Encoding.UTF8);
                    BLL_Bitacora.Registrar("Reservas", "Comprobante generado", CriticidadBitacora.Info,
                        "Comprobante de la reserva #" + _editId);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Generar comprobante");
                ShowError(Tr.T("MSG_RES_ERROR"));
            }
        }

        // Envia el comprobante por email (paso 7). Sin SMTP: genera y guarda el
        // comprobante, abre el cliente de correo (mailto) con destinatario/asunto/
        // cuerpo prellenados y abre la carpeta del archivo para adjuntarlo.
        private void EnviarEmail()
        {
            if (_editId == 0)
            {
                ShowError(Tr.T("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            var reserva = BLL_Reserva.GetById(_editId);
            var cliente = reserva != null && reserva.ClienteId > 0 ? BLL_Cliente.GetById(reserva.ClienteId) : null;
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Email))
            {
                ShowError(Tr.T("MSG_EMAIL_SIN_CORREO"));
                return;
            }
            try
            {
                string html = ComprobanteService.GenerarHtml(_editId);
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Comprobante_Reserva_" + _editId + ".html");
                System.IO.File.WriteAllText(path, html, System.Text.Encoding.UTF8);

                decimal total = reserva.Monto;
                decimal saldo = BLL_Pago.Saldo(_editId);

                var cuerpo = new System.Text.StringBuilder();
                cuerpo.Append(string.Format(Tr.T("EMAIL_SALUDO"), cliente.NombreCompleto)).Append("\n\n");
                cuerpo.Append(string.Format(Tr.T("EMAIL_INTRO"), _editId)).Append("\n\n");
                cuerpo.Append(Tr.T("COL_SALON")).Append(": ").Append(reserva.SalonNombre).Append("\n");
                cuerpo.Append(Tr.T("RES_LBL_FECHA")).Append(": ").Append(reserva.FechaEvento.ToString("yyyy-MM-dd")).Append("\n");
                cuerpo.Append(Tr.T("LBL_TOTAL")).Append(": ").Append(total.ToString("N2")).Append("\n");
                cuerpo.Append(Tr.T("LBL_SALDO")).Append(": ").Append(saldo.ToString("N2")).Append("\n\n");
                cuerpo.Append(Tr.T("EMAIL_CIERRE"));

                string mailto = "mailto:" + Uri.EscapeDataString(cliente.Email)
                    + "?subject=" + Uri.EscapeDataString(Tr.T("EMAIL_ASUNTO") + " #" + _editId)
                    + "&body=" + Uri.EscapeDataString(cuerpo.ToString());
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailto) { UseShellExecute = true });
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });

                BLL_Bitacora.Registrar("Reservas", "Comprobante enviado por email", CriticidadBitacora.Info,
                    "Reserva #" + _editId + " -> " + cliente.Email);

                MessageBox.Show(Tr.T("MSG_EMAIL_ADJUNTAR"), "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Enviar comprobante por email");
                ShowError(Tr.T("MSG_RES_ERROR"));
            }
        }

        private void Guardar()
        {
            _lblError.Visible = false;

            // El monto es la suma de los servicios contratados (no se ingresa a mano).
            decimal monto = BLL_ReservaServicio.Total(_serviciosReserva);

            var reserva = new BE_Reserva
            {
                Id = _editId,
                ClienteId = _cboCliente.SelectedValue is int cid ? cid : 0,
                SalonId = _cboSalon.SelectedValue is int sid ? sid : 0,
                FechaEvento = _dtFecha.Value.Date,
                Estado = _cboEstado.SelectedItem is EstadoReserva es ? es : EstadoReserva.COTIZACION,
                Monto = monto
            };

            int idReserva = _editId;
            ReservaResult result = _editId == 0
                ? BLL_Reserva.Crear(reserva, out idReserva)
                : BLL_Reserva.Actualizar(reserva);

            if (result == ReservaResult.Success)
            {
                try { BLL_ReservaServicio.Guardar(idReserva, _serviciosReserva); }
                catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Reservas", "Guardar servicios de reserva"); }
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
                case ReservaResult.InvalidCliente: return Tr.T("MSG_RES_CLIENTE");
                case ReservaResult.InvalidSalon:   return Tr.T("MSG_RES_SALON");
                case ReservaResult.InvalidFecha:   return Tr.T("MSG_RES_FECHA");
                case ReservaResult.InvalidMonto:   return Tr.T("MSG_RES_MONTO");
                case ReservaResult.SalonOcupado:   return Tr.T("MSG_RES_SALON_OCUPADO");
                case ReservaResult.NotFound:       return Tr.T("MSG_RES_NOTFOUND");
                default:                           return Tr.T("MSG_RES_ERROR");
            }
        }

        private void VerHistorial()
        {
            if (_editId == 0)
            {
                ShowError(Tr.T("MSG_RES_SELECCIONE"));
                return;
            }
            using (var frm = new frmHistorialReserva(_editId))
            {
                frm.ShowDialog(FindForm());
            }
        }

        private void ShowError(string msg)
        {
            _lblError.Text = msg;
            _lblError.Visible = true;
        }
    }
}
