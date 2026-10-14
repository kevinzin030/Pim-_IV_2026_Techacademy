
USE PIMIV_PainelADM;
GO

IF OBJECT_ID(N'dbo.ProfessorTurmaDisciplinas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessorTurmaDisciplinas (
        IdVinculo INT IDENTITY(1,1)
            NOT NULL PRIMARY KEY,

        IdProfessor INT NOT NULL,

        IdTurmaDisciplina INT NOT NULL,

        DataVinculo DATETIME2 NOT NULL
            DEFAULT SYSDATETIME(),

        Ativo BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_Vinculos_Professores
            FOREIGN KEY (IdProfessor)
            REFERENCES dbo.Professores(IdProfessor),

        CONSTRAINT FK_Vinculos_TurmaDisciplinas
            FOREIGN KEY (IdTurmaDisciplina)
            REFERENCES dbo.TurmaDisciplinas(IdTurmaDisciplina),

        CONSTRAINT UQ_ProfessorTurmaDisciplina
            UNIQUE (IdProfessor, IdTurmaDisciplina)
    );
END;
GO
