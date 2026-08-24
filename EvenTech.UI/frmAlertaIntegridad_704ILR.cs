using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Pantalla de alerta que se muestra al iniciar la app cuando la verificacion
    // de integridad (digitos verificadores) detecta inconsistencias. Bloquea el
    // flujo hasta que el administrador la revise (la cierra explicitamente).
    // Borderless heredando de FormBase, con identidad de marca (azul oscuro + dorado).
    public class frmAlertaIntegridad_704ILR : FormBase_704ILR
    {
        public frmAlertaIntegridad_704ILR(IReadOnlyList<string> inconsistencias_704ILR)
        {
            BuildUi_704ILR(inconsistencias_704ILR);
        }

        private void BuildUi_704ILR(IReadOnlyList<string> inconsistencias_704ILR)
        {
            Text = "EvenTech";
            ClientSize = new Size(560, 400);
            BackColor = Theme_704ILR.BgLogin_704ILR;

            // ---------------- Barra de titulo ----------------
            var pnlTitle_704ILR = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme_704ILR.BgTitleBar_704ILR };
            EnableDrag_704ILR(pnlTitle_704ILR);

            var lblBar_704ILR = new Label
            {
                Text = "EvenTech",
                Font = Theme_704ILR.FontBodyBold_704ILR,
                ForeColor = Theme_704ILR.TextLight_704ILR,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag_704ILR(lblBar_704ILR);

            var btnClose_704ILR = WindowButton_704ILR(Theme_704ILR.IcoClose_704ILR, (s_704ILR, e_704ILR) => Close(), danger_704ILR: true);
            btnClose_704ILR.Dock = DockStyle.Right;

            pnlTitle_704ILR.Controls.Add(lblBar_704ILR);
            pnlTitle_704ILR.Controls.Add(btnClose_704ILR);

            // ---------------- Cuerpo ----------------
            // Layout por TableLayoutPanel: encabezado / hint / lista (fill) / boton.
            var body_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme_704ILR.BgLogin_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR, Theme_704ILR.SpaceXl_704ILR, Theme_704ILR.SpaceLg_704ILR)
            };
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // encabezado (icono + titulo)
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // hint
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));        // lista de inconsistencias
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // boton continuar

            // Encabezado de advertencia: icono dorado + titulo.
            var header_704ILR = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR)
            };
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblIcon_704ILR = new Label
            {
                Text = Theme_704ILR.IcoWarning_704ILR,
                Font = Theme_704ILR.FontIcon_704ILR,
                ForeColor = Theme_704ILR.Accent_704ILR,
                AutoSize = false,
                Size = new Size(32, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, Theme_704ILR.SpaceSm_704ILR, 0)
            };

            var lblTitle_704ILR = new Label
            {
                Text = Tr_704ILR.T_704ILR("ALERT_TITULO"),
                Tag = "T:ALERT_TITULO",
                Font = Theme_704ILR.FontH1_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            header_704ILR.Controls.Add(lblIcon_704ILR, 0, 0);
            header_704ILR.Controls.Add(lblTitle_704ILR, 1, 0);

            var lblHint_704ILR = new Label
            {
                Text = Tr_704ILR.T_704ILR("ALERT_HINT"),
                Tag = "T:ALERT_HINT",
                Font = Theme_704ILR.FontSmall_704ILR,
                ForeColor = Theme_704ILR.TextLight_704ILR,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };

            // Lista de inconsistencias: superficie oscura, texto claro, monoespaciado.
            var lst_704ILR = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F),
                BackColor = Theme_704ILR.BgTitleBar_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                Margin = new Padding(0)
            };
            foreach (var i_704ILR in inconsistencias_704ILR) lst_704ILR.Items.Add(i_704ILR);

            // Boton continuar: primario dorado, alineado a la derecha. Cierra el dialogo.
            var btnContinuar_704ILR = Ui_704ILR.Primary_704ILR(Tr_704ILR.T_704ILR("ALERT_BTN"));
            btnContinuar_704ILR.Tag = "T:ALERT_BTN";
            btnContinuar_704ILR.BehindColor_704ILR = Theme_704ILR.BgLogin_704ILR;
            btnContinuar_704ILR.Size = new Size(200, 40);
            btnContinuar_704ILR.Anchor = AnchorStyles.Right;
            btnContinuar_704ILR.Margin = new Padding(0, Theme_704ILR.SpaceMd_704ILR, 0, 0);
            btnContinuar_704ILR.Click += (s_704ILR, e_704ILR) => Close();

            body_704ILR.Controls.Add(header_704ILR, 0, 0);
            body_704ILR.Controls.Add(lblHint_704ILR, 0, 1);
            body_704ILR.Controls.Add(lst_704ILR, 0, 2);
            body_704ILR.Controls.Add(btnContinuar_704ILR, 0, 3);

            Controls.Add(body_704ILR);
            Controls.Add(pnlTitle_704ILR);

            AcceptButton = btnContinuar_704ILR;
        }
    }
}
