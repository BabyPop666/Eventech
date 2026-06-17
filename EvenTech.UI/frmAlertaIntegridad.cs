using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Pantalla de alerta que se muestra al iniciar la app cuando la verificacion
    // de integridad (digitos verificadores) detecta inconsistencias. Bloquea el
    // flujo hasta que el administrador la revise (la cierra explicitamente).
    // Borderless heredando de FormBase, con identidad de marca (azul oscuro + dorado).
    public class frmAlertaIntegridad : FormBase
    {
        public frmAlertaIntegridad(IReadOnlyList<string> inconsistencias)
        {
            BuildUi(inconsistencias);
        }

        private void BuildUi(IReadOnlyList<string> inconsistencias)
        {
            Text = "EvenTech";
            ClientSize = new Size(560, 400);
            BackColor = Theme.BgLogin;

            // ---------------- Barra de titulo ----------------
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgTitleBar };
            EnableDrag(pnlTitle);

            var lblBar = new Label
            {
                Text = "EvenTech",
                Font = Theme.FontBodyBold,
                ForeColor = Theme.TextLight,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Theme.SpaceLg, 0, 0, 0),
                BackColor = Color.Transparent
            };
            EnableDrag(lblBar);

            var btnClose = WindowButton(Theme.IcoClose, (s, e) => Close(), danger: true);
            btnClose.Dock = DockStyle.Right;

            pnlTitle.Controls.Add(lblBar);
            pnlTitle.Controls.Add(btnClose);

            // ---------------- Cuerpo ----------------
            // Layout por TableLayoutPanel: encabezado / hint / lista (fill) / boton.
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme.BgLogin,
                Padding = new Padding(Theme.SpaceXl, Theme.SpaceLg, Theme.SpaceXl, Theme.SpaceLg)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // encabezado (icono + titulo)
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));            // hint
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));        // lista de inconsistencias
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // boton continuar

            // Encabezado de advertencia: icono dorado + titulo.
            var header = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceSm)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblIcon = new Label
            {
                Text = Theme.IcoWarning,
                Font = Theme.FontIcon,
                ForeColor = Theme.Accent,
                AutoSize = false,
                Size = new Size(32, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, Theme.SpaceSm, 0)
            };

            var lblTitle = new Label
            {
                Text = Tr.T("ALERT_TITULO"),
                Tag = "T:ALERT_TITULO",
                Font = Theme.FontH1,
                ForeColor = Theme.TextOnDark,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            header.Controls.Add(lblIcon, 0, 0);
            header.Controls.Add(lblTitle, 1, 0);

            var lblHint = new Label
            {
                Text = Tr.T("ALERT_HINT"),
                Tag = "T:ALERT_HINT",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextLight,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd)
            };

            // Lista de inconsistencias: superficie oscura, texto claro, monoespaciado.
            var lst = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F),
                BackColor = Theme.BgTitleBar,
                ForeColor = Theme.TextOnDark,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                Margin = new Padding(0)
            };
            foreach (var i in inconsistencias) lst.Items.Add(i);

            // Boton continuar: primario dorado, alineado a la derecha. Cierra el dialogo.
            var btnContinuar = Ui.Primary(Tr.T("ALERT_BTN"));
            btnContinuar.Tag = "T:ALERT_BTN";
            btnContinuar.BehindColor = Theme.BgLogin;
            btnContinuar.Size = new Size(200, 40);
            btnContinuar.Anchor = AnchorStyles.Right;
            btnContinuar.Margin = new Padding(0, Theme.SpaceMd, 0, 0);
            btnContinuar.Click += (s, e) => Close();

            body.Controls.Add(header, 0, 0);
            body.Controls.Add(lblHint, 0, 1);
            body.Controls.Add(lst, 0, 2);
            body.Controls.Add(btnContinuar, 0, 3);

            Controls.Add(body);
            Controls.Add(pnlTitle);

            AcceptButton = btnContinuar;
        }
    }
}
