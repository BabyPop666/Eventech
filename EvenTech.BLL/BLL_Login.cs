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
        IncorrectPassword
    }

    // Autenticacion. El password llega ya hasheado desde la UI: el plain text
    // nunca viaja a la BLL ni a la DB.
    public static class BLL_Login
    {
        public static LoginResult Authenticate(string username, string hashedPassword)
        {
            BE_User user = DAL_User.GetByUsername(username);

            if (user == null)
            {
                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL, "Usuario inexistente");
                return LoginResult.UserNotFound;
            }

            if (!AuthenticationService.CompareHashedPasswords(hashedPassword, user.PasswordHash))
            {
                BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_FAIL, "Password incorrecto");
                return LoginResult.IncorrectPassword;
            }

            // Cargar permisos efectivos del perfil (Composite) hacia la sesion.
            // Sin perfil => SIN acceso (la UI lo bloquea y pide contactar al admin).
            HashSet<string> permisos = null;
            bool sinPerfil = !user.PerfilId.HasValue;
            bool accesoTotal = false;
            if (user.PerfilId.HasValue)
            {
                try
                {
                    var arbol = BLL_Perfil.GetArbolPermisos();
                    var asignados = BLL_Perfil.GetPermisosAsignados(user.PerfilId.Value);
                    permisos = new HashSet<string>(
                        BLL_Perfil.CalcularPermisosEfectivos(arbol, asignados).Select(p => p.Clave),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    // Fallo al cargar permisos de un perfil real: no lo bloqueamos
                    // (acceso total) para no dejar afuera a un admin por error transitorio.
                    accesoTotal = true;
                    BLL_Bitacora.RegistrarExcepcion(ex, "Login", "Carga de permisos del perfil");
                }
            }

            SessionManager.Login(user, permisos, accesoTotal, sinPerfil);
            BLL_LoginAudit.Register(username, LoginAuditAction.LOGIN_OK, "Ingreso correcto");
            return LoginResult.Success;
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
