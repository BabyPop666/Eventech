using System.Drawing;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Estilo unificado para DataGridView (header de marca, zebra, seleccion
    // dorada, lineas suaves). Centraliza lo que antes se repetia en cada vista.
    internal static class UiGrid_704ILR
    {
        public static void Style_704ILR(DataGridView g_704ILR, bool editable_704ILR = false)
        {
            g_704ILR.BackgroundColor = Theme_704ILR.Surface_704ILR;
            g_704ILR.BorderStyle = BorderStyle.None;
            g_704ILR.GridColor = Theme_704ILR.GridLines_704ILR;
            g_704ILR.ReadOnly = !editable_704ILR;
            g_704ILR.AllowUserToAddRows = false;
            g_704ILR.AllowUserToDeleteRows = false;
            g_704ILR.AllowUserToResizeRows = false;
            g_704ILR.RowHeadersVisible = false;
            g_704ILR.AutoGenerateColumns = false;
            g_704ILR.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g_704ILR.SelectionMode = editable_704ILR
                ? DataGridViewSelectionMode.CellSelect
                : DataGridViewSelectionMode.FullRowSelect;
            g_704ILR.MultiSelect = false;
            g_704ILR.EnableHeadersVisualStyles = false;
            g_704ILR.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g_704ILR.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g_704ILR.RowTemplate.Height = 30;
            g_704ILR.ColumnHeadersHeight = 36;
            g_704ILR.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g_704ILR.Font = Theme_704ILR.FontSmall_704ILR;

            var h_704ILR = g_704ILR.ColumnHeadersDefaultCellStyle;
            h_704ILR.BackColor = Theme_704ILR.GridHeaderBg_704ILR;
            h_704ILR.ForeColor = Theme_704ILR.GridHeaderFg_704ILR;
            h_704ILR.Font = Theme_704ILR.FontBodyBold_704ILR;
            h_704ILR.Alignment = DataGridViewContentAlignment.MiddleLeft;
            h_704ILR.Padding = new Padding(Theme_704ILR.SpaceSm_704ILR, 0, 0, 0);
            h_704ILR.SelectionBackColor = Theme_704ILR.GridHeaderBg_704ILR;
            h_704ILR.SelectionForeColor = Theme_704ILR.GridHeaderFg_704ILR;
            h_704ILR.WrapMode = DataGridViewTriState.False;

            var d_704ILR = g_704ILR.DefaultCellStyle;
            d_704ILR.BackColor = Theme_704ILR.Surface_704ILR;
            d_704ILR.ForeColor = Theme_704ILR.TextOnLight_704ILR;
            d_704ILR.SelectionBackColor = Theme_704ILR.GridSelectBg_704ILR;
            d_704ILR.SelectionForeColor = Theme_704ILR.GridSelectFg_704ILR;
            d_704ILR.Padding = new Padding(Theme_704ILR.SpaceSm_704ILR, 0, Theme_704ILR.SpaceXs_704ILR, 0);
            d_704ILR.Font = Theme_704ILR.FontSmall_704ILR;

            var a_704ILR = g_704ILR.AlternatingRowsDefaultCellStyle;
            a_704ILR.BackColor = Theme_704ILR.GridZebra_704ILR;
            a_704ILR.ForeColor = Theme_704ILR.TextOnLight_704ILR;
            a_704ILR.SelectionBackColor = Theme_704ILR.GridSelectBg_704ILR;
            a_704ILR.SelectionForeColor = Theme_704ILR.GridSelectFg_704ILR;
        }
    }
}
