using System;
using System.Text.RegularExpressions;
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
    }
}
