# Base de datos — EvenTech

La app se conecta por defecto a `localhost\SQLEXPRESS`, base **`EvenTechDB`**
(Integrated Security). Ver `EvenTech.DAL/DAL_DB_Connection.cs`.

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
ver `EvenTech.Services/CryptoService.cs`). La clave se genera sola en el primer
uso y queda en `%ProgramData%\EvenTech\crypto.key`, protegida con DPAPI de la
**máquina**. Consecuencia: si se restaura un `.bak` en otra PC, los valores
`ENC:` de origen no se pueden descifrar ahí (se muestran tal cual); los valores
legados en texto plano se leen normal y todo se re-cifra con la clave local al
guardar el cliente.

## Opción B — Restaurar el snapshot completo (con datos)

`EvenTechDB.bak` es un backup full con los datos actuales (reservas, perfiles,
usuarios, bitácora, etc.). Restaurar:

```sql
RESTORE DATABASE EvenTechDB
FROM DISK = N'C:\ruta\al\repo\db\EvenTechDB.bak'
WITH MOVE 'EvenTechDB'     TO N'C:\...\MSSQL\DATA\EvenTechDB.mdf',
     MOVE 'EvenTechDB_log' TO N'C:\...\MSSQL\DATA\EvenTechDB_log.ldf',
     REPLACE;
```

(Ajustar las rutas `MOVE` a la carpeta DATA de tu instancia; ver
`SELECT SERVERPROPERTY('InstanceDefaultDataPath')`.) Requiere SQL Server de
igual o mayor versión que el de origen (SQL Server Express 2019 / MSSQL15).
