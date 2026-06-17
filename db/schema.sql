-- EvenTech - Schema inicial
-- Solamente Users + LoginAuditLog (login + auditoria)

IF DB_ID('EvenTechDB') IS NULL
    CREATE DATABASE [EvenTechDB];
GO

USE [EvenTechDB];
GO

-- Tabla de usuarios.
-- PasswordHash: SHA-256 hex (64 chars) generado en cliente. La password en claro
-- nunca viaja a la DB.
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username      NVARCHAR(50)      NOT NULL,
        PasswordHash  NVARCHAR(64)      NOT NULL,
        CreatedAt     DATETIME          NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),
        CONSTRAINT UQ_Users_Username UNIQUE (Username)
    );
END
GO

-- Bitacora de logins / logouts. Se registra cada intento (exitoso o fallido).
IF OBJECT_ID('dbo.LoginAuditLog','U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginAuditLog (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoginAuditLog PRIMARY KEY,
        Username     NVARCHAR(50)      NOT NULL,
        [Action]     NVARCHAR(20)      NOT NULL,  -- LOGIN_OK, LOGIN_FAIL, LOGOUT
        [Timestamp]  DATETIME          NOT NULL CONSTRAINT DF_LoginAuditLog_Timestamp DEFAULT GETDATE(),
        MachineName  NVARCHAR(100)     NULL,
        Details      NVARCHAR(500)     NULL
    );

    CREATE INDEX IX_LoginAuditLog_Username ON dbo.LoginAuditLog(Username);
    CREATE INDEX IX_LoginAuditLog_Timestamp ON dbo.LoginAuditLog([Timestamp] DESC);
END
GO

-- Seed: usuario admin con password 'admin123'.
-- Hash SHA-256 de 'admin123' = 240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash)
    VALUES ('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9');
END
GO

-- ===========================================================================
-- Negocio: Salones + Reservas
-- ===========================================================================

-- Catalogo de salones donde se realizan los eventos.
IF OBJECT_ID('dbo.Salones','U') IS NULL
BEGIN
    CREATE TABLE dbo.Salones (
        Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Salones PRIMARY KEY,
        Nombre     NVARCHAR(100)     NOT NULL,
        Capacidad  INT               NOT NULL CONSTRAINT DF_Salones_Capacidad DEFAULT 0,
        CONSTRAINT UQ_Salones_Nombre UNIQUE (Nombre)
    );
END
GO

-- Reserva de un evento sobre un salon. Entidad central del dominio.
-- Dvh: digito verificador horizontal (lo completa el modulo de integridad; por
-- eso admite NULL hasta que ese modulo este implementado).
IF OBJECT_ID('dbo.Reservas','U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservas (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservas PRIMARY KEY,
        ClienteNombre NVARCHAR(150)     NOT NULL,
        SalonId       INT               NOT NULL,
        FechaEvento   DATETIME          NOT NULL,
        Estado        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Reservas_Estado DEFAULT 'PENDIENTE',
        Monto         DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Reservas_Monto DEFAULT 0,
        CreatedAt     DATETIME          NOT NULL CONSTRAINT DF_Reservas_CreatedAt DEFAULT GETDATE(),
        Dvh           NVARCHAR(64)      NULL,
        CONSTRAINT FK_Reservas_Salones FOREIGN KEY (SalonId) REFERENCES dbo.Salones(Id)
    );

    CREATE INDEX IX_Reservas_FechaEvento ON dbo.Reservas(FechaEvento DESC);
    CREATE INDEX IX_Reservas_SalonId ON dbo.Reservas(SalonId);
END
GO

-- Seed de salones de ejemplo para poder operar.
IF NOT EXISTS (SELECT 1 FROM dbo.Salones)
BEGIN
    INSERT INTO dbo.Salones (Nombre, Capacidad) VALUES
        (N'Salon Principal', 250),
        (N'Salon Jardin',    120),
        (N'Terraza',          80);
END
GO

-- ===========================================================================
-- Auditoria: Bitacora general (T06) + Control de Cambios (T06b)
-- ===========================================================================

-- Bitacora general del sistema: cualquier operacion de negocio, no solo login.
-- Criticidad: 1=Info, 2=Advertencia, 3=Error.
IF OBJECT_ID('dbo.Bitacora','U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
        Fecha       DATETIME          NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT GETDATE(),
        Usuario     NVARCHAR(50)      NOT NULL,
        Modulo      NVARCHAR(50)      NULL,
        Accion      NVARCHAR(100)     NULL,
        Criticidad  TINYINT           NOT NULL CONSTRAINT DF_Bitacora_Criticidad DEFAULT 1,
        Detalle     NVARCHAR(1000)    NULL
    );

    CREATE INDEX IX_Bitacora_Fecha ON dbo.Bitacora(Fecha DESC);
    CREATE INDEX IX_Bitacora_Usuario ON dbo.Bitacora(Usuario);
    CREATE INDEX IX_Bitacora_Modulo ON dbo.Bitacora(Modulo);
END
GO

-- Control de cambios fino: una fila por campo modificado de una entidad.
-- Permite reconstruir el estado anterior campo por campo (quien/cuando/que).
IF OBJECT_ID('dbo.HistorialCambios','U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialCambios (
        Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialCambios PRIMARY KEY,
        Entidad        NVARCHAR(50)      NOT NULL,
        EntidadId      INT               NOT NULL,
        NombreCampo    NVARCHAR(100)     NOT NULL,
        ValorAnterior  NVARCHAR(500)     NULL,
        ValorNuevo     NVARCHAR(500)     NULL,
        Usuario        NVARCHAR(50)      NOT NULL,
        Fecha          DATETIME          NOT NULL CONSTRAINT DF_HistorialCambios_Fecha DEFAULT GETDATE()
    );

    CREATE INDEX IX_HistorialCambios_Entidad ON dbo.HistorialCambios(Entidad, EntidadId);
END
GO

-- ===========================================================================
-- Perfiles de usuario (T04 - patron Composite)
-- ===========================================================================

-- Arbol de permisos. Una sola tabla con relacion REFLEXIVA (PermisoPadreId):
--   EsGrupo = 1  -> nodo compuesto (grupo que agrupa otros permisos/grupos)
--   EsGrupo = 0  -> hoja (permiso concreto, identificado por Clave)
IF OBJECT_ID('dbo.Permisos','U') IS NULL
BEGIN
    CREATE TABLE dbo.Permisos (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permisos PRIMARY KEY,
        Nombre          NVARCHAR(100)     NOT NULL,
        Descripcion     NVARCHAR(250)     NULL,
        EsGrupo         BIT               NOT NULL CONSTRAINT DF_Permisos_EsGrupo DEFAULT 0,
        Clave           NVARCHAR(50)      NULL,
        PermisoPadreId  INT               NULL,
        CONSTRAINT FK_Permisos_Padre FOREIGN KEY (PermisoPadreId) REFERENCES dbo.Permisos(Id)
    );
END
GO

IF OBJECT_ID('dbo.Perfiles','U') IS NULL
BEGIN
    CREATE TABLE dbo.Perfiles (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Perfiles PRIMARY KEY,
        Nombre      NVARCHAR(80)      NOT NULL,
        Descripcion NVARCHAR(250)     NULL,
        CONSTRAINT UQ_Perfiles_Nombre UNIQUE (Nombre)
    );
END
GO

-- Asignacion N:M entre perfil y los componentes (grupos u hojas) que tiene.
IF OBJECT_ID('dbo.PerfilPermiso','U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilPermiso (
        PerfilId   INT NOT NULL,
        PermisoId  INT NOT NULL,
        CONSTRAINT PK_PerfilPermiso PRIMARY KEY (PerfilId, PermisoId),
        CONSTRAINT FK_PerfilPermiso_Perfil  FOREIGN KEY (PerfilId)  REFERENCES dbo.Perfiles(Id),
        CONSTRAINT FK_PerfilPermiso_Permiso FOREIGN KEY (PermisoId) REFERENCES dbo.Permisos(Id)
    );
END
GO

-- Relacion del usuario con su perfil.
IF COL_LENGTH('dbo.Users','PerfilId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD PerfilId INT NULL
        CONSTRAINT FK_Users_Perfil FOREIGN KEY (PerfilId) REFERENCES dbo.Perfiles(Id);
END
GO

-- Seed del arbol de permisos (grupos anidados + hojas).
IF NOT EXISTS (SELECT 1 FROM dbo.Permisos)
BEGIN
    INSERT INTO dbo.Permisos (Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId)
        VALUES (N'Administracion', N'Grupo raiz de administracion', 1, NULL, NULL);
    DECLARE @raiz INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Permisos (Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId)
        VALUES (N'Gestion de Reservas', N'Permisos sobre reservas', 1, NULL, @raiz);
    DECLARE @gReservas INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Permisos (Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId) VALUES
        (N'Crear Reserva',          NULL, 0, N'RESERVA_CREAR',      @gReservas),
        (N'Editar Reserva',         NULL, 0, N'RESERVA_EDITAR',     @gReservas),
        (N'Ver Historial Reserva',  NULL, 0, N'RESERVA_HISTORIAL',  @gReservas);

    INSERT INTO dbo.Permisos (Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId)
        VALUES (N'Auditoria', N'Permisos de auditoria', 1, NULL, @raiz);
    DECLARE @gAudit INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Permisos (Nombre, Descripcion, EsGrupo, Clave, PermisoPadreId) VALUES
        (N'Ver Bitacora',           NULL, 0, N'BITACORA_VER',       @gAudit),
        (N'Ver Auditoria Login',    NULL, 0, N'AUDIT_LOGIN_VER',    @gAudit);
END
GO

-- Seed del perfil Administrador con todos los permisos + vincular al usuario admin.
IF NOT EXISTS (SELECT 1 FROM dbo.Perfiles)
BEGIN
    INSERT INTO dbo.Perfiles (Nombre, Descripcion) VALUES (N'Administrador', N'Acceso total al sistema');
    DECLARE @pAdmin INT = SCOPE_IDENTITY();

    INSERT INTO dbo.PerfilPermiso (PerfilId, PermisoId)
        SELECT @pAdmin, Id FROM dbo.Permisos;

    UPDATE dbo.Users SET PerfilId = @pAdmin WHERE Username = 'admin' AND PerfilId IS NULL;
END
GO

-- ===========================================================================
-- Multiples idiomas (T05 - patron Observer). Modelo propio en BD, sin .resx.
-- ===========================================================================

IF OBJECT_ID('dbo.Idiomas','U') IS NULL
BEGIN
    CREATE TABLE dbo.Idiomas (
        Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Idiomas PRIMARY KEY,
        Codigo  NVARCHAR(5)       NOT NULL,
        Nombre  NVARCHAR(50)      NOT NULL,
        CONSTRAINT UQ_Idiomas_Codigo UNIQUE (Codigo)
    );
END
GO

IF OBJECT_ID('dbo.Traducciones','U') IS NULL
BEGIN
    CREATE TABLE dbo.Traducciones (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Traducciones PRIMARY KEY,
        IdiomaId  INT               NOT NULL,
        Clave     NVARCHAR(60)      NOT NULL,
        Texto     NVARCHAR(250)     NOT NULL,
        CONSTRAINT FK_Traducciones_Idioma FOREIGN KEY (IdiomaId) REFERENCES dbo.Idiomas(Id),
        CONSTRAINT UQ_Traducciones UNIQUE (IdiomaId, Clave)
    );
END
GO

-- Seed de idiomas + leyendas (ES por defecto, EN).
IF NOT EXISTS (SELECT 1 FROM dbo.Idiomas)
BEGIN
    INSERT INTO dbo.Idiomas (Codigo, Nombre) VALUES (N'ES', N'Espanol');
    DECLARE @es INT = SCOPE_IDENTITY();
    INSERT INTO dbo.Idiomas (Codigo, Nombre) VALUES (N'EN', N'English');
    DECLARE @en INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Traducciones (IdiomaId, Clave, Texto) VALUES
        (@es, N'MENU_INICIO',    N'Inicio'),
        (@es, N'MENU_RESERVAS',  N'Reservas'),
        (@es, N'MENU_PERFILES',  N'Perfiles'),
        (@es, N'MENU_IDIOMAS',   N'Idiomas'),
        (@es, N'MENU_BITACORA',  N'Bitacora'),
        (@es, N'MENU_AUDITORIA', N'Auditoria login'),
        (@es, N'MENU_SALIR',     N'Cerrar sesion'),
        (@es, N'MAIN_WELCOME',   N'Bienvenido a EvenTech'),
        (@es, N'MAIN_USER',      N'Usuario'),
        (@es, N'LOGIN_USER',     N'Usuario'),
        (@es, N'LOGIN_PASS',     N'Contrasena'),
        (@es, N'LOGIN_ENTER',    N'Ingresar'),
        (@es, N'LOGIN_CREATE',   N'No tenes cuenta? Crear');

    INSERT INTO dbo.Traducciones (IdiomaId, Clave, Texto) VALUES
        (@en, N'MENU_INICIO',    N'Home'),
        (@en, N'MENU_RESERVAS',  N'Reservations'),
        (@en, N'MENU_PERFILES',  N'Profiles'),
        (@en, N'MENU_IDIOMAS',   N'Languages'),
        (@en, N'MENU_BITACORA',  N'Audit log'),
        (@en, N'MENU_AUDITORIA', N'Login audit'),
        (@en, N'MENU_SALIR',     N'Log out'),
        (@en, N'MAIN_WELCOME',   N'Welcome to EvenTech'),
        (@en, N'MAIN_USER',      N'User'),
        (@en, N'LOGIN_USER',     N'Username'),
        (@en, N'LOGIN_PASS',     N'Password'),
        (@en, N'LOGIN_ENTER',    N'Sign in'),
        (@en, N'LOGIN_CREATE',   N'No account? Create one');
END
GO

-- Portugues. Bloque idempotente: se agrega solo si todavia no existe, asi
-- funciona tanto en base nueva como en una ya creada con ES/EN.
IF NOT EXISTS (SELECT 1 FROM dbo.Idiomas WHERE Codigo = 'PT')
BEGIN
    INSERT INTO dbo.Idiomas (Codigo, Nombre) VALUES (N'PT', N'Portugues');
    DECLARE @pt INT = SCOPE_IDENTITY();

    INSERT INTO dbo.Traducciones (IdiomaId, Clave, Texto) VALUES
        (@pt, N'MENU_INICIO',    N'Inicio'),
        (@pt, N'MENU_RESERVAS',  N'Reservas'),
        (@pt, N'MENU_PERFILES',  N'Perfis'),
        (@pt, N'MENU_IDIOMAS',   N'Idiomas'),
        (@pt, N'MENU_BITACORA',  N'Registro'),
        (@pt, N'MENU_AUDITORIA', N'Auditoria de login'),
        (@pt, N'MENU_SALIR',     N'Sair'),
        (@pt, N'MAIN_WELCOME',   N'Bem-vindo ao EvenTech'),
        (@pt, N'MAIN_USER',      N'Usuario'),
        (@pt, N'LOGIN_USER',     N'Usuario'),
        (@pt, N'LOGIN_PASS',     N'Senha'),
        (@pt, N'LOGIN_ENTER',    N'Entrar'),
        (@pt, N'LOGIN_CREATE',   N'Nao tem conta? Criar');
END
GO

-- ===========================================================================
-- Digitos verificadores (T07/T08). El DV horizontal vive en Reservas.Dvh;
-- el DV vertical (uno por tabla protegida) en esta tabla.
-- ===========================================================================
IF OBJECT_ID('dbo.DVVertical','U') IS NULL
BEGIN
    CREATE TABLE dbo.DVVertical (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DVVertical PRIMARY KEY,
        Tabla       NVARCHAR(50)      NOT NULL,
        Dvv         NVARCHAR(64)      NOT NULL,
        CalculadoEn DATETIME          NOT NULL CONSTRAINT DF_DVVertical_CalculadoEn DEFAULT GETDATE(),
        CONSTRAINT UQ_DVVertical_Tabla UNIQUE (Tabla)
    );
END
GO

-- ===========================================================================
-- Traducciones de TODAS las vistas (ES/EN/PT). Idempotente: inserta solo las
-- claves que falten para cada idioma, sin pisar lo existente.
-- ===========================================================================
;WITH Txt(Codigo, Clave, Texto) AS (
    SELECT * FROM (VALUES
        -- Inicio
        (N'ES', N'MAIN_SESSION',  N'Sesion iniciada por:'), (N'EN', N'MAIN_SESSION',  N'Signed in as:'),               (N'PT', N'MAIN_SESSION',  N'Sessao iniciada por:'),
        (N'ES', N'MAIN_SUBTITLE', N'Usa el menu de la izquierda para gestionar el sistema.'), (N'EN', N'MAIN_SUBTITLE', N'Use the left menu to manage the system.'), (N'PT', N'MAIN_SUBTITLE', N'Use o menu a esquerda para gerenciar o sistema.'),
        (N'ES', N'MAIN_HELLO',    N'Bienvenido'), (N'EN', N'MAIN_HELLO',    N'Welcome'), (N'PT', N'MAIN_HELLO',    N'Bem-vindo'),
        (N'ES', N'MAIN_SIN_ROL_TIT', N'Acceso restringido'), (N'EN', N'MAIN_SIN_ROL_TIT', N'Access restricted'), (N'PT', N'MAIN_SIN_ROL_TIT', N'Acesso restrito'),
        (N'ES', N'MAIN_SIN_ROL', N'Tu cuenta todavia no tiene un perfil asignado. Contactate con un administrador para que te asigne uno.'), (N'EN', N'MAIN_SIN_ROL', N'Your account does not have a profile assigned yet. Contact an administrator to get one.'), (N'PT', N'MAIN_SIN_ROL', N'Sua conta ainda nao tem um perfil atribuido. Entre em contato com um administrador para receber um.'),
        -- Columnas compartidas
        (N'ES', N'COL_ID',         N'Id'),        (N'EN', N'COL_ID',         N'Id'),        (N'PT', N'COL_ID',         N'Id'),
        (N'ES', N'COL_CLIENTE',    N'Cliente'),   (N'EN', N'COL_CLIENTE',    N'Client'),    (N'PT', N'COL_CLIENTE',    N'Cliente'),
        (N'ES', N'COL_SALON',      N'Salon'),     (N'EN', N'COL_SALON',      N'Hall'),      (N'PT', N'COL_SALON',      N'Salao'),
        (N'ES', N'COL_FECHA',      N'Fecha'),     (N'EN', N'COL_FECHA',      N'Date'),      (N'PT', N'COL_FECHA',      N'Data'),
        (N'ES', N'COL_ESTADO',     N'Estado'),    (N'EN', N'COL_ESTADO',     N'Status'),    (N'PT', N'COL_ESTADO',     N'Estado'),
        (N'ES', N'COL_MONTO',      N'Monto'),     (N'EN', N'COL_MONTO',      N'Amount'),    (N'PT', N'COL_MONTO',      N'Valor'),
        (N'ES', N'COL_USUARIO',    N'Usuario'),   (N'EN', N'COL_USUARIO',    N'User'),      (N'PT', N'COL_USUARIO',    N'Usuario'),
        (N'ES', N'COL_MODULO',     N'Modulo'),    (N'EN', N'COL_MODULO',     N'Module'),    (N'PT', N'COL_MODULO',     N'Modulo'),
        (N'ES', N'COL_ACCION',     N'Accion'),    (N'EN', N'COL_ACCION',     N'Action'),    (N'PT', N'COL_ACCION',     N'Acao'),
        (N'ES', N'COL_CRITICIDAD', N'Criticidad'),(N'EN', N'COL_CRITICIDAD', N'Severity'),  (N'PT', N'COL_CRITICIDAD', N'Criticidade'),
        (N'ES', N'COL_DETALLE',    N'Detalle'),   (N'EN', N'COL_DETALLE',    N'Detail'),    (N'PT', N'COL_DETALLE',    N'Detalhe'),
        (N'ES', N'COL_MAQUINA',    N'Maquina'),   (N'EN', N'COL_MAQUINA',    N'Machine'),   (N'PT', N'COL_MAQUINA',    N'Maquina'),
        (N'ES', N'COL_CAMPO',      N'Campo'),     (N'EN', N'COL_CAMPO',      N'Field'),     (N'PT', N'COL_CAMPO',      N'Campo'),
        (N'ES', N'COL_ANTERIOR',   N'Anterior'),  (N'EN', N'COL_ANTERIOR',   N'Previous'),  (N'PT', N'COL_ANTERIOR',   N'Anterior'),
        (N'ES', N'COL_NUEVO',      N'Nuevo'),     (N'EN', N'COL_NUEVO',      N'New'),       (N'PT', N'COL_NUEVO',      N'Novo'),
        (N'ES', N'COL_CLAVE',      N'Clave'),     (N'EN', N'COL_CLAVE',      N'Key'),       (N'PT', N'COL_CLAVE',      N'Chave'),
        (N'ES', N'COL_TEXTO',      N'Texto'),     (N'EN', N'COL_TEXTO',      N'Text'),      (N'PT', N'COL_TEXTO',      N'Texto'),
        (N'ES', N'OPT_TODOS',      N'(Todos)'),   (N'EN', N'OPT_TODOS',      N'(All)'),     (N'PT', N'OPT_TODOS',      N'(Todos)'),
        (N'ES', N'OPT_TODAS',      N'(Todas)'),   (N'EN', N'OPT_TODAS',      N'(All)'),     (N'PT', N'OPT_TODAS',      N'(Todas)'),
        -- Botones compartidos
        (N'ES', N'BTN_NUEVA',      N'Nueva'),     (N'EN', N'BTN_NUEVA',      N'New'),       (N'PT', N'BTN_NUEVA',      N'Nova'),
        (N'ES', N'BTN_GUARDAR',    N'Guardar'),   (N'EN', N'BTN_GUARDAR',    N'Save'),      (N'PT', N'BTN_GUARDAR',    N'Salvar'),
        (N'ES', N'BTN_BUSCAR',     N'Buscar'),    (N'EN', N'BTN_BUSCAR',     N'Search'),    (N'PT', N'BTN_BUSCAR',     N'Buscar'),
        (N'ES', N'BTN_LIMPIAR',    N'Limpiar'),   (N'EN', N'BTN_LIMPIAR',    N'Clear'),     (N'PT', N'BTN_LIMPIAR',    N'Limpar'),
        (N'ES', N'BTN_REFRESCAR',  N'Refrescar'), (N'EN', N'BTN_REFRESCAR',  N'Refresh'),   (N'PT', N'BTN_REFRESCAR',  N'Atualizar'),
        -- Reservas
        (N'ES', N'RES_TITULO',     N'Gestion de Reservas'),       (N'EN', N'RES_TITULO',     N'Reservations Management'),  (N'PT', N'RES_TITULO',     N'Gestao de Reservas'),
        (N'ES', N'RES_HISTORIAL',  N'Ver historial de cambios'),  (N'EN', N'RES_HISTORIAL',  N'View change history'),      (N'PT', N'RES_HISTORIAL',  N'Ver historico de alteracoes'),
        (N'ES', N'RES_FORM_NUEVA', N'Nueva reserva'),             (N'EN', N'RES_FORM_NUEVA', N'New reservation'),          (N'PT', N'RES_FORM_NUEVA', N'Nova reserva'),
        (N'ES', N'RES_FORM_EDITAR',N'Editar reserva'),            (N'EN', N'RES_FORM_EDITAR',N'Edit reservation'),         (N'PT', N'RES_FORM_EDITAR',N'Editar reserva'),
        (N'ES', N'RES_LBL_FECHA',  N'Fecha del evento'),          (N'EN', N'RES_LBL_FECHA',  N'Event date'),               (N'PT', N'RES_LBL_FECHA',  N'Data do evento'),
        (N'ES', N'RES_COUNT',      N'reservas'),                  (N'EN', N'RES_COUNT',      N'reservations'),             (N'PT', N'RES_COUNT',      N'reservas'),
        -- Bitacora
        (N'ES', N'BIT_TITULO',     N'Bitacora del Sistema'),      (N'EN', N'BIT_TITULO',     N'System Audit Log'),         (N'PT', N'BIT_TITULO',     N'Registro do Sistema'),
        (N'ES', N'BIT_DESDE',      N'Desde'),                     (N'EN', N'BIT_DESDE',      N'From'),                     (N'PT', N'BIT_DESDE',      N'De'),
        (N'ES', N'BIT_HASTA',      N'Hasta'),                     (N'EN', N'BIT_HASTA',      N'To'),                       (N'PT', N'BIT_HASTA',      N'Ate'),
        (N'ES', N'BIT_COUNT',      N'registros'),                 (N'EN', N'BIT_COUNT',      N'records'),                  (N'PT', N'BIT_COUNT',      N'registros'),
        -- Perfiles
        (N'ES', N'PERF_TITULO',    N'Gestion de Perfiles'),       (N'EN', N'PERF_TITULO',    N'Profiles Management'),      (N'PT', N'PERF_TITULO',    N'Gestao de Perfis'),
        (N'ES', N'PERF_PERFIL',    N'Perfil:'),                   (N'EN', N'PERF_PERFIL',    N'Profile:'),                 (N'PT', N'PERF_PERFIL',    N'Perfil:'),
        (N'ES', N'PERF_HINT',      N'Tilde los permisos del perfil. Marcar un grupo incluye a sus hijos.'), (N'EN', N'PERF_HINT', N'Check the profile permissions. Checking a group includes its children.'), (N'PT', N'PERF_HINT', N'Marque as permissoes do perfil. Marcar um grupo inclui seus filhos.'),
        (N'ES', N'PERF_GUARDAR',   N'Guardar permisos'),          (N'EN', N'PERF_GUARDAR',   N'Save permissions'),         (N'PT', N'PERF_GUARDAR',   N'Salvar permissoes'),
        (N'ES', N'MSG_PERF_SELECCIONE', N'Seleccione un perfil.'),(N'EN', N'MSG_PERF_SELECCIONE', N'Select a profile.'),  (N'PT', N'MSG_PERF_SELECCIONE', N'Selecione um perfil.'),
        (N'ES', N'MSG_PERF_OK',    N'Permisos guardados.'),       (N'EN', N'MSG_PERF_OK',    N'Permissions saved.'),       (N'PT', N'MSG_PERF_OK',    N'Permissoes salvas.'),
        -- Idiomas
        (N'ES', N'IDI_TITULO',     N'Gestion de Idiomas'),        (N'EN', N'IDI_TITULO',     N'Languages Management'),     (N'PT', N'IDI_TITULO',     N'Gestao de Idiomas'),
        (N'ES', N'IDI_NUEVO',      N'Nuevo idioma'),              (N'EN', N'IDI_NUEVO',      N'New language'),             (N'PT', N'IDI_NUEVO',      N'Novo idioma'),
        (N'ES', N'IDI_CODIGO',     N'Codigo (ej. PT)'),           (N'EN', N'IDI_CODIGO',     N'Code (e.g. PT)'),           (N'PT', N'IDI_CODIGO',     N'Codigo (ex. PT)'),
        (N'ES', N'IDI_NOMBRE',     N'Nombre'),                    (N'EN', N'IDI_NOMBRE',     N'Name'),                     (N'PT', N'IDI_NOMBRE',     N'Nome'),
        (N'ES', N'IDI_CREAR',      N'Crear idioma'),              (N'EN', N'IDI_CREAR',      N'Create language'),          (N'PT', N'IDI_CREAR',      N'Criar idioma'),
        (N'ES', N'IDI_IDIOMA',     N'Idioma:'),                   (N'EN', N'IDI_IDIOMA',     N'Language:'),                (N'PT', N'IDI_IDIOMA',     N'Idioma:'),
        (N'ES', N'IDI_GUARDAR',    N'Guardar traducciones'),      (N'EN', N'IDI_GUARDAR',    N'Save translations'),        (N'PT', N'IDI_GUARDAR',    N'Salvar traducoes'),
        (N'ES', N'MSG_IDI_CREADO', N'Idioma creado. Edite los textos y guarde.'), (N'EN', N'MSG_IDI_CREADO', N'Language created. Edit the texts and save.'), (N'PT', N'MSG_IDI_CREADO', N'Idioma criado. Edite os textos e salve.'),
        (N'ES', N'MSG_IDI_SELECCIONE', N'Seleccione un idioma.'), (N'EN', N'MSG_IDI_SELECCIONE', N'Select a language.'),   (N'PT', N'MSG_IDI_SELECCIONE', N'Selecione um idioma.'),
        (N'ES', N'MSG_IDI_GUARDADO', N'Traducciones guardadas.'), (N'EN', N'MSG_IDI_GUARDADO', N'Translations saved.'),    (N'PT', N'MSG_IDI_GUARDADO', N'Traducoes salvas.'),
        (N'ES', N'MSG_IDI_COD_INV', N'Codigo invalido (1 a 5 caracteres).'), (N'EN', N'MSG_IDI_COD_INV', N'Invalid code (1 to 5 chars).'), (N'PT', N'MSG_IDI_COD_INV', N'Codigo invalido (1 a 5 caracteres).'),
        (N'ES', N'MSG_IDI_NOM_INV', N'Ingrese el nombre del idioma.'), (N'EN', N'MSG_IDI_NOM_INV', N'Enter the language name.'), (N'PT', N'MSG_IDI_NOM_INV', N'Informe o nome do idioma.'),
        (N'ES', N'MSG_IDI_DUP',    N'Ya existe un idioma con ese codigo.'), (N'EN', N'MSG_IDI_DUP', N'A language with that code already exists.'), (N'PT', N'MSG_IDI_DUP', N'Ja existe um idioma com esse codigo.'),
        (N'ES', N'MSG_IDI_ERROR',  N'No se pudo crear el idioma.'),(N'EN', N'MSG_IDI_ERROR',  N'Could not create the language.'), (N'PT', N'MSG_IDI_ERROR', N'Nao foi possivel criar o idioma.'),
        -- Auditoria
        (N'ES', N'AUD_TITULO',     N'Registro de Auditoria'),     (N'EN', N'AUD_TITULO',     N'Login Audit Log'),          (N'PT', N'AUD_TITULO',     N'Registro de Auditoria'),
        (N'ES', N'AUD_COUNT',      N'registros'),                 (N'EN', N'AUD_COUNT',      N'records'),                  (N'PT', N'AUD_COUNT',      N'registros'),
        -- Historial de reserva
        (N'ES', N'HIST_TITULO',    N'Historial de la reserva'),   (N'EN', N'HIST_TITULO',    N'Reservation history'),      (N'PT', N'HIST_TITULO',    N'Historico da reserva'),
        -- Alerta de integridad
        (N'ES', N'ALERT_TITULO',   N'Se detectaron problemas de integridad'), (N'EN', N'ALERT_TITULO', N'Integrity problems detected'), (N'PT', N'ALERT_TITULO', N'Problemas de integridade detectados'),
        (N'ES', N'ALERT_HINT',     N'La verificacion de digitos verificadores encontro datos alterados por fuera del sistema. Avise al administrador antes de operar.'), (N'EN', N'ALERT_HINT', N'The check-digit verification found data altered outside the system. Notify the administrator before operating.'), (N'PT', N'ALERT_HINT', N'A verificacao de digitos verificadores encontrou dados alterados fora do sistema. Avise o administrador antes de operar.'),
        (N'ES', N'ALERT_BTN',      N'Revisado, continuar'),       (N'EN', N'ALERT_BTN',      N'Reviewed, continue'),       (N'PT', N'ALERT_BTN',      N'Revisado, continuar'),
        -- Crear cuenta
        (N'ES', N'CC_TITULO',      N'Crear cuenta'),              (N'EN', N'CC_TITULO',      N'Create account'),           (N'PT', N'CC_TITULO',      N'Criar conta'),
        (N'ES', N'CC_USER',        N'Usuario'),                   (N'EN', N'CC_USER',        N'Username'),                 (N'PT', N'CC_USER',        N'Usuario'),
        (N'ES', N'CC_PASS',        N'Contrasena'),                (N'EN', N'CC_PASS',        N'Password'),                 (N'PT', N'CC_PASS',        N'Senha'),
        (N'ES', N'CC_PASS2',       N'Repetir contrasena'),        (N'EN', N'CC_PASS2',       N'Repeat password'),          (N'PT', N'CC_PASS2',       N'Repetir senha'),
        (N'ES', N'CC_CREAR',       N'Crear'),                     (N'EN', N'CC_CREAR',       N'Create'),                   (N'PT', N'CC_CREAR',       N'Criar'),
        -- Mensajes de reservas
        (N'ES', N'MSG_MONTO_INVALIDO', N'El monto no es un numero valido.'), (N'EN', N'MSG_MONTO_INVALIDO', N'The amount is not a valid number.'), (N'PT', N'MSG_MONTO_INVALIDO', N'O valor nao e um numero valido.'),
        (N'ES', N'MSG_RES_CLIENTE', N'Ingrese el nombre del cliente.'), (N'EN', N'MSG_RES_CLIENTE', N'Enter the client name.'), (N'PT', N'MSG_RES_CLIENTE', N'Informe o nome do cliente.'),
        (N'ES', N'MSG_RES_SALON',  N'Seleccione un salon valido.'),(N'EN', N'MSG_RES_SALON',  N'Select a valid hall.'),     (N'PT', N'MSG_RES_SALON',  N'Selecione um salao valido.'),
        (N'ES', N'MSG_RES_FECHA',  N'La fecha del evento no puede ser anterior a hoy.'), (N'EN', N'MSG_RES_FECHA', N'The event date cannot be before today.'), (N'PT', N'MSG_RES_FECHA', N'A data do evento nao pode ser anterior a hoje.'),
        (N'ES', N'MSG_RES_MONTO',  N'El monto no puede ser negativo.'), (N'EN', N'MSG_RES_MONTO', N'The amount cannot be negative.'), (N'PT', N'MSG_RES_MONTO', N'O valor nao pode ser negativo.'),
        (N'ES', N'MSG_RES_NOTFOUND', N'La reserva ya no existe.'),(N'EN', N'MSG_RES_NOTFOUND', N'The reservation no longer exists.'), (N'PT', N'MSG_RES_NOTFOUND', N'A reserva nao existe mais.'),
        (N'ES', N'MSG_RES_ERROR',  N'No se pudo guardar la reserva.'), (N'EN', N'MSG_RES_ERROR', N'Could not save the reservation.'), (N'PT', N'MSG_RES_ERROR', N'Nao foi possivel salvar a reserva.'),
        (N'ES', N'MSG_RES_SELECCIONE', N'Seleccione una reserva existente para ver su historial.'), (N'EN', N'MSG_RES_SELECCIONE', N'Select an existing reservation to view its history.'), (N'PT', N'MSG_RES_SELECCIONE', N'Selecione uma reserva existente para ver seu historico.'),
        -- Login (tagline, recordar, mensajes)
        (N'ES', N'LOGIN_TAGLINE',  N'Gestion de eventos y reservas'), (N'EN', N'LOGIN_TAGLINE',  N'Event and booking management'), (N'PT', N'LOGIN_TAGLINE',  N'Gestao de eventos e reservas'),
        (N'ES', N'LOGIN_REMEMBER', N'Recordar cuenta'),                (N'EN', N'LOGIN_REMEMBER', N'Remember me'),                   (N'PT', N'LOGIN_REMEMBER', N'Lembrar conta'),
        (N'ES', N'LOGIN_COMPLETAR', N'Completar usuario y contrasena.'), (N'EN', N'LOGIN_COMPLETAR', N'Enter username and password.'), (N'PT', N'LOGIN_COMPLETAR', N'Preencha usuario e senha.'),
        (N'ES', N'LOGIN_ERR_CONEXION', N'Error de conexion:'),         (N'EN', N'LOGIN_ERR_CONEXION', N'Connection error:'),         (N'PT', N'LOGIN_ERR_CONEXION', N'Erro de conexao:'),
        (N'ES', N'LOGIN_ERR_USUARIO', N'Usuario no encontrado.'),       (N'EN', N'LOGIN_ERR_USUARIO', N'User not found.'),            (N'PT', N'LOGIN_ERR_USUARIO', N'Usuario nao encontrado.'),
        (N'ES', N'LOGIN_ERR_PASS', N'Contrasena incorrecta.'),         (N'EN', N'LOGIN_ERR_PASS', N'Incorrect password.'),           (N'PT', N'LOGIN_ERR_PASS', N'Senha incorreta.'),
        -- Crear cuenta (mensajes)
        (N'ES', N'CC_MSG_COMPLETAR', N'Completar todos los campos.'),   (N'EN', N'CC_MSG_COMPLETAR', N'Fill in all fields.'),         (N'PT', N'CC_MSG_COMPLETAR', N'Preencha todos os campos.'),
        (N'ES', N'CC_MSG_NO_COINCIDEN', N'Las contrasenas no coinciden.'), (N'EN', N'CC_MSG_NO_COINCIDEN', N'Passwords do not match.'), (N'PT', N'CC_MSG_NO_COINCIDEN', N'As senhas nao coincidem.'),
        (N'ES', N'CC_MSG_PASS_CORTA', N'La contrasena debe tener al menos 4 caracteres.'), (N'EN', N'CC_MSG_PASS_CORTA', N'Password must be at least 4 characters.'), (N'PT', N'CC_MSG_PASS_CORTA', N'A senha deve ter ao menos 4 caracteres.'),
        (N'ES', N'CC_MSG_OK', N'Usuario creado. Ya podes iniciar sesion.'), (N'EN', N'CC_MSG_OK', N'Account created. You can sign in now.'), (N'PT', N'CC_MSG_OK', N'Conta criada. Voce ja pode entrar.'),
        (N'ES', N'CC_MSG_USER_INVALIDO', N'Usuario invalido (3-50, letras/numeros/._-).'), (N'EN', N'CC_MSG_USER_INVALIDO', N'Invalid username (3-50, letters/digits/._-).'), (N'PT', N'CC_MSG_USER_INVALIDO', N'Usuario invalido (3-50, letras/numeros/._-).'),
        (N'ES', N'CC_MSG_USER_EXISTE', N'Ese usuario ya existe.'),      (N'EN', N'CC_MSG_USER_EXISTE', N'That username already exists.'), (N'PT', N'CC_MSG_USER_EXISTE', N'Esse usuario ja existe.'),
        (N'ES', N'CC_MSG_PASS_INVALIDA', N'Contrasena invalida.'),      (N'EN', N'CC_MSG_PASS_INVALIDA', N'Invalid password.'),       (N'PT', N'CC_MSG_PASS_INVALIDA', N'Senha invalida.'),
        (N'ES', N'CC_MSG_ERROR', N'Error:'),                           (N'EN', N'CC_MSG_ERROR', N'Error:'),                          (N'PT', N'CC_MSG_ERROR', N'Erro:'),
        -- Varios
        (N'ES', N'HIST_VACIO', N'Sin cambios registrados.'),           (N'EN', N'HIST_VACIO', N'No changes recorded.'),              (N'PT', N'HIST_VACIO', N'Sem alteracoes registradas.'),
        (N'ES', N'BTN_CANCELAR', N'Cancelar'),                         (N'EN', N'BTN_CANCELAR', N'Cancel'),                          (N'PT', N'BTN_CANCELAR', N'Cancelar'),
        (N'ES', N'IDI_GESTION', N'Gestionar idiomas'),                 (N'EN', N'IDI_GESTION', N'Manage languages'),                 (N'PT', N'IDI_GESTION', N'Gerenciar idiomas'),
        -- Perfiles (alta + asignacion a usuarios)
        (N'ES', N'COL_PERFIL', N'Perfil'),                             (N'EN', N'COL_PERFIL', N'Profile'),                           (N'PT', N'COL_PERFIL', N'Perfil'),
        (N'ES', N'PERF_NUEVO', N'Nuevo perfil'),                       (N'EN', N'PERF_NUEVO', N'New profile'),                       (N'PT', N'PERF_NUEVO', N'Novo perfil'),
        (N'ES', N'PERF_DESC', N'Descripcion'),                         (N'EN', N'PERF_DESC', N'Description'),                        (N'PT', N'PERF_DESC', N'Descricao'),
        (N'ES', N'PERF_CREAR', N'Crear perfil'),                       (N'EN', N'PERF_CREAR', N'Create profile'),                    (N'PT', N'PERF_CREAR', N'Criar perfil'),
        (N'ES', N'PERF_ASIGNAR_TITULO', N'Asignar perfil a usuarios'), (N'EN', N'PERF_ASIGNAR_TITULO', N'Assign profile to users'),  (N'PT', N'PERF_ASIGNAR_TITULO', N'Atribuir perfil a usuarios'),
        (N'ES', N'PERF_GUARDAR_ASIG', N'Guardar asignaciones'),        (N'EN', N'PERF_GUARDAR_ASIG', N'Save assignments'),           (N'PT', N'PERF_GUARDAR_ASIG', N'Salvar atribuicoes'),
        (N'ES', N'PERF_SIN', N'(sin perfil)'),                         (N'EN', N'PERF_SIN', N'(no profile)'),                        (N'PT', N'PERF_SIN', N'(sem perfil)'),
        (N'ES', N'MSG_PERF_ASIG_OK', N'Asignaciones guardadas.'),      (N'EN', N'MSG_PERF_ASIG_OK', N'Assignments saved.'),          (N'PT', N'MSG_PERF_ASIG_OK', N'Atribuicoes salvas.'),
        (N'ES', N'MSG_PERF_NOM_INV', N'Ingrese el nombre del perfil.'),(N'EN', N'MSG_PERF_NOM_INV', N'Enter the profile name.'),     (N'PT', N'MSG_PERF_NOM_INV', N'Informe o nome do perfil.'),
        (N'ES', N'MSG_PERF_DUP', N'Ya existe un perfil con ese nombre.'), (N'EN', N'MSG_PERF_DUP', N'A profile with that name already exists.'), (N'PT', N'MSG_PERF_DUP', N'Ja existe um perfil com esse nome.'),
        (N'ES', N'MSG_PERF_CREADO', N'Perfil creado.'),               (N'EN', N'MSG_PERF_CREADO', N'Profile created.'),             (N'PT', N'MSG_PERF_CREADO', N'Perfil criado.'),
        -- Auditoria unificada (tabs)
        (N'ES', N'AUD_TAB_BITACORA', N'Bitacora general'),            (N'EN', N'AUD_TAB_BITACORA', N'General audit log'),           (N'PT', N'AUD_TAB_BITACORA', N'Registro geral'),
        (N'ES', N'AUD_TAB_LOGIN', N'Auditoria de login'),             (N'EN', N'AUD_TAB_LOGIN', N'Login audit'),                    (N'PT', N'AUD_TAB_LOGIN', N'Auditoria de login')
    ) AS v(Codigo, Clave, Texto)
)
INSERT INTO dbo.Traducciones (IdiomaId, Clave, Texto)
SELECT i.Id, t.Clave, t.Texto
FROM Txt t
JOIN dbo.Idiomas i ON i.Codigo = t.Codigo
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Traducciones x WHERE x.IdiomaId = i.Id AND x.Clave = t.Clave
);
GO

-- La seccion de auditoria ahora unifica bitacora general + auditoria de login,
-- por eso el item del menu pasa a llamarse simplemente "Auditoria".
UPDATE t SET Texto = CASE i.Codigo WHEN N'EN' THEN N'Audit' ELSE N'Auditoria' END
FROM dbo.Traducciones t
JOIN dbo.Idiomas i ON i.Id = t.IdiomaId
WHERE t.Clave = N'MENU_AUDITORIA';
GO
