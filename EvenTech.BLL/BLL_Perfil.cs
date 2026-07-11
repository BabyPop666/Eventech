using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum PerfilResult { Success, NombreInvalido, NombreDuplicado, ReferenciaCircular }

    // Logica de negocio de perfiles y arbol de permisos (Composite).
    public static class BLL_Perfil
    {
        public static List<BE_IComponentePermiso> GetArbolPermisos() => DAL_Permiso.GetArbol();

        public static List<BE_Perfil> GetPerfiles() => DAL_Perfil.GetAll();

        // Alta de un perfil nuevo (luego se le asignan permisos y usuarios).
        public static PerfilResult CrearPerfil(string nombre, string descripcion, out int nuevoId)
        {
            nuevoId = 0;
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 80)
                return PerfilResult.NombreInvalido;
            nombre = nombre.Trim();
            if (DAL_Perfil.ExistsNombre(nombre))
                return PerfilResult.NombreDuplicado;

            nuevoId = DAL_Perfil.Insert(nombre, string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim());
            BLL_Bitacora.Registrar("Perfiles", "Alta de perfil", CriticidadBitacora.Info, $"Perfil '{nombre}' creado");
            return PerfilResult.Success;
        }

        public static HashSet<int> GetPermisosAsignados(int perfilId) => DAL_Perfil.GetPermisoIds(perfilId);

        public static HashSet<int> GetPerfilesIncluidos(int perfilId) => DAL_Perfil.GetIncluidos(perfilId);

        public static void GuardarAsignaciones(int perfilId, IEnumerable<int> permisoIds)
        {
            DAL_Perfil.SetPermisos(perfilId, permisoIds);
            BLL_Bitacora.Registrar("Perfiles", "Actualizacion de permisos", CriticidadBitacora.Info,
                $"Se actualizaron los permisos del perfil #{perfilId}");
        }

        // Guarda la composicion completa del perfil: sus permisos y los perfiles
        // que incluye (Composite de perfiles). Rechaza composiciones que generen
        // una referencia circular (directa o transitiva).
        public static PerfilResult GuardarComposicion(int perfilId, IEnumerable<int> permisoIds,
            IEnumerable<int> perfilesIncluidos)
        {
            var incluidos = (perfilesIncluidos ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (GeneraCiclo(perfilId, incluidos)) return PerfilResult.ReferenciaCircular;

            DAL_Perfil.SetComposicion(perfilId, permisoIds ?? Enumerable.Empty<int>(), incluidos);
            BLL_Bitacora.Registrar("Perfiles", "Actualizacion de permisos", CriticidadBitacora.Info,
                $"Se actualizo la composicion del perfil #{perfilId} ({incluidos.Count} perfil(es) incluido(s))");
            return PerfilResult.Success;
        }

        // El grafo de inclusiones no admite ciclos: un perfil no puede contenerse
        // a si mismo ni directa ni transitivamente (Gerencial > Vendedor > Gerencial).
        // Se simula el grafo con la composicion propuesta y se busca si desde los
        // incluidos se puede volver al perfil de partida.
        private static bool GeneraCiclo(int perfilId, List<int> incluidosPropuestos)
        {
            if (incluidosPropuestos.Contains(perfilId)) return true;

            var grafo = DAL_Perfil.GetTodasLasInclusiones();
            grafo[perfilId] = incluidosPropuestos;

            var visitados = new HashSet<int>();
            var pendientes = new Stack<int>(incluidosPropuestos);
            while (pendientes.Count > 0)
            {
                int actual = pendientes.Pop();
                if (actual == perfilId) return true;
                if (!visitados.Add(actual)) continue;
                if (grafo.TryGetValue(actual, out var hijos))
                    foreach (int h in hijos) pendientes.Push(h);
            }
            return false;
        }

        // Construye el BE_Perfil compuesto (componentes del arbol asignados +
        // perfiles incluidos, recursivamente) y delega en la operacion
        // polimorfica del Composite para resolver los permisos efectivos.
        public static List<BE_Permiso> GetPermisosEfectivosDePerfil(int perfilId)
        {
            var arbol = GetArbolPermisos();
            BE_Perfil perfil = ConstruirPerfilCompuesto(perfilId, arbol, new Dictionary<int, BE_Perfil>());
            return perfil.ObtenerPermisosEfectivos();
        }

        private static BE_Perfil ConstruirPerfilCompuesto(int perfilId,
            List<BE_IComponentePermiso> arbol, Dictionary<int, BE_Perfil> construidos)
        {
            // Cada perfil se materializa una sola vez (comparte instancia si llega
            // por varios caminos y corta cualquier ciclo residual en datos).
            if (construidos.TryGetValue(perfilId, out var existente)) return existente;

            BE_Perfil perfil = DAL_Perfil.GetById(perfilId) ?? new BE_Perfil { Id = perfilId };
            construidos[perfilId] = perfil;

            HashSet<int> asignados = DAL_Perfil.GetPermisoIds(perfilId);
            AsignarComponentes(arbol, asignados, perfil);

            foreach (int hijoId in DAL_Perfil.GetIncluidos(perfilId))
                perfil.IncluirPerfil(ConstruirPerfilCompuesto(hijoId, arbol, construidos));

            return perfil;
        }

        // Recorre el arbol y asigna al perfil los nodos marcados. Si un grupo esta
        // asignado se toma entero (su subtree ya lo resuelve el Composite), por lo
        // que no hace falta descender dentro de el.
        private static void AsignarComponentes(List<BE_IComponentePermiso> nodos,
            HashSet<int> asignados, BE_Perfil perfil)
        {
            foreach (var nodo in nodos)
            {
                if (asignados.Contains(nodo.Id))
                {
                    perfil.Asignar(nodo);
                }
                else if (nodo is BE_GrupoPermisos grupo)
                {
                    AsignarComponentes(grupo.Hijos, asignados, perfil);
                }
            }
        }

        // Recorre el arbol recursivamente y devuelve los permisos efectivos (hojas)
        // que cubren los componentes asignados al perfil.
        public static List<BE_Permiso> CalcularPermisosEfectivos(
            List<BE_IComponentePermiso> arbol, HashSet<int> asignados)
        {
            var efectivos = new Dictionary<int, BE_Permiso>();
            foreach (var nodo in arbol)
                Recolectar(nodo, asignados, false, efectivos);
            return new List<BE_Permiso>(efectivos.Values);
        }

        private static void Recolectar(BE_IComponentePermiso nodo, HashSet<int> asignados,
            bool heredado, Dictionary<int, BE_Permiso> acumulado)
        {
            bool activo = heredado || asignados.Contains(nodo.Id);

            if (nodo is BE_GrupoPermisos grupo)
            {
                foreach (var hijo in grupo.Hijos)
                    Recolectar(hijo, asignados, activo, acumulado);
            }
            else if (nodo is BE_Permiso permiso && activo)
            {
                acumulado[permiso.Id] = permiso;
            }
        }
    }
}
