
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Turmas (
    IdTurma INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    CodigoTurma NVARCHAR(30) NOT NULL UNIQUE,

    NomeTurma NVARCHAR(100) NOT NULL,

    Periodo NVARCHAR(30) NOT NULL,

    AnoLetivo INT NOT NULL,

    Semestre INT NOT NULL,

    Ativa BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Turmas_Semestre
        CHECK (Semestre IN (1, 2)),

    CONSTRAINT CK_Turmas_AnoLetivo
        CHECK (AnoLetivo BETWEEN 2000 AND 2100)
);
GO
