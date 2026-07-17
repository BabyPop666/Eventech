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

Console.WriteLine("== fin ==");
