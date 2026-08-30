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
    public class ucReservas_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private DataGridView _grid_704ILR;
        private Label _lblCount_704ILR, _lblError_704ILR, _lblFormTitle_704ILR;
        private TextBox _txtMonto_704ILR;   // solo lectura: total = suma de los servicios contratados
        private ComboBox _cboCliente_704ILR, _cboSalon_704ILR, _cboEstado_704ILR;
        private NumericUpDown _numInvitados_704ILR;   // PN1: Capacidad_Requerida (RN-06)
        private DateTimePicker _dtFecha_704ILR;
        private AppButton_704ILR _btnNuevo_704ILR, _btnDisponibilidad_704ILR, _btnGuardar_704ILR, _btnHistorial_704ILR, _btnNuevoCliente_704ILR, _btnServicios_704ILR, _btnPagos_704ILR, _btnComprobante_704ILR, _btnEmail_704ILR, _btnVersiones_704ILR;
        private List<BE_ReservaServicio_704ILR> _serviciosReserva_704ILR = new List<BE_ReservaServicio_704ILR>();

        private int _editId_704ILR; // 0 = alta, >0 = edicion

        public ucReservas_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => { CargarClientes_704ILR(); CargarSalones_704ILR(); LimpiarForm_704ILR(); SafeLoadData_704ILR(); GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this); };
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            // ---------------- Estructura raiz ----------------
            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme_704ILR.BgContent_704ILR
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // barra de titulo
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // cuerpo

            root_704ILR.Controls.Add(BuildHeader_704ILR(), 0, 0);
            root_704ILR.Controls.Add(BuildBody_704ILR(), 0, 1);

            Controls.Add(root_704ILR);
        }

        // Barra superior: titulo de pagina, boton "Nueva", conteo y error.
        private Control BuildHeader_704ILR()
        {
            var header_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 5,
                RowCount = 2,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // titulo
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // boton nueva
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // boton disponibilidad
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // conteo
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // relleno
            header_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Gestion de Reservas");
            lblTitle_704ILR.Tag = "T:RES_TITULO";
            lblTitle_704ILR.Anchor = AnchorStyles.Left;
            lblTitle_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0);

            _btnNuevo_704ILR = Ui_704ILR.Primary_704ILR("Nueva", Theme_704ILR.IcoAdd_704ILR);
            _btnNuevo_704ILR.Tag = "T:BTN_NUEVA";
            _btnNuevo_704ILR.Size = new Size(120, 36);
            _btnNuevo_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR; // vive sobre el area de contenido
            _btnNuevo_704ILR.Anchor = AnchorStyles.Left;
            _btnNuevo_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnNuevo_704ILR.Click += (s_704ILR, e_704ILR) => LimpiarForm_704ILR();

            // Consulta de disponibilidad (Proceso 1, paso 1): se hace antes de
            // armar la reserva, por eso vive en el header y no en la ficha.
            _btnDisponibilidad_704ILR = Ui_704ILR.Secondary_704ILR("Disponibilidad", Theme_704ILR.IcoCalendar_704ILR);
            _btnDisponibilidad_704ILR.Tag = "T:RES_DISPONIBILIDAD_BTN";
            _btnDisponibilidad_704ILR.Size = new Size(160, 36);
            _btnDisponibilidad_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
            _btnDisponibilidad_704ILR.Anchor = AnchorStyles.Left;
            _btnDisponibilidad_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnDisponibilidad_704ILR.Click += (s_704ILR, e_704ILR) => ConsultarDisponibilidad_704ILR();
            _btnDisponibilidad_704ILR.Enabled = Permisos_704ILR.Tiene_704ILR("DISPONIBILIDAD_CONSULTAR");

            _lblCount_704ILR = Ui_704ILR.Body_704ILR();
            _lblCount_704ILR.ForeColor = Theme_704ILR.TextMuted_704ILR;
            _lblCount_704ILR.Anchor = AnchorStyles.Left;
            _lblCount_704ILR.Margin = new Padding(0, 0, 0, 0);

            _lblError_704ILR = Ui_704ILR.Body_704ILR();
            _lblError_704ILR.Font = Theme_704ILR.FontBodyBold_704ILR;
            _lblError_704ILR.ForeColor = Theme_704ILR.Error_704ILR;
            _lblError_704ILR.Visible = false;
            _lblError_704ILR.AutoSize = true;
            _lblError_704ILR.MaximumSize = new Size(900, 0);
            _lblError_704ILR.Anchor = AnchorStyles.Left;
            _lblError_704ILR.Margin = new Padding(0, Theme_704ILR.SpaceXs_704ILR, 0, 0);

            header_704ILR.Controls.Add(lblTitle_704ILR, 0, 0);
            header_704ILR.Controls.Add(_btnNuevo_704ILR, 1, 0);
            header_704ILR.Controls.Add(_btnDisponibilidad_704ILR, 2, 0);
            header_704ILR.Controls.Add(_lblCount_704ILR, 3, 0);
            // El error ocupa toda la fila inferior (debajo del titulo y acciones).
            header_704ILR.Controls.Add(_lblError_704ILR, 0, 1);
            header_704ILR.SetColumnSpan(_lblError_704ILR, 5);

            return header_704ILR;
        }

        // Cuerpo: dos columnas (grilla a la izquierda, ficha a la derecha).
        private Control BuildBody_704ILR()
        {
            var body_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Margin = new Padding(0)
            };
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            body_704ILR.Controls.Add(BuildGridCard_704ILR(), 0, 0);
            body_704ILR.Controls.Add(BuildFormCard_704ILR(), 1, 0);

            return body_704ILR;
        }

        // Tarjeta con la grilla de reservas.
        private Control BuildGridCard_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0),
                Padding = new Padding(Theme_704ILR.SpaceSm_704ILR)
            };

            _grid_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_grid_704ILR);

            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cId",      HeaderText = "Id",      DataPropertyName = "Id_704ILR",            FillWeight = 5 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cCliente", HeaderText = "Cliente", DataPropertyName = "ClienteNombre_704ILR", FillWeight = 18 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSalon",   HeaderText = "Salon",   DataPropertyName = "SalonNombre_704ILR",   FillWeight = 15 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cFecha",   HeaderText = "Fecha",   DataPropertyName = "FechaEvento_704ILR",   FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado",  HeaderText = "Estado",  DataPropertyName = "Estado_704ILR",        FillWeight = 12 });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cInvitados", HeaderText = "Invitados", DataPropertyName = "CantidadInvitados_704ILR", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cMonto",   HeaderText = "Monto",   DataPropertyName = "Monto_704ILR",         FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            // RN-01: vigencia de la cotizacion / reserva pendiente.
            _grid_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cVence",   HeaderText = "Vence",   DataPropertyName = "VenceEl_704ILR",       FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid_704ILR.SelectionChanged += Grid_SelectionChanged_704ILR;
            _grid_704ILR.CellFormatting += Grid_CellFormatting_704ILR;

            card_704ILR.Controls.Add(_grid_704ILR);
            return card_704ILR;
        }

        // Tarjeta con la ficha de alta/edicion.
        private Control BuildFormCard_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(300, 0),
                Margin = new Padding(0),
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };

            // Layout interno de la ficha: titulo, campos (scrollables) y botones.
            var layout_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // titulo ficha
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // campos
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // botones

            _lblFormTitle_704ILR = Ui_704ILR.Title_704ILR("Nueva reserva");
            _lblFormTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            // Pila vertical de campos etiquetados (caption arriba, input abajo).
            // TableLayoutPanel: cada campo Dock=Fill -> ocupa todo el ancho de la
            // ficha y se ajusta solo al redimensionar (sin calculos manuales).
            var fields_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            fields_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i_704ILR = 0; i_704ILR < 7; i_704ILR++) fields_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // empuja los campos hacia arriba

            // Cliente: combo para elegir uno existente + boton de alta rapida.
            _cboCliente_704ILR = Ui_704ILR.Combo_704ILR();
            _cboCliente_704ILR.Dock = DockStyle.Fill;
            _cboCliente_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceXs_704ILR, 0);
            _btnNuevoCliente_704ILR = Ui_704ILR.Secondary_704ILR("", Theme_704ILR.IcoAdd_704ILR);
            _btnNuevoCliente_704ILR.Dock = DockStyle.Fill;
            _btnNuevoCliente_704ILR.Margin = new Padding(0);
            _btnNuevoCliente_704ILR.Click += (s_704ILR, e_704ILR) => NuevoCliente_704ILR();
            _btnNuevoCliente_704ILR.Enabled = Permisos_704ILR.Tiene_704ILR("CLIENTES_GESTION");
            var clientePanel_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            clientePanel_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            clientePanel_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            clientePanel_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            clientePanel_704ILR.Controls.Add(_cboCliente_704ILR, 0, 0);
            clientePanel_704ILR.Controls.Add(_btnNuevoCliente_704ILR, 1, 0);
            var fldCliente_704ILR = Ui_704ILR.Field_704ILR("Cliente", clientePanel_704ILR);
            ((Label)fldCliente_704ILR.GetControlFromPosition(0, 0)).Tag = "T:COL_CLIENTE";

            _cboSalon_704ILR = Ui_704ILR.Combo_704ILR();
            var fldSalon_704ILR = Ui_704ILR.Field_704ILR("Salon", _cboSalon_704ILR);
            ((Label)fldSalon_704ILR.GetControlFromPosition(0, 0)).Tag = "T:COL_SALON";

            _dtFecha_704ILR = Ui_704ILR.DatePicker_704ILR();
            _dtFecha_704ILR.MinDate = DateTime.Today;
            var fldFecha_704ILR = Ui_704ILR.Field_704ILR("Fecha", _dtFecha_704ILR);
            ((Label)fldFecha_704ILR.GetControlFromPosition(0, 0)).Tag = "T:RES_LBL_FECHA";

            // Invitados estimados: el mismo dato con el que se consulta la
            // disponibilidad, ahora persistido en la reserva (PN1 / RN-06).
            _numInvitados_704ILR = new NumericUpDown
            {
                Minimum = 0, Maximum = 100000, Dock = DockStyle.Fill,
                Font = Theme_704ILR.FontInput_704ILR, TextAlign = HorizontalAlignment.Right
            };
            var fldInvitados_704ILR = Ui_704ILR.Field_704ILR("Invitados", _numInvitados_704ILR);
            ((Label)fldInvitados_704ILR.GetControlFromPosition(0, 0)).Tag = "T:RES_LBL_INVITADOS";

            _cboEstado_704ILR = Ui_704ILR.Combo_704ILR();
            _cboEstado_704ILR.Items.AddRange(new object[] { EstadoReserva_704ILR.COTIZACION, EstadoReserva_704ILR.PENDIENTE, EstadoReserva_704ILR.CONFIRMADA, EstadoReserva_704ILR.CANCELADA });
            Ui_704ILR.DibujarEnum_704ILR(_cboEstado_704ILR, o_704ILR => o_704ILR is EstadoReserva_704ILR est_704ILR ? Tr_704ILR.Estado_704ILR(est_704ILR) : o_704ILR?.ToString());
            var fldEstado_704ILR = Ui_704ILR.Field_704ILR("Estado", _cboEstado_704ILR);
            ((Label)fldEstado_704ILR.GetControlFromPosition(0, 0)).Tag = "T:COL_ESTADO";

            // Servicios contratados: boton que abre el dialogo de carga.
            _btnServicios_704ILR = Ui_704ILR.Secondary_704ILR("Servicios", Theme_704ILR.IcoServicio_704ILR);
            _btnServicios_704ILR.Click += (s_704ILR, e_704ILR) => EditarServicios_704ILR();
            var fldServicios_704ILR = Ui_704ILR.Field_704ILR("Servicios", _btnServicios_704ILR);
            ((Label)fldServicios_704ILR.GetControlFromPosition(0, 0)).Tag = "T:MENU_SERVICIOS";

            // Monto = total (suma de servicios), de solo lectura.
            _txtMonto_704ILR = Ui_704ILR.Input_704ILR();
            _txtMonto_704ILR.ReadOnly = true;
            _txtMonto_704ILR.BackColor = Theme_704ILR.SurfaceAlt_704ILR;
            var fldMonto_704ILR = Ui_704ILR.Field_704ILR("Monto", _txtMonto_704ILR);
            ((Label)fldMonto_704ILR.GetControlFromPosition(0, 0)).Tag = "T:COL_MONTO";

            int row_704ILR = 0;
            foreach (var fld_704ILR in new[] { fldCliente_704ILR, fldSalon_704ILR, fldFecha_704ILR, fldInvitados_704ILR, fldEstado_704ILR, fldServicios_704ILR, fldMonto_704ILR })
            {
                fld_704ILR.Dock = DockStyle.Fill;
                fld_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);
                fields_704ILR.Controls.Add(fld_704ILR, 0, row_704ILR++);
            }

            // Botones de accion apilados al pie de la ficha.
            var actions_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0)
            };
            actions_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            actions_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            _btnGuardar_704ILR = Ui_704ILR.Primary_704ILR("Guardar", Theme_704ILR.IcoSave_704ILR);
            _btnGuardar_704ILR.Tag = "T:BTN_GUARDAR";
            _btnGuardar_704ILR.Dock = DockStyle.Fill;
            _btnGuardar_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR);
            _btnGuardar_704ILR.Click += (s_704ILR, e_704ILR) => Guardar_704ILR();

            // Fila inferior: historial + pagos lado a lado.
            var secondary_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            secondary_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secondary_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            secondary_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _btnHistorial_704ILR = Ui_704ILR.Secondary_704ILR("Ver historial de cambios");
            _btnHistorial_704ILR.Tag = "T:RES_HISTORIAL";
            _btnHistorial_704ILR.Dock = DockStyle.Fill;
            _btnHistorial_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceXs_704ILR, 0);
            _btnHistorial_704ILR.Click += (s_704ILR, e_704ILR) => VerHistorial_704ILR();

            _btnPagos_704ILR = Ui_704ILR.Secondary_704ILR("Pagos", Theme_704ILR.IcoPago_704ILR);
            _btnPagos_704ILR.Tag = "T:RES_PAGOS_BTN";
            _btnPagos_704ILR.Dock = DockStyle.Fill;
            _btnPagos_704ILR.Margin = new Padding(Theme_704ILR.SpaceXs_704ILR, 0, 0, 0);
            _btnPagos_704ILR.Click += (s_704ILR, e_704ILR) => EditarPagos_704ILR();

            secondary_704ILR.Controls.Add(_btnHistorial_704ILR, 0, 0);
            secondary_704ILR.Controls.Add(_btnPagos_704ILR, 1, 0);

            // Fila documental: comprobante + email lado a lado.
            var docRow_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0) };
            docRow_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            docRow_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            docRow_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _btnComprobante_704ILR = Ui_704ILR.Secondary_704ILR("Comprobante", Theme_704ILR.IcoDocumento_704ILR);
            _btnComprobante_704ILR.Tag = "T:RES_COMPROBANTE_BTN";
            _btnComprobante_704ILR.Dock = DockStyle.Fill;
            _btnComprobante_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceXs_704ILR, 0);
            _btnComprobante_704ILR.Click += (s_704ILR, e_704ILR) => GenerarComprobante_704ILR();

            _btnEmail_704ILR = Ui_704ILR.Secondary_704ILR("Email", Theme_704ILR.IcoEmail_704ILR);
            _btnEmail_704ILR.Tag = "T:RES_EMAIL_BTN";
            _btnEmail_704ILR.Dock = DockStyle.Fill;
            _btnEmail_704ILR.Margin = new Padding(Theme_704ILR.SpaceXs_704ILR, 0, 0, 0);
            _btnEmail_704ILR.Click += (s_704ILR, e_704ILR) => EnviarEmail_704ILR();

            docRow_704ILR.Controls.Add(_btnComprobante_704ILR, 0, 0);
            docRow_704ILR.Controls.Add(_btnEmail_704ILR, 1, 0);

            // Fila de versiones (patron Memento): abre el dialogo para restaurar
            // la reserva a un estado anterior.
            _btnVersiones_704ILR = Ui_704ILR.Secondary_704ILR("Versiones", Theme_704ILR.IcoDocumento_704ILR);
            _btnVersiones_704ILR.Tag = "T:RES_VERSIONES";
            _btnVersiones_704ILR.Dock = DockStyle.Fill;
            _btnVersiones_704ILR.Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0);
            _btnVersiones_704ILR.Click += (s_704ILR, e_704ILR) => VerVersiones_704ILR();

            actions_704ILR.Controls.Add(_btnGuardar_704ILR, 0, 0);
            actions_704ILR.Controls.Add(secondary_704ILR, 0, 1);
            actions_704ILR.Controls.Add(docRow_704ILR, 0, 2);
            actions_704ILR.Controls.Add(_btnVersiones_704ILR, 0, 3);

            layout_704ILR.Controls.Add(_lblFormTitle_704ILR, 0, 0);
            layout_704ILR.Controls.Add(fields_704ILR, 0, 1);
            layout_704ILR.Controls.Add(actions_704ILR, 0, 2);

            card_704ILR.Controls.Add(layout_704ILR);
            return card_704ILR;
        }

        // Observer: re-traduce textos estaticos, encabezados de grilla y etiquetas dinamicas.
        public void ActualizarTextos_704ILR()
        {
            Tr_704ILR.AplicarTags_704ILR(this);
            if (_grid_704ILR.Columns.Count >= 8)
            {
                _grid_704ILR.Columns["cId"].HeaderText      = Tr_704ILR.T_704ILR("COL_ID");
                _grid_704ILR.Columns["cCliente"].HeaderText = Tr_704ILR.T_704ILR("COL_CLIENTE");
                _grid_704ILR.Columns["cSalon"].HeaderText   = Tr_704ILR.T_704ILR("COL_SALON");
                _grid_704ILR.Columns["cFecha"].HeaderText   = Tr_704ILR.T_704ILR("COL_FECHA");
                _grid_704ILR.Columns["cInvitados"].HeaderText = T_704ILR("COL_INVITADOS", "Invitados");
                _grid_704ILR.Columns["cVence"].HeaderText   = T_704ILR("COL_VENCE", "Vence");
                _grid_704ILR.Columns["cEstado"].HeaderText  = Tr_704ILR.T_704ILR("COL_ESTADO");
                _grid_704ILR.Columns["cMonto"].HeaderText   = Tr_704ILR.T_704ILR("COL_MONTO");
            }
            // Re-traduce los valores de Estado (grilla por celda, combo por display).
            _grid_704ILR.Invalidate();
            _cboEstado_704ILR.Invalidate();
            ActualizarTituloForm_704ILR();
            ActualizarCount_704ILR();
            ActualizarMonto_704ILR();
        }

        private void ActualizarTituloForm_704ILR()
        {
            _lblFormTitle_704ILR.Text = _editId_704ILR == 0
                ? Tr_704ILR.T_704ILR("RES_FORM_NUEVA")
                : Tr_704ILR.T_704ILR("RES_FORM_EDITAR") + " #" + _editId_704ILR;
        }

        private void ActualizarCount_704ILR()
        {
            if (_grid_704ILR.DataSource is List<BE_Reserva_704ILR> data_704ILR)
                _lblCount_704ILR.Text = data_704ILR.Count + " " + Tr_704ILR.T_704ILR("RES_COUNT");
        }

        private void CargarSalones_704ILR()
        {
            try
            {
                _cboSalon_704ILR.DataSource = BLL_Salon_704ILR.GetAll_704ILR();
                _cboSalon_704ILR.DisplayMember = "Nombre_704ILR";
                _cboSalon_704ILR.ValueMember = "Id_704ILR";
                _cboSalon_704ILR.SelectedIndex = _cboSalon_704ILR.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Cargar salones");
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        private void CargarClientes_704ILR()
        {
            try
            {
                _cboCliente_704ILR.DataSource = BLL_Cliente_704ILR.GetAll_704ILR();
                _cboCliente_704ILR.DisplayMember = "NombreCompleto_704ILR";
                _cboCliente_704ILR.ValueMember = "Id_704ILR";
                _cboCliente_704ILR.SelectedIndex = _cboCliente_704ILR.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Cargar clientes");
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        // Alta rapida de cliente desde la ficha (Proceso 1: "si es nuevo, registrarlo").
        // El alta rapida de cliente desde la ficha de reserva persiste igual que la
        // pantalla de Clientes, asi que exige el mismo permiso: si no, seria una
        // via para eludir el gating de CLIENTES_GESTION.
        private void NuevoCliente_704ILR()
        {
            if (!Permisos_704ILR.Exigir_704ILR("CLIENTES_GESTION", FindForm(), "crear un cliente desde la ficha de reserva")) return;
            using (var dlg_704ILR = new frmNuevoCliente_704ILR())
            {
                if (dlg_704ILR.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    CargarClientes_704ILR();
                    // CUN002, paso 5: se informa el alta y el cliente queda
                    // seleccionado para seguir armando la reserva sin buscarlo.
                    _cboCliente_704ILR.SelectedValue = dlg_704ILR.NuevoId_704ILR;
                    MessageBox.Show(T_704ILR("MSG_CLI_CREADO", "Cliente registrado."), "EvenTech",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void SafeLoadData_704ILR()
        {
            try
            {
                _lblError_704ILR.Visible = false;
                List<BE_Reserva_704ILR> data_704ILR = BLL_Reserva_704ILR.GetAll_704ILR();
                _grid_704ILR.DataSource = data_704ILR;
                ActualizarCount_704ILR();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Cargar reservas");
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.GetType().Name + " - " + ex_704ILR.Message);
                _lblCount_704ILR.Text = "";
            }
        }

        private void Grid_SelectionChanged_704ILR(object sender_704ILR, EventArgs e_704ILR)
        {
            if (_grid_704ILR.CurrentRow?.DataBoundItem is BE_Reserva_704ILR r_704ILR) CargarEnForm_704ILR(r_704ILR);
        }

        // Traduce el valor de la columna Estado (el enum se muestra segun el idioma).
        private void Grid_CellFormatting_704ILR(object sender_704ILR, DataGridViewCellFormattingEventArgs e_704ILR)
        {
            if (e_704ILR.RowIndex < 0 || e_704ILR.ColumnIndex < 0 || e_704ILR.ColumnIndex >= _grid_704ILR.Columns.Count) return;
            if (_grid_704ILR.Columns[e_704ILR.ColumnIndex].Name != "cEstado") return;
            if (e_704ILR.Value is EstadoReserva_704ILR est_704ILR) { e_704ILR.Value = Tr_704ILR.Estado_704ILR(est_704ILR); e_704ILR.FormattingApplied = true; }
        }

        private void CargarEnForm_704ILR(BE_Reserva_704ILR r_704ILR)
        {
            _editId_704ILR = r_704ILR.Id_704ILR;
            ActualizarTituloForm_704ILR();
            AjustarEstadosDisponibles_704ILR();
            _cboCliente_704ILR.SelectedValue = r_704ILR.ClienteId_704ILR;
            _cboSalon_704ILR.SelectedValue = r_704ILR.SalonId_704ILR;
            _dtFecha_704ILR.Value = r_704ILR.FechaEvento_704ILR < _dtFecha_704ILR.MinDate ? _dtFecha_704ILR.MinDate : r_704ILR.FechaEvento_704ILR;
            _numInvitados_704ILR.Value = Math.Min(Math.Max(r_704ILR.CantidadInvitados_704ILR, _numInvitados_704ILR.Minimum), _numInvitados_704ILR.Maximum);
            _cboEstado_704ILR.SelectedItem = r_704ILR.Estado_704ILR;
            try { _serviciosReserva_704ILR = BLL_ReservaServicio_704ILR.GetByReserva_704ILR(r_704ILR.Id_704ILR); }
            catch (Exception ex_704ILR) { BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Cargar servicios de reserva"); _serviciosReserva_704ILR = new List<BE_ReservaServicio_704ILR>(); }
            ActualizarMonto_704ILR();
            AplicarModificabilidad_704ILR(r_704ILR);
        }

        // Una reserva cancelada no admite ediciones: se avisa en la ficha y se
        // desactivan Guardar y Pagos. Los pagos importan aparte porque persisten en el
        // acto (no esperan a Guardar), asi que sin bloquearlos se podria seguir moviendo
        // el saldo de una reserva cancelada.
        // Versiones queda HABILITADO: consultar el historial de versiones no es
        // modificar, y la RN-05 prohibe restaurar, no mirar. La restauracion en si la
        // rechaza la BLL (RestaurarVersion_704ILR devuelve NoModificable).
        private void AplicarModificabilidad_704ILR(BE_Reserva_704ILR r_704ILR)
        {
            bool editable_704ILR = BLL_Reserva_704ILR.PuedeModificar_704ILR(r_704ILR);
            _btnGuardar_704ILR.Enabled = editable_704ILR;
            _btnPagos_704ILR.Enabled = editable_704ILR;
            if (!editable_704ILR)
                ShowError_704ILR(T_704ILR("MSG_RES_NO_MODIFICABLE", "La reserva esta cancelada: no admite modificaciones."));
            else
                _lblError_704ILR.Visible = false;
        }

        private void LimpiarForm_704ILR()
        {
            // ClearSelection va PRIMERO: puede disparar Grid_SelectionChanged ->
            // CargarEnForm y repoblar la ficha con la fila que quede seleccionada.
            // Limpiando despues, el estado "nueva reserva" es el que sobrevive.
            _grid_704ILR.ClearSelection();

            _editId_704ILR = 0;
            ActualizarTituloForm_704ILR();
            AjustarEstadosDisponibles_704ILR();
            if (_cboCliente_704ILR.Items.Count > 0) _cboCliente_704ILR.SelectedIndex = 0;
            if (_cboSalon_704ILR.Items.Count > 0) _cboSalon_704ILR.SelectedIndex = 0;
            _dtFecha_704ILR.Value = DateTime.Today;
            _numInvitados_704ILR.Value = 0;
            _cboEstado_704ILR.SelectedItem = EstadoReserva_704ILR.COTIZACION;
            _serviciosReserva_704ILR = new List<BE_ReservaServicio_704ILR>();
            ActualizarMonto_704ILR();
            _btnGuardar_704ILR.Enabled = true;
            _btnPagos_704ILR.Enabled = true;
            _lblError_704ILR.Visible = false;
        }

        // RN-05: dar de baja es una transicion sobre una operacion ya registrada, no un
        // estado inicial. En el alta el combo no ofrece CANCELADA; al editar, si.
        private void AjustarEstadosDisponibles_704ILR()
        {
            bool alta_704ILR = _editId_704ILR == 0;
            bool tieneCancelada_704ILR = _cboEstado_704ILR.Items.Contains(EstadoReserva_704ILR.CANCELADA);
            if (alta_704ILR && tieneCancelada_704ILR)
                _cboEstado_704ILR.Items.Remove(EstadoReserva_704ILR.CANCELADA);
            else if (!alta_704ILR && !tieneCancelada_704ILR)
                _cboEstado_704ILR.Items.Add(EstadoReserva_704ILR.CANCELADA);
        }

        // Refleja el total (suma de servicios) en el campo Monto y el conteo en el boton.
        private void ActualizarMonto_704ILR()
        {
            _txtMonto_704ILR.Text = BLL_ReservaServicio_704ILR.Total_704ILR(_serviciosReserva_704ILR).ToString("0.##");
            if (_btnServicios_704ILR != null)
                _btnServicios_704ILR.Text = Tr_704ILR.T_704ILR("MENU_SERVICIOS") + " (" + _serviciosReserva_704ILR.Count + ")";
        }

        private void EditarServicios_704ILR()
        {
            using (var dlg_704ILR = new frmReservaServicios_704ILR(_serviciosReserva_704ILR, BLL_Servicio_704ILR.GetActivos_704ILR()))
            {
                if (dlg_704ILR.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _serviciosReserva_704ILR = dlg_704ILR.Items_704ILR;
                    ActualizarMonto_704ILR();
                }
            }
        }

        // Consulta de disponibilidad (Proceso 1, paso 1): abre el dialogo y, si
        // el vendedor elige un salon (con la fecha pedida o con la propuesta
        // alternativa), precarga la ficha para continuar la carga de la reserva.
        private void ConsultarDisponibilidad_704ILR()
        {
            if (!Permisos_704ILR.Exigir_704ILR("DISPONIBILIDAD_CONSULTAR", FindForm(), "consultar disponibilidad de salones")) return;
            using (var dlg_704ILR = new frmDisponibilidad_704ILR(_dtFecha_704ILR.Value.Date, (int)_numInvitados_704ILR.Value))
            {
                if (dlg_704ILR.ShowDialog(FindForm()) != DialogResult.OK) return;

                // La consulta arranca una reserva nueva: la ficha se limpia y se
                // precarga con lo elegido (una edicion en curso no se pisa a ciegas).
                if (_editId_704ILR != 0) LimpiarForm_704ILR();
                _cboSalon_704ILR.SelectedValue = dlg_704ILR.SalonSeleccionado_704ILR;
                _dtFecha_704ILR.Value = dlg_704ILR.FechaSeleccionada_704ILR < _dtFecha_704ILR.MinDate ? _dtFecha_704ILR.MinDate : dlg_704ILR.FechaSeleccionada_704ILR;
                // Los invitados con los que se consulto quedan en la ficha: es el
                // dato que despues valida la RN-06 al confirmar.
                _numInvitados_704ILR.Value = Math.Min(dlg_704ILR.InvitadosConsultados_704ILR, _numInvitados_704ILR.Maximum);
            }
        }

        // Abre el dialogo de pagos. Requiere una reserva guardada (los pagos se
        // registran contra su Id y su Monto = total ya persistido).
        private void EditarPagos_704ILR()
        {
            if (_editId_704ILR == 0)
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            using (var dlg_704ILR = new frmReservaPagos_704ILR(_editId_704ILR, BLL_Pago_704ILR.MontoReserva_704ILR(_editId_704ILR)))
                dlg_704ILR.ShowDialog(FindForm());
        }

        // Genera el comprobante/presupuesto HTML de la reserva, lo guarda donde el
        // usuario elija y lo abre en el navegador para imprimir (Proceso 1, paso 6).
        private void GenerarComprobante_704ILR()
        {
            if (_editId_704ILR == 0)
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            try
            {
                string html_704ILR = ComprobanteService_704ILR.GenerarHtml_704ILR(_editId_704ILR);
                if (html_704ILR == null) { ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_RES_NOTFOUND")); return; }

                using (var dlg_704ILR = new SaveFileDialog
                {
                    Title = Tr_704ILR.T_704ILR("RES_COMPROBANTE_BTN"),
                    Filter = Tr_704ILR.T_704ILR("CMP_FILTER"),
                    FileName = Tr_704ILR.T_704ILR("CMP_FILENAME") + _editId_704ILR + ".html",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                })
                {
                    if (dlg_704ILR.ShowDialog(FindForm()) != DialogResult.OK) return;
                    System.IO.File.WriteAllText(dlg_704ILR.FileName, html_704ILR, System.Text.Encoding.UTF8);
                    BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Comprobante generado", CriticidadBitacora_704ILR.Info,
                        "Comprobante de la reserva #" + _editId_704ILR);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg_704ILR.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Generar comprobante");
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_RES_ERROR"));
            }
        }

        // Envia el comprobante por email (paso 7). Sin SMTP: genera y guarda el
        // comprobante, abre el cliente de correo (mailto) con destinatario/asunto/
        // cuerpo prellenados y abre la carpeta del archivo para adjuntarlo.
        private void EnviarEmail_704ILR()
        {
            if (_editId_704ILR == 0)
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_PAGO_GUARDAR_RESERVA"));
                return;
            }
            var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(_editId_704ILR);
            var cliente_704ILR = reserva_704ILR != null && reserva_704ILR.ClienteId_704ILR > 0 ? BLL_Cliente_704ILR.GetById_704ILR(reserva_704ILR.ClienteId_704ILR) : null;
            if (cliente_704ILR == null || string.IsNullOrWhiteSpace(cliente_704ILR.Email_704ILR))
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_EMAIL_SIN_CORREO"));
                return;
            }
            try
            {
                string html_704ILR = ComprobanteService_704ILR.GenerarHtml_704ILR(_editId_704ILR);
                string path_704ILR = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Tr_704ILR.T_704ILR("CMP_FILENAME") + _editId_704ILR + ".html");
                System.IO.File.WriteAllText(path_704ILR, html_704ILR, System.Text.Encoding.UTF8);

                decimal total_704ILR = reserva_704ILR.Monto_704ILR;
                decimal saldo_704ILR = BLL_Pago_704ILR.Saldo_704ILR(_editId_704ILR);

                var cuerpo_704ILR = new System.Text.StringBuilder();
                cuerpo_704ILR.Append(string.Format(Tr_704ILR.T_704ILR("EMAIL_SALUDO"), cliente_704ILR.NombreCompleto_704ILR)).Append("\n\n");
                cuerpo_704ILR.Append(string.Format(Tr_704ILR.T_704ILR("EMAIL_INTRO"), _editId_704ILR)).Append("\n\n");
                cuerpo_704ILR.Append(Tr_704ILR.T_704ILR("COL_SALON")).Append(": ").Append(reserva_704ILR.SalonNombre_704ILR).Append("\n");
                cuerpo_704ILR.Append(Tr_704ILR.T_704ILR("RES_LBL_FECHA")).Append(": ").Append(reserva_704ILR.FechaEvento_704ILR.ToString("yyyy-MM-dd")).Append("\n");
                cuerpo_704ILR.Append(Tr_704ILR.T_704ILR("LBL_TOTAL")).Append(": ").Append(total_704ILR.ToString("N2")).Append("\n");
                cuerpo_704ILR.Append(Tr_704ILR.T_704ILR("LBL_SALDO")).Append(": ").Append(saldo_704ILR.ToString("N2")).Append("\n\n");
                cuerpo_704ILR.Append(Tr_704ILR.T_704ILR("EMAIL_CIERRE"));

                string mailto_704ILR = "mailto:" + Uri.EscapeDataString(cliente_704ILR.Email_704ILR)
                    + "?subject=" + Uri.EscapeDataString(Tr_704ILR.T_704ILR("EMAIL_ASUNTO") + " #" + _editId_704ILR)
                    + "&body=" + Uri.EscapeDataString(cuerpo_704ILR.ToString());
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailto_704ILR) { UseShellExecute = true });
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select,\"" + path_704ILR + "\"") { UseShellExecute = true });

                BLL_Bitacora_704ILR.Registrar_704ILR("Reservas", "Comprobante enviado por email", CriticidadBitacora_704ILR.Info,
                    "Reserva #" + _editId_704ILR + " -> " + cliente_704ILR.Email_704ILR);

                MessageBox.Show(Tr_704ILR.T_704ILR("MSG_EMAIL_ADJUNTAR"), "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Enviar comprobante por email");
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_RES_ERROR"));
            }
        }

        private void Guardar_704ILR()
        {
            // Segunda capa del control de acceso: el alta y la edicion exigen su
            // propio permiso al ejecutarse, no solo al mostrar la seccion.
            string requerido_704ILR = _editId_704ILR == 0 ? "RESERVA_CREAR" : "RESERVA_EDITAR";
            if (!Permisos_704ILR.Exigir_704ILR(requerido_704ILR, FindForm(),
                    _editId_704ILR == 0 ? "crear una reserva" : "editar la reserva #" + _editId_704ILR))
                return;

            _lblError_704ILR.Visible = false;

            // El monto es la suma de los servicios contratados (no se ingresa a mano).
            decimal monto_704ILR = BLL_ReservaServicio_704ILR.Total_704ILR(_serviciosReserva_704ILR);

            var reserva_704ILR = new BE_Reserva_704ILR
            {
                Id_704ILR = _editId_704ILR,
                ClienteId_704ILR = _cboCliente_704ILR.SelectedValue is int cid_704ILR ? cid_704ILR : 0,
                SalonId_704ILR = _cboSalon_704ILR.SelectedValue is int sid_704ILR ? sid_704ILR : 0,
                FechaEvento_704ILR = _dtFecha_704ILR.Value.Date,
                Estado_704ILR = _cboEstado_704ILR.SelectedItem is EstadoReserva_704ILR es_704ILR ? es_704ILR : EstadoReserva_704ILR.COTIZACION,
                CantidadInvitados_704ILR = (int)_numInvitados_704ILR.Value,
                Monto_704ILR = monto_704ILR
            };

            int idReserva_704ILR = _editId_704ILR;

            // RN-02: pasar a CANCELADA no es una edicion mas. Se calcula la politica
            // de cancelacion, se le muestra al vendedor y se confirma antes de aplicarla.
            if (_editId_704ILR != 0 && reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.CANCELADA)
            {
                BE_Reserva_704ILR actual_704ILR = BLL_Reserva_704ILR.GetById_704ILR(_editId_704ILR);
                if (actual_704ILR != null && actual_704ILR.Estado_704ILR != EstadoReserva_704ILR.CANCELADA)
                {
                    BLL_Reserva_704ILR.CalcularCancelacion_704ILR(actual_704ILR,
                        out decimal ret_704ILR, out decimal reem_704ILR);
                    string aviso_704ILR = string.Format(
                        T_704ILR("MSG_RES_CANCELAR", "Cancelar la reserva #{0}?"), _editId_704ILR);
                    if (ret_704ILR > 0 || reem_704ILR > 0)
                        aviso_704ILR += Environment.NewLine + Environment.NewLine + string.Format(
                            T_704ILR("MSG_RES_CANCELADA", "Reserva cancelada. Retenido {0:N2}, reintegro {1:N2}."),
                            ret_704ILR, reem_704ILR);
                    if (MessageBox.Show(aviso_704ILR, "EvenTech",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;

                    var rc_704ILR = BLL_Reserva_704ILR.Cancelar_704ILR(_editId_704ILR,
                        out ret_704ILR, out reem_704ILR);
                    if (rc_704ILR != ReservaResult_704ILR.Success)
                    {
                        ShowError_704ILR(MensajeError_704ILR(rc_704ILR));
                        return;
                    }
                    LimpiarForm_704ILR();
                    SafeLoadData_704ILR();
                    return;
                }
            }

            ReservaResult_704ILR result_704ILR = _editId_704ILR == 0
                ? BLL_Reserva_704ILR.Crear_704ILR(reserva_704ILR, out idReserva_704ILR)
                : BLL_Reserva_704ILR.Actualizar_704ILR(reserva_704ILR);

            // RN-01: si la operacion vencio, se ofrece renovar la vigencia en el acto
            // en vez de dejar al vendedor con una cotizacion trabada.
            if (result_704ILR == ReservaResult_704ILR.Vencida)
            {
                string preg_704ILR = MensajeError_704ILR(result_704ILR) + Environment.NewLine +
                                     Environment.NewLine + T_704ILR("BTN_RENOVAR", "Renovar") + "?";
                if (MessageBox.Show(preg_704ILR, "EvenTech",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes &&
                    BLL_Reserva_704ILR.Renovar_704ILR(_editId_704ILR) == ReservaResult_704ILR.Success)
                {
                    result_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(reserva_704ILR);
                }
            }

            // RN-05: el rechazo se explica nombrando los dos estados involucrados,
            // que es la informacion que el vendedor necesita para corregir.
            if (result_704ILR == ReservaResult_704ILR.TransicionInvalida)
            {
                var persistida_704ILR = BLL_Reserva_704ILR.GetById_704ILR(_editId_704ILR);
                ShowError_704ILR(string.Format(
                    T_704ILR("MSG_RES_TRANSICION", "No se admite pasar de {0} a {1}."),
                    persistida_704ILR != null ? Tr_704ILR.Estado_704ILR(persistida_704ILR.Estado_704ILR) : "-",
                    Tr_704ILR.Estado_704ILR(reserva_704ILR.Estado_704ILR)));
                return;
            }

            if (result_704ILR == ReservaResult_704ILR.Success)
            {
                try { BLL_ReservaServicio_704ILR.Guardar_704ILR(idReserva_704ILR, _serviciosReserva_704ILR); }
                catch (Exception ex_704ILR) { BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Reservas", "Guardar servicios de reserva"); }
                LimpiarForm_704ILR();
                SafeLoadData_704ILR();
            }
            else
            {
                ShowError_704ILR(MensajeError_704ILR(result_704ILR));
            }
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
                case ReservaResult_704ILR.NoModificable:  return T_704ILR("MSG_RES_NO_MODIFICABLE", "La reserva esta cancelada: no admite modificaciones.");
                case ReservaResult_704ILR.Vencida:        return T_704ILR("MSG_RES_VENCIDA", "La operacion vencio: renovala antes de confirmarla.");
                case ReservaResult_704ILR.InvalidInvitados: return T_704ILR("MSG_RES_INVITADOS", "La cantidad de invitados no puede ser negativa.");
                case ReservaResult_704ILR.CapacidadInsuficiente: return T_704ILR("MSG_RES_CAPACIDAD", "El salon no alcanza para la cantidad de invitados indicada.");
                case ReservaResult_704ILR.TransicionInvalida: return T_704ILR("MSG_RES_TRANSICION_GEN", "El cambio de estado solicitado no esta admitido.");
                case ReservaResult_704ILR.NotFound:       return Tr_704ILR.T_704ILR("MSG_RES_NOTFOUND");
                default:                           return Tr_704ILR.T_704ILR("MSG_RES_ERROR");
            }
        }

        private void VerHistorial_704ILR()
        {
            if (_editId_704ILR == 0)
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_RES_SELECCIONE"));
                return;
            }
            if (!Permisos_704ILR.Exigir_704ILR("RESERVA_HISTORIAL", FindForm(), "ver el historial de la reserva #" + _editId_704ILR)) return;
            using (var frm_704ILR = new frmHistorialReserva_704ILR(_editId_704ILR))
            {
                frm_704ILR.ShowDialog(FindForm());
            }
        }

        // Abre las versiones guardadas de la reserva (patron Memento). Si se
        // restauro una, recarga la grilla y reselecciona la reserva para que la
        // ficha muestre los valores repuestos.
        private void VerVersiones_704ILR()
        {
            if (_editId_704ILR == 0)
            {
                ShowError_704ILR(Tr_704ILR.T_704ILR("MSG_RES_SELECCIONE"));
                return;
            }
            // Restaurar repone el estado de negocio: se exige el permiso de edicion.
            if (!Permisos_704ILR.Exigir_704ILR("RESERVA_EDITAR", FindForm(), "restaurar una version de la reserva #" + _editId_704ILR)) return;
            using (var frm_704ILR = new frmVersionesReserva_704ILR(_editId_704ILR))
            {
                if (frm_704ILR.ShowDialog(FindForm()) != DialogResult.OK) return;

                int id_704ILR = _editId_704ILR;
                SafeLoadData_704ILR();
                foreach (DataGridViewRow row_704ILR in _grid_704ILR.Rows)
                {
                    if (row_704ILR.DataBoundItem is BE_Reserva_704ILR r_704ILR && r_704ILR.Id_704ILR == id_704ILR)
                    {
                        _grid_704ILR.CurrentCell = row_704ILR.Cells[0];
                        break;
                    }
                }
            }
        }

        private void ShowError_704ILR(string msg_704ILR)
        {
            _lblError_704ILR.Text = msg_704ILR;
            _lblError_704ILR.Visible = true;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
