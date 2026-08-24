using System;
using System.Windows.Forms;
using EvenTech.Services;

namespace EvenTech.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Verificacion de conectividad ANTES que nada: sin base no hay idiomas,
            // ni verificacion de integridad, ni login. Si falla, se ofrece
            // configurar la instancia y se reinicia con la cadena nueva.
            if (!AsegurarConexion()) return;

            // Carga de idiomas/traducciones desde la base hacia el GestorDeIdioma
            // (patron Observer). Si falla, la app sigue con las claves por defecto.
            try { EvenTech.BLL.BLL_Idioma.Inicializar(); } catch { }

            // Idioma recordado de la sesion anterior (preferencia de la estacion).
            AplicarIdiomaGuardado();

            // Verificacion de integridad (digitos verificadores) ANTES del login.
            // Si hay inconsistencias, se muestra la alerta y se registra en bitacora.
            try
            {
                var integridad = EvenTech.BLL.BLL_Integridad.Verificar();
                if (!integridad.Ok)
                {
                    using (var alerta = new frmAlertaIntegridad(integridad.Inconsistencias))
                        alerta.ShowDialog();
                }
            }
            catch { /* si la verificacion no puede correr, no bloquear el arranque */ }

            // El loop principal vive en frmLogin: al validar credenciales abre
            // frmMain modal y al volver del logout queda esperando otro login.
            // El "✕" del frmLogin termina la app.
            Application.Run(new frmLogin());
        }

        // Devuelve true si hay conexion utilizable. Si no la hay, abre la pantalla
        // de configuracion; cuando el usuario guarda una que conecta, la app se
        // reinicia para que todas las capas tomen la cadena nueva desde cero.
        private static bool AsegurarConexion()
        {
            if (EvenTech.BLL.BLL_Conexion.VerificarActual(out string mensaje)) return true;

            using (var cfg = new frmConfiguracionConexion(mensaje))
            {
                if (cfg.ShowDialog() != DialogResult.OK || !cfg.Configurada)
                    return false;    // el usuario decidio salir sin configurar
            }

            Application.Restart();
            return false;            // este proceso termina; sigue el reiniciado
        }

        private static void AplicarIdiomaGuardado()
        {
            try
            {
                LoginPrefs.Load();
                if (!string.IsNullOrWhiteSpace(LoginPrefs.Idioma))
                    GestorDeIdioma.GetInstance.CambiarIdioma(LoginPrefs.Idioma);
            }
            catch { /* preferencia no critica: se arranca en el idioma por defecto */ }
        }
    }
}
