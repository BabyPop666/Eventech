using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Fabrica de controles con el estilo del design system. Evita repetir
    // configuracion de fuentes/colores en cada vista (cohesion + reuso).
    internal static class Ui
    {
        // ---------- Tipografia / etiquetas (sobre fondo claro) ----------
        public static Label H1(string text = "") => Lbl(text, Theme.FontH1, Theme.TextOnLight);
        public static Label H2(string text = "") => Lbl(text, Theme.FontH2, Theme.TextOnLight);
        public static Label Title(string text = "") => Lbl(text, Theme.FontTitle, Theme.TextOnLight);
        public static Label Body(string text = "") => Lbl(text, Theme.FontBody, Theme.TextOnLight);
        public static Label BodyBold(string text = "") => Lbl(text, Theme.FontBodyBold, Theme.TextOnLight);
        public static Label Caption(string text = "") => Lbl(text, Theme.FontCaption, Theme.TextMuted);
        public static Label FieldLabel(string text = "") => Lbl(text, Theme.FontSmall, Theme.TextMuted);

        private static Label Lbl(string text, Font f, Color c) => new Label
        {
            Text = text,
            Font = f,
            ForeColor = c,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, Theme.SpaceXs)
        };

        // ---------- Botones ----------
        public static AppButton Primary(string text, string glyph = null) => new AppButton { Text = text, Glyph = glyph };
        public static AppButton Secondary(string text, string glyph = null) => new AppButton
        {
            Text = text,
            Glyph = glyph,
            BaseColor = Theme.Neutral,
            HoverColor = Theme.NeutralHover,
            DownColor = Theme.NeutralDown
        };

        // ---------- Inputs sobre fondo claro ----------
        public static TextBox Input() => new TextBox
        {
            Font = Theme.FontInput,
            BorderStyle = BorderStyle.FixedSingle
        };

        public static ComboBox Combo() => new ComboBox
        {
            Font = Theme.FontInput,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };

        public static DateTimePicker DatePicker() => new DateTimePicker
        {
            Font = Theme.FontInput,
            Format = DateTimePickerFormat.Short
        };

        // ---------- Campo etiquetado (caption arriba, input abajo) ----------
        // Devuelve un panel auto-ajustable apto para apilar en un FlowLayoutPanel
        // o ubicar en una celda de TableLayoutPanel (Dock=Top/Fill).
        public static TableLayoutPanel Field(string caption, Control input, int inputHeight = 30)
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, inputHeight + 2));
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0);
            var lbl = FieldLabel(caption);
            lbl.Margin = new Padding(2, 0, 0, 2);
            t.Controls.Add(lbl, 0, 0);
            t.Controls.Add(input, 0, 1);
            return t;
        }

        // ---------- Campo oscuro (estilo login): caption + input con subrayado dorado ----------
        // Devuelve tambien el caption Label (out) para poder re-traducirlo (Observer).
        public static TableLayoutPanel DarkField(string caption, bool password, out TextBox box, out Label captionLabel)
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceMd)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            captionLabel = new Label
            {
                Text = caption,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextLight,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var inputBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                Padding = new Padding(Theme.SpaceSm, 6, Theme.SpaceSm, 0),
                Margin = new Padding(0)
            };
            box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme.FontInput,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextOnDark,
                Dock = DockStyle.Top,
                UseSystemPasswordChar = password
            };
            var underline = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Theme.Accent };

            // Orden de Add = z-order: el ultimo agregado (underline) ancla primero
            // (Bottom, ancho completo); el ojo (Right) queda encima; el textbox
            // (Top) toma el ancho restante a la izquierda del ojo.
            inputBox.Controls.Add(box);
            if (password)
            {
                var theBox = box;
                var eye = new Label
                {
                    Text = Theme.IcoEye,
                    Font = new Font("Segoe MDL2 Assets", 11F),
                    ForeColor = Theme.TextLight,
                    Dock = DockStyle.Right,
                    Width = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Theme.BgInput
                };
                eye.MouseEnter += (s, e) => eye.ForeColor = Theme.TextOnDark;
                eye.MouseLeave += (s, e) => eye.ForeColor = Theme.TextLight;
                eye.Click += (s, e) =>
                {
                    theBox.UseSystemPasswordChar = !theBox.UseSystemPasswordChar;
                    eye.Text = theBox.UseSystemPasswordChar ? Theme.IcoEye : Theme.IcoEyeOff;
                };
                inputBox.Controls.Add(eye);
            }
            inputBox.Controls.Add(underline);

            t.Controls.Add(captionLabel, 0, 0);
            t.Controls.Add(inputBox, 0, 1);
            return t;
        }

        // ---------- Selector de idioma compacto para barras oscuras ----------
        public static ComboBox LangCombo() => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.FontCaption,
            BackColor = Theme.BgTitleBar,
            ForeColor = Theme.TextOnDark
        };
    }
}
