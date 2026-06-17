using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Gestion de idiomas: alta de idioma y edicion de traducciones en grilla editable.
    // Look de panel de administracion (titulo + tarjetas), layout 100% por
    // TableLayoutPanel/FlowLayoutPanel + Dock (sin coordenadas magicas).
    // Observa el cambio de idioma (patron Observer) para re-traducir sus textos.
    public class ucIdiomas : UserControl, IObservadorIdioma
    {
        private ComboBox _cboIdioma;
        private TextBox _txtCodigo, _txtNombre;
        private DataGridView _grid;
        private Label _lblMsg;

        public ucIdiomas()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) => { CargarIdiomas(); GestorDeIdioma.GetInstance.Suscribir(this); };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            // ---------------- Raiz: titulo / alta / selector / grilla / acciones ----------------
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceLg)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // titulo
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116)); // tarjeta nuevo idioma
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // selector de idioma
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // tarjeta con grilla
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // acciones

            // ---------------- Titulo de pagina ----------------
            var lblTitle = Ui.H1("Gestion de Idiomas");
            lblTitle.Tag = "T:IDI_TITULO";
            lblTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            // ---------------- Tarjeta: nuevo idioma ----------------
            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(BuildTarjetaNuevo(), 0, 1);
            root.Controls.Add(BuildSelector(), 0, 2);
            root.Controls.Add(BuildTarjetaGrilla(), 0, 3);
            root.Controls.Add(BuildAcciones(), 0, 4);

            Controls.Add(root);
        }

        // Tarjeta de alta: codigo + nombre + boton crear (en fila, alineados al fondo).
        private CardPanel BuildTarjetaNuevo()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd),
                Padding = new Padding(Theme.SpaceLg)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblNuevo = Ui.H2("Nuevo idioma");
            lblNuevo.Tag = "T:IDI_NUEVO";
            lblNuevo.Margin = new Padding(0, 0, 0, Theme.SpaceSm);

            // Fila horizontal: campo codigo + campo nombre + boton crear.
            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _txtCodigo = Ui.Input();
            _txtCodigo.MaxLength = 5;
            _txtCodigo.Width = 110;
            var fldCodigo = Ui.Field("Codigo (ej. PT)", _txtCodigo);
            fldCodigo.Width = 130;
            fldCodigo.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            ((Label)fldCodigo.GetControlFromPosition(0, 0)).Tag = "T:IDI_CODIGO";

            _txtNombre = Ui.Input();
            _txtNombre.Width = 240;
            var fldNombre = Ui.Field("Nombre", _txtNombre);
            fldNombre.Width = 260;
            fldNombre.Margin = new Padding(0, 0, Theme.SpaceLg, 0);
            ((Label)fldNombre.GetControlFromPosition(0, 0)).Tag = "T:IDI_NOMBRE";

            var btnCrear = Ui.Primary("Crear idioma", Theme.IcoAdd);
            btnCrear.Tag = "T:IDI_CREAR";
            btnCrear.Size = new Size(170, 32);
            // El boton vive sobre la tarjeta blanca -> BehindColor por defecto (Surface) es correcto.
            btnCrear.Margin = new Padding(0, 18, 0, 0); // alinea con el input (debajo del caption)
            btnCrear.Click += (s, e) => CrearIdioma();

            fila.Controls.Add(fldCodigo);
            fila.Controls.Add(fldNombre);
            fila.Controls.Add(btnCrear);

            inner.Controls.Add(lblNuevo, 0, 0);
            inner.Controls.Add(fila, 0, 1);
            card.Controls.Add(inner);
            return card;
        }

        // Fila: label "Idioma:" + combo de seleccion.
        private FlowLayoutPanel BuildSelector()
        {
            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd)
            };

            var lblEditar = Ui.BodyBold("Idioma:");
            lblEditar.Tag = "T:IDI_IDIOMA";
            lblEditar.Margin = new Padding(0, 6, Theme.SpaceMd, 0);
            lblEditar.AutoSize = true;

            _cboIdioma = Ui.Combo();
            _cboIdioma.Width = 240;
            _cboIdioma.Margin = new Padding(0, 2, 0, 0);
            _cboIdioma.SelectedIndexChanged += (s, e) => CargarTraducciones();

            fila.Controls.Add(lblEditar);
            fila.Controls.Add(_cboIdioma);
            return fila;
        }

        // Tarjeta que contiene la grilla editable (Dock=Fill).
        private CardPanel BuildTarjetaGrilla()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd),
                Padding = new Padding(Theme.SpaceSm)
            };

            _grid = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_grid, editable: true);
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;

            var colClave = new DataGridViewTextBoxColumn
            {
                HeaderText = "Clave",
                Name = "colClave",
                FillWeight = 40,
                ReadOnly = true
            };
            colClave.DefaultCellStyle.BackColor = Theme.SurfaceAlt;
            colClave.DefaultCellStyle.ForeColor = Theme.TextMuted;

            var colTexto = new DataGridViewTextBoxColumn
            {
                HeaderText = "Texto",
                Name = "colTexto",
                FillWeight = 60
            };

            _grid.Columns.Add(colClave);
            _grid.Columns.Add(colTexto);

            card.Controls.Add(_grid);
            return card;
        }

        // Fila inferior: boton guardar + mensaje de estado.
        private FlowLayoutPanel BuildAcciones()
        {
            var fila = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var btnGuardar = Ui.Primary("Guardar traducciones", Theme.IcoSave);
            btnGuardar.Tag = "T:IDI_GUARDAR";
            btnGuardar.Size = new Size(220, 38);
            btnGuardar.BehindColor = Theme.BgContent; // vive sobre el area de contenido, no sobre tarjeta
            btnGuardar.Margin = new Padding(0, 0, Theme.SpaceLg, 0);
            btnGuardar.Click += (s, e) => GuardarTraducciones();

            _lblMsg = new Label
            {
                AutoSize = true,
                Font = Theme.FontBodyBold,
                BackColor = Color.Transparent,
                MaximumSize = new Size(560, 0),
                Margin = new Padding(0, 9, 0, 0)
            };

            fila.Controls.Add(btnGuardar);
            fila.Controls.Add(_lblMsg);
            return fila;
        }

        // Observer: re-traduce textos por Tag + encabezados de columnas.
        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_grid.Columns.Count >= 2)
            {
                _grid.Columns["colClave"].HeaderText = Tr.T("COL_CLAVE");
                _grid.Columns["colTexto"].HeaderText = Tr.T("COL_TEXTO");
            }
        }

        private void CargarIdiomas()
        {
            try
            {
                _cboIdioma.DataSource = BLL_Idioma.GetIdiomas();
                _cboIdioma.DisplayMember = "Nombre";
                _cboIdioma.ValueMember = "Id";
                if (_cboIdioma.Items.Count > 0) _cboIdioma.SelectedIndex = 0;
            }
            catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Idiomas", "Cargar idiomas"); Mensaje("Error: " + ex.Message, true); }
        }

        private void CargarTraducciones()
        {
            _grid.Rows.Clear();
            if (!(_cboIdioma.SelectedValue is int idiomaId)) return;
            try
            {
                Dictionary<string, string> trads = BLL_Idioma.GetTraducciones(idiomaId);
                foreach (var kv in trads)
                    _grid.Rows.Add(kv.Key, kv.Value);
            }
            catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Idiomas", "Cargar traducciones"); Mensaje("Error: " + ex.Message, true); }
        }

        private void CrearIdioma()
        {
            try
            {
                IdiomaResult res = BLL_Idioma.CrearIdioma(_txtCodigo.Text, _txtNombre.Text, out int nuevoId);
                if (res != IdiomaResult.Success)
                {
                    Mensaje(MensajeError(res), true);
                    return;
                }
                _txtCodigo.Clear();
                _txtNombre.Clear();
                CargarIdiomas();
                _cboIdioma.SelectedValue = nuevoId;
                RefrescarSelectorPrincipal();
                Mensaje(Tr.T("MSG_IDI_CREADO"), false);
            }
            catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Idiomas", "Crear idioma"); Mensaje("Error: " + ex.Message, true); }
        }

        private void GuardarTraducciones()
        {
            if (!(_cboIdioma.SelectedValue is int idiomaId))
            {
                Mensaje(Tr.T("MSG_IDI_SELECCIONE"), true);
                return;
            }
            try
            {
                _grid.EndEdit();
                var textos = new Dictionary<string, string>();
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    string clave = row.Cells["colClave"].Value?.ToString();
                    string texto = row.Cells["colTexto"].Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(clave)) textos[clave] = texto;
                }
                BLL_Idioma.GuardarTraducciones(idiomaId, textos);
                RefrescarSelectorPrincipal();
                // Si edite el idioma activo, refrescar esta misma vista.
                GestorDeIdioma.GetInstance.CambiarIdioma(GestorDeIdioma.GetInstance.IdiomaActual);
                Mensaje(Tr.T("MSG_IDI_GUARDADO"), false);
            }
            catch (Exception ex) { BLL_Bitacora.RegistrarExcepcion(ex, "Idiomas", "Guardar traducciones"); Mensaje("Error: " + ex.Message, true); }
        }

        private void RefrescarSelectorPrincipal()
        {
            if (FindForm() is frmMain main) main.RefrescarIdiomas();
        }

        private static string MensajeError(IdiomaResult r)
        {
            switch (r)
            {
                case IdiomaResult.CodigoInvalido:  return Tr.T("MSG_IDI_COD_INV");
                case IdiomaResult.NombreInvalido:  return Tr.T("MSG_IDI_NOM_INV");
                case IdiomaResult.CodigoDuplicado: return Tr.T("MSG_IDI_DUP");
                default:                           return Tr.T("MSG_IDI_ERROR");
            }
        }

        private void Mensaje(string texto, bool error)
        {
            _lblMsg.ForeColor = error ? Theme.Error : Theme.Success;
            _lblMsg.Text = texto;
        }
    }
}
