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
    public class ucAuditoriaHub : UserControl, IObservadorIdioma
    {
        private readonly TabControl _tabs;
        private readonly TabPage _tabBitacora;   // null si el usuario no tiene BITACORA_VER
        private readonly TabPage _tabLogin;      // null si el usuario no tiene AUDIT_LOGIN_VER
        private readonly AppButton _btnRecalc;   // null si el usuario no tiene permiso

        public ucAuditoriaHub()
        {
            BackColor = Theme.BgContent;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBody };

            // La seccion se abre con CUALQUIERA de los dos permisos, asi que cada
            // pestana se agrega solo si el usuario tiene el suyo: tener uno no
            // puede conceder el contenido del otro (la bitacora general y la
            // auditoria de login exponen datos distintos).
            if (Permisos.Tiene("BITACORA_VER"))
            {
                _tabBitacora = new TabPage { BackColor = Theme.BgContent, Padding = new Padding(0, Theme.SpaceSm, 0, 0), UseVisualStyleBackColor = true };
                _tabBitacora.Controls.Add(new ucBitacora { Dock = DockStyle.Fill });
                _tabs.TabPages.Add(_tabBitacora);
            }
            if (Permisos.Tiene("AUDIT_LOGIN_VER"))
            {
                _tabLogin = new TabPage { BackColor = Theme.BgContent, Padding = new Padding(0, Theme.SpaceSm, 0, 0), UseVisualStyleBackColor = true };
                _tabLogin.Controls.Add(new ucAuditoria { Dock = DockStyle.Fill });
                _tabs.TabPages.Add(_tabLogin);
            }
            Controls.Add(_tabs);

            if (Permisos.Tiene("INTEGRIDAD_RECALC"))
            {
                var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Theme.BgContent };
                _btnRecalc = Ui.Primary(T("AUD_RECALC_BTN", "Recalcular linea base"));
                _btnRecalc.BehindColor = Theme.BgContent;
                _btnRecalc.Size = new Size(240, 38);
                _btnRecalc.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                _btnRecalc.Location = new Point(toolbar.Width - _btnRecalc.Width - Theme.SpaceLg,
                                                toolbar.Height - _btnRecalc.Height - Theme.SpaceSm);
                _btnRecalc.Click += (s, e) => RecalcularLineaBase();
                toolbar.Controls.Add(_btnRecalc);
                Controls.Add(toolbar);
            }

            ActualizarTextos();
            Load += (s, e) => GestorDeIdioma.GetInstance.Suscribir(this);
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        // Proceso ante corrupcion (T08): el administrador corrige los datos (o
        // restaura una version) y desde aca reestablece la linea base de DV.
        private void RecalcularLineaBase()
        {
            // Segunda capa: el boton solo se crea con permiso, pero la accion
            // vuelve a exigirlo antes de reescribir la linea base de integridad.
            if (!Permisos.Exigir("INTEGRIDAD_RECALC", FindForm(), "recalcular la linea base de DV")) return;

            var confirma = MessageBox.Show(
                T("AUD_RECALC_CONFIRMA",
                  "Recalcular los digitos verificadores de todas las reservas? Usar despues de corregir datos alterados: la linea base nueva pasa a ser la referencia de integridad."),
                "EvenTech", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirma != DialogResult.Yes) return;

            try
            {
                int total = BLL_Integridad.RecalcularTodo();
                var resultado = BLL_Integridad.Verificar();
                MessageBox.Show(
                    string.Format(T("AUD_RECALC_OK",
                        "Linea base recalculada ({0} reservas). Verificacion posterior: {1} inconsistencia(s)."),
                        total, resultado.Inconsistencias.Count),
                    "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Integridad", "Recalculo de linea base");
                MessageBox.Show(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, Tr.T("MSG_ERROR"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        public void ActualizarTextos()
        {
            if (_tabBitacora != null) _tabBitacora.Text = Tr.T("AUD_TAB_BITACORA");
            if (_tabLogin != null) _tabLogin.Text = Tr.T("AUD_TAB_LOGIN");
            if (_btnRecalc != null) _btnRecalc.Text = T("AUD_RECALC_BTN", "Recalcular linea base");
        }
    }
}
