using EvenTech.BLL;
using EvenTech.Services;

Console.WriteLine("== EvenTech smoke test v2 ==");

// Desplazamiento propio de esta corrida. Las reservas de prueba se agendan a
// varios anios vista y con este desfasaje, de modo que NUNCA colisionan con una
// fecha del negocio ni con el rastro de una corrida anterior (RN-03). Se prefirio
// esto antes que limpiar la base: una rutina que cancelara reservas CONFIRMADAS
// por salon+fecha podria dar de baja datos reales sin aviso y sin vuelta atras.
int desfasaje_704ILR = (int)DateTime.Now.TimeOfDay.TotalSeconds % 900;

// RN-07: una reserva se confirma con el adelanto ya cobrado. Este helper registra
// ese cobro para poder ejercitar las transiciones a CONFIRMADA, igual que hace el
// vendedor en la aplicacion: guardar la operacion, cobrar y recien ahi confirmar.
void Adelanto_704ILR(int reservaId_704ILR, decimal monto_704ILR)
{
    var met_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
    if (met_704ILR.Count == 0) return;
    BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
    {
        ReservaId_704ILR = reservaId_704ILR,
        MetodoPagoId_704ILR = met_704ILR[0].Id_704ILR,
        Monto_704ILR = monto_704ILR,
        Observacion_704ILR = "Adelanto"
    }, out _);
}

// [1] Login OK
Console.WriteLine("[1] Login admin/admin123:");
var r1_704ILR = BLL_Login_704ILR.Authenticate_704ILR("admin", Encrypt_704ILR.HashValue_704ILR("admin123"));
Console.WriteLine($"  result={r1_704ILR}, sesionActiva={SessionManager_704ILR.IsSessionActive_704ILR}");
BLL_Login_704ILR.Logout_704ILR();

// [2] Crear usuario nuevo (con timestamp para que sea unico entre corridas)
string newUser_704ILR = "smoke_" + DateTime.Now.ToString("HHmmss");
Console.WriteLine($"[2] Crear usuario '{newUser_704ILR}' password 'pass1234':");
var rc1_704ILR = BLL_User_704ILR.CreateUser_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("pass1234"));
Console.WriteLine($"  result={rc1_704ILR}");

// [3] Crear duplicado
Console.WriteLine($"[3] Crear '{newUser_704ILR}' duplicado:");
var rc2_704ILR = BLL_User_704ILR.CreateUser_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("otra"));
Console.WriteLine($"  result={rc2_704ILR}");

// [4] Username invalido
Console.WriteLine("[4] Crear con username '..' (invalido):");
var rc3_704ILR = BLL_User_704ILR.CreateUser_704ILR("..", Encrypt_704ILR.HashValue_704ILR("xxxx"));
Console.WriteLine($"  result={rc3_704ILR}");

// [5] Login con el usuario recien creado
Console.WriteLine($"[5] Login con '{newUser_704ILR}':");
var r5_704ILR = BLL_Login_704ILR.Authenticate_704ILR(newUser_704ILR, Encrypt_704ILR.HashValue_704ILR("pass1234"));
Console.WriteLine($"  result={r5_704ILR}");
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
else
{
    // Fecha semi-unica por corrida (como el username del caso [2]): el caso [10]
    // confirma esta reserva y una fecha fija chocaria por SalonOcupado contra la
    // corrida anterior del mismo dia. Ventana 1000-1900 para no pisar la del [26].
    var nueva_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = clientes_704ILR[0].Id_704ILR,
        SalonId_704ILR = salones_704ILR[0].Id_704ILR,
        FechaEvento_704ILR = DateTime.Today.AddDays(1000 + (int)DateTime.Now.TimeOfDay.TotalSeconds % 900),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 150000m
    };
    var rr1_704ILR = BLL_Reserva_704ILR.Crear_704ILR(nueva_704ILR, out int nuevoId_704ILR);
    Console.WriteLine($"  result={rr1_704ILR}, nuevoId={nuevoId_704ILR}");

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
    Console.WriteLine($"  result={rr2_704ILR}");

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
        Console.WriteLine($"  result={ru_704ILR}");

        Console.WriteLine($"[11] Historial de cambios de la reserva #{nuevoId_704ILR}:");
        foreach (var c_704ILR in EvenTech.BLL.RegistradorDeCambios_704ILR.GetHistorial_704ILR("Reserva", nuevoId_704ILR))
            Console.WriteLine($"  {c_704ILR.Fecha_704ILR:HH:mm:ss} {c_704ILR.NombreCampo_704ILR,-14} '{c_704ILR.ValorAnterior_704ILR}' -> '{c_704ILR.ValorNuevo_704ILR}'");
    }

    // [12] Bitacora general (ultimas 5)
    Console.WriteLine("[12] Ultimas 5 entradas de bitacora:");
    int mostradas_704ILR = 0;
    foreach (var b_704ILR in EvenTech.BLL.BLL_Bitacora_704ILR.Buscar_704ILR(new EvenTech.BE.BitacoraFiltros_704ILR()))
    {
        Console.WriteLine($"  #{b_704ILR.Id_704ILR} {b_704ILR.Fecha_704ILR:HH:mm:ss} {b_704ILR.Modulo_704ILR,-10} {b_704ILR.Accion_704ILR,-26} {b_704ILR.Criticidad_704ILR}");
        if (++mostradas_704ILR >= 5) break;
    }
}

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
    var asignados_704ILR = BLL_Perfil_704ILR.GetPermisosAsignados_704ILR(perfiles_704ILR[0].Id_704ILR);
    var efectivos_704ILR = BLL_Perfil_704ILR.CalcularPermisosEfectivos_704ILR(arbol_704ILR, asignados_704ILR);
    Console.WriteLine($"[14] Perfil '{perfiles_704ILR[0].Nombre_704ILR}': {efectivos_704ILR.Count} permisos efectivos (hojas).");
}

// [15] Idiomas (Observer): cambio dinamico de traducciones
Console.WriteLine("[15] Idiomas (Observer):");
EvenTech.BLL.BLL_Idioma_704ILR.Inicializar_704ILR();
var gi_704ILR = EvenTech.Services.GestorDeIdioma_704ILR.GetInstance_704ILR;
Console.WriteLine($"  idioma={gi_704ILR.IdiomaActual_704ILR}, MENU_RESERVAS='{gi_704ILR.Traducir_704ILR("MENU_RESERVAS")}'");
gi_704ILR.CambiarIdioma_704ILR("EN");
Console.WriteLine($"  idioma={gi_704ILR.IdiomaActual_704ILR}, MENU_RESERVAS='{gi_704ILR.Traducir_704ILR("MENU_RESERVAS")}'");
gi_704ILR.CambiarIdioma_704ILR("ES");

// [16] Digitos verificadores (T07/T08)
Console.WriteLine("[16] Integridad (digitos verificadores):");
var resInt_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
Console.WriteLine($"  Ok={resInt_704ILR.Ok_704ILR}, inconsistencias={resInt_704ILR.Inconsistencias_704ILR.Count}");
foreach (var i_704ILR in resInt_704ILR.Inconsistencias_704ILR) Console.WriteLine("   - " + i_704ILR);

// Recalculo de linea base (accion administrativa del proceso ante corrupcion):
// tras recalcular, la verificacion tiene que dar limpia si o si.
int recalculadas_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.RecalcularTodo_704ILR();
var resInt2_704ILR = EvenTech.BLL.BLL_Integridad_704ILR.Verificar_704ILR();
Console.WriteLine($"  recalculo de linea base: {recalculadas_704ILR} reservas -> Ok={resInt2_704ILR.Ok_704ILR} (esperado True)");

// [17] Alta de idioma desde la capa de negocio (admin agrega idioma)
Console.WriteLine("[17] Crear idioma 'PT':");
var rIdioma_704ILR = EvenTech.BLL.BLL_Idioma_704ILR.CrearIdioma_704ILR("PT", "Portugues", out int idPt_704ILR);
Console.WriteLine($"  result={rIdioma_704ILR}");
Console.WriteLine($"  idiomas disponibles: {EvenTech.Services.GestorDeIdioma_704ILR.GetInstance_704ILR.IdiomasDisponibles_704ILR.Count}");

// [18] Patron Memento: versionado y restauracion de reservas
Console.WriteLine("[18] Memento (versiones de reserva):");
var clientesM_704ILR = BLL_Cliente_704ILR.GetAll_704ILR();
var salonesM_704ILR = BLL_Salon_704ILR.GetAll_704ILR();
if (clientesM_704ILR.Count == 0 || salonesM_704ILR.Count == 0)
{
    Console.WriteLine("  (faltan clientes/salones seed; corre db/schema.sql)");
}
else
{
    var reservaM_704ILR = new EvenTech.BE.BE_Reserva_704ILR
    {
        ClienteId_704ILR = clientesM_704ILR[0].Id_704ILR,
        SalonId_704ILR = salonesM_704ILR[0].Id_704ILR,
        FechaEvento_704ILR = DateTime.Today.AddDays(1500 + desfasaje_704ILR),
        Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
        CantidadInvitados_704ILR = 60,   // RN-06: sin este dato no se puede confirmar
        Monto_704ILR = 1000m
    };
    var rm_704ILR = BLL_Reserva_704ILR.Crear_704ILR(reservaM_704ILR, out int idM_704ILR);
    Console.WriteLine($"  alta: result={rm_704ILR}, id={idM_704ILR}");

    Adelanto_704ILR(idM_704ILR, 500m);   // RN-07
    var v1_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idM_704ILR);
    v1_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    v1_704ILR.Monto_704ILR = 2000m;
    Console.WriteLine($"  modificar (PENDIENTE/1000 -> CONFIRMADA/2000): result={BLL_Reserva_704ILR.Actualizar_704ILR(v1_704ILR)}");

    var versiones_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idM_704ILR);
    Console.WriteLine($"  versiones guardadas: {versiones_704ILR.Count} (esperado 1)");

    if (versiones_704ILR.Count > 0)
    {
        var rr_704ILR = BLL_Reserva_704ILR.RestaurarVersion_704ILR(idM_704ILR, versiones_704ILR[0].Id_704ILR);
        var restaurada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idM_704ILR);
        Console.WriteLine($"  restaurar: result={rr_704ILR} -> Estado={restaurada_704ILR.Estado_704ILR}, Monto={restaurada_704ILR.Monto_704ILR} (esperado PENDIENTE, 1000)");
        Console.WriteLine($"  versiones tras restaurar: {CaretakerReserva_704ILR.GetVersiones_704ILR(idM_704ILR).Count} (esperado 2: la restauracion versiona el estado que piso)");
    }
}

// [19] Composite de perfiles: un perfil incluye a otro y hereda sus permisos
Console.WriteLine("[19] Composite de perfiles (perfil incluye perfil):");
{
    string suf_704ILR = DateTime.Now.ToString("HHmmss");
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

    BLL_Perfil_704ILR.CrearPerfil_704ILR("Vendedor_" + suf_704ILR, "smoke", out int idVend_704ILR);
    BLL_Perfil_704ILR.CrearPerfil_704ILR("Gerencial_" + suf_704ILR, "smoke", out int idGer_704ILR);

    var rVend_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR, idEditar_704ILR }, new int[0]);
    Console.WriteLine($"  Vendedor (RESERVA_CREAR + RESERVA_EDITAR): result={rVend_704ILR}");

    var rGer_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idGer_704ILR, new[] { idBitacora_704ILR }, new[] { idVend_704ILR });
    Console.WriteLine($"  Gerencial (BITACORA_VER + incluye Vendedor): result={rGer_704ILR}");

    var efectivosGer_704ILR = BLL_Perfil_704ILR.GetPermisosEfectivosDePerfil_704ILR(idGer_704ILR);
    Console.WriteLine($"  permisos efectivos de Gerencial: {string.Join(", ", efectivosGer_704ILR.Select(p_704ILR => p_704ILR.Clave_704ILR))}");
    Console.WriteLine($"  (esperado: BITACORA_VER + RESERVA_CREAR + RESERVA_EDITAR heredados de Vendedor)");

    var rCiclo_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR, idEditar_704ILR }, new[] { idGer_704ILR });
    Console.WriteLine($"  incluir Gerencial dentro de Vendedor: result={rCiclo_704ILR} (esperado ReferenciaCircular)");

    var rSelf_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(idVend_704ILR, new[] { idCrear_704ILR }, new[] { idVend_704ILR });
    Console.WriteLine($"  incluir Vendedor dentro de si mismo: result={rSelf_704ILR} (esperado ReferenciaCircular)");
}

// [20] Cifrado reversible (AES) de datos sensibles del cliente
Console.WriteLine("[20] Cifrado reversible de Email/Telefono de clientes:");
{
    string suf_704ILR = DateTime.Now.ToString("HHmmss");
    var cli_704ILR = new EvenTech.BE.BE_Cliente_704ILR
    {
        Nombre_704ILR = "SmokeCrypto",
        Apellido_704ILR = suf_704ILR,
        Email_704ILR = $"crypto_{suf_704ILR}@test.com",
        Telefono_704ILR = "11-5555-" + suf_704ILR
    };
    var rCli_704ILR = BLL_Cliente_704ILR.Crear_704ILR(cli_704ILR, out int idCli_704ILR);
    Console.WriteLine($"  alta: result={rCli_704ILR}, id={idCli_704ILR}");

    var leido_704ILR = BLL_Cliente_704ILR.GetById_704ILR(idCli_704ILR);
    bool roundtripOk_704ILR = leido_704ILR.Email_704ILR == cli_704ILR.Email_704ILR && leido_704ILR.Telefono_704ILR == cli_704ILR.Telefono_704ILR;
    Console.WriteLine($"  leido por la app: Email='{leido_704ILR.Email_704ILR}', Telefono='{leido_704ILR.Telefono_704ILR}'");
    Console.WriteLine($"  roundtrip cifrar->descifrar: {(roundtripOk_704ILR ? "OK" : "FALLO")} (esperado OK)");

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
            Console.WriteLine($"  cifrado en DB: Email={CryptoService_704ILR.EstaProtegido_704ILR(rawE_704ILR)}, " +
                              $"Telefono={CryptoService_704ILR.EstaProtegido_704ILR(rawT_704ILR)} (esperado True, True)");
        }
    }
}

// [21] Control de acceso: los permisos se conceden solo si estan en el perfil
// (denegar por defecto). Se valida sobre la sesion real de admin.
Console.WriteLine("[21] Permisos de la sesion (denegar por defecto):");
{
    BLL_Login_704ILR.Authenticate_704ILR("admin", Encrypt_704ILR.HashValue_704ILR("admin123"));
    var s_704ILR = SessionManager_704ILR.GetInstance_704ILR;
    Console.WriteLine($"  permisosNoDisponibles={s_704ILR.PermisosNoDisponibles_704ILR} (esperado False)");
    Console.WriteLine($"  admin tiene RESERVA_CREAR: {s_704ILR.TienePermiso_704ILR("RESERVA_CREAR")} (esperado True)");
    Console.WriteLine($"  admin tiene PAGOS_ANULAR: {s_704ILR.TienePermiso_704ILR("PAGOS_ANULAR")} (esperado True)");
    Console.WriteLine($"  clave inexistente NO_EXISTE: {s_704ILR.TienePermiso_704ILR("NO_EXISTE")} (esperado False)");
    Console.WriteLine($"  clave nula: {s_704ILR.TienePermiso_704ILR(null)} (esperado False)");
    BLL_Login_704ILR.Logout_704ILR();
}

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
    Console.WriteLine($"  claves en el arbol: {enArbol_704ILR.Count}; faltantes: " +
                      (faltan_704ILR.Count == 0 ? "ninguna (esperado)" : string.Join(", ", faltan_704ILR)));
}

// [23] Una reserva cancelada es estado terminal: no admite modificaciones.
Console.WriteLine("[23] Reserva cancelada no modificable:");
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
        Console.WriteLine($"  alta directa en CANCELADA: {BLL_Reserva_704ILR.Crear_704ILR(altaDirecta_704ILR, out _)} (esperado TransicionInvalida)");

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
        Console.WriteLine($"  alta pendiente: result={rAlta_704ILR} (esperado Success)");
        Console.WriteLine($"  cancelar: {BLL_Reserva_704ILR.Cancelar_704ILR(idCancel_704ILR, out _, out _)} (esperado Success)");

        var guardada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCancel_704ILR);
        Console.WriteLine($"  PuedeModificar: {BLL_Reserva_704ILR.PuedeModificar_704ILR(guardada_704ILR)} (esperado False)");

        guardada_704ILR.Monto_704ILR = 2000m;
        var rMod_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(guardada_704ILR);
        Console.WriteLine($"  intento de modificar: result={rMod_704ILR} (esperado NoModificable)");

        // Una reserva viva si se modifica.
        var viva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCancel_704ILR);
        viva_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE;
        Console.WriteLine($"  PuedeModificar sobre PENDIENTE: {BLL_Reserva_704ILR.PuedeModificar_704ILR(viva_704ILR)} (esperado True)");

        // Los pagos persisten en el acto, sin pasar por BLL_Reserva.Actualizar:
        // la regla del estado terminal tiene que rechazarlos tambien.
        var metodos_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
        if (metodos_704ILR.Count > 0)
        {
            var pago_704ILR = new EvenTech.BE.BE_Pago_704ILR { ReservaId_704ILR = idCancel_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = 10m };
            var rPago_704ILR = BLL_Pago_704ILR.Registrar_704ILR(pago_704ILR, out _);
            Console.WriteLine($"  cobrar sobre cancelada: result={rPago_704ILR} (esperado ReservaCancelada)");
        }
    }
}

// [24] Configuracion de conexion: la cadena sale del gestor (no hardcodeada) y
// el diagnostico distingue servidor caido de base inexistente.
Console.WriteLine("[24] Configuracion de conexion:");
{
    Console.WriteLine($"  configurada por el usuario: {BLL_Conexion_704ILR.EstaConfigurada_704ILR}");
    Console.WriteLine($"  servidor='{BLL_Conexion_704ILR.ServidorActual_704ILR}', base='{BLL_Conexion_704ILR.BaseDatosActual_704ILR}'");

    bool ok_704ILR = BLL_Conexion_704ILR.VerificarActual_704ILR(out string msgOk_704ILR);
    Console.WriteLine($"  verificar actual: {ok_704ILR} (esperado True){(ok_704ILR ? "" : " -> " + msgOk_704ILR)}");

    bool inexistente_704ILR = BLL_Conexion_704ILR.Probar_704ILR(EvenTech.Services.ConfiguracionConexion_704ILR.ServidorPorDefecto_704ILR,
                                           "BaseQueNoExiste_" + DateTime.Now.ToString("HHmmss"), out string msgNo_704ILR);
    Console.WriteLine($"  base inexistente: {inexistente_704ILR} (esperado False)");
    Console.WriteLine($"    diagnostico: {msgNo_704ILR}");

    Console.WriteLine($"  instancias detectadas: {BLL_Conexion_704ILR.GetInstancias_704ILR().Count}");

    // Roundtrip del archivo cifrado con DPAPI: si guardar/leer fallara, la app
    // quedaria sin poder conectar en el proximo arranque. Se prueba con la
    // configuracion que ya funciona y se deja el entorno como estaba.
    bool estabaConfigurada_704ILR = BLL_Conexion_704ILR.EstaConfigurada_704ILR;
    string servidorPrevio_704ILR = BLL_Conexion_704ILR.ServidorActual_704ILR, basePrevia_704ILR = BLL_Conexion_704ILR.BaseDatosActual_704ILR;

    bool guardo_704ILR = BLL_Conexion_704ILR.Guardar_704ILR(servidorPrevio_704ILR, basePrevia_704ILR, out string msgGuardar_704ILR);
    Console.WriteLine($"  guardar cifrado (DPAPI): {guardo_704ILR} (esperado True){(guardo_704ILR ? "" : " -> " + msgGuardar_704ILR)}");
    Console.WriteLine($"  persistida: {BLL_Conexion_704ILR.EstaConfigurada_704ILR} (esperado True)");

    bool releeOk_704ILR = BLL_Conexion_704ILR.VerificarActual_704ILR(out _);
    Console.WriteLine($"  releida y conecta: {releeOk_704ILR} (esperado True)");
    Console.WriteLine($"  servidor releido='{BLL_Conexion_704ILR.ServidorActual_704ILR}', base='{BLL_Conexion_704ILR.BaseDatosActual_704ILR}' " +
                      $"(esperado '{servidorPrevio_704ILR}', '{basePrevia_704ILR}')");

    if (!estabaConfigurada_704ILR)
    {
        BLL_Conexion_704ILR.Restablecer_704ILR();
        Console.WriteLine($"  entorno restaurado (sin archivo): {!BLL_Conexion_704ILR.EstaConfigurada_704ILR} (esperado True)");
    }
}

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
        Console.WriteLine($"  aceptada: {ok_704ILR} (esperado False)");
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
        // Fecha propia de la corrida (como el username unico del caso [2]): una
        // corrida anterior deja su reserva confirmada en la base y una fecha
        // fija haria fallar la confirmacion por SalonOcupado.
        DateTime fechaEvento_704ILR = DateTime.Today.AddDays(60 + (int)DateTime.Now.TimeOfDay.TotalSeconds % 900);

        // Cotizacion: no compromete el salon. El monto es la suma de servicios.
        var servicios_704ILR = new List<EvenTech.BE.BE_ReservaServicio_704ILR>
        {
            new EvenTech.BE.BE_ReservaServicio_704ILR { ServicioId_704ILR = srv_704ILR[0].Id_704ILR, Cantidad_704ILR = 2, PrecioUnitario_704ILR = srv_704ILR[0].Precio_704ILR },
            new EvenTech.BE.BE_ReservaServicio_704ILR { ServicioId_704ILR = srv_704ILR[1].Id_704ILR, Cantidad_704ILR = 1, PrecioUnitario_704ILR = srv_704ILR[1].Precio_704ILR }
        };
        decimal total_704ILR = BLL_ReservaServicio_704ILR.Total_704ILR(servicios_704ILR);
        decimal esperado_704ILR = srv_704ILR[0].Precio_704ILR * 2 + srv_704ILR[1].Precio_704ILR;
        Console.WriteLine($"  total de servicios: {total_704ILR:N2} (esperado {esperado_704ILR:N2})");

        var cot_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = fechaEvento_704ILR,
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.COTIZACION,
            CantidadInvitados_704ILR = 50,   // RN-06: sin este dato no se puede confirmar
            Monto_704ILR = total_704ILR
        };
        var rCot_704ILR = BLL_Reserva_704ILR.Crear_704ILR(cot_704ILR, out int idFlujo_704ILR);
        BLL_ReservaServicio_704ILR.Guardar_704ILR(idFlujo_704ILR, servicios_704ILR);
        Console.WriteLine($"  alta cotizacion: result={rCot_704ILR}, id={idFlujo_704ILR}");
        Console.WriteLine($"  servicios persistidos: {BLL_ReservaServicio_704ILR.GetByReserva_704ILR(idFlujo_704ILR).Count} (esperado {servicios_704ILR.Count})");

        // RN-07: el adelanto se cobra ANTES de confirmar. Ese es el orden del proceso
        // de negocio: se registra la operacion, se cobra y recien ahi queda firme.
        var metodos_704ILR = BLL_Pago_704ILR.GetMetodos_704ILR();
        Console.WriteLine($"  metodos de pago: {metodos_704ILR.Count} (esperado 5)");
        decimal adelanto_704ILR = Math.Round(total_704ILR / 2, 2);
        var rAde_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = adelanto_704ILR, Observacion_704ILR = "Adelanto" }, out _);
        Console.WriteLine($"  adelanto {adelanto_704ILR:N2}: result={rAde_704ILR} (esperado Success)");
        Console.WriteLine($"  saldo tras adelanto: {BLL_Pago_704ILR.Saldo_704ILR(idFlujo_704ILR):N2} (esperado {total_704ILR - adelanto_704ILR:N2})");

        // Confirmar: recien aca se compromete el salon.
        var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR);
        reserva_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        var rConf_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(reserva_704ILR);
        Console.WriteLine($"  confirmar: result={rConf_704ILR} (esperado Success)");

        // Anti-solapamiento: otra reserva para el mismo salon y fecha no puede
        // confirmarse. Nace PENDIENTE y con su adelanto cobrado (RN-07), de modo que
        // lo unico que puede rechazar la confirmacion es el salon ya comprometido.
        var choque_704ILR = new EvenTech.BE.BE_Reserva_704ILR
        {
            ClienteId_704ILR = cli_704ILR[0].Id_704ILR,
            SalonId_704ILR = sal_704ILR[0].Id_704ILR,
            FechaEvento_704ILR = fechaEvento_704ILR,
            Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE,
            CantidadInvitados_704ILR = 70,   // RN-06: sin este dato no se puede confirmar
            Monto_704ILR = 1000m
        };
        BLL_Reserva_704ILR.Crear_704ILR(choque_704ILR, out int idChoque_704ILR);
        Adelanto_704ILR(idChoque_704ILR, 100m);
        var aChocar_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idChoque_704ILR);
        aChocar_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        var rChoque_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(aChocar_704ILR);
        Console.WriteLine($"  segunda confirmada mismo salon/fecha: result={rChoque_704ILR} (esperado SalonOcupado)");
        BLL_Reserva_704ILR.Cancelar_704ILR(idChoque_704ILR, out _, out _);   // limpieza

        var rExceso_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[0].Id_704ILR, Monto_704ILR = total_704ILR }, out _);
        Console.WriteLine($"  pago que excede el saldo: result={rExceso_704ILR} (esperado ExcedeSaldo)");

        var rSaldo_704ILR = BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idFlujo_704ILR, MetodoPagoId_704ILR = metodos_704ILR[metodos_704ILR.Count - 1].Id_704ILR, Monto_704ILR = total_704ILR - adelanto_704ILR, Observacion_704ILR = "Saldo" }, out _);
        Console.WriteLine($"  saldo restante: result={rSaldo_704ILR} (esperado Success)");
        Console.WriteLine($"  saldo final: {BLL_Pago_704ILR.Saldo_704ILR(idFlujo_704ILR):N2} (esperado 0,00)");

        // RN-04 como invariante: con el total ya cobrado, una edicion que achique la
        // reserva por debajo de lo pagado (quitar servicios) se rechaza.
        var achicada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idFlujo_704ILR);
        achicada_704ILR.Monto_704ILR = achicada_704ILR.Monto_704ILR / 2;
        Console.WriteLine($"  reducir el total por debajo de lo pagado: {BLL_Reserva_704ILR.Actualizar_704ILR(achicada_704ILR)} (esperado MontoInferiorPagado)");

        // [27] Consulta de disponibilidad (Proceso 1, paso 1): la fecha recien
        // confirmada tiene que figurar ocupada para ese salon, con una fecha
        // alternativa propuesta; una capacidad imposible marca insuficiente.
        Console.WriteLine("[27] Consulta de disponibilidad:");
        var disp_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR, 0);
        Console.WriteLine($"  salones evaluados: {disp_704ILR.Count} (esperado {sal_704ILR.Count})");
        var delFlujo_704ILR = disp_704ILR.FirstOrDefault(d_704ILR => d_704ILR.SalonId_704ILR == sal_704ILR[0].Id_704ILR);
        Console.WriteLine($"  salon confirmado libre: {delFlujo_704ILR?.Libre_704ILR} (esperado False)");
        Console.WriteLine($"  propuesta alternativa: {(delFlujo_704ILR?.ProximaFechaLibre_704ILR.HasValue == true ? delFlujo_704ILR.ProximaFechaLibre_704ILR.Value.ToString("yyyy-MM-dd") : "ninguna")} (esperada una fecha)");

        var dispCap_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR, 99999);
        Console.WriteLine($"  capacidad imposible -> disponibles: {dispCap_704ILR.Count(d_704ILR => d_704ILR.Disponible_704ILR)} (esperado 0)");
        Console.WriteLine($"  capacidad imposible -> suficientes: {dispCap_704ILR.Count(d_704ILR => d_704ILR.CapacidadSuficiente_704ILR)} (esperado 0)");

        // Un dia sin reservas confirmadas: todos los salones libres.
        var dispLibre_704ILR = BLL_Disponibilidad_704ILR.Consultar_704ILR(fechaEvento_704ILR.AddDays(2000), 0);
        Console.WriteLine($"  fecha lejana -> disponibles: {dispLibre_704ILR.Count(d_704ILR => d_704ILR.Disponible_704ILR)}/{dispLibre_704ILR.Count} (esperado todos)");

        // Limpieza: se cancela la reserva del flujo para liberar el salon
        // (la corrida queda repetible aunque la fecha se repitiera). Se usa la via
        // de cancelacion, que es la unica admitida para entrar a CANCELADA (RN-05)
        // y la que liquida la politica de reintegro (RN-02).
        var rFin_704ILR = BLL_Reserva_704ILR.Cancelar_704ILR(idFlujo_704ILR, out _, out _);
        Console.WriteLine($"  limpieza (cancelar reserva del flujo): result={rFin_704ILR} (esperado Success)");
    }
}

// [28] RN-01 Vigencia de la operacion: una COTIZACION nace con fecha de
// vencimiento, una CONFIRMADA no; una operacion vencida no se puede confirmar
// hasta renovarla.
Console.WriteLine("[28] RN-01 vigencia de la operacion:");
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
    Console.WriteLine($"  alta cotizacion: {rCot_704ILR} (esperado Success)");
    Console.WriteLine($"  vence: {(leida_704ILR.VenceEl_704ILR.HasValue ? leida_704ILR.VenceEl_704ILR.Value.ToString("yyyy-MM-dd") : "null")} (esperada una fecha)");
    Console.WriteLine($"  vencida hoy: {leida_704ILR.EstaVencida_704ILR} (esperado False)");

    int diasEsperados_704ILR = BLL_Reserva_704ILR.DiasValidezCotizacion_704ILR;
    int diasReales_704ILR = leida_704ILR.VenceEl_704ILR.HasValue
        ? (int)Math.Round((leida_704ILR.VenceEl_704ILR.Value - DateTime.Now).TotalDays) : -1;
    Console.WriteLine($"  plazo: {diasReales_704ILR} dias (esperado {diasEsperados_704ILR})");

    // Al confirmar, la operacion deja de tener plazo.
    Adelanto_704ILR(idCot_704ILR, 100m);   // RN-07
    leida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    var rConf_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(leida_704ILR);
    var confirmada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    Console.WriteLine($"  confirmar: {rConf_704ILR} (esperado Success)");
    Console.WriteLine($"  vence tras confirmar: {(confirmada_704ILR.VenceEl_704ILR.HasValue ? "con fecha" : "null")} (esperado null)");

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
    Console.WriteLine($"  vencida forzada: {vencida_704ILR.EstaVencida_704ILR} (esperado True)");
    vencida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    var rVenc_704ILR = BLL_Reserva_704ILR.Actualizar_704ILR(vencida_704ILR);
    Console.WriteLine($"  confirmar vencida: {rVenc_704ILR} (esperado Vencida)");

    var rRen_704ILR = BLL_Reserva_704ILR.Renovar_704ILR(idCot2_704ILR);
    Console.WriteLine($"  renovar: {rRen_704ILR} (esperado Success)");
    var renovada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot2_704ILR);
    Console.WriteLine($"  vencida tras renovar: {renovada_704ILR.EstaVencida_704ILR} (esperado False)");
    Adelanto_704ILR(idCot2_704ILR, 100m);   // RN-07
    renovada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Console.WriteLine($"  confirmar tras renovar: {BLL_Reserva_704ILR.Actualizar_704ILR(renovada_704ILR)} (esperado Success)");
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
    Console.WriteLine($"  pendiente vence en: {horasReales_704ILR} horas (esperado {BLL_Reserva_704ILR.HorasValidezPendiente_704ILR})");

    leidaPen_704ILR.VenceEl_704ILR = DateTime.Now.AddHours(-1);
    EvenTech.DAL.DAL_Reserva_704ILR.Update_704ILR(leidaPen_704ILR);
    var penVencida_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPen_704ILR);
    penVencida_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Console.WriteLine($"  confirmar pendiente vencida: {BLL_Reserva_704ILR.Actualizar_704ILR(penVencida_704ILR)} (esperado Vencida)");
    Console.WriteLine($"  renovar pendiente: {BLL_Reserva_704ILR.Renovar_704ILR(idPen_704ILR)} (esperado Success)");
    Adelanto_704ILR(idPen_704ILR, 100m);   // RN-07
    var penRenovada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idPen_704ILR);
    penRenovada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
    Console.WriteLine($"  confirmar tras renovar: {BLL_Reserva_704ILR.Actualizar_704ILR(penRenovada_704ILR)} (esperado Success)");
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
    Console.WriteLine($"  evento lejano -> retenido {retLejos_704ILR:0.00} (esperado 0.00), reintegro {reemLejos_704ILR:0.00} (esperado {pagadoRn2_704ILR:0.00})");

    // Mismo calculo con el evento dentro de la ventana de penalidad.
    var cerca_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    cerca_704ILR.FechaEvento_704ILR = DateTime.Today.AddDays(5);
    BLL_Reserva_704ILR.CalcularCancelacion_704ILR(cerca_704ILR,
        out decimal retCerca_704ILR, out decimal reemCerca_704ILR);
    decimal esperadoRet_704ILR = decimal.Round(pagadoRn2_704ILR * BLL_Reserva_704ILR.PorcentajeRetencion_704ILR / 100m, 2);
    Console.WriteLine($"  evento cercano -> retenido {retCerca_704ILR:0.00} (esperado {esperadoRet_704ILR:0.00}), reintegro {reemCerca_704ILR:0.00}");

    var rCan_704ILR = BLL_Reserva_704ILR.Cancelar_704ILR(idCot_704ILR,
        out decimal retFinal_704ILR, out decimal reemFinal_704ILR);
    var cancelada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idCot_704ILR);
    Console.WriteLine($"  cancelar: {rCan_704ILR} (esperado Success), retenido {retFinal_704ILR:0.00}, reintegro {reemFinal_704ILR:0.00}");
    Console.WriteLine($"  estado final: {cancelada_704ILR.Estado_704ILR} (esperado CANCELADA)");
    Console.WriteLine($"  vence tras cancelar: {(cancelada_704ILR.VenceEl_704ILR.HasValue ? "con fecha" : "null")} (esperado null)");
    Console.WriteLine($"  recancelar: {BLL_Reserva_704ILR.Cancelar_704ILR(idCot_704ILR, out _, out _)} (esperado NoModificable)");
}

// [30] RN-05 Transiciones de estado: el ciclo de vida no es libre. COTIZACION
// avanza a cualquier estado, PENDIENTE solo confirma o cancela, CONFIRMADA solo
// cancela y CANCELADA es terminal. Ademas, entrar a CANCELADA exige pasar por la
// via de cancelacion (la unica que liquida la RN-02).
Console.WriteLine("[30] RN-05 transiciones de estado admitidas:");
{
    void Chequear_704ILR(EvenTech.BE.EstadoReserva_704ILR d_704ILR, EvenTech.BE.EstadoReserva_704ILR h_704ILR, bool esperado_704ILR)
    {
        bool real_704ILR = BLL_Reserva_704ILR.TransicionValida_704ILR(d_704ILR, h_704ILR);
        Console.WriteLine($"  {d_704ILR} -> {h_704ILR}: {real_704ILR} (esperado {esperado_704ILR}){(real_704ILR == esperado_704ILR ? "" : "   <-- DIFIERE")}");
    }

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
        Console.WriteLine($"  confirmar cotizacion: {BLL_Reserva_704ILR.Actualizar_704ILR(aConfirmar_704ILR)} (esperado Success)");

        var aRetroceder_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        aRetroceder_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.PENDIENTE;
        Console.WriteLine($"  CONFIRMADA -> PENDIENTE via Actualizar: {BLL_Reserva_704ILR.Actualizar_704ILR(aRetroceder_704ILR)} (esperado TransicionInvalida)");

        var aCancelarMal_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        aCancelarMal_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CANCELADA;
        Console.WriteLine($"  cancelar via Actualizar (saltea RN-02): {BLL_Reserva_704ILR.Actualizar_704ILR(aCancelarMal_704ILR)} (esperado TransicionInvalida)");
        Console.WriteLine($"  cancelar por la via correcta: {BLL_Reserva_704ILR.Cancelar_704ILR(idT_704ILR, out _, out _)} (esperado Success)");

        var estadoFinal_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idT_704ILR);
        Console.WriteLine($"  estado final: {estadoFinal_704ILR.Estado_704ILR} (esperado CANCELADA)");

        // Restaurar una version tampoco reabre una reserva cancelada (RN-05).
        var versionesT_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idT_704ILR);
        if (versionesT_704ILR.Count > 0)
            Console.WriteLine($"  restaurar version sobre cancelada: {BLL_Reserva_704ILR.RestaurarVersion_704ILR(idT_704ILR, versionesT_704ILR[0].Id_704ILR)} (esperado NoModificable)");
    }
}

// [31] RN-06 Capacidad del salon: al confirmar, el salon tiene que poder alojar
// a los invitados estimados. En COTIZACION no se exige (la propuesta se esta armando).
Console.WriteLine("[31] RN-06 capacidad del salon al confirmar:");
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
        Console.WriteLine($"  cotizar con exceso de invitados: {rAltaC_704ILR} (esperado Success: en COTIZACION no se exige)");

        var persistidaC_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        Console.WriteLine($"  invitados persistidos: {persistidaC_704ILR.CantidadInvitados_704ILR} (esperado {exceso_704ILR})");

        persistidaC_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Console.WriteLine($"  confirmar con exceso: {BLL_Reserva_704ILR.Actualizar_704ILR(persistidaC_704ILR)} (esperado CapacidadInsuficiente)");

        // Confirmar sin saber cuanta gente viene tampoco se admite.
        var sinDato_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        sinDato_704ILR.CantidadInvitados_704ILR = 0;
        sinDato_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Console.WriteLine($"  confirmar sin invitados: {BLL_Reserva_704ILR.Actualizar_704ILR(sinDato_704ILR)} (esperado InvalidInvitados)");

        Adelanto_704ILR(idC_704ILR, 100m);   // RN-07
        var ajustada_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idC_704ILR);
        ajustada_704ILR.CantidadInvitados_704ILR = chico_704ILR.Capacidad_704ILR;
        ajustada_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Console.WriteLine($"  confirmar ajustando a la capacidad: {BLL_Reserva_704ILR.Actualizar_704ILR(ajustada_704ILR)} (esperado Success)");

        // El control de cambios registra la correccion de invitados campo por campo.
        var histC_704ILR = RegistradorDeCambios_704ILR.GetHistorial_704ILR("Reserva", idC_704ILR);
        int cambiosInv_704ILR = histC_704ILR.Count(h_704ILR => h_704ILR.NombreCampo_704ILR == "CantidadInvitados");
        Console.WriteLine($"  cambios de CantidadInvitados en el historial: {cambiosInv_704ILR} (esperado 1)");

        // El memento conserva la cantidad de invitados de cada version.
        var versionesC_704ILR = CaretakerReserva_704ILR.GetVersiones_704ILR(idC_704ILR);
        if (versionesC_704ILR.Count > 0)
        {
            var vC_704ILR = CaretakerReserva_704ILR.GetVersion_704ILR(versionesC_704ILR[versionesC_704ILR.Count - 1].Id_704ILR);
            Console.WriteLine($"  invitados en la version mas antigua: {vC_704ILR.CantidadInvitados_704ILR} (esperado {exceso_704ILR})");
        }

        // Limpieza: la reserva confirmada del caso se cancela para no bloquear el
        // salon en la proxima corrida.
        BLL_Reserva_704ILR.Cancelar_704ILR(idC_704ILR, out _, out _);
    }
}

// [32] RN-07 Adelanto para confirmar. Lo que distingue a una reserva CONFIRMADA de
// una PENDIENTE es que el cliente ya puso dinero: sin adelanto registrado no se
// confirma, y como el cobro necesita la reserva ya guardada, tampoco se puede nacer
// CONFIRMADA. El orden del proceso es siempre guardar -> cobrar -> confirmar.
Console.WriteLine("[32] RN-07 Adelanto para confirmar:");
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
        Console.WriteLine($"  alta directa CONFIRMADA: {BLL_Reserva_704ILR.Crear_704ILR(altaDirecta_704ILR, out _)} (esperado SinAdelanto)");

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
        Console.WriteLine($"  alta cotizacion: {rAltaA_704ILR} (esperado Success)");
        Console.WriteLine($"  adelanto registrado: {BLL_Reserva_704ILR.TieneAdelanto_704ILR(idA_704ILR)} (esperado False)");

        var sinPago_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR);
        sinPago_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Console.WriteLine($"  confirmar sin adelanto: {BLL_Reserva_704ILR.Actualizar_704ILR(sinPago_704ILR)} (esperado SinAdelanto)");
        Console.WriteLine($"  estado tras el rechazo: {BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR).Estado_704ILR} (esperado COTIZACION)");

        // Con el adelanto cobrado, la misma confirmacion procede.
        Adelanto_704ILR(idA_704ILR, 300m);
        Console.WriteLine($"  adelanto registrado: {BLL_Reserva_704ILR.TieneAdelanto_704ILR(idA_704ILR)} (esperado True)");
        var conPago_704ILR = BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR);
        conPago_704ILR.Estado_704ILR = EvenTech.BE.EstadoReserva_704ILR.CONFIRMADA;
        Console.WriteLine($"  confirmar con adelanto: {BLL_Reserva_704ILR.Actualizar_704ILR(conPago_704ILR)} (esperado Success)");
        Console.WriteLine($"  estado final: {BLL_Reserva_704ILR.GetById_704ILR(idA_704ILR).Estado_704ILR} (esperado CONFIRMADA)");

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
        Console.WriteLine($"  confirmar pendiente sin adelanto: {BLL_Reserva_704ILR.Actualizar_704ILR(penSinPago_704ILR)} (esperado SinAdelanto)");

        // Limpieza: se liberan los dos salones tomados por el caso.
        BLL_Reserva_704ILR.Cancelar_704ILR(idA_704ILR, out _, out _);
        BLL_Reserva_704ILR.Cancelar_704ILR(idPenA_704ILR, out _, out _);
    }
}

// [33] Anulacion de pago: pasa por las mismas reglas que el registro. Antes esto
// borraba la fila sin mirar si el pago existia, si era de esa reserva o si la
// reserva admitia movimientos.
Console.WriteLine("[33] Anulacion de pago con reglas:");
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
        BLL_Pago_704ILR.Registrar_704ILR(new EvenTech.BE.BE_Pago_704ILR
        { ReservaId_704ILR = idP_704ILR, MetodoPagoId_704ILR = metP_704ILR[0].Id_704ILR, Monto_704ILR = 200m }, out int idPago_704ILR);
        Console.WriteLine($"  pagado: {BLL_Pago_704ILR.TotalPagado_704ILR(idP_704ILR):N2} (esperado 200,00)");

        // Un pago inexistente no se anula.
        Console.WriteLine($"  anular pago inexistente: {BLL_Pago_704ILR.Eliminar_704ILR(999999, idP_704ILR)} (esperado PagoInvalido)");

        // Un pago que existe pero es de otra reserva, tampoco.
        Console.WriteLine($"  anular pago ajeno a la reserva: {BLL_Pago_704ILR.Eliminar_704ILR(idPago_704ILR, idP_704ILR + 100000)} (esperado PagoInvalido)");

        // Sobre una reserva cancelada no se admiten movimientos de cobro (RN-04).
        BLL_Reserva_704ILR.Cancelar_704ILR(idP_704ILR, out _, out _);
        Console.WriteLine($"  anular sobre reserva cancelada: {BLL_Pago_704ILR.Eliminar_704ILR(idPago_704ILR, idP_704ILR)} (esperado ReservaCancelada)");
        Console.WriteLine($"  el pago sigue registrado: {BLL_Pago_704ILR.TotalPagado_704ILR(idP_704ILR):N2} (esperado 200,00)");
    }
}

Console.WriteLine("== fin ==");
