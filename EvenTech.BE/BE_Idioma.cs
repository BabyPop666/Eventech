namespace EvenTech.BE
{
    // Idioma disponible en el sistema. Se cargan desde la base (tabla Idiomas),
    // permitiendo agregar nuevos idiomas sin modificar codigo.
    public class BE_Idioma
    {
        public int Id { get; set; }
        public string Codigo { get; set; }   // ej. "ES", "EN"
        public string Nombre { get; set; }    // ej. "Espanol", "English"

        public override string ToString() => Nombre;
    }
}
