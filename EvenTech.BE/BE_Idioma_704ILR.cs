namespace EvenTech.BE
{
    // Idioma disponible en el sistema. Se cargan desde la base (tabla Idiomas),
    // permitiendo agregar nuevos idiomas sin modificar codigo.
    public class BE_Idioma_704ILR
    {
        public int Id_704ILR { get; set; }
        public string Codigo_704ILR { get; set; }   // ej. "ES", "EN"
        public string Nombre_704ILR { get; set; }    // ej. "Espanol", "English"

        public override string ToString() => Nombre_704ILR;
    }
}
