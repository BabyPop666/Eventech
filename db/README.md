# Base de datos — EvenTech

La app se conecta por defecto a `localhost\SQLEXPRESS`, base **`EvenTechDB`**
(Integrated Security). Ver `EvenTech.DAL/DAL_DB_Connection_704ILR.cs`.

Hay dos formas de tener la base:

## Opción A — Recrear desde el script (recomendada, portable)

`schema.sql` es **idempotente**: crea las tablas si no existen, los seeds base
(idiomas/traducciones, permisos/perfiles, usuario `admin`) y agrega solo las
traducciones que falten. Se puede correr varias veces sin romper datos.

```bat
sqlcmd -S localhost\SQLEXPRESS -d master -E -C -Q "IF DB_ID('EvenTechDB') IS NULL CREATE DATABASE EvenTechDB;"
sqlcmd -S localhost\SQLEXPRESS -d EvenTechDB -E -C -i schema.sql
```

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
bitácora de esas operaciones. Restaurar:

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
