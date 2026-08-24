using System;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Seccion de Auditoria unificada: bitacora general + auditoria de login en
    // pestanas (antes eran dos items separados del menu). Observa el idioma para
    // traducir los titulos de las pestanas; cada UserControl interno se traduce solo.
    // Incluye la accion administrativa "Recalcular linea base" (T08): reestablece
    // los digitos verificadores tras una correccion de datos, solo para quien
    // tenga el permiso INTEGRIDAD_RECALC (el perfil Administrador lo trae).
    public class ucAuditoriaHub_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        private readonly TabControl _tabs_704ILR;
        private readonly TabPage _tabBitacora_704ILR;   // null si el usuario no tiene BITACORA_VER
        private readonly TabPage _tabLogin_704ILR;      // null si el usuario no tiene AUDIT_LOGIN_VER
        private readonly AppButton_704ILR _btnRecalc_704ILR;   // null si el usuario no tiene permiso

        public ucAuditoriaHub_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;

            _tabs_704ILR = new TabControl { Dock = DockStyle.Fill, Font = Theme_704ILR.FontBody_704ILR };

            // La seccion se abre con CUALQUIERA de los dos permisos, asi que cada
            // pestana se agrega solo si el usuario tiene el suyo: tener uno no
            // puede conceder el contenido del otro (la bitacora general y la
            // auditoria de login exponen datos distintos).
            if (Permisos_704ILR.Tiene_704ILR("BITACORA_VER"))
            {
                _tabBitacora_704ILR = new TabPage { BackColor = Theme_704ILR.BgContent_704ILR, Padding = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0), UseVisualStyleBackColor = true };
                _tabBitacora_704ILR.Controls.Add(new ucBitacora_704ILR { Dock = DockStyle.Fill });
                _tabs_704ILR.TabPages.Add(_tabBitacora_704ILR);
            }
            if (Permisos_704ILR.Tiene_704ILR("AUDIT_LOGIN_VER"))
            {
                _tabLogin_704ILR = new TabPage { BackColor = Theme_704ILR.BgContent_704ILR, Padding = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0), UseVisualStyleBackColor = true };
                _tabLogin_704ILR.Controls.Add(new ucAuditoria_704ILR { Dock = DockStyle.Fill });
                _tabs_704ILR.TabPages.Add(_tabLogin_704ILR);
            }
            Controls.Add(_tabs_704ILR);

            if (Permisos_704ILR.Tiene_704ILR("INTEGRIDAD_RECALC"))
            {
                var toolbar_704ILR = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Theme_704ILR.BgContent_704ILR };
                _btnRecalc_704ILR = Ui_704ILR.Primary_704ILR(T_704ILR("AUD_RECALC_BTN", "Recalcular linea base"));
                _btnRecalc_704ILR.BehindColor_704ILR = Theme_704ILR.BgContent_704ILR;
                _btnRecalc_704ILR.Size = new Size(240, 38);
                _btnRecalc_704ILR.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                _btnRecalc_704ILR.Location = new Point(toolbar_704ILR.Width - _btnRecalc_704ILR.Width - Theme_704ILR.SpaceLg_704ILR,
                                                toolbar_704ILR.Height - _btnRecalc_704ILR.Height - Theme_704ILR.SpaceSm_704ILR);
                _btnRecalc_704ILR.Click += (s_704ILR, e_704ILR) => RecalcularLineaBase_704ILR();
                toolbar_704ILR.Controls.Add(_btnRecalc_704ILR);
                Controls.Add(toolbar_704ILR);
            }

            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        // Proceso ante corrupcion (T08): el administrador corrige los datos (o
        // restaura una version) y desde aca reestablece la linea base de DV.
        private void RecalcularLineaBase_704ILR()
        {
            // Segunda capa: el boton solo se crea con permiso, pero la accion
            // vuelve a exigirlo antes de reescribir la linea base de integridad.
            if (!Permisos_704ILR.Exigir_704ILR("INTEGRIDAD_RECALC", FindForm(), "recalcular la linea base de DV")) return;

            var confirma_704ILR = MessageBox.Show(
                T_704ILR("AUD_RECALC_CONFIRMA",
                  "Recalcular los digitos verificadores de todas las reservas? Usar despues de corregir datos alterados: la linea base nueva pasa a ser la referencia de integridad."),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirma_704ILR != DialogResult.Yes) return;

            try
            {
                int total_704ILR = BLL_Integridad_704ILR.RecalcularTodo_704ILR();
                var resultado_704ILR = BLL_Integridad_704ILR.Verificar_704ILR();
                MessageBox.Show(
                    string.Format(T_704ILR("AUD_RECALC_OK",
                        "Linea base recalculada ({0} reservas). Verificacion posterior: {1} inconsistencia(s)."),
                        total_704ILR, resultado_704ILR.Inconsistencias_704ILR.Count),
                    "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Integridad", "Recalculo de linea base");
                MessageBox.Show(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, Tr_704ILR.T_704ILR("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        public void ActualizarTextos_704ILR()
        {
            if (_tabBitacora_704ILR != null) _tabBitacora_704ILR.Text = Tr_704ILR.T_704ILR("AUD_TAB_BITACORA");
            if (_tabLogin_704ILR != null) _tabLogin_704ILR.Text = Tr_704ILR.T_704ILR("AUD_TAB_LOGIN");
            if (_btnRecalc_704ILR != null) _btnRecalc_704ILR.Text = T_704ILR("AUD_RECALC_BTN", "Recalcular linea base");
        }
    }
}
