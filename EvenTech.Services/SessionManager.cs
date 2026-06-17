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
        // AccesoTotal=true cuando el usuario no tiene perfil asignado (superusuario)
        // o no se pudieron cargar los permisos: en ese caso TienePermiso da true.
        private readonly HashSet<string> _permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool AccesoTotal { get; private set; }   // solo true ante fallo de carga (no por falta de perfil)
        public bool SinPerfil { get; private set; }     // true = usuario sin perfil asignado -> bloqueado
        public IReadOnlyCollection<string> Permisos => _permisos;

        public bool TienePermiso(string clave)
            => AccesoTotal || (clave != null && _permisos.Contains(clave));

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

        public static void Login(BE_User user, IEnumerable<string> permisos, bool accesoTotal, bool sinPerfil)
        {
            lock (_lock)
            {
                if (_session != null)
                    throw new SessionAlreadyStartedException("Ya hay una sesion activa.");

                var s = new SessionManager
                {
                    User = user,
                    StartDate = DateTime.Now,
                    AccesoTotal = accesoTotal,
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
