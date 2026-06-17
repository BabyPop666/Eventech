using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Estilo unificado para DataGridView (header de marca, zebra, seleccion
    // dorada, lineas suaves). Centraliza lo que antes se repetia en cada vista.
    internal static class UiGrid
    {
        public static void Style(DataGridView g, bool editable = false)
        {
            g.BackgroundColor = Theme.Surface;
            g.BorderStyle = BorderStyle.None;
            g.GridColor = Theme.GridLines;
            g.ReadOnly = !editable;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.AutoGenerateColumns = false;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.SelectionMode = editable
                ? DataGridViewSelectionMode.CellSelect
                : DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.RowTemplate.Height = 30;
            g.ColumnHeadersHeight = 36;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.Font = Theme.FontSmall;

            var h = g.ColumnHeadersDefaultCellStyle;
            h.BackColor = Theme.GridHeaderBg;
            h.ForeColor = Theme.GridHeaderFg;
            h.Font = Theme.FontBodyBold;
            h.Alignment = DataGridViewContentAlignment.MiddleLeft;
            h.Padding = new Padding(Theme.SpaceSm, 0, 0, 0);
            h.SelectionBackColor = Theme.GridHeaderBg;
            h.SelectionForeColor = Theme.GridHeaderFg;
            h.WrapMode = DataGridViewTriState.False;

            var d = g.DefaultCellStyle;
            d.BackColor = Theme.Surface;
            d.ForeColor = Theme.TextOnLight;
            d.SelectionBackColor = Theme.GridSelectBg;
            d.SelectionForeColor = Theme.GridSelectFg;
            d.Padding = new Padding(Theme.SpaceSm, 0, Theme.SpaceXs, 0);
            d.Font = Theme.FontSmall;

            var a = g.AlternatingRowsDefaultCellStyle;
            a.BackColor = Theme.GridZebra;
            a.ForeColor = Theme.TextOnLight;
            a.SelectionBackColor = Theme.GridSelectBg;
            a.SelectionForeColor = Theme.GridSelectFg;
        }
    }
}
