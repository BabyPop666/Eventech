using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Selector de idioma compacto: SOLO un globo. Al hacer clic abre un menu para
    // elegir el idioma activo y, si allowManage, gestionar idiomas (alta + edicion
    // de traducciones). Reutilizable en login (allowManage=false) y en la ventana
    // principal (allowManage=true). Observa el idioma para marcar el activo.
    public class LangSelector : UserControl, IObservadorIdioma
    {
        private readonly Label _globe;
        private readonly bool _allowManage;
        private readonly Color _baseColor;
        private readonly Color _hoverColor;

        public LangSelector() : this(false, false) { }

        public LangSelector(bool dark, bool allowManage)
        {
            _allowManage = allowManage;
            Size = new Size(34, 30);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            _baseColor = dark ? Theme.Accent : Theme.TextMuted;
            _hoverColor = dark ? Theme.TextOnDark : Theme.Accent;

            _globe = new Label
            {
                Text = Theme.IcoGlobe,
                Font = Theme.FontIcon,
                ForeColor = _baseColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _globe.MouseEnter += (s, e) => _globe.ForeColor = _hoverColor;
            _globe.MouseLeave += (s, e) => _globe.ForeColor = _baseColor;
            _globe.Click += (s, e) => MostrarMenu();
            Click += (s, e) => MostrarMenu();
            Controls.Add(_globe);

            Load += (s, e) => GestorDeIdioma.GetInstance.Suscribir(this);
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void MostrarMenu()
        {
            var g = GestorDeIdioma.GetInstance;
            var menu = new ContextMenuStrip { Font = Theme.FontBody };
            // Se crea uno nuevo por apertura: liberarlo al cerrarse evita acumular
            // handles/ToolStripItems hasta el finalizador (control de uso frecuente).
            menu.Closed += (s, e) => menu.Dispose();

            foreach (var idi in g.IdiomasDisponibles)
            {
                BE_Idioma be = idi;
                var item = new ToolStripMenuItem(be.ToString())
                {
                    Checked = be.Codigo.Equals(g.IdiomaActual, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (s, e) => g.CambiarIdioma(be.Codigo);
                menu.Items.Add(item);
            }

            if (_allowManage)
            {
                menu.Items.Add(new ToolStripSeparator());
                var gestionar = new ToolStripMenuItem(T("IDI_GESTION", "Gestionar idiomas") + "...");
                gestionar.Click += (s, e) => { using (var dlg = new frmIdiomas()) dlg.ShowDialog(FindForm()); };
                menu.Items.Add(gestionar);
            }

            // El globo suele estar al pie: el menu se abre hacia arriba-izquierda.
            menu.Show(_globe, new Point(_globe.Width, 0), ToolStripDropDownDirection.AboveLeft);
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // El menu se arma al abrir, por eso no hay nada que precargar ni re-traducir.
        public void Repopulate() { }
        public void ActualizarTextos() { }
    }
}
