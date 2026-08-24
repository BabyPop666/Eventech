using System;
using System.Windows.Forms;
using EvenTech.Services;

namespace EvenTech.UI
{
    internal static class Program_704ILR
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Verificacion de conectividad ANTES que nada: sin base no hay idiomas,
            // ni verificacion de integridad, ni login. Si falla, se ofrece
            // configurar la instancia y se reinicia con la cadena nueva.
            if (!AsegurarConexion_704ILR()) return;

            // Carga de idiomas/traducciones desde la base hacia el GestorDeIdioma
            // (patron Observer). Si falla, la app sigue con las claves por defecto.
            try { EvenTech.BLL.BLL_Idioma_704ILR.Inicializar_704ILR(); } catch { }

            // Idioma recordado de la sesion anterior (preferencia de la estacion).
            AplicarIdiomaGuardado_704ILR();

            // Verificacion de integridad (digitos verificadores) ANTES del login.
            // Si hay inconsistencias, se muestra la alerta y se registra en bitacora.
            try
            {
                var integridad_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
                if (!integridad_704ILR.Ok_704ILR)
                {
                    using (var alerta_704ILR = new frmAlertaIntegridad_704ILR(integridad_704ILR.Inconsistencias_704ILR))
                        alerta_704ILR.ShowDialog();
                }
            }
            catch { /* si la verificacion no puede correr, no bloquear el arranque */ }

            // El loop principal vive en frmLogin: al validar credenciales abre
            // frmMain modal y al volver del logout queda esperando otro login.
            // El "✕" del frmLogin termina la app.
            Application.Run(new frmLogin_704ILR());
        }

        // Devuelve true si hay conexion utilizable. Si no la hay, abre la pantalla
        // de configuracion; cuando el usuario guarda una que conecta, la app se
        // reinicia para que todas las capas tomen la cadena nueva desde cero.
        private static bool AsegurarConexion_704ILR()
        {
            if (EvenTech.BLL.BLL_Conexion_704ILR.VerificarActual_704ILR(out string mensaje_704ILR)) return true;

            using (var cfg_704ILR = new frmConfiguracionConexion_704ILR(mensaje_704ILR))
            {
                if (cfg_704ILR.ShowDialog() != DialogResult.OK || !cfg_704ILR.Configurada_704ILR)
                    return false;    // el usuario decidio salir sin configurar
            }

            Application.Restart();
            return false;            // este proceso termina; sigue el reiniciado
        }

        private static void AplicarIdiomaGuardado_704ILR()
        {
            try
            {
                LoginPrefs_704ILR.Load_704ILR();
                if (!string.IsNullOrWhiteSpace(LoginPrefs_704ILR.Idioma_704ILR))
                    GestorDeIdioma_704ILR.GetInstance_704ILR.CambiarIdioma_704ILR(LoginPrefs_704ILR.Idioma_704ILR);
            }
            catch { /* preferencia no critica: se arranca en el idioma por defecto */ }
        }
    }
}
