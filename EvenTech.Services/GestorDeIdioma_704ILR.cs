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
    public class GestorDeIdioma_704ILR
    {
        private static GestorDeIdioma_704ILR _instance_704ILR;
        private static readonly object _lock_704ILR = new object();

        private readonly List<IObservadorIdioma_704ILR> _observadores_704ILR = new List<IObservadorIdioma_704ILR>();
        private readonly List<BE_Idioma_704ILR> _idiomas_704ILR = new List<BE_Idioma_704ILR>();
        // codigoIdioma -> (clave -> texto)
        private readonly Dictionary<string, Dictionary<string, string>> _traducciones_704ILR =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private const string IdiomaPorDefecto_704ILR = "ES";

        private GestorDeIdioma_704ILR() { }

        public static GestorDeIdioma_704ILR GetInstance_704ILR
        {
            get
            {
                if (_instance_704ILR == null)
                {
                    lock (_lock_704ILR)
                    {
                        if (_instance_704ILR == null) _instance_704ILR = new GestorDeIdioma_704ILR();
                    }
                }
                return _instance_704ILR;
            }
        }

        public string IdiomaActual_704ILR { get; private set; } = IdiomaPorDefecto_704ILR;

        public IReadOnlyList<BE_Idioma_704ILR> IdiomasDisponibles_704ILR => _idiomas_704ILR;

        public void CargarIdiomas_704ILR(List<BE_Idioma_704ILR> idiomas_704ILR)
        {
            _idiomas_704ILR.Clear();
            if (idiomas_704ILR != null) _idiomas_704ILR.AddRange(idiomas_704ILR);
        }

        public void CargarTraducciones_704ILR(string codigoIdioma_704ILR, Dictionary<string, string> tabla_704ILR)
        {
            if (string.IsNullOrEmpty(codigoIdioma_704ILR) || tabla_704ILR == null) return;
            _traducciones_704ILR[codigoIdioma_704ILR] = tabla_704ILR;
        }

        // --- Patron Observer ---

        public void Suscribir_704ILR(IObservadorIdioma_704ILR observador_704ILR)
        {
            lock (_lock_704ILR)
            {
                if (observador_704ILR != null && !_observadores_704ILR.Contains(observador_704ILR))
                    _observadores_704ILR.Add(observador_704ILR);
            }
        }

        public void Desuscribir_704ILR(IObservadorIdioma_704ILR observador_704ILR)
        {
            lock (_lock_704ILR)
            {
                _observadores_704ILR.Remove(observador_704ILR);
            }
        }

        public void CambiarIdioma_704ILR(string codigoIdioma_704ILR)
        {
            if (string.IsNullOrEmpty(codigoIdioma_704ILR) || codigoIdioma_704ILR.Equals(IdiomaActual_704ILR, StringComparison.OrdinalIgnoreCase))
                return;

            IdiomaActual_704ILR = codigoIdioma_704ILR;
            NotificarObservadores_704ILR();
        }

        private void NotificarObservadores_704ILR()
        {
            // Copia para evitar problemas si un observador se desuscribe durante la notificacion.
            IObservadorIdioma_704ILR[] copia_704ILR;
            lock (_lock_704ILR) { copia_704ILR = _observadores_704ILR.ToArray(); }
            foreach (var o_704ILR in copia_704ILR)
            {
                try { o_704ILR.ActualizarTextos_704ILR(); } catch { /* un observador no debe romper a los demas */ }
            }
        }

        // Traduce una clave al idioma actual; si falta, cae al idioma por defecto;
        // si tampoco esta, devuelve la propia clave (util para detectar faltantes).
        public string Traducir_704ILR(string clave_704ILR)
        {
            if (string.IsNullOrEmpty(clave_704ILR)) return clave_704ILR;

            if (_traducciones_704ILR.TryGetValue(IdiomaActual_704ILR, out var actual_704ILR) &&
                actual_704ILR.TryGetValue(clave_704ILR, out var texto_704ILR))
                return texto_704ILR;

            if (_traducciones_704ILR.TryGetValue(IdiomaPorDefecto_704ILR, out var defecto_704ILR) &&
                defecto_704ILR.TryGetValue(clave_704ILR, out var textoDef_704ILR))
                return textoDef_704ILR;

            return clave_704ILR;
        }
    }
}
