using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum CreateUserResult_704ILR
    {
        Success,
        InvalidUsername,
        UsernameAlreadyExists,
        InvalidPassword
    }

    // Alta de usuarios. La password en claro nunca llega aca: la UI manda solo
    // el hash SHA-256. Se valida formato de username y se delega al DAL.
    public static class BLL_User_704ILR
    {
        private static readonly Regex UsernameRegex_704ILR = new Regex(@"^[a-zA-Z0-9_\.\-]{3,50}$");

        public static CreateUserResult_704ILR CreateUser_704ILR(string username_704ILR, string hashedPassword_704ILR)
        {
            if (string.IsNullOrWhiteSpace(username_704ILR) || !UsernameRegex_704ILR.IsMatch(username_704ILR))
                return CreateUserResult_704ILR.InvalidUsername;

            if (string.IsNullOrEmpty(hashedPassword_704ILR) || hashedPassword_704ILR.Length != 64)
                return CreateUserResult_704ILR.InvalidPassword;

            if (DAL_User_704ILR.ExistsUsername_704ILR(username_704ILR))
                return CreateUserResult_704ILR.UsernameAlreadyExists;

            DAL_User_704ILR.Insert_704ILR(username_704ILR, hashedPassword_704ILR);
            return CreateUserResult_704ILR.Success;
        }

        // --- Asignacion de perfiles (T04) ---

        public static List<BE_User_704ILR> GetAll_704ILR() => DAL_User_704ILR.GetAll_704ILR();

        public static void AsignarPerfil_704ILR(int userId_704ILR, int? perfilId_704ILR)
        {
            DAL_User_704ILR.SetPerfil_704ILR(userId_704ILR, perfilId_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Perfiles", "Asignacion de perfil", CriticidadBitacora_704ILR.Info,
                $"Usuario #{userId_704ILR} -> perfil {(perfilId_704ILR.HasValue ? "#" + perfilId_704ILR.Value : "(ninguno)")}");
        }

        // Desbloqueo de cuenta por un administrador (RF01.3): quita el bloqueo y
        // resetea el contador de intentos fallidos.
        public static void Desbloquear_704ILR(int userId_704ILR)
        {
            DAL_User_704ILR.Desbloquear_704ILR(userId_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Usuarios", "Desbloqueo de cuenta", CriticidadBitacora_704ILR.Info,
                $"Usuario #{userId_704ILR} desbloqueado");
        }
    }
}
