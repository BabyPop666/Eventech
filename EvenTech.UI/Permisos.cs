using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Segunda capa del control de acceso (T04).
    //
    // Ocultar un item del menu segun el perfil es solo la primera capa: es una
    // ayuda visual, no una barrera. Toda accion sensible vuelve a exigir su
    // permiso justo antes de ejecutarse, de modo que una vista alcanzada por
    // otra via (atajo, reuso del control, dialogo abierto desde otra pantalla)
    // no pueda operar sin autorizacion.
    //
    // Todo rechazo se registra en la bitacora con criticidad Advertencia: un
    // intento de operar sin permiso es informacion de seguridad, no ruido.
    internal static class Permisos
    {
        // Consulta silenciosa: no avisa ni registra. Para decidir si mostrar o
        // habilitar un control.
        public static bool Tiene(string clave)
        {
            try
            {
                return SessionManager.IsSessionActive &&
                       SessionManager.GetInstance.TienePermiso(clave);
            }
            catch
            {
                return false;   // sin sesion resoluble no hay permiso
            }
        }

        public static bool TieneAlguno(params string[] claves)
        {
            if (claves == null) return false;
            foreach (var c in claves)
                if (Tiene(c)) return true;
            return false;
        }

        // Exige el permiso antes de ejecutar una accion. Si falta, avisa al
        // usuario, lo deja en la bitacora y devuelve false para cortar el flujo.
        public static bool Exigir(string clave, IWin32Window owner = null, string accion = null)
        {
            if (Tiene(clave)) return true;
            Denegar(clave, accion, owner);
            return false;
        }

        // Variante para acciones que se habilitan con cualquiera de varios permisos.
        public static bool ExigirAlguno(IWin32Window owner, string accion, params string[] claves)
        {
            if (TieneAlguno(claves)) return true;
            Denegar(claves == null || claves.Length == 0 ? "?" : string.Join(" | ", claves), accion, owner);
            return false;
        }

        private static void Denegar(string clave, string accion, IWin32Window owner)
        {
            string detalle = "Permiso faltante: " + clave +
                             (string.IsNullOrEmpty(accion) ? "" : " | Accion: " + accion);
            BLL_Bitacora.Registrar("Seguridad", "Acceso denegado", CriticidadBitacora.Advertencia, detalle);

            string msg = T("MSG_SIN_PERMISO", "No tenes permiso para realizar esta accion.");
            if (owner != null)
                MessageBox.Show(owner, msg, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(msg, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
