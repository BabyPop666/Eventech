using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum CreateUserResult
    {
        Success,
        InvalidUsername,
        UsernameAlreadyExists,
        InvalidPassword
    }

    // Alta de usuarios. La password en claro nunca llega aca: la UI manda solo
    // el hash SHA-256. Se valida formato de username y se delega al DAL.
    public static class BLL_User
    {
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_\.\-]{3,50}$");

        public static CreateUserResult CreateUser(string username, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || !UsernameRegex.IsMatch(username))
                return CreateUserResult.InvalidUsername;

            if (string.IsNullOrEmpty(hashedPassword) || hashedPassword.Length != 64)
                return CreateUserResult.InvalidPassword;

            if (DAL_User.ExistsUsername(username))
                return CreateUserResult.UsernameAlreadyExists;

            DAL_User.Insert(username, hashedPassword);
            return CreateUserResult.Success;
        }

        // --- Asignacion de perfiles (T04) ---

        public static List<BE_User> GetAll() => DAL_User.GetAll();

        public static void AsignarPerfil(int userId, int? perfilId)
        {
            DAL_User.SetPerfil(userId, perfilId);
            BLL_Bitacora.Registrar("Perfiles", "Asignacion de perfil", CriticidadBitacora.Info,
                $"Usuario #{userId} -> perfil {(perfilId.HasValue ? "#" + perfilId.Value : "(ninguno)")}");
        }

        // Desbloqueo de cuenta por un administrador (RF01.3): quita el bloqueo y
        // resetea el contador de intentos fallidos.
        public static void Desbloquear(int userId)
        {
            DAL_User.Desbloquear(userId);
            BLL_Bitacora.Registrar("Usuarios", "Desbloqueo de cuenta", CriticidadBitacora.Info,
                $"Usuario #{userId} desbloqueado");
        }
    }
}
