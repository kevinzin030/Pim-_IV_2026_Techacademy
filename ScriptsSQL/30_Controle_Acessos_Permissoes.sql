
USE PIMIV_PainelADM;
GO

-- Cria o catalogo de permissoes
IF OBJECT_ID('dbo.Permissoes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissoes
    (
        IdPermissao INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Permissoes PRIMARY KEY,

        Codigo NVARCHAR(100) NOT NULL
            CONSTRAINT UQ_Permissoes_Codigo UNIQUE,

        Descricao NVARCHAR(200) NOT NULL,

        Plataforma NVARCHAR(30) NOT NULL,

        Ativo BIT NOT NULL
            CONSTRAINT DF_Permissoes_Ativo DEFAULT 1
    );
END;
GO

-- Relaciona os perfis com suas permissoes
IF OBJECT_ID('dbo.PerfilPermissoes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilPermissoes
    (
        IdPerfil INT NOT NULL,
        IdPermissao INT NOT NULL,

        CONSTRAINT PK_PerfilPermissoes
            PRIMARY KEY (IdPerfil, IdPermissao),

        CONSTRAINT FK_PerfilPermissoes_Perfis
            FOREIGN KEY (IdPerfil)
            REFERENCES dbo.Perfis(IdPerfil),

        CONSTRAINT FK_PerfilPermissoes_Permissoes
            FOREIGN KEY (IdPermissao)
            REFERENCES dbo.Permissoes(IdPermissao)
    );
END;
GO

-- Cadastra as funcionalidades disponiveis
INSERT INTO dbo.Permissoes
    (Codigo, Descricao, Plataforma)
SELECT
    v.Codigo,
    v.Descricao,
    v.Plataforma
FROM
(
    VALUES
    (
        'GERENCIAR_USUARIOS',
        'Gerenciar usuários e contas',
        'Desktop'
    ),
    (
        'GERENCIAR_ACESSOS',
        'Gerenciar perfis e permissões',
        'Desktop'
    ),
    (
        'CONSULTAR_AUDITORIA',
        'Consultar registros de atividades',
        'Desktop'
    ),
    (
        'GERENCIAR_TURMAS',
        'Gerenciar turmas',
        'Desktop'
    ),
    (
        'GERENCIAR_DISCIPLINAS',
        'Gerenciar disciplinas',
        'Desktop'
    ),
    (
        'GERENCIAR_MATRICULAS',
        'Gerenciar matrículas',
        'Desktop'
    ),
    (
        'ALOCAR_PROFESSORES',
        'Alocar professores',
        'Desktop'
    ),
    (
        'GERENCIAR_CALENDARIO',
        'Gerenciar calendário acadêmico',
        'Desktop'
    ),
    (
        'LANCAR_NOTAS',
        'Lançar notas e frequências',
        'Web/Mobile'
    ),
    (
        'PUBLICAR_MATERIAIS',
        'Publicar materiais e atividades',
        'Web/Mobile'
    ),
    (
        'CONSULTAR_TURMAS',
        'Consultar turmas atribuídas',
        'Web/Mobile'
    ),
    (
        'CONSULTAR_NOTAS',
        'Consultar notas e frequências',
        'Web/Mobile'
    ),
    (
        'CONSULTAR_MATERIAIS',
        'Consultar materiais e atividades',
        'Web/Mobile'
    ),
    (
        'CONSULTAR_CRONOGRAMA',
        'Consultar cronograma e notificações',
        'Web/Mobile'
    )
) AS v(Codigo, Descricao, Plataforma)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Permissoes p
    WHERE p.Codigo = v.Codigo
);
GO

-- Associa as permissoes iniciais a cada perfil
INSERT INTO dbo.PerfilPermissoes
    (IdPerfil, IdPermissao)
SELECT
    p.IdPerfil,
    pm.IdPermissao
FROM dbo.Perfis p
CROSS JOIN dbo.Permissoes pm
WHERE
(
    (
        p.NomePerfil = 'Administrador'
        AND pm.Codigo IN
        (
            'GERENCIAR_USUARIOS',
            'GERENCIAR_ACESSOS',
            'CONSULTAR_AUDITORIA'
        )
    )
    OR
    (
        p.NomePerfil = 'Coordenador'
        AND pm.Codigo IN
        (
            'GERENCIAR_TURMAS',
            'GERENCIAR_DISCIPLINAS',
            'GERENCIAR_MATRICULAS',
            'ALOCAR_PROFESSORES',
            'GERENCIAR_CALENDARIO'
        )
    )
    OR
    (
        p.NomePerfil = 'Professor'
        AND pm.Codigo IN
        (
            'LANCAR_NOTAS',
            'PUBLICAR_MATERIAIS',
            'CONSULTAR_TURMAS'
        )
    )
    OR
    (
        p.NomePerfil = 'Aluno'
        AND pm.Codigo IN
        (
            'CONSULTAR_NOTAS',
            'CONSULTAR_MATERIAIS',
            'CONSULTAR_CRONOGRAMA'
        )
    )
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.PerfilPermissoes pp
    WHERE pp.IdPerfil = p.IdPerfil
      AND pp.IdPermissao = pm.IdPermissao
);
GO

-- Exibe as permissoes de cada perfil
SELECT
    p.IdPerfil,
    p.NomePerfil AS Perfil,
    pm.Codigo,
    pm.Descricao AS Permissao,
    pm.Plataforma
FROM dbo.PerfilPermissoes pp
INNER JOIN dbo.Perfis p
    ON pp.IdPerfil = p.IdPerfil
INNER JOIN dbo.Permissoes pm
    ON pp.IdPermissao = pm.IdPermissao
ORDER BY
    p.IdPerfil,
    pm.IdPermissao;
GO

-- Confere a quantidade de registros
SELECT
    (SELECT COUNT(*) FROM dbo.Permissoes)
        AS TotalPermissoes,

    (SELECT COUNT(*) FROM dbo.PerfilPermissoes)
        AS TotalVinculos;
GO
