using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum PerfilResult_704ILR { Success, NombreInvalido, NombreDuplicado, ReferenciaCircular }

    // Logica de negocio de perfiles y arbol de permisos (Composite).
    public static class BLL_Perfil_704ILR
    {
        public static List<BE_IComponentePermiso_704ILR> GetArbolPermisos_704ILR() => DAL_Permiso_704ILR.GetArbol_704ILR();

        public static List<BE_Perfil_704ILR> GetPerfiles_704ILR() => DAL_Perfil_704ILR.GetAll_704ILR();

        // Alta de un perfil nuevo (luego se le asignan permisos y usuarios).
        public static PerfilResult_704ILR CrearPerfil_704ILR(string nombre_704ILR, string descripcion_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            if (string.IsNullOrWhiteSpace(nombre_704ILR) || nombre_704ILR.Trim().Length > 80)
                return PerfilResult_704ILR.NombreInvalido;
            nombre_704ILR = nombre_704ILR.Trim();
            if (DAL_Perfil_704ILR.ExistsNombre_704ILR(nombre_704ILR))
                return PerfilResult_704ILR.NombreDuplicado;

            nuevoId_704ILR = DAL_Perfil_704ILR.Insert_704ILR(nombre_704ILR, string.IsNullOrWhiteSpace(descripcion_704ILR) ? null : descripcion_704ILR.Trim());
            BLL_Bitacora_704ILR.Registrar_704ILR("Perfiles", "Alta de perfil", CriticidadBitacora_704ILR.Info, $"Perfil '{nombre_704ILR}' creado");
            return PerfilResult_704ILR.Success;
        }

        public static HashSet<int> GetPermisosAsignados_704ILR(int perfilId_704ILR) => DAL_Perfil_704ILR.GetPermisoIds_704ILR(perfilId_704ILR);

        public static HashSet<int> GetPerfilesIncluidos_704ILR(int perfilId_704ILR) => DAL_Perfil_704ILR.GetIncluidos_704ILR(perfilId_704ILR);

        public static void GuardarAsignaciones_704ILR(int perfilId_704ILR, IEnumerable<int> permisoIds_704ILR)
        {
            DAL_Perfil_704ILR.SetPermisos_704ILR(perfilId_704ILR, permisoIds_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Perfiles", "Actualizacion de permisos", CriticidadBitacora_704ILR.Info,
                $"Se actualizaron los permisos del perfil #{perfilId_704ILR}");
        }

        // Guarda la composicion completa del perfil: sus permisos y los perfiles
        // que incluye (Composite de perfiles). Rechaza composiciones que generen
        // una referencia circular (directa o transitiva).
        public static PerfilResult_704ILR GuardarComposicion_704ILR(int perfilId_704ILR, IEnumerable<int> permisoIds_704ILR,
            IEnumerable<int> perfilesIncluidos_704ILR)
        {
            var incluidos_704ILR = (perfilesIncluidos_704ILR ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (GeneraCiclo_704ILR(perfilId_704ILR, incluidos_704ILR)) return PerfilResult_704ILR.ReferenciaCircular;

            DAL_Perfil_704ILR.SetComposicion_704ILR(perfilId_704ILR, permisoIds_704ILR ?? Enumerable.Empty<int>(), incluidos_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Perfiles", "Actualizacion de permisos", CriticidadBitacora_704ILR.Info,
                $"Se actualizo la composicion del perfil #{perfilId_704ILR} ({incluidos_704ILR.Count} perfil(es) incluido(s))");
            return PerfilResult_704ILR.Success;
        }

        // El grafo de inclusiones no admite ciclos: un perfil no puede contenerse
        // a si mismo ni directa ni transitivamente (Gerencial > Vendedor > Gerencial).
        // Se simula el grafo con la composicion propuesta y se busca si desde los
        // incluidos se puede volver al perfil de partida.
        private static bool GeneraCiclo_704ILR(int perfilId_704ILR, List<int> incluidosPropuestos_704ILR)
        {
            if (incluidosPropuestos_704ILR.Contains(perfilId_704ILR)) return true;

            var grafo_704ILR = DAL_Perfil_704ILR.GetTodasLasInclusiones_704ILR();
            grafo_704ILR[perfilId_704ILR] = incluidosPropuestos_704ILR;

            var visitados_704ILR = new HashSet<int>();
            var pendientes_704ILR = new Stack<int>(incluidosPropuestos_704ILR);
            while (pendientes_704ILR.Count > 0)
            {
                int actual_704ILR = pendientes_704ILR.Pop();
                if (actual_704ILR == perfilId_704ILR) return true;
                if (!visitados_704ILR.Add(actual_704ILR)) continue;
                if (grafo_704ILR.TryGetValue(actual_704ILR, out var hijos_704ILR))
                    foreach (int h_704ILR in hijos_704ILR) pendientes_704ILR.Push(h_704ILR);
            }
            return false;
        }

        // Construye el BE_Perfil compuesto (componentes del arbol asignados +
        // perfiles incluidos, recursivamente) y delega en la operacion
        // polimorfica del Composite para resolver los permisos efectivos.
        public static List<BE_Permiso_704ILR> GetPermisosEfectivosDePerfil_704ILR(int perfilId_704ILR)
        {
            var arbol_704ILR = GetArbolPermisos_704ILR();
            BE_Perfil_704ILR perfil_704ILR = ConstruirPerfilCompuesto_704ILR(perfilId_704ILR, arbol_704ILR, new Dictionary<int, BE_Perfil_704ILR>());
            return perfil_704ILR.ObtenerPermisosEfectivos_704ILR();
        }

        private static BE_Perfil_704ILR ConstruirPerfilCompuesto_704ILR(int perfilId_704ILR,
            List<BE_IComponentePermiso_704ILR> arbol_704ILR, Dictionary<int, BE_Perfil_704ILR> construidos_704ILR)
        {
            // Cada perfil se materializa una sola vez (comparte instancia si llega
            // por varios caminos y corta cualquier ciclo residual en datos).
            if (construidos_704ILR.TryGetValue(perfilId_704ILR, out var existente_704ILR)) return existente_704ILR;

            BE_Perfil_704ILR perfil_704ILR = DAL_Perfil_704ILR.GetById_704ILR(perfilId_704ILR) ?? new BE_Perfil_704ILR { Id_704ILR = perfilId_704ILR };
            construidos_704ILR[perfilId_704ILR] = perfil_704ILR;

            HashSet<int> asignados_704ILR = DAL_Perfil_704ILR.GetPermisoIds_704ILR(perfilId_704ILR);
            AsignarComponentes_704ILR(arbol_704ILR, asignados_704ILR, perfil_704ILR);

            foreach (int hijoId_704ILR in DAL_Perfil_704ILR.GetIncluidos_704ILR(perfilId_704ILR))
                perfil_704ILR.IncluirPerfil_704ILR(ConstruirPerfilCompuesto_704ILR(hijoId_704ILR, arbol_704ILR, construidos_704ILR));

            return perfil_704ILR;
        }

        // Recorre el arbol y asigna al perfil los nodos marcados. Si un grupo esta
        // asignado se toma entero (su subtree ya lo resuelve el Composite), por lo
        // que no hace falta descender dentro de el.
        private static void AsignarComponentes_704ILR(List<BE_IComponentePermiso_704ILR> nodos_704ILR,
            HashSet<int> asignados_704ILR, BE_Perfil_704ILR perfil_704ILR)
        {
            foreach (var nodo_704ILR in nodos_704ILR)
            {
                if (asignados_704ILR.Contains(nodo_704ILR.Id_704ILR))
                {
                    perfil_704ILR.Asignar_704ILR(nodo_704ILR);
                }
                else if (nodo_704ILR is BE_GrupoPermisos_704ILR grupo_704ILR)
                {
                    AsignarComponentes_704ILR(grupo_704ILR.Hijos_704ILR, asignados_704ILR, perfil_704ILR);
                }
            }
        }

        // Recorre el arbol recursivamente y devuelve los permisos efectivos (hojas)
        // que cubren los componentes asignados al perfil.
        public static List<BE_Permiso_704ILR> CalcularPermisosEfectivos_704ILR(
            List<BE_IComponentePermiso_704ILR> arbol_704ILR, HashSet<int> asignados_704ILR)
        {
            var efectivos_704ILR = new Dictionary<int, BE_Permiso_704ILR>();
            foreach (var nodo_704ILR in arbol_704ILR)
                Recolectar_704ILR(nodo_704ILR, asignados_704ILR, false, efectivos_704ILR);
            return new List<BE_Permiso_704ILR>(efectivos_704ILR.Values);
        }

        private static void Recolectar_704ILR(BE_IComponentePermiso_704ILR nodo_704ILR, HashSet<int> asignados_704ILR,
            bool heredado_704ILR, Dictionary<int, BE_Permiso_704ILR> acumulado_704ILR)
        {
            bool activo_704ILR = heredado_704ILR || asignados_704ILR.Contains(nodo_704ILR.Id_704ILR);

            if (nodo_704ILR is BE_GrupoPermisos_704ILR grupo_704ILR)
            {
                foreach (var hijo_704ILR in grupo_704ILR.Hijos_704ILR)
                    Recolectar_704ILR(hijo_704ILR, asignados_704ILR, activo_704ILR, acumulado_704ILR);
            }
            else if (nodo_704ILR is BE_Permiso_704ILR permiso_704ILR && activo_704ILR)
            {
                acumulado_704ILR[permiso_704ILR.Id_704ILR] = permiso_704ILR;
            }
        }
    }
}
