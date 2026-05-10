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
