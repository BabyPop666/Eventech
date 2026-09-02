# Base de datos — EvenTech

La cadena de fábrica apunta a `localhost\SQLEXPRESS`, base **`EvenTechDB`**, con
seguridad integrada; se arma en
`EvenTech.Services/ConfiguracionConexion_704ILR.cs`, que también guarda la cadena
elegida por el usuario (cifrada con DPAPI). `EvenTech.DAL/DAL_DB_Connection_704ILR.cs`
es el consumidor de esa cadena y, si la conexión falla, prueba las instancias más
habituales —entre ellas `localhost\SQLEXPRESS`— para sugerir una en la pantalla de
configuración.

Hay dos formas de tener la base:

## Opción A — Recrear desde el script (recomendada, portable)

`schema.sql` es **idempotente**: crea las tablas que falten, aplica las migraciones
de columnas y de valores por defecto, y siembra los datos base (idiomas y
traducciones, permisos y perfiles, salones, servicios, métodos de pago y el usuario
`admin`), agregando solo lo que no esté. Se puede correr varias veces sin romper
datos.

Los comandos se corren **desde esta carpeta** (`db`), que es donde está el script:

```bat
sqlcmd -S localhost\SQLEXPRESS -d master -E -C -Q "IF DB_ID('EvenTechDB') IS NULL CREATE DATABASE EvenTechDB;"
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -i schema.sql
```

> Un `CREATE TABLE` dentro de un `IF OBJECT_ID(...) IS NULL` solo se ejecuta la
> primera vez: editarlo **no** cambia una base ya creada. Toda corrección de una
> columna o de un `DEFAULT` tiene que llevar además su `ALTER` idempotente en la
> zona de migraciones del script.

Usuario inicial: **admin / admin123** (perfil Administrador, acceso total).

## Datos cifrados (Email/Telefono de Clientes)

Email y Telefono de `Clientes` se guardan cifrados con AES-256 (prefijo `ENC:`,
ver `EvenTech.Services/CryptoService_704ILR.cs`). La clave se genera sola en el primer
uso y queda en `%ProgramData%\EvenTech\crypto.key`, protegida con DPAPI de la
**máquina**, y por eso **no viaja con el `.bak`**: un valor `ENC:` restaurado en
otra PC no se puede descifrar ahí y se mostraría tal cual.

Por eso **el snapshot que se entrega lleva los datos de contacto en texto plano**.
Al primer guardado de cada cliente, la aplicación los cifra con la clave local de
esa máquina: el cifrado se ejercita igual y los datos se leen desde el arranque.
La aplicación tampoco rechaza un valor que siga cifrado (no lo confunde con un
email mal escrito), de modo que la ficha del cliente nunca queda trabada.

## Opción B — Restaurar el snapshot completo (con datos)

`EvenTechDB.bak` es un backup full con los datos de demostración: 12 clientes,
24 reservas repartidas en los tres salones y los cuatro estados, sus servicios
contratados y pagos, tres perfiles (Administrador, Vendedor y Gerencial) y la
bitácora de esas operaciones.

Antes de restaurar, dos pasos que evitan los dos errores más comunes:

1. **Dejar el `.bak` donde el motor pueda leerlo.** Quien abre el archivo no es el
   usuario que corre el comando sino la cuenta de servicio de SQL Server, que
   normalmente no tiene acceso a las carpetas del perfil del usuario (Descargas,
   Documentos, Escritorio). Restaurando desde ahí el motor devuelve
   `Msg 3201 ... Operating system error 5 (Acceso denegado)`, que **no** significa
   que el backup esté dañado. La salida más simple es copiar `EvenTechDB.bak` a la
   carpeta de backups de la instancia —`SELECT SERVERPROPERTY('InstanceDefaultBackupPath')`—
   y restaurar desde ahí; la alternativa es dar permiso de lectura sobre la carpeta
   del repositorio a `NT Service\MSSQL$SQLEXPRESS`.
2. **Confirmar los nombres lógicos** antes de armar los `MOVE`:

```sql
RESTORE FILELISTONLY FROM DISK = N'C:\ruta\al\.bak\EvenTechDB.bak';
-- Devuelve los nombres lógicos a usar: EvenTechDB y EvenTechDB_log.
```

Restaurar:

```sql
RESTORE DATABASE EvenTechDB
FROM DISK = N'C:\ruta\al\repo\db\EvenTechDB.bak'
WITH MOVE 'EvenTechDB'     TO N'C:\...\MSSQL\DATA\EvenTechDB.mdf',
     MOVE 'EvenTechDB_log' TO N'C:\...\MSSQL\DATA\EvenTechDB_log.ldf',
     REPLACE;
```

(Ajustar las rutas `MOVE` a la carpeta DATA de la instancia local; ver
`SELECT SERVERPROPERTY('InstanceDefaultDataPath')`.) Requiere SQL Server de
igual o mayor versión que el de origen (SQL Server Express 2019 / MSSQL15).
