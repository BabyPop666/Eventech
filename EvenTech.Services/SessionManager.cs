using System;
using EvenTech.BE;

namespace EvenTech.Services
{
    // Singleton clasico (lock-based, lazy). Mantiene la sesion activa del usuario
    // entre el frmLogin y el resto de la app.
    public class SessionManager
    {
        public BE_User User { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

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

        public static void Login(BE_User user)
        {
            lock (_lock)
            {
                if (_session != null)
                    throw new SessionAlreadyStartedException("Ya hay una sesion activa.");

                _session = new SessionManager
                {
                    User = user,
                    StartDate = DateTime.Now
                };
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
