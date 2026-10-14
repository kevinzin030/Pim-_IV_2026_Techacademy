
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Frequencias (
    IdFrequencia INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdMatricula INT NOT NULL,

    IdAula INT NOT NULL,

    Presente BIT NOT NULL,

    DataRegistro DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Frequencias_Matriculas
        FOREIGN KEY (IdMatricula)
        REFERENCES dbo.Matriculas(IdMatricula),

    CONSTRAINT FK_Frequencias_Aulas
        FOREIGN KEY (IdAula)
        REFERENCES dbo.Aulas(IdAula),

    CONSTRAINT UQ_Frequencias_MatriculaAula
        UNIQUE (IdMatricula, IdAula)
);
GO
