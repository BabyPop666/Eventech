using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Pantalla de alerta que se muestra al iniciar la app cuando la verificacion
    // de integridad (digitos verificadores) detecta inconsistencias. Bloquea el
    // flujo hasta que el administrador la revise (la cierra explicitamente).
    public class frmAlertaIntegridad : Form
    {
        public frmAlertaIntegridad(IReadOnlyList<string> inconsistencias)
        {
            BuildUi(inconsistencias);
        }

        private void BuildUi(IReadOnlyList<string> inconsistencias)
        {
            Text = "Alerta de integridad";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 380);
            BackColor = Theme.BgLogin;

            var lblTitle = new Label
            {
                Text = "⚠  Se detectaron problemas de integridad",
                Font = new Font("Ebrima", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 210, 120),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(20, 18),
                Size = new Size(520, 36)
            };

            var lblHint = new Label
            {
                Text = "La verificacion de digitos verificadores encontro datos alterados por " +
                       "fuera del sistema. Avise al administrador antes de operar.",
                Font = new Font("Ebrima", 10F),
                ForeColor = Theme.TextLight,
                AutoSize = false,
                Location = new Point(20, 58),
                Size = new Size(520, 46)
            };

            var lst = new ListBox
            {
                Location = new Point(20, 110),
                Size = new Size(520, 200),
                Font = new Font("Consolas", 9.5F),
                BackColor = Color.FromArgb(36, 43, 73),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            foreach (var i in inconsistencias) lst.Items.Add(i);

            var btnContinuar = new Button
            {
                Text = "Revisado, continuar",
                Font = Theme.FontButton,
                BackColor = Theme.AccentButton,
                ForeColor = Theme.TextOnDark,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 40),
                Location = new Point(340, 325),
                Cursor = Cursors.Hand
            };
            btnContinuar.FlatAppearance.BorderSize = 0;
            btnContinuar.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(lblHint);
            Controls.Add(lst);
            Controls.Add(btnContinuar);
        }
    }
}
