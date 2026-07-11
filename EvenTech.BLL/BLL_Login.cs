using System;
using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum LoginResult
    {
        Success,
        UserNotFound,
        IncorrectPassword,
        UserBlocked,        // cuenta bloqueada por intentos fallidos (RF01.3)
        AccountInactive     // cuenta dada de baja / inactiva (RF01.4)
    }

    // Resultado del login con info para que la UI muestre los intentos restantes.
    public class LoginResponse
    {
        public LoginResult Result { get; set; }
        public int FailedAttempts { get; set; }
        public int MaxAttempts { get; set; } = BLL_Login.MaxIntentos;
        public override string ToString() => Result.ToString();
    }

    // Autenticacion. El password llega ya hasheado desde la UI: el plain text
    // nunca viaja a la BLL ni a la DB. Controla estado de cuenta e intentos
    // fallidos: tras MaxIntentos fallos la cuenta queda bloqueada (RF01.3/RF01.4).
    public static class BLL_Login
    {
        public const int MaxIntentos = 3;

        public static LoginResponse Authenticate(string username, string hashedPassword)
        {
            var resp = new LoginResponse { MaxAttempts = MaxIntentos };

            BE_User user = DAL_User.GetByUsername(username);
            if (user == null)
            {
                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL, "Usuario inexistente");
                resp.Result = LoginResult.UserNotFound;
                return resp;
            }

            // RF01.4 - estado de cuenta
            if (!user.Activo)
            {
                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL, "Cuenta inactiva");
                resp.Result = LoginResult.AccountInactive;
                resp.FailedAttempts = user.FailedAttempts;
                return resp;
            }

            // RF01.3 - cuenta ya bloqueada (o que llego al maximo sin marcarse)
            if (user.Blocked || user.FailedAttempts >= MaxIntentos)
            {
                if (!user.Blocked) DAL_User.SetBlocked(username, true);
                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL, "Intento sobre cuenta bloqueada");
                resp.Result = LoginResult.UserBlocked;
                resp.FailedAttempts = Math.Max(user.FailedAttempts, MaxIntentos);
                return resp;
            }

            // RF01.2 - verificacion segura de contrasena
            if (!AuthenticationService.CompareHashedPasswords(hashedPassword, user.PasswordHash))
            {
                int intentos = DAL_User.IncrementFailedAttempts(username);
                resp.FailedAttempts = intentos;

                if (intentos >= MaxIntentos)
                {
                    DAL_User.SetBlocked(username, true);
                    BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL,
                        $"Password incorrecto - cuenta bloqueada ({intentos}/{MaxIntentos})");
                    resp.Result = LoginResult.UserBlocked;
                    return resp;
                }

                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL,
                    $"Password incorrecto (intento {intentos}/{MaxIntentos})");
                resp.Result = LoginResult.IncorrectPassword;
                return resp;
            }

            // Credenciales OK: resetear intentos y abrir sesion.
            DAL_User.ResetFailedAttempts(username);

            // Permisos efectivos del perfil (Composite) hacia la sesion.
            HashSet<string> permisos = null;
            bool sinPerfil = !user.PerfilId.HasValue;
            bool accesoTotal = false;
            if (user.PerfilId.HasValue)
            {
                try
                {
                    // Perfil compuesto (Composite): incluye los permisos heredados
                    // de los perfiles contenidos dentro del perfil del usuario.
                    permisos = new HashSet<string>(
                        BLL_Perfil.GetPermisosEfectivosDePerfil(user.PerfilId.Value).Select(p => p.Clave),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    accesoTotal = true;
                    BLL_Bitacora.RegistrarExcepcion(ex, "Login", "Carga de permisos del perfil");
                }
            }

            SessionManager.Login(user, permisos, accesoTotal, sinPerfil);
            BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_OK, "Ingreso correcto");
            resp.Result = LoginResult.Success;
            return resp;
        }

        public static void Logout()
        {
            if (!SessionManager.IsSessionActive) return;

            string username = SessionManager.GetInstance.User.Username;
            SessionManager.Logout();
            BLL_LoginAudit.Register(username, LoginAuditAction.LOGOUT, "Cierre de sesion");
        }
    }
}
