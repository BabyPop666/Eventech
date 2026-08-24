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
    internal static class LoginPrefs
    {
        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvenTech");
        private static string Archivo => Path.Combine(Dir, "login.cfg");

        public static bool Remember { get; private set; }
        public static string Username { get; private set; } = "";
        public static string Idioma { get; private set; } = "";

        public static void Load()
        {
            try
            {
                if (!File.Exists(Archivo)) return;
                var lineas = File.ReadAllLines(Archivo);
                Remember = lineas.Length > 0 && lineas[0].Trim() == "1";
                Username = (Remember && lineas.Length > 1) ? lineas[1] : "";
                Idioma   = lineas.Length > 2 ? lineas[2].Trim() : "";
            }
            catch { /* preferencia no critica: si falla, se ignora */ }
        }

        // Guarda la preferencia de cuenta preservando el idioma ya elegido.
        public static void Save(bool remember, string username)
        {
            Remember = remember;
            Username = remember ? (username ?? "") : "";
            Escribir();
        }

        // Guarda el idioma preservando la preferencia de cuenta.
        public static void GuardarIdioma(string codigo)
        {
            Idioma = codigo ?? "";
            Escribir();
        }

        private static void Escribir()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllLines(Archivo, new[] { Remember ? "1" : "0", Username, Idioma });
            }
            catch { /* preferencia no critica */ }
        }
    }
}
