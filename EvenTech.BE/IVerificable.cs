namespace EvenTech.BE
{
    // Contrato para entidades que se protegen con digito verificador. Permite que
    // el mecanismo de integridad sea generico (aplicable a cualquier entidad) sin
    // duplicar codigo: la entidad expone, en orden estable, los valores de los
    // atributos que entran en el calculo del DV.
    public interface IVerificable
    {
        // Valores de los atributos en orden fijo. El orden importa: el algoritmo
        // pondera la posicion del atributo dentro de la entidad.
        string[] ObtenerCamposParaDV();
    }
}
