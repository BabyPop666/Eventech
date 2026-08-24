using System;
using System.IO;

namespace EvenTech.UI
{
    // Preferencias locales de la estacion de trabajo, en %APPDATA%\EvenTech\login.cfg.
    // Guarda el "Recordar cuenta" (solo el nombre de usuario, NUNCA la contrasena)
    // y el ultimo idioma elegido, para que la app arranque en ese idioma.
    //
    // Formato (una linea por campo, tolerante a archivos viejos mas cortos):
    //   [0] "1"/"0"  -> recordar cuenta
    //   [1] username -> vacio si no se recuerda
    //   [2] idioma   -> codigo del idioma (ES/EN/PT/...)
    internal static class LoginPrefs_704ILR
    {
        private static string Dir_704ILR =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvenTech");
        private static string Archivo_704ILR => Path.Combine(Dir_704ILR, "login.cfg");

        public static bool Remember_704ILR { get; private set; }
        public static string Username_704ILR { get; private set; } = "";
        public static string Idioma_704ILR { get; private set; } = "";

        public static void Load_704ILR()
        {
            try
            {
                if (!File.Exists(Archivo_704ILR)) return;
                var lineas_704ILR = File.ReadAllLines(Archivo_704ILR);
                Remember_704ILR = lineas_704ILR.Length > 0 && lineas_704ILR[0].Trim() == "1";
                Username_704ILR = (Remember_704ILR && lineas_704ILR.Length > 1) ? lineas_704ILR[1] : "";
                Idioma_704ILR   = lineas_704ILR.Length > 2 ? lineas_704ILR[2].Trim() : "";
            }
            catch { /* preferencia no critica: si falla, se ignora */ }
        }

        // Guarda la preferencia de cuenta preservando el idioma ya elegido.
        public static void Save_704ILR(bool remember_704ILR, string username_704ILR)
        {
            Remember_704ILR = remember_704ILR;
            Username_704ILR = remember_704ILR ? (username_704ILR ?? "") : "";
            Escribir_704ILR();
        }

        // Guarda el idioma preservando la preferencia de cuenta.
        public static void GuardarIdioma_704ILR(string codigo_704ILR)
        {
            Idioma_704ILR = codigo_704ILR ?? "";
            Escribir_704ILR();
        }

        private static void Escribir_704ILR()
        {
            try
            {
                Directory.CreateDirectory(Dir_704ILR);
                File.WriteAllLines(Archivo_704ILR, new[] { Remember_704ILR ? "1" : "0", Username_704ILR, Idioma_704ILR });
            }
            catch { /* preferencia no critica */ }
        }
    }
}
