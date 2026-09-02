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
| Herramienta de linea de comandos | `sqlcmd` (incluida en SQL Server) |
| Para compilar | .NET 8 SDK |

Hardware sugerido: procesador Intel Core i5 o equivalente, 8 GB de RAM,
500 MB de disco y una resolucion de 1366x768 o superior.

## Puesta en marcha

### 1. Crear la base de datos

`db/schema.sql` es **idempotente**: crea las tablas que falten, aplica las
migraciones de columnas y siembra los datos base (idiomas y traducciones,
permisos y perfiles, salones, servicios, metodos de pago y el usuario inicial).
Se puede ejecutar varias veces sin perder informacion.

```bat
sqlcmd -S localhost\SQLEXPRESS -d master -E -C -Q "IF DB_ID('EvenTechDB') IS NULL CREATE DATABASE EvenTechDB;"
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -i db\schema.sql
```

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

Conviene cambiarla despues del primer ingreso.

### Conexion a la base

La cadena de fabrica apunta a `localhost\SQLEXPRESS`, base `EvenTechDB`, con
seguridad integrada. Si no logra conectarse, la aplicacion
prueba las instancias mas habituales —entre ellas `localhost\SQLEXPRESS`— y abre
la pantalla de configuracion antes del login, donde se indican la instancia y el
nombre de la base; la cadena resultante se guarda cifrada con DPAPI en
`%APPDATA%\EvenTech\connection.cfg`.

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

Unicos paquetes NuGet: `Microsoft.Data.SqlClient` y
`System.Security.Cryptography.ProtectedData`.

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
  oculta y la accion se vuelve a exigir al ejecutarse.
- Bitacora de toda operacion relevante y control de cambios campo por campo.

## Pruebas

`EvenTech.SmokeTest` recorre el sistema end-to-end contra la base real: login y
auditoria, alta y modificacion de reservas, control de cambios, arbol de permisos,
idiomas, integridad, memento, cifrado, configuracion de conexion, el flujo completo
del RF1 y las siete reglas de negocio. Son 33 casos numerados `[1]` a `[33]`.

```bat
dotnet run --project EvenTech.SmokeTest
```

Los casos imprimen el resultado obtenido y, en las verificaciones clave, el valor esperado junto a el.

## Estructura del repositorio

| Ruta | Contenido |
|---|---|
| `db/schema.sql` | Esquema idempotente con migraciones y datos base |
| `db/EvenTechDB.bak` | Snapshot completo con datos de prueba |
| `db/README.md` | Procedimiento detallado de creacion y restauracion |
| `_build.bat` | Compilacion con log en `_build_log.txt` |
