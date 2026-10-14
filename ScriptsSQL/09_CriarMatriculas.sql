
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Matriculas (
    IdMatricula INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdAluno INT NOT NULL UNIQUE,

    IdTurma INT NOT NULL,

    DataMatricula DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    Situacao NVARCHAR(20) NOT NULL
        DEFAULT N'Ativa',

    CONSTRAINT FK_Matriculas_Alunos
        FOREIGN KEY (IdAluno)
        REFERENCES dbo.Alunos(IdAluno),

    CONSTRAINT FK_Matriculas_Turmas
        FOREIGN KEY (IdTurma)
        REFERENCES dbo.Turmas(IdTurma),

    CONSTRAINT CK_Matriculas_Situacao
        CHECK (Situacao IN (
            N'Ativa',
            N'Trancada',
            N'Concluida',
            N'Cancelada'
        ))
);
GO
