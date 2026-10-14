
USE PIMIV_PainelADM;
GO

INSERT INTO dbo.Perfis (NomePerfil, Descricao)
VALUES
(N'Administrador', N'Gerencia usuários, perfis e acessos'),
(N'Coordenador', N'Gerencia estrutura e vínculos acadêmicos'),
(N'Professor', N'Utilizará as plataformas Web e Mobile'),
(N'Aluno', N'Utilizará as plataformas Web e Mobile');
GO

SELECT * FROM dbo.Perfis;
