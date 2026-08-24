using System;
using System.Collections.Generic;
using EvenTech.BE;

namespace EvenTech.Services
{
    // Singleton clasico (lock-based, lazy). Mantiene la sesion activa del usuario
    // entre el frmLogin y el resto de la app, incluidos sus permisos efectivos.
    public class SessionManager_704ILR
    {
        public BE_User_704ILR User_704ILR { get; private set; }
        public DateTime StartDate_704ILR { get; private set; }
        public DateTime EndDate_704ILR { get; private set; }

        // Permisos efectivos (claves de hoja del Composite) del usuario actual.
        //
        // Politica: DENEGAR POR DEFECTO. Un permiso se concede unicamente si su
        // clave esta en el conjunto resuelto desde el perfil. Si la resolucion
        // falla (base caida, perfil inconsistente), la sesion queda sin ningun
        // permiso y se marca PermisosNoDisponibles: un error de infraestructura
        // no debe transformarse en una escalada de privilegios.
        private readonly HashSet<string> _permisos_704ILR = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool PermisosNoDisponibles_704ILR { get; private set; } // true = no se pudieron resolver -> sin permisos
        public bool SinPerfil_704ILR { get; private set; }             // true = usuario sin perfil asignado -> bloqueado
        public IReadOnlyCollection<string> Permisos_704ILR => _permisos_704ILR;

        public bool TienePermiso_704ILR(string clave_704ILR)
            => clave_704ILR != null && _permisos_704ILR.Contains(clave_704ILR);

        private static SessionManager_704ILR _session_704ILR;
        private static readonly object _lock_704ILR = new object();

        public static bool IsSessionActive_704ILR => _session_704ILR != null;

        public static SessionManager_704ILR GetInstance_704ILR
        {
            get
            {
                if (_session_704ILR == null)
                    throw new SessionNotStartedException_704ILR("No hay sesion activa.");
                return _session_704ILR;
            }
        }

        public static void Login_704ILR(BE_User_704ILR user_704ILR, IEnumerable<string> permisos_704ILR, bool permisosNoDisponibles_704ILR, bool sinPerfil_704ILR)
        {
            lock (_lock_704ILR)
            {
                if (_session_704ILR != null)
                    throw new SessionAlreadyStartedException_704ILR("Ya hay una sesion activa.");

                var s_704ILR = new SessionManager_704ILR
                {
                    User_704ILR = user_704ILR,
                    StartDate_704ILR = DateTime.Now,
                    PermisosNoDisponibles_704ILR = permisosNoDisponibles_704ILR,
                    SinPerfil_704ILR = sinPerfil_704ILR
                };
                if (permisos_704ILR != null)
                    foreach (var p_704ILR in permisos_704ILR)
                        if (!string.IsNullOrEmpty(p_704ILR)) s_704ILR._permisos_704ILR.Add(p_704ILR);

                _session_704ILR = s_704ILR;
            }
        }

        public static void Logout_704ILR()
        {
            lock (_lock_704ILR)
            {
                if (_session_704ILR == null)
                    throw new SessionNotStartedException_704ILR("No hay sesion activa.");

                _session_704ILR.EndDate_704ILR = DateTime.Now;
                _session_704ILR = null;
            }
        }
    }

    public class SessionNotStartedException_704ILR : Exception
    {
        public SessionNotStartedException_704ILR(string message_704ILR) : base(message_704ILR) { }
    }

    public class SessionAlreadyStartedException_704ILR : Exception
    {
        public SessionAlreadyStartedException_704ILR(string message_704ILR) : base(message_704ILR) { }
    }
}
