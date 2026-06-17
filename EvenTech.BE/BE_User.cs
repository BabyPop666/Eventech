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
    }
}
