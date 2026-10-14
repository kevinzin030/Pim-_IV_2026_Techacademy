
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Disciplinas (
    IdDisciplina INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    CodigoDisciplina NVARCHAR(30) NOT NULL UNIQUE,

    NomeDisciplina NVARCHAR(150) NOT NULL,

    CargaHoraria INT NOT NULL,

    Ativa BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Disciplinas_CargaHoraria
        CHECK (CargaHoraria > 0)
);
GO
