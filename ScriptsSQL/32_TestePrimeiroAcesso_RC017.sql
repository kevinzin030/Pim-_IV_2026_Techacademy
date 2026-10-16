USE PIMIV_PainelADM;
GO

UPDATE dbo.Usuarios
SET DeveAlterarSenha = 1
WHERE Login = 'RC017'
  AND IdPerfil = 2
  AND Ativo = 1;

SELECT
    IdUsuario,
    NomeCompleto,
    Login,
    DeveAlterarSenha
FROM dbo.Usuarios
WHERE Login = 'RC017';