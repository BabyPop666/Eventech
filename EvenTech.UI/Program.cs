using System;
using System.Windows.Forms;

namespace EvenTech.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Red de seguridad global: cualquier excepcion no controlada (p.ej. una
            // SqlException por caida de conexion) se registra en bitacora y se muestra
            // al usuario, en vez de cerrar la app de golpe sin dejar rastro.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ManejarExcepcion(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ManejarExcepcion(e.ExceptionObject as Exception);

            // Carga de idiomas/traducciones desde la base hacia el GestorDeIdioma
            // (patron Observer). Si falla, la app sigue con las claves por defecto.
            try { EvenTech.BLL.BLL_Idioma.Inicializar(); } catch { }

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

        private static void ManejarExcepcion(Exception ex)
        {
            if (ex == null) return;
            try { EvenTech.BLL.BLL_Bitacora.RegistrarExcepcion(ex, "App", "Excepcion no controlada"); } catch { }
            try
            {
                MessageBox.Show(
                    "Ocurrio un error inesperado y la operacion no pudo completarse.\n\n" + ex.Message,
                    "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
