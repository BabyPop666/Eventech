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

Console.WriteLine("== fin ==");
