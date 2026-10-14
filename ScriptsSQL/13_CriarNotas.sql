
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Notas (
    IdNota INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdMatricula INT NOT NULL,

    IdAvaliacao INT NOT NULL,

    ValorNota DECIMAL(5,2) NOT NULL,

    DataLancamento DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Notas_Matriculas
        FOREIGN KEY (IdMatricula)
        REFERENCES dbo.Matriculas(IdMatricula),

    CONSTRAINT FK_Notas_Avaliacoes
        FOREIGN KEY (IdAvaliacao)
        REFERENCES dbo.Avaliacoes(IdAvaliacao),

    CONSTRAINT UQ_Notas_MatriculaAvaliacao
        UNIQUE (IdMatricula, IdAvaliacao),

    CONSTRAINT CK_Notas_Valor
        CHECK (ValorNota >= 0 AND ValorNota <= 10)
);
GO
