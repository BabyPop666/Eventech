using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Fabrica de controles con el estilo del design system. Evita repetir
    // configuracion de fuentes/colores en cada vista (cohesion + reuso).
    internal static class Ui_704ILR
    {
        // ---------- Tipografia / etiquetas (sobre fondo claro) ----------
        public static Label H1_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontH1_704ILR, Theme_704ILR.TextOnLight_704ILR);
        public static Label H2_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontH2_704ILR, Theme_704ILR.TextOnLight_704ILR);
        public static Label Title_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontTitle_704ILR, Theme_704ILR.TextOnLight_704ILR);
        public static Label Body_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontBody_704ILR, Theme_704ILR.TextOnLight_704ILR);
        public static Label BodyBold_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontBodyBold_704ILR, Theme_704ILR.TextOnLight_704ILR);
        public static Label Caption_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontCaption_704ILR, Theme_704ILR.TextMuted_704ILR);
        public static Label FieldLabel_704ILR(string text_704ILR = "") => Lbl_704ILR(text_704ILR, Theme_704ILR.FontSmall_704ILR, Theme_704ILR.TextMuted_704ILR);

        private static Label Lbl_704ILR(string text_704ILR, Font f_704ILR, Color c_704ILR) => new Label
        {
            Text = text_704ILR,
            Font = f_704ILR,
            ForeColor = c_704ILR,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceXs_704ILR)
        };

        // ---------- Botones ----------
        public static AppButton_704ILR Primary_704ILR(string text_704ILR, string glyph_704ILR = null) => new AppButton_704ILR { Text = text_704ILR, Glyph_704ILR = glyph_704ILR };
        public static AppButton_704ILR Secondary_704ILR(string text_704ILR, string glyph_704ILR = null) => new AppButton_704ILR
        {
            Text = text_704ILR,
            Glyph_704ILR = glyph_704ILR,
            BaseColor_704ILR = Theme_704ILR.Neutral_704ILR,
            HoverColor_704ILR = Theme_704ILR.NeutralHover_704ILR,
            DownColor_704ILR = Theme_704ILR.NeutralDown_704ILR
        };

        // ---------- Inputs sobre fondo claro ----------
        public static TextBox Input_704ILR() => new TextBox
        {
            Font = Theme_704ILR.FontInput_704ILR,
            BorderStyle = BorderStyle.FixedSingle
        };

        public static ComboBox Combo_704ILR() => new ComboBox
        {
            Font = Theme_704ILR.FontInput_704ILR,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };

        // Hace que un ComboBox dibuje cada item con texto traducido EN VIVO: como el
        // texto se resuelve en cada repintado, al cambiar el idioma basta con un
        // Invalidate() para refrescar (incluido el item seleccionado cerrado).
        // textOf convierte el item (enum/wrapper/string) al texto a mostrar.
        public static void DibujarEnum_704ILR(ComboBox cbo_704ILR, System.Func<object, string> textOf_704ILR)
        {
            cbo_704ILR.DrawMode = DrawMode.OwnerDrawFixed;
            cbo_704ILR.DrawItem += (s_704ILR, e_704ILR) =>
            {
                e_704ILR.DrawBackground();
                if (e_704ILR.Index >= 0 && e_704ILR.Index < cbo_704ILR.Items.Count)
                {
                    string txt_704ILR = textOf_704ILR(cbo_704ILR.Items[e_704ILR.Index]) ?? string.Empty;
                    TextRenderer.DrawText(e_704ILR.Graphics, txt_704ILR, cbo_704ILR.Font, e_704ILR.Bounds, e_704ILR.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                e_704ILR.DrawFocusRectangle();
            };
        }

        public static DateTimePicker DatePicker_704ILR() => new DateTimePicker
        {
            Font = Theme_704ILR.FontInput_704ILR,
            Format = DateTimePickerFormat.Short
        };

        // ---------- Campo etiquetado (caption arriba, input abajo) ----------
        // Devuelve un panel auto-ajustable apto para apilar en un FlowLayoutPanel
        // o ubicar en una celda de TableLayoutPanel (Dock=Top/Fill).
        public static TableLayoutPanel Field_704ILR(string caption_704ILR, Control input_704ILR, int inputHeight_704ILR = 30)
        {
            var t_704ILR = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };
            t_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, inputHeight_704ILR + 2));
            input_704ILR.Dock = DockStyle.Fill;
            input_704ILR.Margin = new Padding(0);
            var lbl_704ILR = FieldLabel_704ILR(caption_704ILR);
            lbl_704ILR.Margin = new Padding(2, 0, 0, 2);
            t_704ILR.Controls.Add(lbl_704ILR, 0, 0);
            t_704ILR.Controls.Add(input_704ILR, 0, 1);
            return t_704ILR;
        }

        // ---------- Campo oscuro (estilo login): caption + input con subrayado dorado ----------
        // Devuelve tambien el caption Label (out) para poder re-traducirlo (Observer).
        public static TableLayoutPanel DarkField_704ILR(string caption_704ILR, bool password_704ILR, out TextBox box_704ILR, out Label captionLabel_704ILR)
        {
            var t_704ILR = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR)
            };
            t_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            t_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            captionLabel_704ILR = new Label
            {
                Text = caption_704ILR,
                Font = Theme_704ILR.FontSmall_704ILR,
                ForeColor = Theme_704ILR.TextLight_704ILR,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var inputBox_704ILR = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme_704ILR.BgInput_704ILR,
                Padding = new Padding(Theme_704ILR.SpaceSm_704ILR, 6, Theme_704ILR.SpaceSm_704ILR, 0),
                Margin = new Padding(0)
            };
            box_704ILR = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Theme_704ILR.FontInput_704ILR,
                BackColor = Theme_704ILR.BgInput_704ILR,
                ForeColor = Theme_704ILR.TextOnDark_704ILR,
                Dock = DockStyle.Top,
                UseSystemPasswordChar = password_704ILR
            };
            var underline_704ILR = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Theme_704ILR.Accent_704ILR };

            // Orden de Add = z-order: el ultimo agregado (underline) ancla primero
            // (Bottom, ancho completo); el ojo (Right) queda encima; el textbox
            // (Top) toma el ancho restante a la izquierda del ojo.
            inputBox_704ILR.Controls.Add(box_704ILR);
            if (password_704ILR)
            {
                var theBox_704ILR = box_704ILR;
                var eye_704ILR = new Label
                {
                    Text = Theme_704ILR.IcoEye_704ILR,
                    Font = new Font("Segoe MDL2 Assets", 11F),
                    ForeColor = Theme_704ILR.TextLight_704ILR,
                    Dock = DockStyle.Right,
                    Width = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    BackColor = Theme_704ILR.BgInput_704ILR
                };
                eye_704ILR.MouseEnter += (s_704ILR, e_704ILR) => eye_704ILR.ForeColor = Theme_704ILR.TextOnDark_704ILR;
                eye_704ILR.MouseLeave += (s_704ILR, e_704ILR) => eye_704ILR.ForeColor = Theme_704ILR.TextLight_704ILR;
                eye_704ILR.Click += (s_704ILR, e_704ILR) =>
                {
                    theBox_704ILR.UseSystemPasswordChar = !theBox_704ILR.UseSystemPasswordChar;
                    eye_704ILR.Text = theBox_704ILR.UseSystemPasswordChar ? Theme_704ILR.IcoEye_704ILR : Theme_704ILR.IcoEyeOff_704ILR;
                };
                inputBox_704ILR.Controls.Add(eye_704ILR);
            }
            inputBox_704ILR.Controls.Add(underline_704ILR);

            t_704ILR.Controls.Add(captionLabel_704ILR, 0, 0);
            t_704ILR.Controls.Add(inputBox_704ILR, 0, 1);
            return t_704ILR;
        }

        // ---------- Selector de idioma compacto para barras oscuras ----------
        public static ComboBox LangCombo_704ILR() => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = Theme_704ILR.FontCaption_704ILR,
            BackColor = Theme_704ILR.BgTitleBar_704ILR,
            ForeColor = Theme_704ILR.TextOnDark_704ILR
        };
    }
}
