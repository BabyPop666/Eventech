using System.Windows.Forms;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Seccion de Auditoria unificada: bitacora general + auditoria de login en
    // pestanas (antes eran dos items separados del menu). Observa el idioma para
    // traducir los titulos de las pestanas; cada UserControl interno se traduce solo.
    public class ucAuditoriaHub : UserControl, IObservadorIdioma
    {
        private readonly TabControl _tabs;
        private readonly TabPage _tabBitacora;
        private readonly TabPage _tabLogin;

        public ucAuditoriaHub()
        {
            BackColor = Theme.BgContent;

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBody };
            _tabBitacora = new TabPage { BackColor = Theme.BgContent, Padding = new Padding(0, Theme.SpaceSm, 0, 0), UseVisualStyleBackColor = true };
            _tabLogin = new TabPage { BackColor = Theme.BgContent, Padding = new Padding(0, Theme.SpaceSm, 0, 0), UseVisualStyleBackColor = true };

            _tabBitacora.Controls.Add(new ucBitacora { Dock = DockStyle.Fill });
            _tabLogin.Controls.Add(new ucAuditoria { Dock = DockStyle.Fill });

            // Cada pestana exige su permiso: con solo BITACORA_VER no debe verse la
            // auditoria de login (y viceversa). Antes se mostraban ambas siempre.
            bool verBitacora = !SessionManager.IsSessionActive || SessionManager.GetInstance.TienePermiso("BITACORA_VER");
            bool verLogin = !SessionManager.IsSessionActive || SessionManager.GetInstance.TienePermiso("AUDIT_LOGIN_VER");
            if (verBitacora) _tabs.TabPages.Add(_tabBitacora);
            if (verLogin) _tabs.TabPages.Add(_tabLogin);
            Controls.Add(_tabs);

            ActualizarTextos();
            Load += (s, e) => GestorDeIdioma.GetInstance.Suscribir(this);
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        public void ActualizarTextos()
        {
            _tabBitacora.Text = Tr.T("AUD_TAB_BITACORA");
            _tabLogin.Text = Tr.T("AUD_TAB_LOGIN");
        }
    }
}
