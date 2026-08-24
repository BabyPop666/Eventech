using System;

namespace EvenTech.BE
{
    public class BE_User_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Username_704ILR { get; set; }
        public string PasswordHash_704ILR { get; set; }
        public DateTime CreatedAt_704ILR { get; set; }

        // Perfil asignado (T04). NULL = sin perfil (acceso total / superusuario).
        public int? PerfilId_704ILR { get; set; }

        // Estado de cuenta y control de intentos fallidos (RF01.3 / RF01.4).
        public bool Activo_704ILR { get; set; } = true;
        public bool Blocked_704ILR { get; set; }
        public int FailedAttempts_704ILR { get; set; }
    }
}
