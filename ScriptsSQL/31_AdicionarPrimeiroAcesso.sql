USE PIMIV_PainelADM;
GO

IF COL_LENGTH('dbo.Usuarios', 'DeveAlterarSenha') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD DeveAlterarSenha BIT NOT NULL
        CONSTRAINT DF_Usuarios_DeveAlterarSenha DEFAULT (1);
END;
GO

-- Contas existentes permanecem liberadas.
-- Os novos cadastros terão troca obrigatória.
UPDATE dbo.Usuarios
SET DeveAlterarSenha = 0;
GO

SELECT
    IdUsuario,
    NomeCompleto,
    Login,
    DeveAlterarSenha
FROM dbo.Usuarios
ORDER BY IdUsuario;