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
    public class LangSelector_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private readonly Label _globe_704ILR;
        private readonly Color _baseColor_704ILR;
        private readonly Color _hoverColor_704ILR;

        // Habilita la opcion "Gestionar idiomas" del menu. La ventana principal
        // la ajusta segun el permiso IDIOMAS_GESTION del perfil; el login la deja
        // siempre en false (todavia no hay sesion).
        public bool PermitirGestion_704ILR { get; set; }

        public LangSelector_704ILR() : this(false, false) { }

        public LangSelector_704ILR(bool dark_704ILR, bool allowManage_704ILR)
        {
            PermitirGestion_704ILR = allowManage_704ILR;
            Size = new Size(34, 30);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            _baseColor_704ILR = dark_704ILR ? Theme_704ILR.Accent_704ILR : Theme_704ILR.TextMuted_704ILR;
            _hoverColor_704ILR = dark_704ILR ? Theme_704ILR.TextOnDark_704ILR : Theme_704ILR.Accent_704ILR;

            _globe_704ILR = new Label
            {
                Text = Theme_704ILR.IcoGlobe_704ILR,
                Font = Theme_704ILR.FontIcon_704ILR,
                ForeColor = _baseColor_704ILR,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            _globe_704ILR.MouseEnter += (s_704ILR, e_704ILR) => _globe_704ILR.ForeColor = _hoverColor_704ILR;
            _globe_704ILR.MouseLeave += (s_704ILR, e_704ILR) => _globe_704ILR.ForeColor = _baseColor_704ILR;
            _globe_704ILR.Click += (s_704ILR, e_704ILR) => MostrarMenu_704ILR();
            Click += (s_704ILR, e_704ILR) => MostrarMenu_704ILR();
            Controls.Add(_globe_704ILR);

            Load += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void MostrarMenu_704ILR()
        {
            var g_704ILR = GestorDeIdioma_704ILR.GetInstance_704ILR;
            var menu_704ILR = new ContextMenuStrip { Font = Theme_704ILR.FontBody_704ILR };

            foreach (var idi_704ILR in g_704ILR.IdiomasDisponibles_704ILR)
            {
                BE_Idioma_704ILR be_704ILR = idi_704ILR;
                var item_704ILR = new ToolStripMenuItem(be_704ILR.ToString())
                {
                    Checked = be_704ILR.Codigo_704ILR.Equals(g_704ILR.IdiomaActual_704ILR, StringComparison.OrdinalIgnoreCase)
                };
                // El idioma elegido se recuerda para el proximo arranque.
                item_704ILR.Click += (s_704ILR, e_704ILR) => { g_704ILR.CambiarIdioma_704ILR(be_704ILR.Codigo_704ILR); LoginPrefs_704ILR.GuardarIdioma_704ILR(be_704ILR.Codigo_704ILR); };
                menu_704ILR.Items.Add(item_704ILR);
            }

            if (PermitirGestion_704ILR)
            {
                menu_704ILR.Items.Add(new ToolStripSeparator());
                var gestionar_704ILR = new ToolStripMenuItem(T_704ILR("IDI_GESTION", "Gestionar idiomas") + "...");
                // Segunda capa: el permiso se vuelve a exigir al abrir el ABM.
                gestionar_704ILR.Click += (s_704ILR, e_704ILR) =>
                {
                    if (!Permisos_704ILR.Exigir_704ILR("IDIOMAS_GESTION", FindForm(), "gestionar idiomas")) return;
                    using (var dlg_704ILR = new frmIdiomas_704ILR()) dlg_704ILR.ShowDialog(FindForm());
                };
                menu_704ILR.Items.Add(gestionar_704ILR);
            }

            // El globo suele estar al pie: el menu se abre hacia arriba-izquierda.
            menu_704ILR.Show(_globe_704ILR, new Point(_globe_704ILR.Width, 0), ToolStripDropDownDirection.AboveLeft);
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        // El menu se arma al abrir, por eso no hay nada que precargar ni re-traducir.
        public void Repopulate_704ILR() { }
        public void ActualizarTextos_704ILR() { }
    }
}
