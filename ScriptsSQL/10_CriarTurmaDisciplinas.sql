
USE PIMIV_PainelADM;
GO

IF OBJECT_ID(N'dbo.TurmaDisciplinas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TurmaDisciplinas (
        IdTurmaDisciplina INT IDENTITY(1,1)
            NOT NULL PRIMARY KEY,

        IdTurma INT NOT NULL,

        IdDisciplina INT NOT NULL,

        Ativa BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_TurmaDisciplinas_Turmas
            FOREIGN KEY (IdTurma)
            REFERENCES dbo.Turmas(IdTurma),

        CONSTRAINT FK_TurmaDisciplinas_Disciplinas
            FOREIGN KEY (IdDisciplina)
            REFERENCES dbo.Disciplinas(IdDisciplina),

        CONSTRAINT UQ_TurmaDisciplinas
            UNIQUE (IdTurma, IdDisciplina)
    );
END;
GO
