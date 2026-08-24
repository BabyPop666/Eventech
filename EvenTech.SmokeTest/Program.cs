using EvenTech.BLL;
using EvenTech.Services;

Console.WriteLine("== EvenTech smoke test v2 ==");

// [1] Login OK
Console.WriteLine("[1] Login admin/admin123:");
var r1 = BLL_Login.Authenticate("admin", Encrypt.HashValue("admin123"));
Console.WriteLine($"  result={r1}, sesionActiva={SessionManager.IsSessionActive}");
BLL_Login.Logout();

// [2] Crear usuario nuevo (con timestamp para que sea unico entre corridas)
string newUser = "smoke_" + DateTime.Now.ToString("HHmmss");
Console.WriteLine($"[2] Crear usuario '{newUser}' password 'pass1234':");
var rc1 = BLL_User.CreateUser(newUser, Encrypt.HashValue("pass1234"));
Console.WriteLine($"  result={rc1}");

// [3] Crear duplicado
Console.WriteLine($"[3] Crear '{newUser}' duplicado:");
var rc2 = BLL_User.CreateUser(newUser, Encrypt.HashValue("otra"));
Console.WriteLine($"  result={rc2}");

// [4] Username invalido
Console.WriteLine("[4] Crear con username '..' (invalido):");
var rc3 = BLL_User.CreateUser("..", Encrypt.HashValue("xxxx"));
Console.WriteLine($"  result={rc3}");

// [5] Login con el usuario recien creado
Console.WriteLine($"[5] Login con '{newUser}':");
var r5 = BLL_Login.Authenticate(newUser, Encrypt.HashValue("pass1234"));
Console.WriteLine($"  result={r5}");
BLL_Login.Logout();

// [6] Leer auditoria (ultimas 5)
Console.WriteLine("[6] Ultimas 5 entradas de auditoria:");
foreach (var e in BLL_LoginAudit.GetAll(5))
{
    Console.WriteLine($"  #{e.Id} {e.Timestamp:HH:mm:ss} {e.Username,-20} {e.Action,-12} {e.Details}");
}

// [7] Reservas: alta valida (la reserva referencia al cliente por Id)
Console.WriteLine("[7] Crear reserva valida:");
var salones = BLL_Salon.GetAll();
var clientes = BLL_Cliente.GetAll();
if (salones.Count == 0 || clientes.Count == 0)
{
    Console.WriteLine("  (no hay salones/clientes seed; corre db/schema.sql)");
}
else
{
    var nueva = new EvenTech.BE.BE_Reserva
    {
        ClienteId = clientes[0].Id,
        SalonId = salones[0].Id,
        FechaEvento = DateTime.Today.AddDays(30),
        Estado = EvenTech.BE.EstadoReserva.PENDIENTE,
        Monto = 150000m
    };
    var rr1 = BLL_Reserva.Crear(nueva, out int nuevoId);
    Console.WriteLine($"  result={rr1}, nuevoId={nuevoId}");

    // [8] Reserva con fecha pasada (debe fallar)
    Console.WriteLine("[8] Crear reserva con fecha pasada (invalida):");
    var pasada = new EvenTech.BE.BE_Reserva
    {
        ClienteId = clientes[0].Id,
        SalonId = salones[0].Id,
        FechaEvento = DateTime.Today.AddDays(-1),
        Estado = EvenTech.BE.EstadoReserva.PENDIENTE,
        Monto = 1000m
    };
    var rr2 = BLL_Reserva.Crear(pasada, out _);
    Console.WriteLine($"  result={rr2}");

    // [9] Listado
    Console.WriteLine("[9] Total de reservas:");
    Console.WriteLine($"  {BLL_Reserva.GetAll().Count} reservas");

    // [10] Control de cambios: modificar la reserva recien creada
    if (rr1 == ReservaResult.Success)
    {
        Console.WriteLine($"[10] Modificar reserva #{nuevoId} (estado + monto):");
        var editada = BLL_Reserva.GetById(nuevoId);
        editada.Estado = EvenTech.BE.EstadoReserva.CONFIRMADA;
        editada.Monto = 175000m;
        var ru = BLL_Reserva.Actualizar(editada);
        Console.WriteLine($"  result={ru}");

        Console.WriteLine($"[11] Historial de cambios de la reserva #{nuevoId}:");
        foreach (var c in EvenTech.BLL.RegistradorDeCambios.GetHistorial("Reserva", nuevoId))
            Console.WriteLine($"  {c.Fecha:HH:mm:ss} {c.NombreCampo,-14} '{c.ValorAnterior}' -> '{c.ValorNuevo}'");
    }

    // [12] Bitacora general (ultimas 5)
    Console.WriteLine("[12] Ultimas 5 entradas de bitacora:");
    int mostradas = 0;
    foreach (var b in EvenTech.BLL.BLL_Bitacora.Buscar(new EvenTech.BE.BitacoraFiltros()))
    {
        Console.WriteLine($"  #{b.Id} {b.Fecha:HH:mm:ss} {b.Modulo,-10} {b.Accion,-26} {b.Criticidad}");
        if (++mostradas >= 5) break;
    }
}

// [13] Composite de perfiles: recorrer arbol y permisos efectivos
Console.WriteLine("[13] Arbol de permisos (Composite):");
var arbol = BLL_Perfil.GetArbolPermisos();
void Imprimir(EvenTech.BE.BE_IComponentePermiso n, int nivel)
{
    Console.WriteLine($"  {new string(' ', nivel * 2)}{(n.EsGrupo ? "[G]" : "[P]")} {n.Nombre}");
    if (n is EvenTech.BE.BE_GrupoPermisos g)
        foreach (var h in g.Hijos) Imprimir(h, nivel + 1);
}
foreach (var raiz in arbol) Imprimir(raiz, 0);

var perfiles = BLL_Perfil.GetPerfiles();
if (perfiles.Count > 0)
{
    var asignados = BLL_Perfil.GetPermisosAsignados(perfiles[0].Id);
    var efectivos = BLL_Perfil.CalcularPermisosEfectivos(arbol, asignados);
    Console.WriteLine($"[14] Perfil '{perfiles[0].Nombre}': {efectivos.Count} permisos efectivos (hojas).");
}

// [15] Idiomas (Observer): cambio dinamico de traducciones
Console.WriteLine("[15] Idiomas (Observer):");
EvenTech.BLL.BLL_Idioma.Inicializar();
var gi = EvenTech.Services.GestorDeIdioma.GetInstance;
Console.WriteLine($"  idioma={gi.IdiomaActual}, MENU_RESERVAS='{gi.Traducir("MENU_RESERVAS")}'");
gi.CambiarIdioma("EN");
Console.WriteLine($"  idioma={gi.IdiomaActual}, MENU_RESERVAS='{gi.Traducir("MENU_RESERVAS")}'");
gi.CambiarIdioma("ES");

// [16] Digitos verificadores (T07/T08)
Console.WriteLine("[16] Integridad (digitos verificadores):");
var resInt = EvenTech.BLL.BLL_Integridad.Verificar();
Console.WriteLine($"  Ok={resInt.Ok}, inconsistencias={resInt.Inconsistencias.Count}");
foreach (var i in resInt.Inconsistencias) Console.WriteLine("   - " + i);

// Recalculo de linea base (accion administrativa del proceso ante corrupcion):
// tras recalcular, la verificacion tiene que dar limpia si o si.
int recalculadas = EvenTech.BLL.BLL_Integridad.RecalcularTodo();
var resInt2 = EvenTech.BLL.BLL_Integridad.Verificar();
Console.WriteLine($"  recalculo de linea base: {recalculadas} reservas -> Ok={resInt2.Ok} (esperado True)");

// [17] Alta de idioma desde la capa de negocio (admin agrega idioma)
Console.WriteLine("[17] Crear idioma 'PT':");
var rIdioma = EvenTech.BLL.BLL_Idioma.CrearIdioma("PT", "Portugues", out int idPt);
Console.WriteLine($"  result={rIdioma}");
Console.WriteLine($"  idiomas disponibles: {EvenTech.Services.GestorDeIdioma.GetInstance.IdiomasDisponibles.Count}");

// [18] Patron Memento: versionado y restauracion de reservas
Console.WriteLine("[18] Memento (versiones de reserva):");
var clientesM = BLL_Cliente.GetAll();
var salonesM = BLL_Salon.GetAll();
if (clientesM.Count == 0 || salonesM.Count == 0)
{
    Console.WriteLine("  (faltan clientes/salones seed; corre db/schema.sql)");
}
else
{
    var reservaM = new EvenTech.BE.BE_Reserva
    {
        ClienteId = clientesM[0].Id,
        SalonId = salonesM[0].Id,
        FechaEvento = DateTime.Today.AddDays(45),
        Estado = EvenTech.BE.EstadoReserva.PENDIENTE,
        Monto = 1000m
    };
    var rm = BLL_Reserva.Crear(reservaM, out int idM);
    Console.WriteLine($"  alta: result={rm}, id={idM}");

    var v1 = BLL_Reserva.GetById(idM);
    v1.Estado = EvenTech.BE.EstadoReserva.CONFIRMADA;
    v1.Monto = 2000m;
    Console.WriteLine($"  modificar (PENDIENTE/1000 -> CONFIRMADA/2000): result={BLL_Reserva.Actualizar(v1)}");

    var versiones = CaretakerReserva.GetVersiones(idM);
    Console.WriteLine($"  versiones guardadas: {versiones.Count} (esperado 1)");

    if (versiones.Count > 0)
    {
        var rr = BLL_Reserva.RestaurarVersion(idM, versiones[0].Id);
        var restaurada = BLL_Reserva.GetById(idM);
        Console.WriteLine($"  restaurar: result={rr} -> Estado={restaurada.Estado}, Monto={restaurada.Monto} (esperado PENDIENTE, 1000)");
        Console.WriteLine($"  versiones tras restaurar: {CaretakerReserva.GetVersiones(idM).Count} (esperado 2: la restauracion versiona el estado que piso)");
    }
}

// [19] Composite de perfiles: un perfil incluye a otro y hereda sus permisos
Console.WriteLine("[19] Composite de perfiles (perfil incluye perfil):");
{
    string suf = DateTime.Now.ToString("HHmmss");
    var arbolC = BLL_Perfil.GetArbolPermisos();

    // Busca el id de una hoja por su clave, recorriendo el arbol Composite.
    int BuscarClave(IEnumerable<EvenTech.BE.BE_IComponentePermiso> nodos, string clave)
    {
        foreach (var n in nodos)
        {
            if (n is EvenTech.BE.BE_Permiso p && p.Clave == clave) return p.Id;
            if (n is EvenTech.BE.BE_GrupoPermisos g)
            {
                int r = BuscarClave(g.Hijos, clave);
                if (r > 0) return r;
            }
        }
        return 0;
    }

    int idCrear = BuscarClave(arbolC, "RESERVA_CREAR");
    int idEditar = BuscarClave(arbolC, "RESERVA_EDITAR");
    int idBitacora = BuscarClave(arbolC, "BITACORA_VER");

    BLL_Perfil.CrearPerfil("Vendedor_" + suf, "smoke", out int idVend);
    BLL_Perfil.CrearPerfil("Gerencial_" + suf, "smoke", out int idGer);

    var rVend = BLL_Perfil.GuardarComposicion(idVend, new[] { idCrear, idEditar }, new int[0]);
    Console.WriteLine($"  Vendedor (RESERVA_CREAR + RESERVA_EDITAR): result={rVend}");

    var rGer = BLL_Perfil.GuardarComposicion(idGer, new[] { idBitacora }, new[] { idVend });
    Console.WriteLine($"  Gerencial (BITACORA_VER + incluye Vendedor): result={rGer}");

    var efectivosGer = BLL_Perfil.GetPermisosEfectivosDePerfil(idGer);
    Console.WriteLine($"  permisos efectivos de Gerencial: {string.Join(", ", efectivosGer.Select(p => p.Clave))}");
    Console.WriteLine($"  (esperado: BITACORA_VER + RESERVA_CREAR + RESERVA_EDITAR heredados de Vendedor)");

    var rCiclo = BLL_Perfil.GuardarComposicion(idVend, new[] { idCrear, idEditar }, new[] { idGer });
    Console.WriteLine($"  incluir Gerencial dentro de Vendedor: result={rCiclo} (esperado ReferenciaCircular)");

    var rSelf = BLL_Perfil.GuardarComposicion(idVend, new[] { idCrear }, new[] { idVend });
    Console.WriteLine($"  incluir Vendedor dentro de si mismo: result={rSelf} (esperado ReferenciaCircular)");
}

// [20] Cifrado reversible (AES) de datos sensibles del cliente
Console.WriteLine("[20] Cifrado reversible de Email/Telefono de clientes:");
{
    string suf = DateTime.Now.ToString("HHmmss");
    var cli = new EvenTech.BE.BE_Cliente
    {
        Nombre = "SmokeCrypto",
        Apellido = suf,
        Email = $"crypto_{suf}@test.com",
        Telefono = "11-5555-" + suf
    };
    var rCli = BLL_Cliente.Crear(cli, out int idCli);
    Console.WriteLine($"  alta: result={rCli}, id={idCli}");

    var leido = BLL_Cliente.GetById(idCli);
    bool roundtripOk = leido.Email == cli.Email && leido.Telefono == cli.Telefono;
    Console.WriteLine($"  leido por la app: Email='{leido.Email}', Telefono='{leido.Telefono}'");
    Console.WriteLine($"  roundtrip cifrar->descifrar: {(roundtripOk ? "OK" : "FALLO")} (esperado OK)");

    // Lectura cruda, salteando la DAL: en la DB tiene que estar cifrado.
    using (var cn = new EvenTech.DAL.DAL_DB_Connection())
    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
        "SELECT Email, Telefono FROM dbo.Clientes WHERE Id = @id", cn.OpenConnection()))
    {
        cmd.Parameters.AddWithValue("@id", idCli);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            string rawE = r.GetString(0), rawT = r.GetString(1);
            Console.WriteLine($"  crudo en DB: Email='{rawE[..Math.Min(44, rawE.Length)]}...'");
            Console.WriteLine($"  cifrado en DB: Email={CryptoService.EstaProtegido(rawE)}, " +
                              $"Telefono={CryptoService.EstaProtegido(rawT)} (esperado True, True)");
        }
    }
}

// [21] Control de acceso: los permisos se conceden solo si estan en el perfil
// (denegar por defecto). Se valida sobre la sesion real de admin.
Console.WriteLine("[21] Permisos de la sesion (denegar por defecto):");
{
    BLL_Login.Authenticate("admin", Encrypt.HashValue("admin123"));
    var s = SessionManager.GetInstance;
    Console.WriteLine($"  permisosNoDisponibles={s.PermisosNoDisponibles} (esperado False)");
    Console.WriteLine($"  admin tiene RESERVA_CREAR: {s.TienePermiso("RESERVA_CREAR")} (esperado True)");
    Console.WriteLine($"  admin tiene PAGOS_ANULAR: {s.TienePermiso("PAGOS_ANULAR")} (esperado True)");
    Console.WriteLine($"  clave inexistente NO_EXISTE: {s.TienePermiso("NO_EXISTE")} (esperado False)");
    Console.WriteLine($"  clave nula: {s.TienePermiso(null)} (esperado False)");
    BLL_Login.Logout();
}

// [22] Todas las claves que la UI exige tienen que existir en el arbol: si una
// falta, la seccion queda invisible para todos y el problema pasa inadvertido.
Console.WriteLine("[22] Claves de permiso usadas por la UI presentes en el arbol:");
{
    string[] usadas = { "RESERVA_CREAR", "RESERVA_EDITAR", "RESERVA_HISTORIAL",
                        "CLIENTES_GESTION", "SERVICIOS_GESTION", "PERFILES_GESTION",
                        "IDIOMAS_GESTION", "BITACORA_VER", "AUDIT_LOGIN_VER",
                        "INTEGRIDAD_RECALC", "PAGOS_REGISTRAR", "PAGOS_ANULAR",
                        "DISPONIBILIDAD_CONSULTAR" };
    var enArbol = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    void Recorrer(IEnumerable<EvenTech.BE.BE_IComponentePermiso> nodos)
    {
        foreach (var n in nodos)
        {
            if (n is EvenTech.BE.BE_Permiso hoja && !string.IsNullOrEmpty(hoja.Clave)) enArbol.Add(hoja.Clave);
            if (n is EvenTech.BE.BE_GrupoPermisos g) Recorrer(g.Hijos);
        }
    }
    Recorrer(BLL_Perfil.GetArbolPermisos());
    var faltan = usadas.Where(c => !enArbol.Contains(c)).ToList();
    Console.WriteLine($"  claves en el arbol: {enArbol.Count}; faltantes: " +
                      (faltan.Count == 0 ? "ninguna (esperado)" : string.Join(", ", faltan)));
}

// [23] Una reserva cancelada es estado terminal: no admite modificaciones.
Console.WriteLine("[23] Reserva cancelada no modificable:");
{
    var sal = BLL_Salon.GetAll();
    var cli = BLL_Cliente.GetAll();
    if (sal.Count == 0 || cli.Count == 0)
    {
        Console.WriteLine("  (no hay salones/clientes seed; corre db/schema.sql)");
    }
    else
    {
        var res = new EvenTech.BE.BE_Reserva
        {
            ClienteId = cli[0].Id,
            SalonId = sal[0].Id,
            FechaEvento = DateTime.Today.AddDays(45),
            Estado = EvenTech.BE.EstadoReserva.CANCELADA,
            Monto = 1000m
        };
        var rAlta = BLL_Reserva.Crear(res, out int idCancel);
        Console.WriteLine($"  alta cancelada: result={rAlta}, id={idCancel}");

        var guardada = BLL_Reserva.GetById(idCancel);
        Console.WriteLine($"  PuedeModificar: {BLL_Reserva.PuedeModificar(guardada)} (esperado False)");

        guardada.Monto = 2000m;
        var rMod = BLL_Reserva.Actualizar(guardada);
        Console.WriteLine($"  intento de modificar: result={rMod} (esperado NoModificable)");

        // Una reserva viva si se modifica.
        var viva = BLL_Reserva.GetById(idCancel);
        viva.Estado = EvenTech.BE.EstadoReserva.PENDIENTE;
        Console.WriteLine($"  PuedeModificar sobre PENDIENTE: {BLL_Reserva.PuedeModificar(viva)} (esperado True)");

        // Los pagos persisten en el acto, sin pasar por BLL_Reserva.Actualizar:
        // la regla del estado terminal tiene que rechazarlos tambien.
        var metodos = BLL_Pago.GetMetodos();
        if (metodos.Count > 0)
        {
            var pago = new EvenTech.BE.BE_Pago { ReservaId = idCancel, MetodoPagoId = metodos[0].Id, Monto = 10m };
            var rPago = BLL_Pago.Registrar(pago, out _);
            Console.WriteLine($"  cobrar sobre cancelada: result={rPago} (esperado ReservaCancelada)");
        }
    }
}

// [24] Configuracion de conexion: la cadena sale del gestor (no hardcodeada) y
// el diagnostico distingue servidor caido de base inexistente.
Console.WriteLine("[24] Configuracion de conexion:");
{
    Console.WriteLine($"  configurada por el usuario: {BLL_Conexion.EstaConfigurada}");
    Console.WriteLine($"  servidor='{BLL_Conexion.ServidorActual}', base='{BLL_Conexion.BaseDatosActual}'");

    bool ok = BLL_Conexion.VerificarActual(out string msgOk);
    Console.WriteLine($"  verificar actual: {ok} (esperado True){(ok ? "" : " -> " + msgOk)}");

    bool inexistente = BLL_Conexion.Probar(EvenTech.Services.ConfiguracionConexion.ServidorPorDefecto,
                                           "BaseQueNoExiste_" + DateTime.Now.ToString("HHmmss"), out string msgNo);
    Console.WriteLine($"  base inexistente: {inexistente} (esperado False)");
    Console.WriteLine($"    diagnostico: {msgNo}");

    Console.WriteLine($"  instancias detectadas: {BLL_Conexion.GetInstancias().Count}");

    // Roundtrip del archivo cifrado con DPAPI: si guardar/leer fallara, la app
    // quedaria sin poder conectar en el proximo arranque. Se prueba con la
    // configuracion que ya funciona y se deja el entorno como estaba.
    bool estabaConfigurada = BLL_Conexion.EstaConfigurada;
    string servidorPrevio = BLL_Conexion.ServidorActual, basePrevia = BLL_Conexion.BaseDatosActual;

    bool guardo = BLL_Conexion.Guardar(servidorPrevio, basePrevia, out string msgGuardar);
    Console.WriteLine($"  guardar cifrado (DPAPI): {guardo} (esperado True){(guardo ? "" : " -> " + msgGuardar)}");
    Console.WriteLine($"  persistida: {BLL_Conexion.EstaConfigurada} (esperado True)");

    bool releeOk = BLL_Conexion.VerificarActual(out _);
    Console.WriteLine($"  releida y conecta: {releeOk} (esperado True)");
    Console.WriteLine($"  servidor releido='{BLL_Conexion.ServidorActual}', base='{BLL_Conexion.BaseDatosActual}' " +
                      $"(esperado '{servidorPrevio}', '{basePrevia}')");

    if (!estabaConfigurada)
    {
        BLL_Conexion.Restablecer();
        Console.WriteLine($"  entorno restaurado (sin archivo): {!BLL_Conexion.EstaConfigurada} (esperado True)");
    }
}

// [25] Diagnostico de conexion: una base sin el esquema tiene que rechazarse, si
// no la app quedaria conectada a una base inservible sin volver a ofrecer configurar.
Console.WriteLine("[25] Base existente pero sin esquema:");
{
    const string tmpDb = "EvenTechSmokeVacia";
    string cs = EvenTech.Services.ConfiguracionConexion.Construir(
        EvenTech.Services.ConfiguracionConexion.ServidorActual, tmpDb);
    try
    {
        using (var cn = new Microsoft.Data.SqlClient.SqlConnection(
            EvenTech.Services.ConfiguracionConexion.Construir(EvenTech.Services.ConfiguracionConexion.ServidorActual, "master")))
        {
            cn.Open();
            using var crear = new Microsoft.Data.SqlClient.SqlCommand(
                $"IF DB_ID('{tmpDb}') IS NULL CREATE DATABASE [{tmpDb}]", cn);
            crear.ExecuteNonQuery();
        }

        bool ok = EvenTech.DAL.DAL_DB_Connection.Probar(cs, out string msg);
        Console.WriteLine($"  aceptada: {ok} (esperado False)");
        Console.WriteLine($"    diagnostico: {msg}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  (no se pudo crear la base de prueba: {ex.Message})");
    }
    finally
    {
        try
        {
            using var cn = new Microsoft.Data.SqlClient.SqlConnection(
                EvenTech.Services.ConfiguracionConexion.Construir(EvenTech.Services.ConfiguracionConexion.ServidorActual, "master"));
            cn.Open();
            using var borrar = new Microsoft.Data.SqlClient.SqlCommand(
                $"IF DB_ID('{tmpDb}') IS NOT NULL BEGIN ALTER DATABASE [{tmpDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{tmpDb}]; END", cn);
            borrar.ExecuteNonQuery();
            Console.WriteLine("  base de prueba eliminada");
        }
        catch (Exception ex) { Console.WriteLine($"  (no se pudo limpiar la base de prueba: {ex.Message})"); }
    }
}

// [26] Flujo completo del Proceso 1 (RF1): cotizacion con servicios -> total =
// suma de subtotales -> confirmacion (anti-solapamiento) -> adelanto y saldo
// (tope = total). Es el happy path que la UI recorre pantalla por pantalla.
Console.WriteLine("[26] Flujo RF1 completo (servicios, confirmacion, pagos):");
{
    var sal = BLL_Salon.GetAll();
    var cli = BLL_Cliente.GetAll();
    var srv = BLL_Servicio.GetActivos();
    if (sal.Count == 0 || cli.Count == 0 || srv.Count < 2)
    {
        Console.WriteLine("  (faltan salones/clientes/servicios seed; corre db/schema.sql)");
    }
    else
    {
        // Fecha propia de la corrida (como el username unico del caso [2]): una
        // corrida anterior deja su reserva confirmada en la base y una fecha
        // fija haria fallar la confirmacion por SalonOcupado.
        DateTime fechaEvento = DateTime.Today.AddDays(60 + (int)DateTime.Now.TimeOfDay.TotalSeconds % 900);

        // Cotizacion: no compromete el salon. El monto es la suma de servicios.
        var servicios = new List<EvenTech.BE.BE_ReservaServicio>
        {
            new EvenTech.BE.BE_ReservaServicio { ServicioId = srv[0].Id, Cantidad = 2, PrecioUnitario = srv[0].Precio },
            new EvenTech.BE.BE_ReservaServicio { ServicioId = srv[1].Id, Cantidad = 1, PrecioUnitario = srv[1].Precio }
        };
        decimal total = BLL_ReservaServicio.Total(servicios);
        decimal esperado = srv[0].Precio * 2 + srv[1].Precio;
        Console.WriteLine($"  total de servicios: {total:N2} (esperado {esperado:N2})");

        var cot = new EvenTech.BE.BE_Reserva
        {
            ClienteId = cli[0].Id,
            SalonId = sal[0].Id,
            FechaEvento = fechaEvento,
            Estado = EvenTech.BE.EstadoReserva.COTIZACION,
            Monto = total
        };
        var rCot = BLL_Reserva.Crear(cot, out int idFlujo);
        BLL_ReservaServicio.Guardar(idFlujo, servicios);
        Console.WriteLine($"  alta cotizacion: result={rCot}, id={idFlujo}");
        Console.WriteLine($"  servicios persistidos: {BLL_ReservaServicio.GetByReserva(idFlujo).Count} (esperado {servicios.Count})");

        // Confirmar: recien aca se compromete el salon.
        var reserva = BLL_Reserva.GetById(idFlujo);
        reserva.Estado = EvenTech.BE.EstadoReserva.CONFIRMADA;
        var rConf = BLL_Reserva.Actualizar(reserva);
        Console.WriteLine($"  confirmar: result={rConf} (esperado Success)");

        // Anti-solapamiento: otra CONFIRMADA para el mismo salon y fecha se rechaza.
        var choque = new EvenTech.BE.BE_Reserva
        {
            ClienteId = cli[0].Id,
            SalonId = sal[0].Id,
            FechaEvento = fechaEvento,
            Estado = EvenTech.BE.EstadoReserva.CONFIRMADA,
            Monto = 1000m
        };
        var rChoque = BLL_Reserva.Crear(choque, out _);
        Console.WriteLine($"  segunda confirmada mismo salon/fecha: result={rChoque} (esperado SalonOcupado)");

        // Cobros: adelanto, intento de exceso y saldo exacto.
        var metodos = BLL_Pago.GetMetodos();
        Console.WriteLine($"  metodos de pago: {metodos.Count} (esperado 5)");
        decimal adelanto = Math.Round(total / 2, 2);

        var rAde = BLL_Pago.Registrar(new EvenTech.BE.BE_Pago
        { ReservaId = idFlujo, MetodoPagoId = metodos[0].Id, Monto = adelanto, Observacion = "Adelanto" }, out _);
        Console.WriteLine($"  adelanto {adelanto:N2}: result={rAde} (esperado Success)");
        Console.WriteLine($"  saldo tras adelanto: {BLL_Pago.Saldo(idFlujo):N2} (esperado {total - adelanto:N2})");

        var rExceso = BLL_Pago.Registrar(new EvenTech.BE.BE_Pago
        { ReservaId = idFlujo, MetodoPagoId = metodos[0].Id, Monto = total }, out _);
        Console.WriteLine($"  pago que excede el saldo: result={rExceso} (esperado ExcedeSaldo)");

        var rSaldo = BLL_Pago.Registrar(new EvenTech.BE.BE_Pago
        { ReservaId = idFlujo, MetodoPagoId = metodos[metodos.Count - 1].Id, Monto = total - adelanto, Observacion = "Saldo" }, out _);
        Console.WriteLine($"  saldo restante: result={rSaldo} (esperado Success)");
        Console.WriteLine($"  saldo final: {BLL_Pago.Saldo(idFlujo):N2} (esperado 0,00)");

        // [27] Consulta de disponibilidad (Proceso 1, paso 1): la fecha recien
        // confirmada tiene que figurar ocupada para ese salon, con una fecha
        // alternativa propuesta; una capacidad imposible marca insuficiente.
        Console.WriteLine("[27] Consulta de disponibilidad:");
        var disp = BLL_Disponibilidad.Consultar(fechaEvento, 0);
        Console.WriteLine($"  salones evaluados: {disp.Count} (esperado {sal.Count})");
        var delFlujo = disp.FirstOrDefault(d => d.SalonId == sal[0].Id);
        Console.WriteLine($"  salon confirmado libre: {delFlujo?.Libre} (esperado False)");
        Console.WriteLine($"  propuesta alternativa: {(delFlujo?.ProximaFechaLibre.HasValue == true ? delFlujo.ProximaFechaLibre.Value.ToString("yyyy-MM-dd") : "ninguna")} (esperada una fecha)");

        var dispCap = BLL_Disponibilidad.Consultar(fechaEvento, 99999);
        Console.WriteLine($"  capacidad imposible -> disponibles: {dispCap.Count(d => d.Disponible)} (esperado 0)");
        Console.WriteLine($"  capacidad imposible -> suficientes: {dispCap.Count(d => d.CapacidadSuficiente)} (esperado 0)");

        // Un dia sin reservas confirmadas: todos los salones libres.
        var dispLibre = BLL_Disponibilidad.Consultar(fechaEvento.AddDays(2000), 0);
        Console.WriteLine($"  fecha lejana -> disponibles: {dispLibre.Count(d => d.Disponible)}/{dispLibre.Count} (esperado todos)");

        // Limpieza: se cancela la reserva del flujo para liberar el salon
        // (la corrida queda repetible aunque la fecha se repitiera).
        var fin = BLL_Reserva.GetById(idFlujo);
        fin.Estado = EvenTech.BE.EstadoReserva.CANCELADA;
        var rFin = BLL_Reserva.Actualizar(fin);
        Console.WriteLine($"  limpieza (cancelar reserva del flujo): result={rFin} (esperado Success)");
    }
}

Console.WriteLine("== fin ==");
