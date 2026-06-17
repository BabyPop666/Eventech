namespace EvenTech.BE
{
    // Traduccion de una leyenda para un idioma. Clave es el identificador
    // logico (ej. "BTN_GUARDAR") y Texto el literal en el idioma dado.
    public class BE_Traduccion
    {
        public int Id { get; set; }
        public int IdiomaId { get; set; }
        public string Clave { get; set; }
        public string Texto { get; set; }
    }
}
