/*
 TechAcademy - Banco de dados PIM IV
 SQL Server / Azure SQL Database
 Execute este script dentro do banco TechAcademy já selecionado.
 Ele cria o esquema e os perfis básicos, mas NÃO cria usuários com senhas padrão.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Perfis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Perfis (
        PerfilId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Perfis PRIMARY KEY,
        Nome NVARCHAR(40) NOT NULL,
        Descricao NVARCHAR(200) NULL,
        CONSTRAINT UQ_Perfis_Nome UNIQUE (Nome)
    );
END;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        PerfilId INT NOT NULL,
        Nome NVARCHAR(120) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        SenhaHash NVARCHAR(255) NOT NULL,
        Matricula NVARCHAR(30) NULL,
        Telefone NVARCHAR(25) NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Usuarios_Ativo DEFAULT (1),
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_CriadoEm DEFAULT (SYSUTCDATETIME()),
        AtualizadoEm DATETIME2(0) NULL,
        CONSTRAINT FK_Usuarios_Perfis FOREIGN KEY (PerfilId) REFERENCES dbo.Perfis(PerfilId),
        CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
        CONSTRAINT UQ_Usuarios_Matricula UNIQUE (Matricula)
    );
END;
GO

IF OBJECT_ID('dbo.Cursos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cursos (
        CursoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cursos PRIMARY KEY,
        Nome NVARCHAR(120) NOT NULL,
        Descricao NVARCHAR(1000) NULL,
        CargaHoraria INT NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Cursos_Ativo DEFAULT (1),
        CONSTRAINT CK_Cursos_CargaHoraria CHECK (CargaHoraria IS NULL OR CargaHoraria > 0),
        CONSTRAINT UQ_Cursos_Nome UNIQUE (Nome)
    );
END;
GO

IF OBJECT_ID('dbo.PeriodosLetivos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PeriodosLetivos (
        PeriodoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PeriodosLetivos PRIMARY KEY,
        Nome NVARCHAR(60) NOT NULL,
        DataInicio DATE NOT NULL,
        DataFim DATE NOT NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_PeriodosLetivos_Ativo DEFAULT (0),
        CONSTRAINT CK_PeriodosLetivos_Datas CHECK (DataFim >= DataInicio),
        CONSTRAINT UQ_PeriodosLetivos_Nome UNIQUE (Nome)
    );
END;
GO

IF OBJECT_ID('dbo.Disciplinas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Disciplinas (
        DisciplinaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Disciplinas PRIMARY KEY,
        CursoId INT NOT NULL,
        Nome NVARCHAR(120) NOT NULL,
        Descricao NVARCHAR(1000) NULL,
        CargaHoraria INT NULL,
        CONSTRAINT FK_Disciplinas_Cursos FOREIGN KEY (CursoId) REFERENCES dbo.Cursos(CursoId),
        CONSTRAINT CK_Disciplinas_CargaHoraria CHECK (CargaHoraria IS NULL OR CargaHoraria > 0),
        CONSTRAINT UQ_Disciplinas_Curso_Nome UNIQUE (CursoId, Nome)
    );
END;
GO

IF OBJECT_ID('dbo.Turmas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Turmas (
        TurmaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Turmas PRIMARY KEY,
        CursoId INT NOT NULL,
        PeriodoId INT NOT NULL,
        Nome NVARCHAR(80) NOT NULL,
        Turno NVARCHAR(20) NULL,
        DataInicio DATE NULL,
        DataFim DATE NULL,
        Ativa BIT NOT NULL CONSTRAINT DF_Turmas_Ativa DEFAULT (1),
        CONSTRAINT FK_Turmas_Cursos FOREIGN KEY (CursoId) REFERENCES dbo.Cursos(CursoId),
        CONSTRAINT FK_Turmas_Periodos FOREIGN KEY (PeriodoId) REFERENCES dbo.PeriodosLetivos(PeriodoId),
        CONSTRAINT CK_Turmas_Datas CHECK (DataInicio IS NULL OR DataFim IS NULL OR DataFim >= DataInicio),
        CONSTRAINT UQ_Turmas_Periodo_Nome UNIQUE (PeriodoId, Nome)
    );
END;
GO

IF OBJECT_ID('dbo.ProfessorTurma', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessorTurma (
        ProfessorTurmaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProfessorTurma PRIMARY KEY,
        ProfessorId INT NOT NULL,
        TurmaId INT NOT NULL,
        DisciplinaId INT NOT NULL,
        CONSTRAINT FK_ProfessorTurma_Usuario FOREIGN KEY (ProfessorId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_ProfessorTurma_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId),
        CONSTRAINT FK_ProfessorTurma_Disciplina FOREIGN KEY (DisciplinaId) REFERENCES dbo.Disciplinas(DisciplinaId),
        CONSTRAINT UQ_ProfessorTurma UNIQUE (ProfessorId, TurmaId, DisciplinaId)
    );
END;
GO

IF OBJECT_ID('dbo.Matriculas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Matriculas (
        MatriculaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Matriculas PRIMARY KEY,
        AlunoId INT NOT NULL,
        TurmaId INT NOT NULL,
        DataMatricula DATE NOT NULL CONSTRAINT DF_Matriculas_Data DEFAULT (CONVERT(date, GETDATE())),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Matriculas_Status DEFAULT (N'Ativa'),
        CONSTRAINT FK_Matriculas_Aluno FOREIGN KEY (AlunoId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Matriculas_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId),
        CONSTRAINT UQ_Matriculas_Aluno_Turma UNIQUE (AlunoId, TurmaId),
        CONSTRAINT CK_Matriculas_Status CHECK (Status IN (N'Ativa', N'Trancada', N'Concluida', N'Cancelada'))
    );
END;
GO

IF OBJECT_ID('dbo.Avaliacoes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Avaliacoes (
        AvaliacaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Avaliacoes PRIMARY KEY,
        TurmaId INT NOT NULL,
        DisciplinaId INT NOT NULL,
        ProfessorId INT NOT NULL,
        Titulo NVARCHAR(150) NOT NULL,
        Descricao NVARCHAR(1000) NULL,
        DataAvaliacao DATE NULL,
        NotaMaxima DECIMAL(5,2) NOT NULL CONSTRAINT DF_Avaliacoes_NotaMaxima DEFAULT (10),
        Peso DECIMAL(6,2) NOT NULL CONSTRAINT DF_Avaliacoes_Peso DEFAULT (1),
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Avaliacoes_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Avaliacoes_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId),
        CONSTRAINT FK_Avaliacoes_Disciplina FOREIGN KEY (DisciplinaId) REFERENCES dbo.Disciplinas(DisciplinaId),
        CONSTRAINT FK_Avaliacoes_Professor FOREIGN KEY (ProfessorId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT CK_Avaliacoes_NotaMaxima CHECK (NotaMaxima > 0),
        CONSTRAINT CK_Avaliacoes_Peso CHECK (Peso > 0)
    );
END;
GO

IF OBJECT_ID('dbo.Notas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notas (
        NotaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notas PRIMARY KEY,
        AvaliacaoId INT NOT NULL,
        AlunoId INT NOT NULL,
        Nota DECIMAL(5,2) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        LancadaPor INT NOT NULL,
        LancadaEm DATETIME2(0) NOT NULL CONSTRAINT DF_Notas_LancadaEm DEFAULT (SYSUTCDATETIME()),
        AtualizadaEm DATETIME2(0) NULL,
        CONSTRAINT FK_Notas_Avaliacao FOREIGN KEY (AvaliacaoId) REFERENCES dbo.Avaliacoes(AvaliacaoId),
        CONSTRAINT FK_Notas_Aluno FOREIGN KEY (AlunoId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Notas_LancadaPor FOREIGN KEY (LancadaPor) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT UQ_Notas_Avaliacao_Aluno UNIQUE (AvaliacaoId, AlunoId),
        CONSTRAINT CK_Notas_Valor CHECK (Nota >= 0)
    );
END;
GO

IF OBJECT_ID('dbo.Aulas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Aulas (
        AulaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Aulas PRIMARY KEY,
        TurmaId INT NOT NULL,
        DisciplinaId INT NOT NULL,
        ProfessorId INT NOT NULL,
        DataAula DATE NOT NULL,
        Conteudo NVARCHAR(1000) NULL,
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Aulas_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Aulas_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId),
        CONSTRAINT FK_Aulas_Disciplina FOREIGN KEY (DisciplinaId) REFERENCES dbo.Disciplinas(DisciplinaId),
        CONSTRAINT FK_Aulas_Professor FOREIGN KEY (ProfessorId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID('dbo.Frequencias', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Frequencias (
        FrequenciaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Frequencias PRIMARY KEY,
        AulaId INT NOT NULL,
        AlunoId INT NOT NULL,
        Presente BIT NOT NULL,
        Justificativa NVARCHAR(500) NULL,
        RegistradaPor INT NOT NULL,
        RegistradaEm DATETIME2(0) NOT NULL CONSTRAINT DF_Frequencias_RegistradaEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Frequencias_Aula FOREIGN KEY (AulaId) REFERENCES dbo.Aulas(AulaId),
        CONSTRAINT FK_Frequencias_Aluno FOREIGN KEY (AlunoId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Frequencias_RegistradaPor FOREIGN KEY (RegistradaPor) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT UQ_Frequencias_Aula_Aluno UNIQUE (AulaId, AlunoId)
    );
END;
GO

IF OBJECT_ID('dbo.Materiais', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Materiais (
        MaterialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Materiais PRIMARY KEY,
        TurmaId INT NULL,
        DisciplinaId INT NULL,
        Titulo NVARCHAR(180) NOT NULL,
        Descricao NVARCHAR(1000) NULL,
        UrlArquivo NVARCHAR(1000) NOT NULL,
        PublicadoPor INT NOT NULL,
        PublicadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Materiais_PublicadoEm DEFAULT (SYSUTCDATETIME()),
        Ativo BIT NOT NULL CONSTRAINT DF_Materiais_Ativo DEFAULT (1),
        CONSTRAINT FK_Materiais_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId),
        CONSTRAINT FK_Materiais_Disciplina FOREIGN KEY (DisciplinaId) REFERENCES dbo.Disciplinas(DisciplinaId),
        CONSTRAINT FK_Materiais_PublicadoPor FOREIGN KEY (PublicadoPor) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID('dbo.Comunicados', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comunicados (
        ComunicadoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comunicados PRIMARY KEY,
        Titulo NVARCHAR(180) NOT NULL,
        Mensagem NVARCHAR(MAX) NOT NULL,
        AutorId INT NOT NULL,
        TurmaId INT NULL,
        Publicado BIT NOT NULL CONSTRAINT DF_Comunicados_Publicado DEFAULT (0),
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Comunicados_CriadoEm DEFAULT (SYSUTCDATETIME()),
        PublicadoEm DATETIME2(0) NULL,
        CONSTRAINT FK_Comunicados_Autor FOREIGN KEY (AutorId) REFERENCES dbo.Usuarios(UsuarioId),
        CONSTRAINT FK_Comunicados_Turma FOREIGN KEY (TurmaId) REFERENCES dbo.Turmas(TurmaId)
    );
END;
GO

IF OBJECT_ID('dbo.Notificacoes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificacoes (
        NotificacaoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificacoes PRIMARY KEY,
        UsuarioId INT NOT NULL,
        Titulo NVARCHAR(160) NOT NULL,
        Mensagem NVARCHAR(1000) NOT NULL,
        Tipo NVARCHAR(40) NULL,
        Lida BIT NOT NULL CONSTRAINT DF_Notificacoes_Lida DEFAULT (0),
        CriadaEm DATETIME2(0) NOT NULL CONSTRAINT DF_Notificacoes_CriadaEm DEFAULT (SYSUTCDATETIME()),
        LidaEm DATETIME2(0) NULL,
        CONSTRAINT FK_Notificacoes_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

IF OBJECT_ID('dbo.LogAuditoria', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogAuditoria (
        LogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LogAuditoria PRIMARY KEY,
        UsuarioId INT NULL,
        Acao NVARCHAR(100) NOT NULL,
        Entidade NVARCHAR(100) NOT NULL,
        EntidadeId NVARCHAR(50) NULL,
        Detalhes NVARCHAR(MAX) NULL,
        Ip NVARCHAR(45) NULL,
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_LogAuditoria_CriadoEm DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_LogAuditoria_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
END;
GO

/* Índices para as consultas mais comuns da API */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Perfil_Ativo' AND object_id = OBJECT_ID('dbo.Usuarios'))
    CREATE INDEX IX_Usuarios_Perfil_Ativo ON dbo.Usuarios(PerfilId, Ativo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Matriculas_Turma_Status' AND object_id = OBJECT_ID('dbo.Matriculas'))
    CREATE INDEX IX_Matriculas_Turma_Status ON dbo.Matriculas(TurmaId, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notas_Aluno' AND object_id = OBJECT_ID('dbo.Notas'))
    CREATE INDEX IX_Notas_Aluno ON dbo.Notas(AlunoId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Aulas_Turma_Data' AND object_id = OBJECT_ID('dbo.Aulas'))
    CREATE INDEX IX_Aulas_Turma_Data ON dbo.Aulas(TurmaId, DataAula);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Frequencias_Aluno' AND object_id = OBJECT_ID('dbo.Frequencias'))
    CREATE INDEX IX_Frequencias_Aluno ON dbo.Frequencias(AlunoId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notificacoes_Usuario_Lida' AND object_id = OBJECT_ID('dbo.Notificacoes'))
    CREATE INDEX IX_Notificacoes_Usuario_Lida ON dbo.Notificacoes(UsuarioId, Lida);
GO

/* Perfis usados pela API para autorização. Seguro executar mais de uma vez. */
IF NOT EXISTS (SELECT 1 FROM dbo.Perfis WHERE Nome = N'Administrador')
    INSERT INTO dbo.Perfis (Nome, Descricao) VALUES (N'Administrador', N'Administra usuários e configurações do sistema');
IF NOT EXISTS (SELECT 1 FROM dbo.Perfis WHERE Nome = N'Professor')
    INSERT INTO dbo.Perfis (Nome, Descricao) VALUES (N'Professor', N'Lança notas e frequência das turmas atribuídas');
IF NOT EXISTS (SELECT 1 FROM dbo.Perfis WHERE Nome = N'Aluno')
    INSERT INTO dbo.Perfis (Nome, Descricao) VALUES (N'Aluno', N'Consulta notas, frequência e materiais');
GO

/* Views de consulta para a API */
CREATE OR ALTER VIEW dbo.vw_NotasAluno AS
SELECT
    n.NotaId, n.AlunoId, u.Nome AS AlunoNome, a.AvaliacaoId, a.Titulo AS Avaliacao,
    d.DisciplinaId, d.Nome AS Disciplina, t.TurmaId, t.Nome AS Turma,
    n.Nota, a.NotaMaxima, n.LancadaEm
FROM dbo.Notas n
JOIN dbo.Usuarios u ON u.UsuarioId = n.AlunoId
JOIN dbo.Avaliacoes a ON a.AvaliacaoId = n.AvaliacaoId
JOIN dbo.Disciplinas d ON d.DisciplinaId = a.DisciplinaId
JOIN dbo.Turmas t ON t.TurmaId = a.TurmaId;
GO

CREATE OR ALTER VIEW dbo.vw_FrequenciaAluno AS
SELECT
    f.FrequenciaId, f.AlunoId, u.Nome AS AlunoNome, au.AulaId, au.DataAula,
    d.DisciplinaId, d.Nome AS Disciplina, t.TurmaId, t.Nome AS Turma,
    f.Presente, f.Justificativa
FROM dbo.Frequencias f
JOIN dbo.Usuarios u ON u.UsuarioId = f.AlunoId
JOIN dbo.Aulas au ON au.AulaId = f.AulaId
JOIN dbo.Disciplinas d ON d.DisciplinaId = au.DisciplinaId
JOIN dbo.Turmas t ON t.TurmaId = au.TurmaId;
GO
