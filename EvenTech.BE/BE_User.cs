using System;

namespace EvenTech.BE
{
    public class BE_User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }

        // Perfil asignado (T04). NULL = sin perfil (acceso total / superusuario).
        public int? PerfilId { get; set; }

        // Estado de cuenta y control de intentos fallidos (RF01.3 / RF01.4).
        public bool Activo { get; set; } = true;
        public bool Blocked { get; set; }
        public int FailedAttempts { get; set; }
    }
}
