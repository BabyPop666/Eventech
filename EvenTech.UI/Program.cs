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
            // El loop principal vive en frmLogin: al validar credenciales abre
            // frmMain modal y al volver del logout queda esperando otro login.
            // El "✕" del frmLogin termina la app.
            Application.Run(new frmLogin());
        }
    }
}
