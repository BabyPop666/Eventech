using System;
using System.IO;

namespace EvenTech.UI
{
    // Persistencia simple de la preferencia "Recordar cuenta". Guarda solo el
    // nombre de usuario (NUNCA la contrasena) en %APPDATA%\EvenTech\login.cfg.
    internal static class LoginPrefs
    {
        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvenTech");
        private static string Archivo => Path.Combine(Dir, "login.cfg");

        public static bool Remember { get; private set; }
        public static string Username { get; private set; } = "";

        public static void Load()
        {
            try
            {
                if (!File.Exists(Archivo)) return;
                var lineas = File.ReadAllLines(Archivo);
                Remember = lineas.Length > 0 && lineas[0].Trim() == "1";
                Username = (Remember && lineas.Length > 1) ? lineas[1] : "";
            }
            catch { /* preferencia no critica: si falla, se ignora */ }
        }

        public static void Save(bool remember, string username)
        {
            Remember = remember;
            Username = remember ? (username ?? "") : "";
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllLines(Archivo, new[] { remember ? "1" : "0", Username });
            }
            catch { /* preferencia no critica */ }
        }
    }
}
