using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Dialogo que aloja la gestion de idiomas (alta de idioma + edicion de
    // traducciones). Se abre desde el selector de idioma (globo) de la ventana
    // principal, ya que la seccion salio del menu lateral.
    public class frmIdiomas : FormBase
    {
        public frmIdiomas()
        {
            Text = "EvenTech";
            ClientSize = new Size(940, 620);
            BackColor = Theme.BgContent;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTop);
            var lblTitle = new Label
            {
                Text = T("IDI_GESTION", "Gestionar idiomas"),
                Font = Theme.FontH2,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(lblTitle);
            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Close(), danger: true);
            btnClose.Dock = DockStyle.Right;
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(btnClose);

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgContent,
                Padding = new Padding(Theme.SpaceLg)
            };
            host.Controls.Add(new ucIdiomas { Dock = DockStyle.Fill });

            Controls.Add(host);
            Controls.Add(pnlTop);
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
