using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Gestion de idiomas (T05). Permite al administrador dar de alta un idioma
    // nuevo y editar las traducciones de cada leyenda. Al guardar, recarga el
    // GestorDeIdioma en caliente y refresca el selector de la ventana principal.
    public class ucIdiomas : UserControl
    {
        private ComboBox _cboIdioma;
        private TextBox _txtCodigo, _txtNombre;
        private DataGridView _grid;
        private Label _lblMsg;

        public ucIdiomas()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            Load += (s, e) => CargarIdiomas();
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "Gestion de Idiomas",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            // --- Panel alta de idioma nuevo ---
            var pnlNuevo = new Panel
            {
                Location = new Point(12, 52),
                Size = new Size(560, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblNuevo = new Label { Text = "Nuevo idioma", Font = new Font("Ebrima", 11F, FontStyle.Bold), ForeColor = Theme.TextOnLight, AutoSize = true, Location = new Point(10, 8) };
            var lblCod = new Label { Text = "Codigo (ej. PT)", Font = new Font("Ebrima", 8.5F), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(12, 32) };
            _txtCodigo = new TextBox { Location = new Point(12, 48), Size = new Size(80, 24), Font = new Font("Ebrima", 10F), MaxLength = 5 };
            var lblNom = new Label { Text = "Nombre", Font = new Font("Ebrima", 8.5F), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(105, 32) };
            _txtNombre = new TextBox { Location = new Point(105, 48), Size = new Size(180, 24), Font = new Font("Ebrima", 10F) };
            var btnCrear = MakeButton("Crear idioma", 300, 46, Theme.AccentButton);
            btnCrear.Size = new Size(140, 26);
            btnCrear.Click += (s, e) => CrearIdioma();

            pnlNuevo.Controls.Add(lblNuevo);
            pnlNuevo.Controls.Add(lblCod); pnlNuevo.Controls.Add(_txtCodigo);
            pnlNuevo.Controls.Add(lblNom); pnlNuevo.Controls.Add(_txtNombre);
            pnlNuevo.Controls.Add(btnCrear);

            // --- Editor de traducciones ---
            var lblEditar = new Label { Text = "Idioma:", Font = new Font("Ebrima", 11F), ForeColor = Theme.TextOnLight, AutoSize = true, Location = new Point(12, 138) };
            _cboIdioma = new ComboBox { Location = new Point(75, 134), Size = new Size(220, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Ebrima", 11F) };
            _cboIdioma.SelectedIndexChanged += (s, e) => CargarTraducciones();

            _grid = new DataGridView
            {
                Location = new Point(12, 172),
                Size = new Size(905, 360),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EnableHeadersVisualStyles = false,
                Font = new Font("Ebrima", 9.5F)
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.BgTitleBar;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextOnDark;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Ebrima", 10F, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 30;

            var colClave = new DataGridViewTextBoxColumn { HeaderText = "Clave", Name = "colClave", FillWeight = 40, ReadOnly = true };
            colClave.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            var colTexto = new DataGridViewTextBoxColumn { HeaderText = "Texto", Name = "colTexto", FillWeight = 60 };
            _grid.Columns.Add(colClave);
            _grid.Columns.Add(colTexto);

            var btnGuardar = MakeButton("Guardar traducciones", 12, 540, Theme.AccentButton);
            btnGuardar.Size = new Size(200, 34);
            btnGuardar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnGuardar.Click += (s, e) => GuardarTraducciones();

            _lblMsg = new Label { AutoSize = true, Font = new Font("Ebrima", 10F, FontStyle.Bold), Location = new Point(225, 548), Anchor = AnchorStyles.Bottom | AnchorStyles.Left, MaximumSize = new Size(560, 0) };

            Controls.Add(lblTitle);
            Controls.Add(pnlNuevo);
            Controls.Add(lblEditar);
            Controls.Add(_cboIdioma);
            Controls.Add(_grid);
            Controls.Add(btnGuardar);
            Controls.Add(_lblMsg);
        }

        private Button MakeButton(string text, int x, int y, Color back)
        {
            var b = new Button
            {
                Text = text, Font = Theme.FontButton, BackColor = back, ForeColor = Theme.TextOnDark,
                FlatStyle = FlatStyle.Flat, Size = new Size(140, 30), Location = new Point(x, y), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
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
            catch (Exception ex) { Mensaje("Error cargando idiomas: " + ex.Message, true); }
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
            catch (Exception ex) { Mensaje("Error cargando traducciones: " + ex.Message, true); }
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
                Mensaje("Idioma creado. Edite los textos y guarde.", false);
            }
            catch (Exception ex) { Mensaje("No se pudo crear el idioma: " + ex.Message, true); }
        }

        private void GuardarTraducciones()
        {
            if (!(_cboIdioma.SelectedValue is int idiomaId))
            {
                Mensaje("Seleccione un idioma.", true);
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
                Mensaje($"Guardado ({textos.Count} leyendas).", false);
            }
            catch (Exception ex) { Mensaje("No se pudo guardar: " + ex.Message, true); }
        }

        // Refresca el combo de idiomas de la ventana principal para que el idioma
        // nuevo este disponible sin reiniciar la app.
        private void RefrescarSelectorPrincipal()
        {
            if (FindForm() is frmMain main) main.RefrescarIdiomas();
        }

        private static string MensajeError(IdiomaResult r)
        {
            switch (r)
            {
                case IdiomaResult.CodigoInvalido:  return "Codigo invalido (1 a 5 caracteres).";
                case IdiomaResult.NombreInvalido:  return "Ingrese el nombre del idioma.";
                case IdiomaResult.CodigoDuplicado: return "Ya existe un idioma con ese codigo.";
                default:                           return "No se pudo crear el idioma.";
            }
        }

        private void Mensaje(string texto, bool error)
        {
            _lblMsg.ForeColor = error ? Color.Firebrick : Color.SeaGreen;
            _lblMsg.Text = texto;
        }
    }
}
