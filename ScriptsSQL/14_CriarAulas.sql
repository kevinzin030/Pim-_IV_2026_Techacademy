
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Aulas (
    IdAula INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdTurmaDisciplina INT NOT NULL,

    DataAula DATE NOT NULL,

    Conteudo NVARCHAR(300) NULL,

    Realizada BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Aulas_TurmaDisciplinas
        FOREIGN KEY (IdTurmaDisciplina)
        REFERENCES dbo.TurmaDisciplinas(IdTurmaDisciplina)
);
GO
