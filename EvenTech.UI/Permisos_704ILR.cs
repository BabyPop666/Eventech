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
    internal static class Permisos_704ILR
    {
        // Consulta silenciosa: no avisa ni registra. Para decidir si mostrar o
        // habilitar un control.
        public static bool Tiene_704ILR(string clave_704ILR)
        {
            try
            {
                return SessionManager_704ILR.IsSessionActive_704ILR &&
                       SessionManager_704ILR.GetInstance_704ILR.TienePermiso_704ILR(clave_704ILR);
            }
            catch
            {
                return false;   // sin sesion resoluble no hay permiso
            }
        }

        public static bool TieneAlguno_704ILR(params string[] claves_704ILR)
        {
            if (claves_704ILR == null) return false;
            foreach (var c_704ILR in claves_704ILR)
                if (Tiene_704ILR(c_704ILR)) return true;
            return false;
        }

        // Exige el permiso antes de ejecutar una accion. Si falta, avisa al
        // usuario, lo deja en la bitacora y devuelve false para cortar el flujo.
        public static bool Exigir_704ILR(string clave_704ILR, IWin32Window owner_704ILR = null, string accion_704ILR = null)
        {
            if (Tiene_704ILR(clave_704ILR)) return true;
            Denegar_704ILR(clave_704ILR, accion_704ILR, owner_704ILR);
            return false;
        }

        // Variante para acciones que se habilitan con cualquiera de varios permisos.
        public static bool ExigirAlguno_704ILR(IWin32Window owner_704ILR, string accion_704ILR, params string[] claves_704ILR)
        {
            if (TieneAlguno_704ILR(claves_704ILR)) return true;
            Denegar_704ILR(claves_704ILR == null || claves_704ILR.Length == 0 ? "?" : string.Join(" | ", claves_704ILR), accion_704ILR, owner_704ILR);
            return false;
        }

        private static void Denegar_704ILR(string clave_704ILR, string accion_704ILR, IWin32Window owner_704ILR)
        {
            string detalle_704ILR = "Permiso faltante: " + clave_704ILR +
                             (string.IsNullOrEmpty(accion_704ILR) ? "" : " | Accion: " + accion_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Seguridad", "Acceso denegado", CriticidadBitacora_704ILR.Advertencia, detalle_704ILR);

            string msg_704ILR = T_704ILR("MSG_SIN_PERMISO", "No tenes permiso para realizar esta accion.");
            if (owner_704ILR != null)
                MessageBox.Show(owner_704ILR, msg_704ILR, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(msg_704ILR, "EvenTech", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
