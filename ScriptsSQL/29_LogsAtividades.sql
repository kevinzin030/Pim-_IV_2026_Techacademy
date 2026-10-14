
USE PIMIV_PainelADM;
GO

-- Cria a tabela de registros de atividades
CREATE TABLE dbo.LogsAtividades
(
    IdLog INT IDENTITY(1,1) PRIMARY KEY,

    IdUsuario INT NULL,

    Acao NVARCHAR(100) NOT NULL,

    Descricao NVARCHAR(500) NOT NULL,

    DataHora DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_LogsAtividades_Usuarios
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuarios(IdUsuario)
);
GO

-- Cria um indice para agilizar as consultas
CREATE INDEX IX_LogsAtividades_DataHora
ON dbo.LogsAtividades(DataHora DESC);
GO

-- Confirma a criacao da tabela
SELECT
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'LogsAtividades';
GO
