using EvenTech.BLL;
using EvenTech.Services;

Console.WriteLine("== EvenTech smoke test v2 ==");

// ---------------------------------------------------------------------------
// Contador de fallos: la prueba tiene que poder FALLAR SOLA.
// Antes cada caso imprimia el valor obtenido y, entre parentesis, el esperado,
// pero nadie los comparaba: la corrida terminaba igual de "verde" aunque un
// numero no coincidiera, y el proceso devolvia siempre 0. Ahora toda
// verificacion pasa por Esperar_704ILR, que compara, marca la diferencia y suma
// un fallo; al cierre el programa informa el total y devuelve un codigo de
// salida distinto de cero si algo no coincidio.
// ---------------------------------------------------------------------------
int fallos_704ILR = 0;
int verificaciones_704ILR = 0;

string Mostrar_704ILR(object valor_704ILR) =>
    valor_704ILR == null ? "null"
    : valor_704ILR is decimal dec_704ILR ? dec_704ILR.ToString("0.00")
    : valor_704ILR is DateTime f_704ILR ? f_704ILR.ToString("yyyy-MM-dd")
    : valor_704ILR.ToString();

void Esperar_704ILR(string etiqueta_704ILR, object real_704ILR, object esperado_704ILR)
{
    verificaciones_704ILR++;
    bool ok_704ILR = object.Equals(real_704ILR, esperado_704ILR);
    if (!ok_704ILR) fallos_704ILR++;
    Console.WriteLine($"  {etiqueta_704ILR}: {Mostrar_704ILR(real_704ILR)} " +
                      $"(esperado {Mostrar_704ILR(esperado_704ILR)})" + (ok_704ILR ? "" : "   <-- DIFIERE"));
}

// Un caso que aborta por excepcion tambien es un fallo: se anota y la corrida
// sigue con el resto (antes una excepcion en [18] se llevaba puestos [19]-[33]).
void Excepcion_704ILR(string caso_704ILR, Exception ex_704ILR)
{
    fallos_704ILR++;
    Console.WriteLine($"  EXCEPCION en {caso_704ILR}: {ex_704ILR.GetType().Name}: {ex_704ILR.Message}   <-- DIFIERE");
}

// Asientos de bitacora de un modulo (opcionalmente, de una accion). Las
// postcondiciones de los CUN prometen dejar traza de la operacion: se cuentan
// antes y despues. Se filtra por MODULO y no por el texto de la accion a
// proposito: ese texto es una leyenda para el usuario y cambia (el alta ya dice
// "Cotizacion generada" o "Reserva generada" segun el estado); atar la prueba a
// la leyenda la haria fallar por un cambio de redaccion, no por un defecto.
List<EvenTech.BE.BE_BitacoraEntry_704ILR> Bitacora_704ILR(string modulo_704ILR, string accion_704ILR = null) =>
    EvenTech.BLL.BLL_Bitacora_704ILR.Buscar_704ILR(new EvenTech.BE.BitacoraFiltros_704ILR
    { Modulo_704ILR = modulo_704ILR, Accion_704ILR = accion_704ILR });

int Asientos_704ILR(string modulo_704ILR, string accion_704ILR = null) => Bitacora_704ILR(modulo_704ILR, accion_704ILR).Count;

// Sufijo unico de la corrida. Con HHmmss dos corridas de dias distintos a la
// misma hora chocaban (usuario duplicado, perfil duplicado, DNI duplicado);
// con la fecha adelante el choque exige repetir el segundo del mismo dia.
string suf_704ILR = DateTime.Now.ToString("yyMMddHHmmss");

// Desplazamiento propio de esta corrida. Las reservas de prueba se agendan a
// varios anios vista y con este desfasaje para no pisar fechas del negocio
// (RN-03), y todas las que quedan CONFIRMADAS se cancelan al cerrar su caso, de
// modo que no bloqueen el salon en la proxima corrida. Donde igual podria haber
// rastro (la ventana cercana de [26]) la fecha se elige con la propia consulta
// de disponibilidad, no a ciegas. Se prefirio esto antes que limpiar la base:
// una rutina que cancelara reservas CONFIRMADAS por salon+fecha podria dar de
// baja datos reales sin aviso y sin vuelta atras.
int desfasaje_704ILR = (int)DateTime.Now.TimeOfDay.TotalSeconds % 900;

// Primera fecha en la que el salon indicado admite una reserva firme, a partir
// de 'desde'. Se resuelve con la consulta de disponibilidad del propio sistema
// (la misma que usa el vendedor), de modo que el caso no de un falso rojo por
// chocar contra los datos de demostracion o contra el rastro de otra corrida.
DateTime FechaLibre_704ILR(int salonId_704ILR, DateTime desde_704ILR)
{
    var d_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(desde_704ILR, 0)
        .FirstOrDefault(x_704ILR => x_704ILR.SalonId_704ILR == salonId_704ILR);
    if (d_704ILR == null || d_704ILR.Libre_704ILR) return desde_704ILR.Date;
    return d_704ILR.ProximaFechaLibre_704ILR ?? desde_704ILR.Date;
}

// Limpieza de los perfiles que crea [19]. La aplicacion no ofrece dar de baja un
// perfil (no es una operacion del alcance), asi que el rastro de la prueba se
// borra directamente contra la base, igual que [25] crea y elimina la suya.
void BorrarPerfilDePrueba_704ILR(int perfilId_704ILR)
{
    if (perfilId_704ILR <= 0) return;
    using var cn_704ILR = new EvenTech.DAL.DAL_DB_Connection_704ILR();
    using var cmd_704ILR = new Microsoft.Data.SqlClient.SqlCommand(
        "UPDATE dbo.Users SET PerfilId = NULL WHERE PerfilId = @id; " +
        "DELETE FROM dbo.PerfilIncluido WHERE PerfilPadreId = @id OR PerfilHijoId = @id; " +
        "DELETE FROM dbo.PerfilPermiso WHERE PerfilId = @id; " +
        "DELETE FROM dbo.Perfiles WHERE Id = @id;", cn_704ILR.OpenConnection_704ILR());
    cmd_704ILR.Parameters.AddWithValue("@id", perfilId_704ILR);
    cmd_704ILR.ExecuteNonQuery();
}

// RN-07: una reserva se confirma con el adelanto ya cobrado. Este helper registra
// ese cobro para poder ejercitar las transiciones a CONFIRMADA, igual que hace el
// vendedor en la aplicacion: guardar la operacion, cobrar y recien ahi confirmar.
void Adelanto_704ILR(int reservaId_704ILR, decimal monto_704ILR)
{
    var met_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
    if (met_704ILR.Count == 0) return;
    var rAd_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
    {
        ReservaId_704ILR = reservaId_704ILR,
        MetodoPagoId_704ILR = met_704ILR[0].Id_704ILR,
        Monto_704ILR = monto_704ILR,
        Observacion_704ILR = "Adelanto"
    }, out _);

    // Si el adelanto no entra, la confirmacion que viene despues falla por RN-07
    // y el caso reportaria un motivo equivocado: se anota aca.
    if (rAd_704ILR != PagoResult_704ILR.Success_704ILR)
    {
        fallos_704ILR++;
        Console.WriteLine($"  adelanto de {monto_704ILR:0.00} en la reserva #{reservaId_704ILR}: " +
                          $"{rAd_704ILR} (esperado Success_704ILR)   <-- DIFIERE");
    }
}

// [1] Login OK
Console.WriteLine("[1] Login admin/admin123:");
var r1_704ILR = BLL_Login_704ILR.Authenticate_704ILR("admin", Encrypt_704ILR.HashValue_704ILR("admin123"));
Esperar_704ILR("result", r1_704ILR.Result_704ILR, LoginResult_704ILR.Success_704ILR);
Esperar_704ILR("sesion activa", SessionManager_704ILR.IsSessionActive_704ILR, true);
BLL_Login_704ILR.Logout_704ILR();

// [2] Crear usuario nuevo (con timestamp para que sea unico entre corridas)
string newUser_704ILR = "smoke_" + suf_704ILR;
Console.WriteLine($"[2] Crear usuario '{newUser_704ILR}' password 'pass1234':");
var rc1_704ILR = BLL_User_704ILR.CreateUser_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("pass1234"));
Esperar_704ILR("result", rc1_704ILR, CreateUserResult_704ILR.Success_704ILR);

// [3] Crear duplicado
Console.WriteLine($"[3] Crear '{newUser_704ILR}' duplicado:");
var rc2_704ILR = BLL_User_704ILR.CreateUser_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("otra"));
Esperar_704ILR("result", rc2_704ILR, CreateUserResult_704ILR.UsernameAlreadyExists_704ILR);

// [4] Username invalido
Console.WriteLine("[4] Crear con username '..' (invalido):");
var rc3_704ILR = BLL_User_704ILR.CreateUser_704ILR("..", Encrypt_704ILR.HashValue_704ILR("xxxx"));
Esperar_704ILR("result", rc3_704ILR, CreateUserResult_704ILR.InvalidUsername_704ILR);

// [5] Login con el usuario recien creado. Nace SIN perfil asignado: la sesion
// tiene que quedar marcada como tal y sin un solo permiso (denegar por defecto),
// que es la bandera con la que la ventana principal bloquea al usuario.
Console.WriteLine($"[5] Login con '{newUser_704ILR}':");
var r5_704ILR = BLL_Login_704ILR.Authenticate_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("pass1234"));
Esperar_704ILR("result", r5_704ILR.Result_704ILR, LoginResult_704ILR.Success_704ILR);
if (SessionManager_704ILR.IsSessionActive_704ILR)
{
    Esperar_704ILR("sesion sin perfil asignado", SessionManager_704ILR.GetInstance_704ILR.SinPerfil_704ILR, true);
    Esperar_704ILR("permisos de la sesion", SessionManager_704ILR.GetInstance_704ILR.Permisos_704ILR.Count, 0);
    Esperar_704ILR("RESERVA_CREAR sin perfil", SessionManager_704ILR.GetInstance_704ILR.TienePermiso_704ILR("RESERVA_CREAR"), false);
}
BLL_Login_704ILR.Logout_704ILR();

// [6] Leer auditoria (ultimas 5)
Console.WriteLine("[6] Ultimas 5 entradas de auditoria:");
foreach (var e_704ILR in BLL_LoginAudit_704ILR.GetAll_704ILR(5))
{
    Console.WriteLine($"  #{e_704ILR.Id_704ILR} {e_704ILR.Timestamp_704ILR:HH:mm:ss} {e_704ILR.Username_704ILR,-20} {e_704ILR.Action_704ILR,-12} {e_704ILR.Details_704ILR}");
}

// [7] Reservas: alta valida (la reserva referencia al cliente por Id)
Console.WriteLine("[7] Crear reserva valida:");
var salones_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
var clientes_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
if (salones_704ILR.Count == 0 || clientes_704ILR.Count == 0)
{
    Console.WriteLine("  (no hay salones/clientes seed; corre db/schema.sql)");
}
else try
{
    // Fecha propia de la corrida (como el username del caso [2]) y ademas libre de
    // verdad para ese salon: el caso [10] confirma esta reserva y una fecha ya
    // comprometida daria SalonOcupado por un motivo ajeno a lo que se prueba.
    // Ventana 1000-1900 para no pisar la del [26].
    var nueva_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = clientes_704ILR[0].Id_704ILR,
        SalonId_704ILR = salones_704ILR[0].Id_704ILR,
        FechaEvento_704ILR = FechaLibre_704ILR(salones_704ILR[0].Id_704ILR, DateTime.Today.AddDays(1000 + desfasaje_704ILR)),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 150000m
    };

    // Postcondicion del CUN005: el alta deja su propio asiento en la bitacora.
    int altasAntes_704ILR = Asientos_704ILR("Reservas");
    var rr1_704ILR = BLL_Reserva_704ILR.Crear_704ILR(nueva_704ILR, out int nuevoId_704ILR);
    Esperar_704ILR("result", rr1_704ILR, ReservaResult_704ILR.Success_704ILR);
    Esperar_704ILR("id asignado", nuevoId_704ILR > 0, true);
    Esperar_704ILR("asientos del modulo Reservas tras el alta", Asientos_704ILR("Reservas"), altasAntes_704ILR + 1);
    var asientoAlta_704ILR = Bitacora_704ILR("Reservas")[0];
    Esperar_704ILR("el asiento nombra la reserva creada",
        asientoAlta_704ILR.Detalle_704ILR.Contains($"#{nuevoId_704ILR}"), true);

    // [8] Reserva con fecha pasada (debe fallar)
    Console.WriteLine("[8] Crear reserva con fecha pasada (invalida):");
    var pasada_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = clientes_704ILR[0].Id_704ILR,
        SalonId_704ILR = salones_704ILR[0].Id_704ILR,
        FechaEvento_704ILR = DateTime.Today.AddDays(-1),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 1000m
    };
    var rr2_704ILR = BLL_Reserva_704ILR.Crear_704ILR(pasada_704ILR, out _);
    Esperar_704ILR("result", rr2_704ILR, ReservaResult_704ILR.InvalidFecha_704ILR);

    // [9] Listado
    Console.WriteLine("[9] Total de reservas:");
    Console.WriteLine($"  {BLL_Reserva_704ILR.GetAll_704ILR().Count} reservas");

    // [10] Control de cambios: modificar la reserva recien creada
    if (rr1_704ILR == ReservaResult_704ILR.Success_704ILR)
    {
        Console.WriteLine($"[10] Modificar reserva #{nuevoId_704ILR} (estado + monto):");
        Adelanto_704ILR(nuevoId_704ILR, 1000m);   // RN-07: sin adelanto no se confirma
        var editada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(nuevoId_704ILR);
        editada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        editada_704ILR.Monto_704ILR = 175000m;
        var ru_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(editada_704ILR);
        Esperar_704ILR("result", ru_704ILR, ReservaResult_704ILR.Success_704ILR);

        Console.WriteLine($"[11] Historial de cambios de la reserva #{nuevoId_704ILR}:");
        var hist_704ILR = EvenTech.BLL.RegistradorDeCambios_704ILR.GetHistorial_704ILR("Reserva", nuevoId_704ILR);
        foreach (var c_704ILR in hist_704ILR)
            Console.WriteLine($"  {c_704ILR.Fecha_704ILR:HH:mm:ss} {c_704ILR.NombreCampo_704ILR,-14} '{c_704ILR.ValorAnterior_704ILR}' -> '{c_704ILR.ValorNuevo_704ILR}'");
        // La edicion toco dos campos de negocio auditados: Estado y Monto.
        Esperar_704ILR("campos registrados por el control de cambios", hist_704ILR.Count, 2);
    }

    // [12] Bitacora general (ultimas 5)
    Console.WriteLine("[12] Ultimas 5 entradas de bitacora:");
    int mostradas_704ILR = 0;
    foreach (var b_704ILR in EvenTech.BLL.BLL_Bitacora_704ILR.Buscar_704ILR(new EvenTech.BE.BitacoraFiltros_704ILR()))
    {
        Console.WriteLine($"  #{b_704ILR.Id_704ILR} {b_704ILR.Fecha_704ILR:HH:mm:ss} {b_704ILR.Modulo_704ILR,-10} {b_704ILR.Accion_704ILR,-26} {b_704ILR.Criticidad_704ILR}");
        if (++mostradas_704ILR >= 5) break;
    }

    // Limpieza: [10] dejo la reserva CONFIRMADA y asi bloquearia ese salon y esa
    // fecha en la proxima corrida. Se da de baja por la via de cancelacion, que
    // es la unica admitida para entrar a CANCELADA (RN-05) y la que liquida la RN-02.
    if (rr1_704ILR == ReservaResult_704ILR.Success_704ILR)
        Esperar_704ILR("limpieza (cancelar la reserva de [7]/[10])",
            BLL_Reserva_704ILR.Cancelar_704ILR(nuevoId_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
}
catch (Exception ex7_704ILR) { Excepcion_704ILR("[7]-[12]", ex7_704ILR); }

// [13] Composite de perfiles: recorrer arbol y permisos efectivos
Console.WriteLine("[13] Arbol de permisos (Composite):");
var arbol_704ILR = BLL_Perfil_704ILR.GetArbolPermisos_704ILR();
void Imprimir_704ILR(EvenTech.BE.BE_IComponentePermiso_704ILR n_704ILR, int nivel_704ILR)
{
    Console.WriteLine($"  {new string(' ', nivel_704ILR * 2)}{(n_704ILR.EsGrupo_704ILR ? "[G]" : "[P]")} {n_704ILR.Nombre_704ILR}");
    if (n_704ILR is EvenTech.BE.BE_GrupoPermisos_704ILR g_704ILR)
        foreach (var h_704ILR in g_704ILR.Hijos_704ILR) Imprimir_704ILR(h_704ILR, nivel_704ILR + 1);
}
foreach (var raiz_704ILR in arbol_704ILR) Imprimir_704ILR(raiz_704ILR, 0);

var perfiles_704ILR = BLL_Perfil_704ILR.GetPerfiles_704ILR();
if (perfiles_704ILR.Count > 0)
{
    // Se resuelve con el MISMO algoritmo que usa el login (Composite sobre
    // BE_Perfil): los permisos efectivos son las hojas que cubren los componentes
    // asignados, incluidas las que llegan por los perfiles incluidos.
    var asignados_704ILR = BLL_Perfil_704ILR.GetPermisosAsignados_704ILR(perfiles_704ILR[0].Id_704ILR);
    var efectivos_704ILR = BLL_Perfil_704ILR.GetPermisosEfectivosDePerfil_704ILR(perfiles_704ILR[0].Id_704ILR);
    Console.WriteLine($"[14] Perfil '{perfiles_704ILR[0].Nombre_704ILR}': {asignados_704ILR.Count} componente(s) asignado(s) " +
                      $"-> {efectivos_704ILR.Count} permisos efectivos (hojas).");
    Esperar_704ILR("el perfil resuelve al menos un permiso", efectivos_704ILR.Count > 0, true);
}

// [15] Idiomas (Observer): el gestor notifica a sus observadores cuando el idioma
// cambia en caliente. Se suscribe un observador de prueba —el mismo rol que cumple
// cada formulario de la aplicacion— y se cuenta cuantas veces lo llamo.
Console.WriteLine("[15] Idiomas (Observer):");
EvenTech.BLL.BLL_Idioma_704ILR.Inicializar_704ILR();
var gi_704ILR = EvenTech.Services.GestorDeIdioma_704ILR.GetInstance_704ILR;
var obs_704ILR = new ObservadorPrueba_704ILR();
gi_704ILR.Suscribir_704ILR(obs_704ILR);
Esperar_704ILR("idioma inicial", gi_704ILR.IdiomaActual_704ILR, "ES");
Esperar_704ILR("MENU_RESERVAS en ES", gi_704ILR.Traducir_704ILR("MENU_RESERVAS"), "Reservas");

gi_704ILR.CambiarIdioma_704ILR("EN");
Esperar_704ILR("idioma tras el cambio", gi_704ILR.IdiomaActual_704ILR, "EN");
Esperar_704ILR("notificaciones al observador", obs_704ILR.Llamadas_704ILR, 1);
Esperar_704ILR("MENU_RESERVAS en EN", gi_704ILR.Traducir_704ILR("MENU_RESERVAS"), "Reservations");

gi_704ILR.CambiarIdioma_704ILR("ES");
Esperar_704ILR("notificaciones tras volver a ES", obs_704ILR.Llamadas_704ILR, 2);

// Desuscribir corta la notificacion: un formulario cerrado no debe seguir avisado.
gi_704ILR.Desuscribir_704ILR(obs_704ILR);
gi_704ILR.CambiarIdioma_704ILR("EN");
Esperar_704ILR("notificaciones tras desuscribir", obs_704ILR.Llamadas_704ILR, 2);
gi_704ILR.CambiarIdioma_704ILR("ES");

// [16] Digitos verificadores (T07/T08)
Console.WriteLine("[16] Integridad (digitos verificadores):");
var resInt_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
Esperar_704ILR("Ok", resInt_704ILR.Ok_704ILR, true);
Esperar_704ILR("inconsistencias", resInt_704ILR.Inconsistencias_704ILR.Count, 0);
foreach (var i_704ILR in resInt_704ILR.Inconsistencias_704ILR) Console.WriteLine("   - " + i_704ILR);

// Recalculo de linea base: es la accion administrativa ante datos corruptos, no
// un paso de rutina. Antes se corria SIEMPRE, con dos efectos malos: dejaba un
// asiento de criticidad Advertencia por corrida y volvia tautologica la
// verificacion siguiente (reescribe todos los DV, no puede dar False). Ahora se
// ejecuta solo si la verificacion encontro algo, y ahi si se exige que limpie.
if (resInt_704ILR.Ok_704ILR)
{
    Console.WriteLine("  recalculo de linea base: no hizo falta (la verificacion dio limpia)");
}
else
{
    Console.WriteLine("  ATENCION: la linea base estaba inconsistente; se recalcula (accion administrativa)");
    int recalculadas_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.RecalcularTodo_704ILR();
    var resInt2_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
    Console.WriteLine($"  reservas recalculadas: {recalculadas_704ILR}");
    Esperar_704ILR("Ok tras el recalculo", resInt2_704ILR.Ok_704ILR, true);
}

// [17] Alta de idioma desde la capa de negocio (admin agrega idioma). 'PT' ya
// viene sembrado por db/schema.sql: lo que ejercita el caso es el rechazo del
// codigo duplicado, mas las dos validaciones de datos que no dejan residuo.
Console.WriteLine("[17] Crear idioma 'PT' (ya sembrado por schema.sql):");
var rIdioma_704ILR = EvenTech.BLL.BLL_Idioma_704ILR.CrearIdioma_704ILR("PT", "Portugues", out _);
Esperar_704ILR("codigo duplicado", rIdioma_704ILR, IdiomaResult_704ILR.CodigoDuplicado_704ILR);
Esperar_704ILR("codigo vacio", EvenTech.BLL.BLL_Idioma_704ILR.CrearIdioma_704ILR("", "Sin codigo", out _),
    IdiomaResult_704ILR.CodigoInvalido_704ILR);
Esperar_704ILR("nombre vacio", EvenTech.BLL.BLL_Idioma_704ILR.CrearIdioma_704ILR("XX", "", out _),
    IdiomaResult_704ILR.NombreInvalido_704ILR);

var codigos_704ILR = new SortedSet<string>(
    EvenTech.Services.GestorDeIdioma_704ILR.GetInstance_704ILR.IdiomasDisponibles_704ILR
        .Select(i_704ILR => i_704ILR.Codigo_704ILR), StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"  idiomas disponibles: {codigos_704ILR.Count} ({string.Join(", ", codigos_704ILR)})");
Esperar_704ILR("los tres idiomas del sistema presentes",
    codigos_704ILR.IsSupersetOf(new[] { "ES", "EN", "PT" }), true);

// [18] Patron Memento: versionado y restauracion de reservas
Console.WriteLine("[18] Memento (versiones de reserva):");
var clientesM_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
var salonesM_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
if (clientesM_704ILR.Count == 0 || salonesM_704ILR.Count == 0)
{
    Console.WriteLine("  (faltan clientes/salones seed; corre db/schema.sql)");
}
else try
{
    var reservaM_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = clientesM_704ILR[0].Id_704ILR,
        SalonId_704ILR = salonesM_704ILR[0].Id_704ILR,
        FechaEvento_704ILR = FechaLibre_704ILR(salonesM_704ILR[0].Id_704ILR, DateTime.Today.AddDays(1500 + desfasaje_704ILR)),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 1000m
    };
    var rm_704ILR = BLL_Reserva_704ILR.Crear_704ILR(reservaM_704ILR, out int idM_704ILR);
    Esperar_704ILR("alta", rm_704ILR, ReservaResult_704ILR.Success_704ILR);

    // Sin la reserva de prueba no hay nada que versionar: se corta el caso en
    // lugar de seguir sobre un null y llevarse puestos los casos siguientes.
    if (rm_704ILR == ReservaResult_704ILR.Success_704ILR)
    {
        Adelanto_704ILR(idM_704ILR, 500m);   // RN-07
        var v1_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idM_704ILR);
        v1_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        v1_704ILR.Monto_704ILR = 2000m;
        Esperar_704ILR("modificar (PENDIENTE/1000 -> CONFIRMADA/2000)",
            BLL_Reserva_704ILR.Actualizar_704ILR(v1_704ILR), ReservaResult_704ILR.Success_704ILR);

        var versiones_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idM_704ILR);
        Esperar_704ILR("versiones guardadas", versiones_704ILR.Count, 1);

        if (versiones_704ILR.Count > 0)
        {
            var rr_704ILR = BLL_Reserva_704ILR.RestaurarVersion_704ILR(idM_704ILR, versiones_704ILR[0].Id_704ILR);
            var restaurada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idM_704ILR);
            Esperar_704ILR("restaurar", rr_704ILR, ReservaResult_704ILR.Success_704ILR);
            Esperar_704ILR("estado repuesto", restaurada_704ILR.Estado_704ILR, EvenTech.BE.EstadoReserva_704ILR.PENDIENTE);
            Esperar_704ILR("monto repuesto", restaurada_704ILR.Monto_704ILR, 1000m);
            // La restauracion versiona el estado que piso: quedan dos versiones.
            Esperar_704ILR("versiones tras restaurar", CaretakerReserva_704ILR.GetVersiones_704ILR(idM_704ILR).Count, 2);
        }

        // Limpieza: la reserva de prueba se da de baja para no dejar residuo.
        Esperar_704ILR("limpieza (cancelar la reserva de [18])",
            BLL_Reserva_704ILR.Cancelar_704ILR(idM_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
    }
}
catch (Exception ex18_704ILR) { Excepcion_704ILR("[18]", ex18_704ILR); }

// [19] Composite de perfiles: un perfil incluye a otro y hereda sus permisos
Console.WriteLine("[19] Composite de perfiles (perfil incluye perfil):");
try
{
    var arbolC_704ILR = BLL_Perfil_704ILR.GetArbolPermisos_704ILR();

    // Busca el id de una hoja por su clave, recorriendo el arbol Composite.
    int BuscarClave_704ILR(IEnumerable<EvenTech.BE.BE_IComponentePermiso_704ILR> nodos_704ILR, string clave_704ILR)
    {
        foreach (var n_704ILR in nodos_704ILR)
        {
            if (n_704ILR is EvenTech.BE.BE_Permiso_704ILR p_704ILR && p_704ILR.Clave_704ILR == clave_704ILR) return p_704ILR.Id_704ILR;
            if (n_704ILR is EvenTech.BE.BE_GrupoPermisos_704ILR g_704ILR)
            {
                int r_704ILR = BuscarClave_704ILR(g_704ILR.Hijos_704ILR, clave_704ILR);
                if (r_704ILR > 0) return r_704ILR;
            }
        }
        return 0;
    }

    int idCrear_704ILR = BuscarClave_704ILR(arbolC_704ILR, "RESERVA_CREAR");
    int idEditar_704ILR = BuscarClave_704ILR(arbolC_704ILR, "RESERVA_EDITAR");
    int idBitacora_704ILR = BuscarClave_704ILR(arbolC_704ILR, "BITACORA_VER");

    Esperar_704ILR("alta del perfil Vendedor",
        BLL_Perfil_704ILR.CrearPerfil_704ILR("Vendedor_" + suf_704ILR, "smoke", out int idVend_704ILR),
        PerfilResult_704ILR.Success_704ILR);
    Esperar_704ILR("alta del perfil Gerencial",
        BLL_Perfil_704ILR.CrearPerfil_704ILR("Gerencial_" + suf_704ILR, "smoke", out int idGer_704ILR),
        PerfilResult_704ILR.Success_704ILR);

    var rVend_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR, idEditar_704ILR }, new int[0]);
    Esperar_704ILR("Vendedor (RESERVA_CREAR + RESERVA_EDITAR)", rVend_704ILR, PerfilResult_704ILR.Success_704ILR);

    var rGer_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idGer_704ILR, new[] { idBitacora_704ILR }, new[] { idVend_704ILR });
    Esperar_704ILR("Gerencial (BITACORA_VER + incluye Vendedor)", rGer_704ILR, PerfilResult_704ILR.Success_704ILR);

    // Las claves efectivas se comparan de verdad (antes solo se imprimian junto a
    // un "(esperado ...)" escrito a mano, que nadie contrastaba).
    var clavesGer_704ILR = new SortedSet<string>(
        BLL_Perfil_704ILR.GetPermisosEfectivosDePerfil_704ILR(idGer_704ILR).Select(p_704ILR => p_704ILR.Clave_704ILR),
        StringComparer.OrdinalIgnoreCase);
    Esperar_704ILR("permisos efectivos de Gerencial (BITACORA_VER propio + los dos heredados de Vendedor)",
        string.Join(", ", clavesGer_704ILR), "BITACORA_VER, RESERVA_CREAR, RESERVA_EDITAR");

    var rCiclo_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR, idEditar_704ILR }, new[] { idGer_704ILR });
    Esperar_704ILR("incluir Gerencial dentro de Vendedor", rCiclo_704ILR, PerfilResult_704ILR.ReferenciaCircular_704ILR);

    var rSelf_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR }, new[] { idVend_704ILR });
    Esperar_704ILR("incluir Vendedor dentro de si mismo", rSelf_704ILR, PerfilResult_704ILR.ReferenciaCircular_704ILR);

    // Denegar por defecto, sobre una sesion real y restringida: el usuario de
    // prueba recibe el perfil Vendedor y entra. Tiene que poder crear reservas y
    // NO poder anular pagos, que es un permiso que su perfil no contiene. Hasta
    // ahora la unica denegacion probada era una clave inexistente sobre admin.
    var uSmoke_704ILR = BLL_User_704ILR.GetAll_704ILR()
        .FirstOrDefault(u_704ILR => u_704ILR.Username_704ILR == newUser_704ILR);
    Esperar_704ILR("usuario de prueba disponible", uSmoke_704ILR != null, true);
    if (uSmoke_704ILR != null)
    {
        BLL_User_704ILR.AsignarPerfil_704ILR(uSmoke_704ILR.Id_704ILR, idVend_704ILR);
        var rLoginV_704ILR = BLL_Login_704ILR.Authenticate_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("pass1234"));
        Esperar_704ILR("login del usuario con perfil Vendedor", rLoginV_704ILR.Result_704ILR, LoginResult_704ILR.Success_704ILR);

        if (SessionManager_704ILR.IsSessionActive_704ILR)
        {
            var sV_704ILR = SessionManager_704ILR.GetInstance_704ILR;
            Esperar_704ILR("sesion con perfil", sV_704ILR.SinPerfil_704ILR, false);
            Esperar_704ILR("permisos resueltos", sV_704ILR.PermisosNoDisponibles_704ILR, false);
            Esperar_704ILR("Vendedor tiene RESERVA_CREAR", sV_704ILR.TienePermiso_704ILR("RESERVA_CREAR"), true);
            Esperar_704ILR("Vendedor tiene RESERVA_EDITAR", sV_704ILR.TienePermiso_704ILR("RESERVA_EDITAR"), true);
            Esperar_704ILR("Vendedor NO tiene PAGOS_ANULAR", sV_704ILR.TienePermiso_704ILR("PAGOS_ANULAR"), false);
            Esperar_704ILR("Vendedor NO tiene PERFILES_GESTION", sV_704ILR.TienePermiso_704ILR("PERFILES_GESTION"), false);
            Esperar_704ILR("permisos efectivos de la sesion", sV_704ILR.Permisos_704ILR.Count, 2);
            BLL_Login_704ILR.Logout_704ILR();
        }

        BLL_User_704ILR.AsignarPerfil_704ILR(uSmoke_704ILR.Id_704ILR, null);
    }

    // Limpieza: los perfiles de prueba se dan de baja. Corridas anteriores
    // llegaron a dejar 76 perfiles 'Vendedor_'/'Gerencial_' en la base de demostracion.
    BorrarPerfilDePrueba_704ILR(idGer_704ILR);
    BorrarPerfilDePrueba_704ILR(idVend_704ILR);
    var nombresPerfiles_704ILR = BLL_Perfil_704ILR.GetPerfiles_704ILR().Select(p_704ILR => p_704ILR.Nombre_704ILR).ToList();
    Esperar_704ILR("perfiles de prueba eliminados",
        nombresPerfiles_704ILR.Any(n_704ILR => n_704ILR.EndsWith(suf_704ILR, StringComparison.Ordinal)), false);
}
catch (Exception ex19_704ILR) { Excepcion_704ILR("[19]", ex19_704ILR); }

// [20] Cifrado reversible (AES) de datos sensibles del cliente
Console.WriteLine("[20] Alta de cliente (CUN002) y cifrado reversible de Email/Telefono:");
try
{
    var cli_704ILR = new EvenTech.BE.BE_Cliente_704ILR
    {
        Nombre_704ILR = "SmokeCrypto",
        Apellido_704ILR = suf_704ILR,
        Dni_704ILR = "9" + suf_704ILR,
        Email_704ILR = $"crypto_{suf_704ILR}@test.com",
        Telefono_704ILR = "11-5555-" + suf_704ILR
    };
    int altasCli_704ILR = Asientos_704ILR("Clientes");
    var rCli_704ILR = BLL_Cliente_704ILR.Crear_704ILR(cli_704ILR, out int idCli_704ILR);
    Esperar_704ILR("alta", rCli_704ILR, ClienteResult_704ILR.Success_704ILR);
    // Postcondicion del CUN002: el alta queda asentada en la bitacora.
    Esperar_704ILR("asientos del modulo Clientes tras el alta", Asientos_704ILR("Clientes"), altasCli_704ILR + 1);

    var leido_704ILR = BLL_Cliente_704ILR.GetById_704ILR(idCli_704ILR);
    Console.WriteLine($"  leido por la app: Email='{leido_704ILR.Email_704ILR}', Telefono='{leido_704ILR.Telefono_704ILR}'");
    Esperar_704ILR("roundtrip del email (cifrar -> descifrar)", leido_704ILR.Email_704ILR, cli_704ILR.Email_704ILR);
    Esperar_704ILR("roundtrip del telefono (cifrar -> descifrar)", leido_704ILR.Telefono_704ILR, cli_704ILR.Telefono_704ILR);
    Esperar_704ILR("DNI persistido", leido_704ILR.Dni_704ILR, cli_704ILR.Dni_704ILR);

    // Flujos alternativos del CUN002 (3.1 DNI ya registrado, 3.2 datos invalidos):
    // ninguno tenia una sola asercion, aunque los tres resultados estan implementados.
    var dup_704ILR = new EvenTech.BE.BE_Cliente_704ILR
    { Nombre_704ILR = "Otro", Apellido_704ILR = "Cliente", Dni_704ILR = "9" + suf_704ILR };
    Esperar_704ILR("alta con el mismo DNI", BLL_Cliente_704ILR.Crear_704ILR(dup_704ILR, out _),
        ClienteResult_704ILR.DniDuplicado_704ILR);

    var sinNombre_704ILR = new EvenTech.BE.BE_Cliente_704ILR { Nombre_704ILR = "  ", Apellido_704ILR = "SinNombre" };
    Esperar_704ILR("alta sin nombre", BLL_Cliente_704ILR.Crear_704ILR(sinNombre_704ILR, out _),
        ClienteResult_704ILR.NombreInvalido_704ILR);

    var mailMalo_704ILR = new EvenTech.BE.BE_Cliente_704ILR
    { Nombre_704ILR = "Mail", Apellido_704ILR = "Invalido", Email_704ILR = "sin-arroba" };
    Esperar_704ILR("alta con email invalido", BLL_Cliente_704ILR.Crear_704ILR(mailMalo_704ILR, out _),
        ClienteResult_704ILR.EmailInvalido_704ILR);

    // Lectura cruda, salteando la DAL: en la DB tiene que estar cifrado.
    using (var cn_704ILR = new EvenTech.DAL.DAL_DB_Connection_704ILR())
    using (var cmd_704ILR = new Microsoft.Data.SqlClient.SqlCommand(
        "SELECT Email, Telefono FROM dbo.Clientes WHERE Id = @id", cn_704ILR.OpenConnection_704ILR()))
    {
        cmd_704ILR.Parameters.AddWithValue("@id", idCli_704ILR);
        using var r_704ILR = cmd_704ILR.ExecuteReader();
        if (r_704ILR.Read())
        {
            string rawE_704ILR = r_704ILR.GetString(0), rawT_704ILR = r_704ILR.GetString(1);
            Console.WriteLine($"  crudo en DB: Email='{rawE_704ILR[..Math.Min(44, rawE_704ILR.Length)]}...'");
            Esperar_704ILR("email cifrado en la base", CryptoService_704ILR.EstaProtegido_704ILR(rawE_704ILR), true);
            Esperar_704ILR("telefono cifrado en la base", CryptoService_704ILR.EstaProtegido_704ILR(rawT_704ILR), true);
        }
    }

    // Limpieza: el cliente de prueba se borra para no engordar la base de
    // demostracion (no lo referencia ninguna reserva: se creo aca y solo aca).
    using (var cnDel_704ILR = new EvenTech.DAL.DAL_DB_Connection_704ILR())
    using (var del_704ILR = new Microsoft.Data.SqlClient.SqlCommand(
        "DELETE FROM dbo.Clientes WHERE Id = @id AND NOT EXISTS (SELECT 1 FROM dbo.Reservas WHERE ClienteId = @id)",
        cnDel_704ILR.OpenConnection_704ILR()))
    {
        del_704ILR.Parameters.AddWithValue("@id", idCli_704ILR);
        del_704ILR.ExecuteNonQuery();
    }
    Esperar_704ILR("limpieza (cliente de prueba eliminado)", BLL_Cliente_704ILR.GetById_704ILR(idCli_704ILR) == null, true);
}
catch (Exception ex20_704ILR) { Excepcion_704ILR("[20]", ex20_704ILR); }

// [21] Control de acceso: los permisos se conceden solo si estan en el perfil
// (denegar por defecto). Se valida sobre la sesion real de admin.
Console.WriteLine("[21] Permisos de la sesion (denegar por defecto):");
try
{
    BLL_Login_704ILR.Authenticate_704ILR("admin", Encrypt_704ILR.HashValue_704ILR("admin123"));
    var s_704ILR = SessionManager_704ILR.GetInstance_704ILR;
    Esperar_704ILR("permisos no disponibles", s_704ILR.PermisosNoDisponibles_704ILR, false);
    Esperar_704ILR("admin sin perfil", s_704ILR.SinPerfil_704ILR, false);
    Esperar_704ILR("admin tiene RESERVA_CREAR", s_704ILR.TienePermiso_704ILR("RESERVA_CREAR"), true);
    Esperar_704ILR("admin tiene PAGOS_ANULAR", s_704ILR.TienePermiso_704ILR("PAGOS_ANULAR"), true);
    Esperar_704ILR("clave inexistente NO_EXISTE", s_704ILR.TienePermiso_704ILR("NO_EXISTE"), false);
    Esperar_704ILR("clave nula", s_704ILR.TienePermiso_704ILR(null), false);
    BLL_Login_704ILR.Logout_704ILR();
}
catch (Exception ex21_704ILR) { Excepcion_704ILR("[21]", ex21_704ILR); }

// [22] Todas las claves que la UI exige tienen que existir en el arbol: si una
// falta, la seccion queda invisible para todos y el problema pasa inadvertido.
Console.WriteLine("[22] Claves de permiso usadas por la UI presentes en el arbol:");
{
    string[] usadas_704ILR = { "RESERVA_CREAR", "RESERVA_EDITAR", "RESERVA_HISTORIAL",
                        "CLIENTES_GESTION", "SERVICIOS_GESTION", "PERFILES_GESTION",
                        "IDIOMAS_GESTION", "BITACORA_VER", "AUDIT_LOGIN_VER",
                        "INTEGRIDAD_RECALC", "PAGOS_REGISTRAR", "PAGOS_ANULAR",
                        "DISPONIBILIDAD_CONSULTAR" };
    var enArbol_704ILR = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    void Recorrer_704ILR(IEnumerable<EvenTech.BE.BE_IComponentePermiso_704ILR> nodos_704ILR)
    {
        foreach (var n_704ILR in nodos_704ILR)
        {
            if (n_704ILR is EvenTech.BE.BE_Permiso_704ILR hoja_704ILR && !string.IsNullOrEmpty(hoja_704ILR.Clave_704ILR)) enArbol_704ILR.Add(hoja_704ILR.Clave_704ILR);
            if (n_704ILR is EvenTech.BE.BE_GrupoPermisos_704ILR g_704ILR) Recorrer_704ILR(g_704ILR.Hijos_704ILR);
        }
    }
    Recorrer_704ILR(BLL_Perfil_704ILR.GetArbolPermisos_704ILR());
    var faltan_704ILR = usadas_704ILR.Where(c_704ILR => !enArbol_704ILR.Contains(c_704ILR)).ToList();
    Console.WriteLine($"  claves en el arbol: {enArbol_704ILR.Count}");
    Esperar_704ILR("claves de la UI que faltan en el arbol",
        faltan_704ILR.Count == 0 ? "ninguna" : string.Join(", ", faltan_704ILR), "ninguna");
}

// [23] Una reserva cancelada es estado terminal: no admite modificaciones.
Console.WriteLine("[23] Reserva cancelada no modificable:");
try
{
    var sal_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    var cli_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    if (sal_704ILR.Count == 0 || cli_704ILR.Count == 0)
    {
        Console.WriteLine("  (no hay salones/clientes seed; corre db/schema.sql)");
    }
    else
    {
        // RN-05: no se puede nacer cancelado. Se da de alta pendiente y se cancela
        // por la via correcta, que es la unica que liquida la RN-02.
        var altaDirecta_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(2500 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CANCELADA,
            Monto_704ILR = 1000m
        };
        Esperar_704ILR("alta directa en CANCELADA", BLL_Reserva_704ILR.Crear_704ILR(altaDirecta_704ILR, out _),
            ReservaResult_704ILR.TransicionInvalida_704ILR);

        var res_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(2500 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
            CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
            Monto_704ILR = 1000m
        };
        var rAlta_704ILR = BLL_Reserva_704ILR.Crear_704ILR(res_704ILR, out int idCancel_704ILR);
        Esperar_704ILR("alta pendiente", rAlta_704ILR, ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("cancelar", BLL_Reserva_704ILR.Cancelar_704ILR(idCancel_704ILR, out _, out _),
            ReservaResult_704ILR.Success_704ILR);

        var guardada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCancel_704ILR);
        Esperar_704ILR("PuedeModificar", BLL_Reserva_704ILR.PuedeModificar_704ILR(guardada_704ILR), false);

        guardada_704ILR.Monto_704ILR = 2000m;
        var rMod_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(guardada_704ILR);
        Esperar_704ILR("intento de modificar", rMod_704ILR, ReservaResult_704ILR.NoModificable_704ILR);

        // Una reserva viva si se modifica.
        var viva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCancel_704ILR);
        viva_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE;
        Esperar_704ILR("PuedeModificar sobre PENDIENTE", BLL_Reserva_704ILR.PuedeModificar_704ILR(viva_704ILR), true);

        // Los pagos persisten en el acto, sin pasar por BLL_Reserva.Actualizar:
        // la regla del estado terminal tiene que rechazarlos tambien.
        var metodos_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
        if (metodos_704ILR.Count > 0)
        {
            var pago_704ILR = new EvenTech.BE.BE_Pago_704ILR { ReservaId_704ILR = idCancel_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = 10m };
            var rPago_704ILR = BLL_Pago_704ILR.Registrar_704ILR(pago_704ILR, out _);
            Esperar_704ILR("cobrar sobre cancelada", rPago_704ILR, PagoResult_704ILR.ReservaCancelada_704ILR);
        }
    }
}
catch (Exception ex23_704ILR) { Excepcion_704ILR("[23]", ex23_704ILR); }

// [24] Configuracion de conexion: la cadena sale del gestor (no hardcodeada) y
// el diagnostico distingue servidor caido de base inexistente.
Console.WriteLine("[24] Configuracion de conexion:");
try
{
    Console.WriteLine($"  configurada por el usuario: {BLL_Conexion_704ILR.EstaConfigurada_704ILR}");
    Console.WriteLine($"  servidor='{BLL_Conexion_704ILR.ServidorActual_704ILR}', base='{BLL_Conexion_704ILR.BaseDatosActual_704ILR}'");

    bool ok_704ILR = BLL_Conexion_704ILR.VerificarActual_704ILR(out string msgOk_704ILR);
    Esperar_704ILR("verificar actual", ok_704ILR, true);
    if (!ok_704ILR) Console.WriteLine($"    diagnostico: {msgOk_704ILR}");

    bool inexistente_704ILR = BLL_Conexion_704ILR.Probar_704ILR(EvenTech.Services.ConfiguracionConexion_704ILR.ServidorActual_704ILR,
                                           "BaseQueNoExiste_" + suf_704ILR, out string msgNo_704ILR);
    Esperar_704ILR("base inexistente aceptada", inexistente_704ILR, false);
    Console.WriteLine($"    diagnostico: {msgNo_704ILR}");

    Console.WriteLine($"  instancias detectadas: {BLL_Conexion_704ILR.GetInstancias_704ILR().Count}");

    // Roundtrip del archivo cifrado con DPAPI: si guardar/leer fallara, la app
    // quedaria sin poder conectar en el proximo arranque. Se prueba con la
    // configuracion que ya funciona y se deja el entorno como estaba.
    bool estabaConfigurada_704ILR = BLL_Conexion_704ILR.EstaConfigurada_704ILR;
    string servidorPrevio_704ILR = BLL_Conexion_704ILR.ServidorActual_704ILR, basePrevia_704ILR = BLL_Conexion_704ILR.BaseDatosActual_704ILR;

    bool guardo_704ILR = BLL_Conexion_704ILR.Guardar_704ILR(servidorPrevio_704ILR, basePrevia_704ILR, out string msgGuardar_704ILR);
    Esperar_704ILR("guardar cifrado (DPAPI)", guardo_704ILR, true);
    if (!guardo_704ILR) Console.WriteLine($"    diagnostico: {msgGuardar_704ILR}");
    Esperar_704ILR("persistida", BLL_Conexion_704ILR.EstaConfigurada_704ILR, true);

    bool releeOk_704ILR = BLL_Conexion_704ILR.VerificarActual_704ILR(out _);
    Esperar_704ILR("releida y conecta", releeOk_704ILR, true);
    Esperar_704ILR("servidor releido", BLL_Conexion_704ILR.ServidorActual_704ILR, servidorPrevio_704ILR);
    Esperar_704ILR("base releida", BLL_Conexion_704ILR.BaseDatosActual_704ILR, basePrevia_704ILR);

    if (!estabaConfigurada_704ILR)
    {
        BLL_Conexion_704ILR.Restablecer_704ILR();
        Esperar_704ILR("entorno restaurado (sin archivo)", BLL_Conexion_704ILR.EstaConfigurada_704ILR, false);
    }
}
catch (Exception ex24_704ILR) { Excepcion_704ILR("[24]", ex24_704ILR); }

// [25] Diagnostico de conexion: una base sin el esquema tiene que rechazarse, si
// no la app quedaria conectada a una base inservible sin volver a ofrecer configurar.
Console.WriteLine("[25] Base existente pero sin esquema:");
{
    const string tmpDb_704ILR = "EvenTechSmokeVacia";
    string cs_704ILR = EvenTech.Services.ConfiguracionConexion_704ILR.Construir_704ILR(
        EvenTech.Services.ConfiguracionConexion_704ILR.ServidorActual_704ILR, tmpDb_704ILR);
    try
    {
        using (var cn_704ILR = new Microsoft.Data.SqlClient.SqlConnection(
            EvenTech.Services.ConfiguracionConexion_704ILR.Construir_704ILR(EvenTech.Services.ConfiguracionConexion_704ILR.ServidorActual_704ILR, "master")))
        {
            cn_704ILR.Open();
            using var crear_704ILR = new Microsoft.Data.SqlClient.SqlCommand(
                $"IF DB_ID('{tmpDb_704ILR}') IS NULL CREATE DATABASE [{tmpDb_704ILR}]", cn_704ILR);
            crear_704ILR.ExecuteNonQuery();
        }

        bool ok_704ILR = EvenTech.DAL.DAL_DB_Connection_704ILR.Probar_704ILR(cs_704ILR, out string msg_704ILR);
        Esperar_704ILR("base sin esquema aceptada", ok_704ILR, false);
        Console.WriteLine($"    diagnostico: {msg_704ILR}");
    }
    catch (Exception ex_704ILR)
    {
        Console.WriteLine($"  (no se pudo crear la base de prueba: {ex_704ILR.Message})");
    }
    finally
    {
        try
        {
            using var cn_704ILR = new Microsoft.Data.SqlClient.SqlConnection(
                EvenTech.Services.ConfiguracionConexion_704ILR.Construir_704ILR(EvenTech.Services.ConfiguracionConexion_704ILR.ServidorActual_704ILR, "master"));
            cn_704ILR.Open();
            using var borrar_704ILR = new Microsoft.Data.SqlClient.SqlCommand(
                $"IF DB_ID('{tmpDb_704ILR}') IS NOT NULL BEGIN ALTER DATABASE [{tmpDb_704ILR}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{tmpDb_704ILR}]; END", cn_704ILR);
            borrar_704ILR.ExecuteNonQuery();
            Console.WriteLine("  base de prueba eliminada");
        }
        catch (Exception ex_704ILR) { Console.WriteLine($"  (no se pudo limpiar la base de prueba: {ex_704ILR.Message})"); }
    }
}

// [26] Flujo completo del Proceso 1 (RF1): cotizacion con servicios -> total =
// suma de subtotales -> confirmacion (anti-solapamiento) -> adelanto y saldo
// (tope = total). Es el happy path que la UI recorre pantalla por pantalla.
Console.WriteLine("[26] Flujo RF1 completo (servicios, confirmacion, pagos):");
try
{
    var sal_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    var cli_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    var srv_704ILR = BLL_Servicio_704ILR.GetActivos_704ILR();
    if (sal_704ILR.Count == 0 || cli_704ILR.Count == 0 || srv_704ILR.Count < 2)
    {
        Console.WriteLine("  (faltan salones/clientes/servicios seed; corre db/schema.sql)");
    }
    else
    {
        // Fecha propia de la corrida y ademas libre de verdad para ese salon. La
        // ventana cercana (+60 dias) se cruza con los datos de demostracion, asi
        // que la fecha se resuelve con la consulta de disponibilidad en lugar de
        // apostar a que el dia elegido este vacio: si no, la confirmacion del
        // happy path fallaba por SalonOcupado sin que hubiera nada roto.
        DateTime fechaEvento_704ILR = FechaLibre_704ILR(sal_704ILR[0].Id_704ILR, DateTime.Today.AddDays(60 + desfasaje_704ILR));
        Console.WriteLine($"  salon '{sal_704ILR[0].Nombre_704ILR}', fecha del evento {fechaEvento_704ILR:yyyy-MM-dd}");

        // Cotizacion: no compromete el salon. El monto es la suma de servicios.
        var servicios_704ILR = new List<EvenTech.BE.BE_ReservaServicio_704ILR>
        {
            new EvenTech.BE.BE_ReservaServicio_704ILR { ServicioId_704ILR = srv_704ILR[0].Id_704ILR, Cantidad_704ILR = 2, PrecioUnitario_704ILR = srv_704ILR[0].Precio_704ILR },
            new EvenTech.BE.BE_ReservaServicio_704ILR { ServicioId_704ILR = srv_704ILR[1].Id_704ILR, Cantidad_704ILR = 1, PrecioUnitario_704ILR = srv_704ILR[1].Precio_704ILR }
        };
        decimal total_704ILR = BLL_ReservaServicio_704ILR.Total_704ILR(servicios_704ILR);
        decimal esperado_704ILR = srv_704ILR[0].Precio_704ILR * 2 + srv_704ILR[1].Precio_704ILR;
        Esperar_704ILR("total de servicios", total_704ILR, esperado_704ILR);

        var cot_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = fechaEvento_704ILR,
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 50,   // RN-06: sin este dato no se puede confirmar
            Monto_704ILR = total_704ILR
        };
        // Cabecera y lineas en una sola transaccion, igual que la aplicacion.
        var rCot_704ILR = BLL_Reserva_704ILR.Crear_704ILR(cot_704ILR, servicios_704ILR, out int idFlujo_704ILR);
        Esperar_704ILR("alta cotizacion", rCot_704ILR, ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("servicios persistidos", BLL_ReservaServicio_704ILR.GetByReserva_704ILR(idFlujo_704ILR).Count, servicios_704ILR.Count);

        // RN-07: el adelanto se cobra ANTES de confirmar. Ese es el orden del proceso
        // de negocio: se registra la operacion, se cobra y recien ahi queda firme.
        var metodos_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
        Esperar_704ILR("metodos de pago", metodos_704ILR.Count, 5);
        decimal adelanto_704ILR = Math.Round(total_704ILR / 2, 2);
        var rAde_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = adelanto_704ILR, Observacion_704ILR = "Adelanto" }, out _);
        Esperar_704ILR($"adelanto de {adelanto_704ILR:0.00}", rAde_704ILR, PagoResult_704ILR.Success_704ILR);
        Esperar_704ILR("saldo tras adelanto", BLL_Pago_704ILR.Saldo_704ILR(idFlujo_704ILR), total_704ILR - adelanto_704ILR);

        // Confirmar: recien aca se compromete el salon.
        var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR);
        reserva_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        var rConf_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(reserva_704ILR);
        Esperar_704ILR("confirmar", rConf_704ILR, ReservaResult_704ILR.Success_704ILR);

        // RN-03: el salon queda comprometido SOLO para reservas firmes. Una
        // cotizacion y una reserva pendiente para el mismo salon y la misma fecha
        // tienen que poder convivir con la confirmada; lo que no puede es una
        // SEGUNDA confirmada. Antes solo se probaba el rechazo: si alguien
        // cambiaba el filtro a "distinto de CANCELADA" la corrida seguia verde.
        var choque_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = fechaEvento_704ILR,
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
            CantidadInvitados_704ILR = 70,   // RN-06: sin este dato no se puede confirmar
            Monto_704ILR = 1000m
        };
        Esperar_704ILR("pendiente sobre salon/fecha ya confirmados",
            BLL_Reserva_704ILR.Crear_704ILR(choque_704ILR, out int idChoque_704ILR), ReservaResult_704ILR.Success_704ILR);

        var coex_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = fechaEvento_704ILR,
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 40,
            Monto_704ILR = 500m
        };
        Esperar_704ILR("cotizacion sobre salon/fecha ya confirmados",
            BLL_Reserva_704ILR.Crear_704ILR(coex_704ILR, out int idCoex_704ILR), ReservaResult_704ILR.Success_704ILR);

        // La pendiente nace con su adelanto cobrado (RN-07), de modo que lo unico
        // que puede rechazar su confirmacion es el salon ya comprometido.
        Adelanto_704ILR(idChoque_704ILR, 100m);
        var aChocar_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idChoque_704ILR);
        aChocar_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        var rChoque_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(aChocar_704ILR);
        Esperar_704ILR("segunda confirmada mismo salon/fecha", rChoque_704ILR, ReservaResult_704ILR.SalonOcupado_704ILR);
        BLL_Reserva_704ILR.Cancelar_704ILR(idChoque_704ILR, out _, out _);   // limpieza

        var rExceso_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = total_704ILR }, out _);
        Esperar_704ILR("pago que excede el saldo", rExceso_704ILR, PagoResult_704ILR.ExcedeSaldo_704ILR);

        var rSaldo_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[metodos_704ILR.Count - 1].Id_704ILR, Monto_704ILR = total_704ILR - adelanto_704ILR, Observacion_704ILR = "Saldo" }, out _);
        Esperar_704ILR("saldo restante", rSaldo_704ILR, PagoResult_704ILR.Success_704ILR);
        Esperar_704ILR("saldo final", BLL_Pago_704ILR.Saldo_704ILR(idFlujo_704ILR), 0m);

        // RN-04 como invariante: con el total ya cobrado, una edicion que achique la
        // reserva por debajo de lo pagado (quitar servicios) se rechaza.
        var achicada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR);
        achicada_704ILR.Monto_704ILR = achicada_704ILR.Monto_704ILR / 2;
        Esperar_704ILR("reducir el total por debajo de lo pagado",
            BLL_Reserva_704ILR.Actualizar_704ILR(achicada_704ILR), ReservaResult_704ILR.MontoInferiorPagado_704ILR);

        // Memento sobre la operacion COMPLETA: la version guarda tambien los
        // servicios contratados, asi que restaurarla tiene que reponerlos. Se
        // quita una linea (sin bajar el total: la RN-04 no lo permitiria con todo
        // cobrado) y se vuelve a la version anterior.
        var unServicio_704ILR = new List<EvenTech.BE.BE_ReservaServicio_704ILR>
        {
            new EvenTech.BE.BE_ReservaServicio_704ILR { ServicioId_704ILR = srv_704ILR[0].Id_704ILR, Cantidad_704ILR = 2, PrecioUnitario_704ILR = srv_704ILR[0].Precio_704ILR }
        };
        var recortada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR);
        Esperar_704ILR("quitar una linea de servicio",
            BLL_Reserva_704ILR.Actualizar_704ILR(recortada_704ILR, unServicio_704ILR), ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("servicios tras la edicion", BLL_ReservaServicio_704ILR.GetByReserva_704ILR(idFlujo_704ILR).Count, 1);

        var versionesFlujo_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idFlujo_704ILR);
        Esperar_704ILR("versiones de la reserva del flujo", versionesFlujo_704ILR.Count, 2);
        Esperar_704ILR("restaurar la version con las dos lineas",
            BLL_Reserva_704ILR.RestaurarVersion_704ILR(idFlujo_704ILR, versionesFlujo_704ILR[0].Id_704ILR),
            ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("servicios repuestos por la restauracion",
            BLL_ReservaServicio_704ILR.GetByReserva_704ILR(idFlujo_704ILR).Count, 2);
        Esperar_704ILR("monto tras la restauracion", BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR).Monto_704ILR, total_704ILR);

        // RN-04 tambien al restaurar: reponer una version mas barata que lo ya
        // cobrado dejaria el total por debajo del tope de cobranza. Se prueba en una
        // operacion propia, porque en la del flujo todas las versiones valen lo mismo.
        var barata_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(6000 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 10,
            Monto_704ILR = 1000m
        };
        Esperar_704ILR("alta de la cotizacion barata",
            BLL_Reserva_704ILR.Crear_704ILR(barata_704ILR, out int idBarata_704ILR), ReservaResult_704ILR.Success_704ILR);
        var ampliada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idBarata_704ILR);
        ampliada_704ILR.Monto_704ILR = 3000m;
        Esperar_704ILR("ampliar el total a 3000 (versiona el de 1000)",
            BLL_Reserva_704ILR.Actualizar_704ILR(ampliada_704ILR), ReservaResult_704ILR.Success_704ILR);
        Adelanto_704ILR(idBarata_704ILR, 2000m);
        var versionesBarata_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idBarata_704ILR);
        Esperar_704ILR("restaurar una version por debajo de lo cobrado",
            BLL_Reserva_704ILR.RestaurarVersion_704ILR(idBarata_704ILR, versionesBarata_704ILR[0].Id_704ILR),
            ReservaResult_704ILR.MontoInferiorPagado_704ILR);
        Esperar_704ILR("el total no se movio", BLL_Reserva_704ILR.GetById_704ILR(idBarata_704ILR).Monto_704ILR, 3000m);
        BLL_Reserva_704ILR.Cancelar_704ILR(idBarata_704ILR, out _, out _);   // limpieza

        // [27] Consulta de disponibilidad (Proceso 1, paso 1): la fecha recien
        // confirmada tiene que figurar ocupada para ese salon, con una fecha
        // alternativa propuesta; una capacidad imposible marca insuficiente.
        Console.WriteLine("[27] Consulta de disponibilidad:");
        var disp_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR, 0);
        Esperar_704ILR("salones evaluados", disp_704ILR.Count, sal_704ILR.Count);
        var delFlujo_704ILR = disp_704ILR.FirstOrDefault(d_704ILR => d_704ILR.SalonId_704ILR == sal_704ILR[0].Id_704ILR);
        Esperar_704ILR("salon confirmado libre", delFlujo_704ILR?.Libre_704ILR, false);
        Esperar_704ILR("propone una fecha alternativa", delFlujo_704ILR?.ProximaFechaLibre_704ILR.HasValue, true);
        if (delFlujo_704ILR?.ProximaFechaLibre_704ILR != null)
            Console.WriteLine($"    propuesta: {delFlujo_704ILR.ProximaFechaLibre_704ILR.Value:yyyy-MM-dd}");

        var dispCap_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR, 99999);
        Esperar_704ILR("capacidad imposible -> disponibles", dispCap_704ILR.Count(d_704ILR => d_704ILR.Disponible_704ILR), 0);
        Esperar_704ILR("capacidad imposible -> suficientes", dispCap_704ILR.Count(d_704ILR => d_704ILR.CapacidadSuficiente_704ILR), 0);
        // Sin capacidad suficiente no se calcula propuesta alternativa: ofrecer otra
        // fecha de un salon que igual no entra no le sirve a nadie (paso 4 del CUN001).
        Esperar_704ILR("capacidad imposible -> sin propuesta alternativa",
            dispCap_704ILR.All(d_704ILR => !d_704ILR.ProximaFechaLibre_704ILR.HasValue), true);

        // Flujo alternativo 2.1 del CUN001: una fecha anterior a hoy se ajusta al dia
        // de hoy en lugar de rechazarse (el vendedor consulta "a partir de").
        var dispPasado_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(DateTime.Today.AddDays(-5), 0);
        Esperar_704ILR("fecha pasada ajustada a hoy",
            dispPasado_704ILR.All(d_704ILR => d_704ILR.FechaConsultada_704ILR == DateTime.Today), true);

        // Un dia sin reservas confirmadas: todos los salones libres.
        var dispLibre_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR.AddDays(2000), 0);
        Esperar_704ILR("fecha lejana -> disponibles",
            dispLibre_704ILR.Count(d_704ILR => d_704ILR.Disponible_704ILR), dispLibre_704ILR.Count);

        // Limpieza: se cancela la reserva del flujo para liberar el salon
        // (la corrida queda repetible aunque la fecha se repitiera). Se usa la via
        // de cancelacion, que es la unica admitida para entrar a CANCELADA (RN-05)
        // y la que liquida la politica de reintegro (RN-02).
        var rFin_704ILR = BLL_Reserva_704ILR.Cancelar_704ILR(idFlujo_704ILR, out _, out _);
        Esperar_704ILR("limpieza (cancelar reserva del flujo)", rFin_704ILR, ReservaResult_704ILR.Success_704ILR);

        // RN-03, otra vez: una reserva CANCELADA deja de comprometer el salon. Con
        // la del flujo dada de baja, ese mismo salon y esa misma fecha vuelven a
        // admitir una reserva firme.
        Esperar_704ILR("salon liberado tras la cancelacion",
            BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR, 0)
                .First(d_704ILR => d_704ILR.SalonId_704ILR == sal_704ILR[0].Id_704ILR).Libre_704ILR, true);

        var reocupa_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCoex_704ILR);
        reocupa_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Adelanto_704ILR(idCoex_704ILR, 100m);   // RN-07
        Esperar_704ILR("confirmar sobre la fecha liberada",
            BLL_Reserva_704ILR.Actualizar_704ILR(reocupa_704ILR), ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("limpieza (cancelar la reserva de coexistencia)",
            BLL_Reserva_704ILR.Cancelar_704ILR(idCoex_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
    }
}
catch (Exception ex26_704ILR) { Excepcion_704ILR("[26]-[27]", ex26_704ILR); }

// [28] RN-01 Vigencia de la operacion: una COTIZACION nace con fecha de
// vencimiento, una CONFIRMADA no; una operacion vencida no se puede confirmar
// hasta renovarla.
Console.WriteLine("[28] RN-01 vigencia de la operacion:");
try
{
    int cliRn_704ILR = BLL_Cliente_704ILR.GetAll_704ILR().First().Id_704ILR;
    int salRn_704ILR = BLL_Salon_704ILR.GetAll_704ILR().First().Id_704ILR;
    DateTime fechaRn_704ILR = DateTime.Today.AddDays(3000 + desfasaje_704ILR);

    var cot_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = cliRn_704ILR,
        SalonId_704ILR = salRn_704ILR,
        FechaEvento_704ILR = fechaRn_704ILR,
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
        CantidadInvitados_704ILR = 50,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 1000m
    };
    var rCot_704ILR = BLL_Reserva_704ILR.Crear_704ILR(cot_704ILR, out int idCot_704ILR);
    var leida_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    Esperar_704ILR("alta cotizacion", rCot_704ILR, ReservaResult_704ILR.Success_704ILR);
    Esperar_704ILR("nace con fecha de vencimiento", leida_704ILR.VenceEl_704ILR.HasValue, true);
    Esperar_704ILR("vencida hoy", leida_704ILR.EstaVencida_704ILR, false);

    int diasEsperados_704ILR = BLL_Reserva_704ILR.DiasValidezCotizacion_704ILR;
    int diasReales_704ILR = leida_704ILR.VenceEl_704ILR.HasValue
        ? (int)Math.Round((leida_704ILR.VenceEl_704ILR.Value - DateTime.Now).TotalDays) : -1;
    Esperar_704ILR("plazo en dias", diasReales_704ILR, diasEsperados_704ILR);

    // Al confirmar, la operacion deja de tener plazo.
    Adelanto_704ILR(idCot_704ILR, 100m);   // RN-07
    leida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    var rConf_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(leida_704ILR);
    var confirmada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    Esperar_704ILR("confirmar", rConf_704ILR, ReservaResult_704ILR.Success_704ILR);
    Esperar_704ILR("vence tras confirmar", confirmada_704ILR.VenceEl_704ILR.HasValue, false);

    // El vencimiento se prueba sobre una SEGUNDA cotizacion: una vez confirmada,
    // la RN-05 ya no admite volver a COTIZACION (no se puede "desconfirmar").
    var cot2_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = cliRn_704ILR,
        SalonId_704ILR = salRn_704ILR,
        FechaEvento_704ILR = fechaRn_704ILR.AddDays(1),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
        CantidadInvitados_704ILR = 50,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 500m
    };
    BLL_Reserva_704ILR.Crear_704ILR(cot2_704ILR, out int idCot2_704ILR);

    var paraVencer_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot2_704ILR);
    paraVencer_704ILR.VenceEl_704ILR = DateTime.Now.AddDays(-1);
    EvenTech.DAL.DAL_Reserva_704ILR.Update_704ILR(paraVencer_704ILR);

    var vencida_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot2_704ILR);
    Esperar_704ILR("vencida forzada", vencida_704ILR.EstaVencida_704ILR, true);
    vencida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    var rVenc_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(vencida_704ILR);
    Esperar_704ILR("confirmar vencida", rVenc_704ILR, ReservaResult_704ILR.Vencida_704ILR);

    var rRen_704ILR = BLL_Reserva_704ILR.Renovar_704ILR(idCot2_704ILR);
    Esperar_704ILR("renovar", rRen_704ILR, ReservaResult_704ILR.Success_704ILR);
    var renovada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot2_704ILR);
    Esperar_704ILR("vencida tras renovar", renovada_704ILR.EstaVencida_704ILR, false);
    Adelanto_704ILR(idCot2_704ILR, 100m);   // RN-07
    renovada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Esperar_704ILR("confirmar tras renovar", BLL_Reserva_704ILR.Actualizar_704ILR(renovada_704ILR),
        ReservaResult_704ILR.Success_704ILR);
    BLL_Reserva_704ILR.Cancelar_704ILR(idCot2_704ILR, out _, out _);   // limpieza: libera el salon

    // Rama PENDIENTE de la RN-01: 72 horas de vigencia desde que entro al estado.
    var pen_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = cliRn_704ILR,
        SalonId_704ILR = salRn_704ILR,
        FechaEvento_704ILR = fechaRn_704ILR.AddDays(2),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 50,
        Monto_704ILR = 700m
    };
    BLL_Reserva_704ILR.Crear_704ILR(pen_704ILR, out int idPen_704ILR);
    var leidaPen_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPen_704ILR);
    int horasReales_704ILR = leidaPen_704ILR.VenceEl_704ILR.HasValue
        ? (int)Math.Round((leidaPen_704ILR.VenceEl_704ILR.Value - DateTime.Now).TotalHours) : -1;
    Esperar_704ILR("pendiente vence en (horas)", horasReales_704ILR, BLL_Reserva_704ILR.HorasValidezPendiente_704ILR);

    leidaPen_704ILR.VenceEl_704ILR = DateTime.Now.AddHours(-1);
    EvenTech.DAL.DAL_Reserva_704ILR.Update_704ILR(leidaPen_704ILR);
    var penVencida_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPen_704ILR);
    penVencida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Esperar_704ILR("confirmar pendiente vencida", BLL_Reserva_704ILR.Actualizar_704ILR(penVencida_704ILR),
        ReservaResult_704ILR.Vencida_704ILR);
    Esperar_704ILR("renovar pendiente", BLL_Reserva_704ILR.Renovar_704ILR(idPen_704ILR), ReservaResult_704ILR.Success_704ILR);
    Adelanto_704ILR(idPen_704ILR, 100m);   // RN-07
    var penRenovada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPen_704ILR);
    penRenovada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Esperar_704ILR("confirmar tras renovar", BLL_Reserva_704ILR.Actualizar_704ILR(penRenovada_704ILR),
        ReservaResult_704ILR.Success_704ILR);
    BLL_Reserva_704ILR.Cancelar_704ILR(idPen_704ILR, out _, out _);   // limpieza

    // [29] RN-02 Politica de cancelacion: con antelacion se reintegra todo; sin
    // antelacion se retiene el porcentaje definido. El calculo queda en bitacora.
    Console.WriteLine("[29] RN-02 politica de cancelacion:");
    var conAntelacion_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    var metodo_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR().First();
    BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
    {
        ReservaId_704ILR = idCot_704ILR,
        MetodoPagoId_704ILR = metodo_704ILR.Id_704ILR,
        Monto_704ILR = 400m
    }, out _);

    // Lo esperado se deriva de lo REALMENTE cobrado sobre la reserva, no de un
    // literal: la operacion pudo recibir otros pagos antes (por ejemplo el adelanto
    // que exige la RN-07 para confirmarla) y un numero fijo daria un falso rojo.
    decimal pagadoRn2_704ILR = BLL_Pago_704ILR.TotalPagado_704ILR(idCot_704ILR);
    BLL_Reserva_704ILR.CalcularCancelacion_704ILR(conAntelacion_704ILR,
        out decimal retLejos_704ILR, out decimal reemLejos_704ILR);
    Console.WriteLine($"  total cobrado: {pagadoRn2_704ILR:0.00}");
    Esperar_704ILR("evento lejano -> retenido", retLejos_704ILR, 0m);
    Esperar_704ILR("evento lejano -> reintegro", reemLejos_704ILR, pagadoRn2_704ILR);

    // Mismo calculo con el evento dentro de la ventana de penalidad.
    var cerca_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    cerca_704ILR.FechaEvento_704ILR = DateTime.Today.AddDays(5);
    BLL_Reserva_704ILR.CalcularCancelacion_704ILR(cerca_704ILR,
        out decimal retCerca_704ILR, out decimal reemCerca_704ILR);
    decimal esperadoRet_704ILR = decimal.Round(pagadoRn2_704ILR * BLL_Reserva_704ILR.PorcentajeRetencion_704ILR / 100m, 2);
    Esperar_704ILR("evento cercano -> retenido", retCerca_704ILR, esperadoRet_704ILR);
    Esperar_704ILR("evento cercano -> reintegro", reemCerca_704ILR, pagadoRn2_704ILR - esperadoRet_704ILR);

    // Postcondicion de la RN-02: el calculo queda asentado en la bitacora, no solo
    // devuelto por el metodo. Se cuenta el asiento y se lee el importe que dejo.
    int cancelacionesAntes_704ILR = Asientos_704ILR("Reservas");
    var rCan_704ILR = BLL_Reserva_704ILR.Cancelar_704ILR(idCot_704ILR,
        out decimal retFinal_704ILR, out decimal reemFinal_704ILR);
    var cancelada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    Esperar_704ILR("cancelar", rCan_704ILR, ReservaResult_704ILR.Success_704ILR);
    // El evento seguia lejos en la base (la fecha cercana era solo del calculo).
    Esperar_704ILR("retenido al cancelar", retFinal_704ILR, 0m);
    Esperar_704ILR("reintegro al cancelar", reemFinal_704ILR, pagadoRn2_704ILR);
    Esperar_704ILR("estado final", cancelada_704ILR.Estado_704ILR, EvenTech.BE.EstadoReserva_704ILR.CANCELADA);
    Esperar_704ILR("vence tras cancelar", cancelada_704ILR.VenceEl_704ILR.HasValue, false);

    var asientosCan_704ILR = Bitacora_704ILR("Reservas");
    Esperar_704ILR("asientos del modulo Reservas tras la cancelacion", asientosCan_704ILR.Count, cancelacionesAntes_704ILR + 1);
    Esperar_704ILR("el asiento deja el reintegro calculado",
        asientosCan_704ILR.Count > 0 && asientosCan_704ILR[0].Detalle_704ILR.Contains($"reintegro {reemFinal_704ILR:0.00}"), true);
    Esperar_704ILR("criticidad del asiento",
        asientosCan_704ILR.Count > 0 ? asientosCan_704ILR[0].Criticidad_704ILR : default,
        EvenTech.BE.CriticidadBitacora_704ILR.Advertencia);

    Esperar_704ILR("recancelar", BLL_Reserva_704ILR.Cancelar_704ILR(idCot_704ILR, out _, out _),
        ReservaResult_704ILR.NoModificable_704ILR);
}
catch (Exception ex28_704ILR) { Excepcion_704ILR("[28]-[29]", ex28_704ILR); }

// [30] RN-05 Transiciones de estado: el ciclo de vida no es libre. COTIZACION
// avanza a cualquier estado, PENDIENTE solo confirma o cancela, CONFIRMADA solo
// cancela y CANCELADA es terminal. Ademas, entrar a CANCELADA exige pasar por la
// via de cancelacion (la unica que liquida la RN-02).
Console.WriteLine("[30] RN-05 transiciones de estado admitidas:");
try
{
    void Chequear_704ILR(EvenTech.BE.EstadoReserva_704ILR d_704ILR, EvenTech.BE.EstadoReserva_704ILR h_704ILR, bool esperado_704ILR)
        => Esperar_704ILR($"{d_704ILR} -> {h_704ILR}", BLL_Reserva_704ILR.TransicionValida_704ILR(d_704ILR, h_704ILR), esperado_704ILR);

    var COT_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION;
    var PEN_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE;
    var CON_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    var CAN_704ILR = EvenTech.BE.EstadoReserva_704ILR.CANCELADA;

    Chequear_704ILR(COT_704ILR, PEN_704ILR, true);
    Chequear_704ILR(COT_704ILR, CON_704ILR, true);
    Chequear_704ILR(COT_704ILR, CAN_704ILR, true);
    Chequear_704ILR(PEN_704ILR, CON_704ILR, true);
    Chequear_704ILR(PEN_704ILR, CAN_704ILR, true);
    Chequear_704ILR(PEN_704ILR, COT_704ILR, false);
    Chequear_704ILR(CON_704ILR, CAN_704ILR, true);
    Chequear_704ILR(CON_704ILR, COT_704ILR, false);
    Chequear_704ILR(CON_704ILR, PEN_704ILR, false);
    Chequear_704ILR(CAN_704ILR, COT_704ILR, false);
    Chequear_704ILR(CAN_704ILR, PEN_704ILR, false);
    Chequear_704ILR(CAN_704ILR, CON_704ILR, false);
    Chequear_704ILR(CAN_704ILR, CAN_704ILR, false);
    // Conservar el estado no es una transicion: siempre se admite, salvo en CANCELADA.
    Chequear_704ILR(COT_704ILR, COT_704ILR, true);
    Chequear_704ILR(PEN_704ILR, PEN_704ILR, true);
    Chequear_704ILR(CON_704ILR, CON_704ILR, true);

    // Verificacion end-to-end contra la base: una CONFIRMADA no vuelve atras.
    var cliT_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    var salT_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    if (cliT_704ILR.Count > 0 && salT_704ILR.Count > 0)
    {
        var rT_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliT_704ILR[0].Id_704ILR,
            SalonId_704ILR = salT_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(4000 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 10,
            Monto_704ILR = 800m
        };
        BLL_Reserva_704ILR.Crear_704ILR(rT_704ILR, out int idT_704ILR);

        Adelanto_704ILR(idT_704ILR, 100m);   // RN-07
        var aConfirmar_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        aConfirmar_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar cotizacion", BLL_Reserva_704ILR.Actualizar_704ILR(aConfirmar_704ILR),
            ReservaResult_704ILR.Success_704ILR);

        var aRetroceder_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        aRetroceder_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE;
        Esperar_704ILR("CONFIRMADA -> PENDIENTE via Actualizar", BLL_Reserva_704ILR.Actualizar_704ILR(aRetroceder_704ILR),
            ReservaResult_704ILR.TransicionInvalida_704ILR);

        var aCancelarMal_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        aCancelarMal_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CANCELADA;
        Esperar_704ILR("cancelar via Actualizar (saltea RN-02)", BLL_Reserva_704ILR.Actualizar_704ILR(aCancelarMal_704ILR),
            ReservaResult_704ILR.TransicionInvalida_704ILR);
        Esperar_704ILR("cancelar por la via correcta", BLL_Reserva_704ILR.Cancelar_704ILR(idT_704ILR, out _, out _),
            ReservaResult_704ILR.Success_704ILR);

        var estadoFinal_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        Esperar_704ILR("estado final", estadoFinal_704ILR.Estado_704ILR, EvenTech.BE.EstadoReserva_704ILR.CANCELADA);

        // Restaurar una version tampoco reabre una reserva cancelada (RN-05).
        var versionesT_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idT_704ILR);
        if (versionesT_704ILR.Count > 0)
            Esperar_704ILR("restaurar version sobre cancelada",
                BLL_Reserva_704ILR.RestaurarVersion_704ILR(idT_704ILR, versionesT_704ILR[0].Id_704ILR),
                ReservaResult_704ILR.NoModificable_704ILR);
    }
}
catch (Exception ex30_704ILR) { Excepcion_704ILR("[30]", ex30_704ILR); }

// [31] RN-06 Capacidad del salon: al confirmar, el salon tiene que poder alojar
// a los invitados estimados. En COTIZACION no se exige (la propuesta se esta armando).
Console.WriteLine("[31] RN-06 capacidad del salon al confirmar:");
try
{
    var cliC_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    var salC_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    if (cliC_704ILR.Count == 0 || salC_704ILR.Count == 0)
    {
        Console.WriteLine("  (faltan clientes/salones seed; corre db/schema.sql)");
    }
    else
    {
        // El salon mas chico, para que el exceso sea inequivoco.
        var chico_704ILR = salC_704ILR.OrderBy(s_704ILR => s_704ILR.Capacidad_704ILR).First();
        int exceso_704ILR = chico_704ILR.Capacidad_704ILR + 10;
        Console.WriteLine($"  salon '{chico_704ILR.Nombre_704ILR}' capacidad {chico_704ILR.Capacidad_704ILR}; se piden {exceso_704ILR} invitados");

        var rC_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliC_704ILR[0].Id_704ILR,
            SalonId_704ILR = chico_704ILR.Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(5000 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = exceso_704ILR,
            Monto_704ILR = 1500m
        };
        var rAltaC_704ILR = BLL_Reserva_704ILR.Crear_704ILR(rC_704ILR, out int idC_704ILR);
        Esperar_704ILR("cotizar con exceso de invitados (en COTIZACION no se exige)",
            rAltaC_704ILR, ReservaResult_704ILR.Success_704ILR);

        var persistidaC_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        Esperar_704ILR("invitados persistidos", persistidaC_704ILR.CantidadInvitados_704ILR, exceso_704ILR);

        persistidaC_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar con exceso", BLL_Reserva_704ILR.Actualizar_704ILR(persistidaC_704ILR),
            ReservaResult_704ILR.CapacidadInsuficiente_704ILR);

        // Confirmar sin saber cuanta gente viene tampoco se admite.
        var sinDato_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        sinDato_704ILR.CantidadInvitados_704ILR = 0;
        sinDato_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar sin invitados", BLL_Reserva_704ILR.Actualizar_704ILR(sinDato_704ILR),
            ReservaResult_704ILR.InvalidInvitados_704ILR);

        Adelanto_704ILR(idC_704ILR, 100m);   // RN-07
        var ajustada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        ajustada_704ILR.CantidadInvitados_704ILR = chico_704ILR.Capacidad_704ILR;
        ajustada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar ajustando a la capacidad", BLL_Reserva_704ILR.Actualizar_704ILR(ajustada_704ILR),
            ReservaResult_704ILR.Success_704ILR);

        // El control de cambios registra la correccion de invitados campo por campo.
        var histC_704ILR = RegistradorDeCambios_704ILR.GetHistorial_704ILR("Reserva", idC_704ILR);
        int cambiosInv_704ILR = histC_704ILR.Count(h_704ILR => h_704ILR.NombreCampo_704ILR == "CantidadInvitados");
        Esperar_704ILR("cambios de CantidadInvitados en el historial", cambiosInv_704ILR, 1);

        // El memento conserva la cantidad de invitados de cada version.
        var versionesC_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idC_704ILR);
        Esperar_704ILR("versiones guardadas", versionesC_704ILR.Count > 0, true);
        if (versionesC_704ILR.Count > 0)
        {
            var vC_704ILR = CaretakerReserva_704ILR.GetVersion_704ILR(versionesC_704ILR[versionesC_704ILR.Count - 1].Id_704ILR);
            Esperar_704ILR("invitados en la version mas antigua", vC_704ILR.CantidadInvitados_704ILR, exceso_704ILR);
        }

        // Limpieza: la reserva confirmada del caso se cancela para no bloquear el
        // salon en la proxima corrida.
        Esperar_704ILR("limpieza (cancelar la reserva de [31])",
            BLL_Reserva_704ILR.Cancelar_704ILR(idC_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
    }
}
catch (Exception ex31_704ILR) { Excepcion_704ILR("[31]", ex31_704ILR); }

// [32] RN-07 Adelanto para confirmar. Lo que distingue a una reserva CONFIRMADA de
// una PENDIENTE es que el cliente ya puso dinero: sin adelanto registrado no se
// confirma, y como el cobro necesita la reserva ya guardada, tampoco se puede nacer
// CONFIRMADA. El orden del proceso es siempre guardar -> cobrar -> confirmar.
Console.WriteLine("[32] RN-07 Adelanto para confirmar:");
try
{
    var cliA_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    var salA_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    if (cliA_704ILR.Count > 0 && salA_704ILR.Count > 0)
    {
        var salA0_704ILR = salA_704ILR[0];

        // Alta directa en CONFIRMADA: rechazada, no hay reserva a la que imputar el cobro.
        var altaDirecta_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliA_704ILR[0].Id_704ILR,
            SalonId_704ILR = salA0_704ILR.Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(5500 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA,
            CantidadInvitados_704ILR = 20,
            Monto_704ILR = 900m
        };
        Esperar_704ILR("alta directa CONFIRMADA", BLL_Reserva_704ILR.Crear_704ILR(altaDirecta_704ILR, out _),
            ReservaResult_704ILR.SinAdelanto_704ILR);

        // Alta normal en COTIZACION y confirmacion sin ningun pago: rechazada.
        var cotA_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliA_704ILR[0].Id_704ILR,
            SalonId_704ILR = salA0_704ILR.Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(5500 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 20,
            Monto_704ILR = 900m
        };
        var rAltaA_704ILR = BLL_Reserva_704ILR.Crear_704ILR(cotA_704ILR, out int idA_704ILR);
        Esperar_704ILR("alta cotizacion", rAltaA_704ILR, ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("adelanto registrado", BLL_Reserva_704ILR.TieneAdelanto_704ILR(idA_704ILR), false);

        var sinPago_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR);
        sinPago_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar sin adelanto", BLL_Reserva_704ILR.Actualizar_704ILR(sinPago_704ILR),
            ReservaResult_704ILR.SinAdelanto_704ILR);
        Esperar_704ILR("estado tras el rechazo", BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR).Estado_704ILR,
            EvenTech.BE.EstadoReserva_704ILR.COTIZACION);

        // Con el adelanto cobrado, la misma confirmacion procede.
        Adelanto_704ILR(idA_704ILR, 300m);
        Esperar_704ILR("adelanto registrado", BLL_Reserva_704ILR.TieneAdelanto_704ILR(idA_704ILR), true);
        var conPago_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR);
        conPago_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar con adelanto", BLL_Reserva_704ILR.Actualizar_704ILR(conPago_704ILR),
            ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("estado final", BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR).Estado_704ILR,
            EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA);

        // Una PENDIENTE sin cobros tampoco confirma.
        var penA_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliA_704ILR[0].Id_704ILR,
            SalonId_704ILR = salA0_704ILR.Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(5600 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
            CantidadInvitados_704ILR = 20,
            Monto_704ILR = 900m
        };
        BLL_Reserva_704ILR.Crear_704ILR(penA_704ILR, out int idPenA_704ILR);
        var penSinPago_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPenA_704ILR);
        penSinPago_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Esperar_704ILR("confirmar pendiente sin adelanto", BLL_Reserva_704ILR.Actualizar_704ILR(penSinPago_704ILR),
            ReservaResult_704ILR.SinAdelanto_704ILR);

        // Limpieza: se liberan los dos salones tomados por el caso.
        Esperar_704ILR("limpieza (cancelar la confirmada de [32])",
            BLL_Reserva_704ILR.Cancelar_704ILR(idA_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
        Esperar_704ILR("limpieza (cancelar la pendiente de [32])",
            BLL_Reserva_704ILR.Cancelar_704ILR(idPenA_704ILR, out _, out _), ReservaResult_704ILR.Success_704ILR);
    }
}
catch (Exception ex32_704ILR) { Excepcion_704ILR("[32]", ex32_704ILR); }

// [33] Anulacion de pago: pasa por las mismas reglas que el registro. Antes esto
// borraba la fila sin mirar si el pago existia, si era de esa reserva o si la
// reserva admitia movimientos.
Console.WriteLine("[33] Cobro y anulacion de pago con reglas (CUN004):");
try
{
    var cliP_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
    var salP_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
    if (cliP_704ILR.Count > 0 && salP_704ILR.Count > 0)
    {
        var rP_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cliP_704ILR[0].Id_704ILR,
            SalonId_704ILR = salP_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = DateTime.Today.AddDays(5700 + desfasaje_704ILR),
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 15,
            Monto_704ILR = 600m
        };
        BLL_Reserva_704ILR.Crear_704ILR(rP_704ILR, out int idP_704ILR);

        var metP_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
        Esperar_704ILR("cobro valido", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = 200m }, out int idPago_704ILR),
            PagoResult_704ILR.Success_704ILR);
        Esperar_704ILR("pagado", BLL_Pago_704ILR.TotalPagado_704ILR(idP_704ILR), 200m);

        // Paso 4 del CUN004: el importe tiene que ser positivo y el metodo, valido.
        Esperar_704ILR("cobro por cero", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = 0m }, out _),
            PagoResult_704ILR.MontoInvalido_704ILR);
        Esperar_704ILR("cobro por importe negativo", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = -50m }, out _),
            PagoResult_704ILR.MontoInvalido_704ILR);
        Esperar_704ILR("cobro sin metodo de pago", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = 0, Monto_704ILR = 100m }, out _),
            PagoResult_704ILR.MetodoInvalido_704ILR);

        // Flujo 5.1 del CUN004: la anulacion de un pago propio de una reserva viva
        // procede y queda asentada con criticidad Advertencia. Antes el caso solo
        // ejercitaba los rechazos: la anulacion exitosa nunca llegaba a correr.
        int anulAntes_704ILR = Asientos_704ILR("Pagos");
        Esperar_704ILR("anular el pago registrado", BLL_Pago_704ILR.Eliminar_704ILR(idPago_704ILR, idP_704ILR),
            PagoResult_704ILR.Success_704ILR);
        Esperar_704ILR("total tras la anulacion", BLL_Pago_704ILR.TotalPagado_704ILR(idP_704ILR), 0m);
        var asientosAnul_704ILR = Bitacora_704ILR("Pagos");
        Esperar_704ILR("asientos del modulo Pagos tras la anulacion", asientosAnul_704ILR.Count, anulAntes_704ILR + 1);
        Esperar_704ILR("criticidad del asiento",
            asientosAnul_704ILR.Count > 0 ? asientosAnul_704ILR[0].Criticidad_704ILR : default,
            EvenTech.BE.CriticidadBitacora_704ILR.Advertencia);

        // Se vuelve a cobrar para probar los rechazos sobre un pago que existe.
        Esperar_704ILR("recobro tras la anulacion", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = 200m }, out int idPago2_704ILR),
            PagoResult_704ILR.Success_704ILR);

        // Un pago inexistente no se anula.
        Esperar_704ILR("anular pago inexistente", BLL_Pago_704ILR.Eliminar_704ILR(999999, idP_704ILR),
            PagoResult_704ILR.PagoInvalido_704ILR);

        // Un pago que existe pero es de otra reserva, tampoco.
        Esperar_704ILR("anular pago ajeno a la reserva", BLL_Pago_704ILR.Eliminar_704ILR(idPago2_704ILR, idP_704ILR + 100000),
            PagoResult_704ILR.PagoInvalido_704ILR);

        // Sobre una reserva cancelada no se admiten movimientos de cobro (RN-04).
        BLL_Reserva_704ILR.Cancelar_704ILR(idP_704ILR, out _, out _);
        Esperar_704ILR("anular sobre reserva cancelada", BLL_Pago_704ILR.Eliminar_704ILR(idPago2_704ILR, idP_704ILR),
            PagoResult_704ILR.ReservaCancelada_704ILR);
        Esperar_704ILR("cobrar sobre reserva cancelada", BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = 50m }, out _),
            PagoResult_704ILR.ReservaCancelada_704ILR);
        Esperar_704ILR("el pago sigue registrado", BLL_Pago_704ILR.TotalPagado_704ILR(idP_704ILR), 200m);
    }
}
catch (Exception ex33_704ILR) { Excepcion_704ILR("[33]", ex33_704ILR); }

// ---------------------------------------------------------------------------
// Cierre: la linea base de integridad tiene que seguir sana DESPUES de todas las
// altas, ediciones, cancelaciones y restauraciones de los casos [7] a [33] — que
// son justamente las operaciones que recalculan los digitos verificadores. Hasta
// ahora [16] la verificaba una sola vez, antes de que ocurriera nada de eso.
// ---------------------------------------------------------------------------
Console.WriteLine("[cierre] Integridad tras la corrida completa:");
try
{
    var resFin_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
    Esperar_704ILR("integridad al cierre", resFin_704ILR.Ok_704ILR, true);
    foreach (var i_704ILR in resFin_704ILR.Inconsistencias_704ILR) Console.WriteLine("   - " + i_704ILR);
}
catch (Exception exFin_704ILR) { Excepcion_704ILR("[cierre]", exFin_704ILR); }

Console.WriteLine($"== fin: {verificaciones_704ILR} verificaciones, {fallos_704ILR} fallo(s) en 33 casos ==");
return fallos_704ILR == 0 ? 0 : 1;

// Observador de prueba del patron Observer (idiomas). Cumple el mismo rol que un
// formulario de la aplicacion: se suscribe al gestor y cuenta cuantas veces le
// pidieron refrescar sus textos.
class ObservadorPrueba_704ILR : IObservadorIdioma_704ILR
{
    public int Llamadas_704ILR { get; private set; }

    public void ActualizarTextos_704ILR() => Llamadas_704ILR++;
}
