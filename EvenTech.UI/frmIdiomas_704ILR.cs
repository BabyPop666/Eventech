using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Dialogo que aloja la gestion de idiomas (alta de idioma + edicion de
    // traducciones). Se abre desde el selector de idioma (globo) de la ventana
    // principal, ya que la seccion salio del menu lateral.
    public class frmIdiomas_704ILR : FormBase_704ILR
    {
        public frmIdiomas_704ILR()
        {
            Text = "EvenTech";
            ClientSize = new Size(940, 620);
            BackColor = Theme_704ILR.BgContent_704ILR;

            var pnlTop_704ILR = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTop_704ILR);
            var lblTitle_704ILR = new Label
            {
                Text = T_704ILR("IDI_GESTION", "Gestionar idiomas"),
                Font = Theme_704ILR.FontH2_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(lblTitle_704ILR);
            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => Close(), danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Right;
            pnlTop_704ILR.Controls.Add(lblTitle_704ILR);
            pnlTop_704ILR.Controls.Add(btnClose_704ILR);

            var host_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme_704ILR.BgContent_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };
            host_704ILR.Controls.Add(new ucIdiomas_704ILR { Dock = DockStyle.Fill });

            Controls.Add(host_704ILR);
            Controls.Add(pnlTop_704ILR);
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
