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

            SessionManager.Login(user);
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
