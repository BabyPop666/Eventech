using System;
using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum LoginResult_704ILR
    {
        Success_704ILR,
        UserNotFound_704ILR,
        IncorrectPassword_704ILR,
        UserBlocked_704ILR,        // cuenta bloqueada por intentos fallidos (RF01.3)
        AccountInactive_704ILR     // cuenta dada de baja / inactiva (RF01.4)
    }

    // Resultado del login con info para que la UI muestre los intentos restantes.
    public class LoginResponse_704ILR
    {
        public LoginResult_704ILR Result_704ILR { get; set; }
        public int FailedAttempts_704ILR { get; set; }
        public int MaxAttempts_704ILR { get; set; } = BLL_Login_704ILR.MaxIntentos_704ILR;
        public override string ToString() => Result_704ILR.ToString();
    }

    // Autenticacion. El password llega ya hasheado desde la UI: el plain text
    // nunca viaja a la BLL ni a la DB. Controla estado de cuenta e intentos
    // fallidos: tras MaxIntentos fallos la cuenta queda bloqueada (RF01.3/RF01.4).
    public static class BLL_Login_704ILR
    {
        public const int MaxIntentos_704ILR = 3;

        public static LoginResponse_704ILR Authenticate_704ILR(string username_704ILR, string hashedPassword_704ILR)
        {
            var resp_704ILR = new LoginResponse_704ILR { MaxAttempts_704ILR = MaxIntentos_704ILR };

            BE_User_704ILR user_704ILR = DAL_User_704ILR.GetByUsername_704ILR(username_704ILR);
            if (user_704ILR == null)
            {
                BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_FAIL, "Usuario inexistente");
                resp_704ILR.Result_704ILR = LoginResult_704ILR.UserNotFound_704ILR;
                return resp_704ILR;
            }

            // RF01.4 - estado de cuenta
            if (!user_704ILR.Activo_704ILR)
            {
                BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_FAIL, "Cuenta inactiva");
                resp_704ILR.Result_704ILR = LoginResult_704ILR.AccountInactive_704ILR;
                resp_704ILR.FailedAttempts_704ILR = user_704ILR.FailedAttempts_704ILR;
                return resp_704ILR;
            }

            // RF01.3 - cuenta ya bloqueada (o que llego al maximo sin marcarse)
            if (user_704ILR.Blocked_704ILR || user_704ILR.FailedAttempts_704ILR >= MaxIntentos_704ILR)
            {
                if (!user_704ILR.Blocked_704ILR) DAL_User_704ILR.SetBlocked_704ILR(username_704ILR, true);
                BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_FAIL, "Intento sobre cuenta bloqueada");
                resp_704ILR.Result_704ILR = LoginResult_704ILR.UserBlocked_704ILR;
                resp_704ILR.FailedAttempts_704ILR = Math.Max(user_704ILR.FailedAttempts_704ILR, MaxIntentos_704ILR);
                return resp_704ILR;
            }

            // RF01.2 - verificacion segura de contrasena
            if (!AuthenticationService_704ILR.CompareHashedPasswords_704ILR(hashedPassword_704ILR, user_704ILR.PasswordHash_704ILR))
            {
                int intentos_704ILR = DAL_User_704ILR.IncrementFailedAttempts_704ILR(username_704ILR);
                resp_704ILR.FailedAttempts_704ILR = intentos_704ILR;

                if (intentos_704ILR >= MaxIntentos_704ILR)
                {
                    DAL_User_704ILR.SetBlocked_704ILR(username_704ILR, true);
                    BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_FAIL,
                        $"Password incorrecto - cuenta bloqueada ({intentos_704ILR}/{MaxIntentos_704ILR})");
                    resp_704ILR.Result_704ILR = LoginResult_704ILR.UserBlocked_704ILR;
                    return resp_704ILR;
                }

                BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_FAIL,
                    $"Password incorrecto (intento {intentos_704ILR}/{MaxIntentos_704ILR})");
                resp_704ILR.Result_704ILR = LoginResult_704ILR.IncorrectPassword_704ILR;
                return resp_704ILR;
            }

            // Credenciales OK: resetear intentos y abrir sesion.
            DAL_User_704ILR.ResetFailedAttempts_704ILR(username_704ILR);

            // Permisos efectivos del perfil (Composite) hacia la sesion.
            HashSet<string> permisos_704ILR = null;
            bool sinPerfil_704ILR = !user_704ILR.PerfilId_704ILR.HasValue;
            bool permisosNoDisponibles_704ILR = false;
            if (user_704ILR.PerfilId_704ILR.HasValue)
            {
                try
                {
                    // Perfil compuesto (Composite): incluye los permisos heredados
                    // de los perfiles contenidos dentro del perfil del usuario.
                    permisos_704ILR = new HashSet<string>(
                        BLL_Perfil_704ILR.GetPermisosEfectivosDePerfil_704ILR(user_704ILR.PerfilId_704ILR.Value).Select(p_704ILR => p_704ILR.Clave_704ILR),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex_704ILR)
                {
                    // Denegar por defecto: si los permisos no se pueden resolver la
                    // sesion arranca SIN ninguno. Conceder acceso total ante el fallo
                    // convertiria una caida de base en una escalada de privilegios.
                    permisosNoDisponibles_704ILR = true;
                    permisos_704ILR = null;
                    BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Login", "Carga de permisos del perfil");
                    BLL_Bitacora_704ILR.Registrar_704ILR("Seguridad", "Permisos no disponibles", CriticidadBitacora_704ILR.Error,
                        $"No se pudieron resolver los permisos del perfil #{user_704ILR.PerfilId_704ILR.Value} de '{username_704ILR}': " +
                        "la sesion queda sin permisos hasta que se restablezca el acceso.");
                }
            }

            SessionManager_704ILR.Login_704ILR(user_704ILR, permisos_704ILR, permisosNoDisponibles_704ILR, sinPerfil_704ILR);
            BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGIN_OK, "Ingreso correcto");
            resp_704ILR.Result_704ILR = LoginResult_704ILR.Success_704ILR;
            return resp_704ILR;
        }

        public static void Logout_704ILR()
        {
            if (!SessionManager_704ILR.IsSessionActive_704ILR) return;

            string username_704ILR = SessionManager_704ILR.GetInstance_704ILR.User_704ILR.Username_704ILR;
            SessionManager_704ILR.Logout_704ILR();
            BLL_LoginAudit_704ILR.Register_704ILR(username_704ILR, LoginAuditAction_704ILR.LOGOUT, "Cierre de sesion");
        }
    }
}
