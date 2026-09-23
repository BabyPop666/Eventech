# EvenTech

Sistema de gestion operativa de salones de fiestas y eventos: reservas y
cotizaciones, clientes, servicios contratados, cobros y comprobantes, con
seguridad, auditoria e integridad de datos de forma transversal.

Aplicacion de escritorio **WinForms sobre .NET 8** con **SQL Server Express**,
organizada en cinco capas y sin frameworks de persistencia de terceros.

> **Trabajo de Diploma** — Reser, Ivan Leonel (DNI 38.823.704, Legajo
> A0900013691-T1). Comision 3-B-N, Sede Centro, 2026.
> Clases, metodos, propiedades, campos, variables y miembros de enumerados de
> resultado llevan el sufijo de autoria `_704ILR`. Quedan sin sufijo, a proposito,
> las tablas y columnas de la base, los valores que se persisten como dato (estados
> de la reserva, criticidades, claves de permisos y de traducciones) y lo que el
> framework no permite renombrar.

---

## Requisitos

| Componente | Version minima |
|---|---|
| Sistema operativo | Windows 10 (x64) |
| Runtime | .NET 8 Desktop Runtime |
| Motor de base de datos | SQL Server Express 2019 (MSSQL15) |
| Herramienta de linea de comandos | `sqlcmd` (ver nota) |
| Para compilar | .NET 8 SDK |

`sqlcmd` **no viene con el motor**: se instala con SQL Server Management Studio
(SSMS), con el paquete *Microsoft Command Line Utilities for SQL Server* (el
`sqlcmd` ODBC) o como `go-sqlcmd` (`winget install sqlcmd`). Cualquiera de los
tres acepta los parametros que se usan abajo (`-E -C -b -i`); `-C` (confiar en el
certificado del servidor) exige una version reciente. Si no se quiere instalar la
herramienta, `db\schema.sql` tambien se puede abrir y ejecutar desde SSMS sobre la
base ya creada.

Hardware sugerido: procesador Intel Core i5 o equivalente, 8 GB de RAM,
500 MB de disco y una resolucion efectiva de 1366x768 o superior (la de la
pantalla dividida por la escala configurada en Windows).

## Puesta en marcha

### 1. Crear la base de datos

`db/schema.sql` es **idempotente**: crea las tablas que falten, aplica las
migraciones de columnas y de valores por defecto y siembra los datos base
(idiomas y traducciones, permisos y los cuatro perfiles, salones, servicios,
metodos de pago y el usuario inicial). Se puede ejecutar varias veces: agrega lo
que falte, regulariza el vencimiento (`VenceEl`) de las operaciones anteriores y
corrige los textos de fabrica solo mientras conserven su valor original; las
traducciones editadas por el usuario y los idiomas agregados se conservan.

El script corre **sobre la base que indica `-d`**: no la crea ni cambia de
contexto, y aborta con error si `-d` falta (para no ejecutarse sobre `master`).
Por eso la base se crea aparte, en el primer comando:

```bat
sqlcmd -S localhost\SQLEXPRESS -d master -E -C -Q "IF DB_ID('EvenTechDB') IS NULL CREATE DATABASE EvenTechDB;"
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -b -i db\schema.sql
```

`-b` corta la ejecucion en el primer error, en lugar de seguir y dejar una base a
medias. `-I` (QUOTED_IDENTIFIER ON) es opcional: el script ya lo fija al inicio,
que es lo que exigen sus indices filtrados. El nombre `EvenTechDB` es el de la
cadena de fabrica; si se usa otro, se indica el mismo nombre en los dos comandos y
despues en la pantalla de conexion de la aplicacion.

Alternativamente se puede restaurar el snapshot con datos de prueba
`db/EvenTechDB.bak`. El procedimiento completo, con las dos opciones y sus
advertencias, esta en **[db/README.md](db/README.md)**.

### 2. Compilar y ejecutar

```bat
dotnet build EvenTech.sln
```

o `_build.bat`, que deja el resultado en `_build_log.txt`. El ejecutable queda
en `EvenTech.UI\bin\Debug\net8.0-windows\EvenTech.UI.exe`.

### 3. Credencial inicial

| Usuario | Contrasena | Perfil |
|---|---|---|
| `admin` | `admin123` | Administrador (acceso total) |

Es la unica cuenta que siembra `schema.sql`. El snapshot `db/EvenTechDB.bak`
suma ademas tres cuentas de demostracion, todas con contrasena `demo123`, para
mostrar el menu segun el perfil:

| Usuario | Perfil |
|---|---|
| `dsosa` | Vendedor |
| `mojeda` | Supervisor (incluye a Vendedor) |
| `mgutierrez` | Gerencial (incluye a Supervisor) |

La version actual **no incluye el cambio de contrasena desde la aplicacion**
(previsto para la iteracion de gestion de usuarios, junto con la administracion de
la cuenta propia). Si hace falta reemplazar una contrasena, se actualiza
`Users.PasswordHash` con el SHA-256 hexadecimal en minusculas (64 caracteres) de
la contrasena nueva, que es el mismo formato que calcula la pantalla de acceso:

```bat
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -Q "UPDATE dbo.Users SET PasswordHash = '<sha256 hex>' WHERE Username = 'admin';"
```

**Cuenta bloqueada.** Tres contrasenas erroneas seguidas bloquean la cuenta y el
bloqueo no expira. Se levanta desde *Perfiles* (boton *Desbloquear*, requiere otro
usuario con el permiso `PERFILES_GESTION`, que solo tiene el Administrador). Si la
cuenta bloqueada es la unica de Administrador, el desbloqueo es por SQL:

```bat
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -Q "UPDATE dbo.Users SET Blocked = 0, FailedAttempts = 0 WHERE Username = 'admin';"
```

### Conexion a la base

La cadena de fabrica apunta a `localhost\SQLEXPRESS`, base `EvenTechDB`, con
seguridad integrada. Al arrancar, la aplicacion prueba la cadena guardada (o la
de fabrica si no hay ninguna): que el servidor responda, que la base exista y que
tenga el esquema completo. Si algo falla, abre la pantalla de configuracion antes
del login con el motivo, y ofrece en un combo las instancias detectadas en la
maquina (`sqlcmd -L`) mas las instalaciones tipicas —entre ellas
`localhost\SQLEXPRESS`— para elegir o tipear una; no las prueba por su cuenta.
Ahi se indican la instancia y el nombre de la base, se prueba la conexion y la
cadena resultante se guarda cifrada con DPAPI en `%APPDATA%\EvenTech\connection.cfg`.

Una base de una revision anterior (con `Users` pero sin alguna tabla o columna
que la version actual necesita) se rechaza con el detalle de lo que falta: se
completa corriendo `db\schema.sql` sobre esa base y se vuelve a probar.

## Arquitectura

```
EvenTech.sln
├── EvenTech.BE          entidades de negocio (BE_*)
├── EvenTech.DAL         acceso a datos, SQL parametrizado a mano (DAL_*)
├── EvenTech.BLL         reglas de negocio y validaciones (BLL_*)
├── EvenTech.Services    transversales: sesion, cifrado, idiomas, integridad
├── EvenTech.UI          WinForms: frmLogin, frmMain y UserControls por seccion
└── EvenTech.SmokeTest   validacion programatica end-to-end contra la base real
```

Dependencias entre capas, tal como las declaran los `ProjectReference`:
`UI -> BLL, BE, Services` · `BLL -> DAL, BE, Services` · `DAL -> BE, Services` ·
`Services -> BE` · `BE` no referencia a ninguna otra.

Unico paquete NuGet: `Microsoft.Data.SqlClient`, el proveedor de SQL Server que usa la
DAL (.NET 8 no trae ninguno en el framework). No contiene logica del sistema: todo el SQL
esta escrito a mano. DPAPI (`ProtectedData`) se toma del runtime de escritorio de .NET 8.

**Patrones aplicados:** Singleton (gestion de sesion: instancia unica con
constructor privado y acceso sincronizado), Composite (arbol de perfiles y
permisos), Observer (cambio de idioma en caliente) y Memento (versiones de una
reserva, con restauracion auditada). El hash de contrasenas y la comparacion en
tiempo constante viven en clases estaticas de servicio, sin estado propio.

## Reglas de negocio implementadas

| Regla | Enunciado |
|---|---|
| RN-01 | Vigencia: una cotizacion vale 15 dias corridos; una reserva PENDIENTE, 72 horas. Vencido el plazo la operacion no avanza de estado hasta que se renueve su vigencia. |
| RN-02 | Cancelacion: con 30 dias o mas de antelacion se reintegra el 100 %; con menos se retiene el 50 %. El sistema calcula, informa y asienta ambos importes. |
| RN-03 | Solo una reserva CONFIRMADA compromete el salon para la fecha del evento. |
| RN-04 | La suma de los pagos nunca supera el importe total, y una reserva cancelada no admite cobros. |
| RN-05 | Transiciones de estado: COTIZACION avanza a cualquier estado, PENDIENTE solo confirma o cancela, CONFIRMADA solo cancela y CANCELADA es terminal. |
| RN-06 | Al confirmar, el salon elegido tiene que poder alojar a la cantidad de invitados estimada. |
| RN-07 | Una reserva queda CONFIRMADA con el adelanto ya cobrado: el orden es guardar la operacion, cobrar y recien entonces confirmar. |

## Seguridad e integridad

- Contrasenas con hash SHA-256 aplicado en el cliente antes de salir de la interfaz.
- Email y telefono de los clientes cifrados con AES-256 reversible; la clave se
  protege con DPAPI de maquina en `%ProgramData%\EvenTech\crypto.key`.
- Cadena de conexion cifrada con DPAPI en el perfil del usuario.
- Digitos verificadores horizontal (por reserva) y vertical (por conjunto), que se
  verifican al arrancar, antes del login.
- Permisos por perfil con **denegar por defecto** y doble control: la seccion se
  oculta y la accion se vuelve a exigir al ejecutarse. Los permisos efectivos se
  resuelven al iniciar sesion y rigen durante toda ella: un cambio de perfil, de
  composicion o de asignacion se aplica desde el siguiente inicio de sesion del
  usuario afectado.
- Bitacora de toda operacion relevante y control de cambios campo por campo.

## Pruebas

`EvenTech.SmokeTest` recorre el sistema end-to-end contra la base configurada:
login y auditoria, alta y modificacion de reservas, control de cambios, arbol de
permisos, idiomas, integridad, memento, cifrado, configuracion de conexion, el
flujo completo del RF1, las siete reglas de negocio, cobros simultaneos y
restauracion de versiones. Son 42 casos numerados `[1]` a `[42]`, mas un bloque
`[limpieza]` y un `[cierre]` que vuelve a verificar la integridad de toda la base
al terminar.

```bat
dotnet run --project EvenTech.SmokeTest
```

Cada verificacion compara el valor obtenido con el esperado y marca la diferencia
con `<-- DIFIERE`; un caso que no puede correr por faltar un dato (catalogo vacio,
base de prueba que no se pudo crear) se declara **omitido**, no aprobado. El
resumen final informa verificaciones, fallos y casos ejecutados, y el proceso
devuelve un codigo de salida que sirve para automatizar:

| Codigo | Significado |
|---|---|
| `0` | todo aprobado |
| `1` | al menos una verificacion fallo o un caso termino por excepcion |
| `2` | sin fallos, pero con casos omitidos (cobertura incompleta) |

**La corrida deja rastro.** La suite elimina al final su usuario, sus perfiles,
su cliente y su idioma de prueba, pero las reservas que crea quedan CANCELADAS con
sus lineas, pagos, versiones e historial (la aplicacion no borra reservas), y se
suman los asientos de bitacora y de auditoria de acceso; el bloque `[limpieza]`
imprime el detalle. Por eso conviene correrla sobre una copia de la base o
restaurar `db\EvenTechDB.bak` despues, y nunca sobre la base desde la que se va a
generar un snapshot. Los casos `[1]` y `[21]` asumen la credencial inicial
`admin / admin123`.

## Estructura del repositorio

| Ruta | Contenido |
|---|---|
| `db/schema.sql` | Esquema idempotente con migraciones y datos base (UTF-8 con BOM) |
| `db/EvenTechDB.bak` | Snapshot completo con datos de demostracion |
| `db/README.md` | Procedimiento detallado de creacion y restauracion |
| `_build.bat` | Compilacion con log en `_build_log.txt` |
