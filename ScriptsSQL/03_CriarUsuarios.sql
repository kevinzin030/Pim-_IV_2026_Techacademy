
USE PIMIV_PainelADM;
GO

CREATE TABLE dbo.Usuarios (
    IdUsuario INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NomeCompleto NVARCHAR(150) NOT NULL,
    Login NVARCHAR(100) NOT NULL UNIQUE,
    SenhaHash NVARCHAR(500) NOT NULL,
    IdPerfil INT NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    DataCadastro DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Usuarios_Perfis
        FOREIGN KEY (IdPerfil)
        REFERENCES dbo.Perfis(IdPerfil)
);
GO


