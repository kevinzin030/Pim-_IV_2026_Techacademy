
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Alunos (
    IdAluno INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdUsuario INT NOT NULL UNIQUE,

    MatriculaAluno NVARCHAR(30) NOT NULL UNIQUE,

    DataCadastro DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Alunos_Usuarios
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuarios(IdUsuario)
);
GO
