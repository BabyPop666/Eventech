using System;
using System.Collections.Generic;
using EvenTech.BE;

namespace EvenTech.Services
{
    // Singleton clasico (lock-based, lazy). Mantiene la sesion activa del usuario
    // entre el frmLogin y el resto de la app, incluidos sus permisos efectivos.
    public class SessionManager
    {
        public BE_User User { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        // Permisos efectivos (claves de hoja del Composite) del usuario actual.
        //
        // Politica: DENEGAR POR DEFECTO. Un permiso se concede unicamente si su
        // clave esta en el conjunto resuelto desde el perfil. Si la resolucion
        // falla (base caida, perfil inconsistente), la sesion queda sin ningun
        // permiso y se marca PermisosNoDisponibles: un error de infraestructura
        // no debe transformarse en una escalada de privilegios.
        private readonly HashSet<string> _permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool PermisosNoDisponibles { get; private set; } // true = no se pudieron resolver -> sin permisos
        public bool SinPerfil { get; private set; }             // true = usuario sin perfil asignado -> bloqueado
        public IReadOnlyCollection<string> Permisos => _permisos;

        public bool TienePermiso(string clave)
            => clave != null && _permisos.Contains(clave);

        private static SessionManager _session;
        private static readonly object _lock = new object();

        public static bool IsSessionActive => _session != null;

        public static SessionManager GetInstance
        {
            get
            {
                if (_session == null)
                    throw new SessionNotStartedException("No hay sesion activa.");
                return _session;
            }
        }

        public static void Login(BE_User user, IEnumerable<string> permisos, bool permisosNoDisponibles, bool sinPerfil)
        {
            lock (_lock)
            {
                if (_session != null)
                    throw new SessionAlreadyStartedException("Ya hay una sesion activa.");

                var s = new SessionManager
                {
                    User = user,
                    StartDate = DateTime.Now,
                    PermisosNoDisponibles = permisosNoDisponibles,
                    SinPerfil = sinPerfil
                };
                if (permisos != null)
                    foreach (var p in permisos)
                        if (!string.IsNullOrEmpty(p)) s._permisos.Add(p);

                _session = s;
            }
        }

        public static void Logout()
        {
            lock (_lock)
            {
                if (_session == null)
                    throw new SessionNotStartedException("No hay sesion activa.");

                _session.EndDate = DateTime.Now;
                _session = null;
            }
        }
    }

    public class SessionNotStartedException : Exception
    {
        public SessionNotStartedException(string message) : base(message) { }
    }

    public class SessionAlreadyStartedException : Exception
    {
        public SessionAlreadyStartedException(string message) : base(message) { }
    }
}
