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
    }
}
