
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Avaliacoes (
    IdAvaliacao INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdTurmaDisciplina INT NOT NULL,

    Titulo NVARCHAR(150) NOT NULL,

    DataAvaliacao DATE NOT NULL,

    ValorMaximo DECIMAL(5,2) NOT NULL
        DEFAULT 10.00,

    Ativa BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Avaliacoes_TurmaDisciplinas
        FOREIGN KEY (IdTurmaDisciplina)
        REFERENCES dbo.TurmaDisciplinas(IdTurmaDisciplina),

    CONSTRAINT CK_Avaliacoes_ValorMaximo
        CHECK (ValorMaximo > 0 AND ValorMaximo <= 10)
);
GO
