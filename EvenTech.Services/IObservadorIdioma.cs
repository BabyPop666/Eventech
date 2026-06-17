namespace EvenTech.Services
{
    // Observador del patron Observer para el cambio de idioma. Cada formulario o
    // control que muestre texto traducible implementa esta interfaz y se suscribe
    // al GestorDeIdioma; cuando el idioma cambia, recibe ActualizarTextos().
    public interface IObservadorIdioma
    {
        void ActualizarTextos();
    }
}
