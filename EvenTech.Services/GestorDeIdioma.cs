using System;
using System.Collections.Generic;
using EvenTech.BE;

namespace EvenTech.Services
{
    // Sujeto observable del patron Observer (Singleton clasico lock-based).
    // Mantiene la lista de observadores y el diccionario de traducciones por
    // idioma. No accede a la base: la capa BLL lo alimenta al iniciar la app
    // (CargarIdiomas / CargarTraducciones). Al cambiar de idioma, notifica a
    // todos los observadores sin acoplamiento directo entre ellos.
    public class GestorDeIdioma
    {
        private static GestorDeIdioma _instance;
        private static readonly object _lock = new object();

        private readonly List<IObservadorIdioma> _observadores = new List<IObservadorIdioma>();
        private readonly List<BE_Idioma> _idiomas = new List<BE_Idioma>();
        // codigoIdioma -> (clave -> texto)
        private readonly Dictionary<string, Dictionary<string, string>> _traducciones =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private const string IdiomaPorDefecto = "ES";

        private GestorDeIdioma() { }

        public static GestorDeIdioma GetInstance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null) _instance = new GestorDeIdioma();
                    }
                }
                return _instance;
            }
        }

        public string IdiomaActual { get; private set; } = IdiomaPorDefecto;

        public IReadOnlyList<BE_Idioma> IdiomasDisponibles => _idiomas;

        public void CargarIdiomas(List<BE_Idioma> idiomas)
        {
            _idiomas.Clear();
            if (idiomas != null) _idiomas.AddRange(idiomas);
        }

        public void CargarTraducciones(string codigoIdioma, Dictionary<string, string> tabla)
        {
            if (string.IsNullOrEmpty(codigoIdioma) || tabla == null) return;
            _traducciones[codigoIdioma] = tabla;
        }

        // --- Patron Observer ---

        public void Suscribir(IObservadorIdioma observador)
        {
            lock (_lock)
            {
                if (observador != null && !_observadores.Contains(observador))
                    _observadores.Add(observador);
            }
        }

        public void Desuscribir(IObservadorIdioma observador)
        {
            lock (_lock)
            {
                _observadores.Remove(observador);
            }
        }

        public void CambiarIdioma(string codigoIdioma)
        {
            if (string.IsNullOrEmpty(codigoIdioma) || codigoIdioma.Equals(IdiomaActual, StringComparison.OrdinalIgnoreCase))
                return;

            IdiomaActual = codigoIdioma;
            NotificarObservadores();
        }

        // Fuerza el re-render de todos los observadores con el idioma actual, sin
        // cambiarlo. Lo usa la edicion de traducciones del idioma ya activo (que de
        // otro modo no dispararia ninguna notificacion).
        public void RefrescarIdiomaActual() => NotificarObservadores();

        private void NotificarObservadores()
        {
            // Copia para evitar problemas si un observador se desuscribe durante la notificacion.
            IObservadorIdioma[] copia;
            lock (_lock) { copia = _observadores.ToArray(); }
            foreach (var o in copia)
            {
                try { o.ActualizarTextos(); } catch { /* un observador no debe romper a los demas */ }
            }
        }

        // Traduce una clave al idioma actual; si falta, cae al idioma por defecto;
        // si tampoco esta, devuelve la propia clave (util para detectar faltantes).
        public string Traducir(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return clave;

            if (_traducciones.TryGetValue(IdiomaActual, out var actual) &&
                actual.TryGetValue(clave, out var texto))
                return texto;

            if (_traducciones.TryGetValue(IdiomaPorDefecto, out var defecto) &&
                defecto.TryGetValue(clave, out var textoDef))
                return textoDef;

            return clave;
        }
    }
}
