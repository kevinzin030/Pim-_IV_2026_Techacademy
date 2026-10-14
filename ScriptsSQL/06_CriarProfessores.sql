
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Professores (
    IdProfessor INT IDENTITY(1,1) NOT NULL
        PRIMARY KEY,

    IdUsuario INT NOT NULL UNIQUE,

    RegistroProfessor NVARCHAR(30) NOT NULL UNIQUE,

    DataCadastro DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Professores_Usuarios
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuarios(IdUsuario)
);
GO
