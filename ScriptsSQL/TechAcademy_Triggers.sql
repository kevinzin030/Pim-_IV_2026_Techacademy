-- Triggers exportadas do Azure SQL em 27/11/2026.
-- Executar somente na recriacao do esquema, apos as tabelas.
GO

CREATE   TRIGGER dbo.TR_Aulas_Notificar
ON dbo.Aulas
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Notifica os alunos ativos da turma.
    INSERT INTO dbo.Notificacoes (
        IdUsuario,
        Tipo,
        Titulo,
        Mensagem
    )
    SELECT DISTINCT
        al.IdUsuario,
        'Aula',
        N'Nova aula disponível',
        CONCAT(
            d.NomeDisciplina,
            N': ',
            i.Conteudo
        )
    FROM inserted AS i
    INNER JOIN dbo.TurmaDisciplinas AS td
        ON td.IdTurmaDisciplina = i.IdTurmaDisciplina
    INNER JOIN dbo.Disciplinas AS d
        ON d.IdDisciplina = td.IdDisciplina
    INNER JOIN dbo.Turmas AS t
        ON t.IdTurma = td.IdTurma
    INNER JOIN dbo.Matriculas AS m
        ON m.IdTurma = td.IdTurma
    INNER JOIN dbo.Alunos AS al
        ON al.IdAluno = m.IdAluno
    INNER JOIN dbo.Usuarios AS u
        ON u.IdUsuario = al.IdUsuario
    WHERE m.Situacao = 'Ativa'
        AND u.Ativo = 1
        AND td.Ativa = 1
        AND d.Ativa = 1
        AND t.Ativa = 1;
END;
GO

CREATE   TRIGGER dbo.TR_Frequencias_Notificar
ON dbo.Frequencias
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LimiteFaltas INT = 15;

    -- Identifica as disciplinas afetadas pela operação.
    ;WITH RegistrosAfetados AS (
        SELECT
            i.IdMatricula,
            a.IdTurmaDisciplina
        FROM inserted AS i
        INNER JOIN dbo.Aulas AS a
            ON a.IdAula = i.IdAula
        WHERE a.Realizada = 1

        UNION

        SELECT
            anterior.IdMatricula,
            a.IdTurmaDisciplina
        FROM deleted AS anterior
        INNER JOIN dbo.Aulas AS a
            ON a.IdAula = anterior.IdAula
        WHERE a.Realizada = 1
    ),

    -- Calcula o total atual de faltas.
    FaltasAtuais AS (
        SELECT
            r.IdMatricula,
            r.IdTurmaDisciplina,
            COUNT(f.IdFrequencia) AS TotalFaltas
        FROM RegistrosAfetados AS r
        INNER JOIN dbo.Aulas AS a
            ON a.IdTurmaDisciplina = r.IdTurmaDisciplina
        INNER JOIN dbo.Frequencias AS f
            ON f.IdAula = a.IdAula
            AND f.IdMatricula = r.IdMatricula
        WHERE a.Realizada = 1
            AND f.Presente = 0
        GROUP BY
            r.IdMatricula,
            r.IdTurmaDisciplina
    ),

    -- Calcula a variação de faltas causada pela operação.
    Alteracoes AS (
        SELECT
            i.IdMatricula,
            a.IdTurmaDisciplina,
            1 AS Variacao
        FROM inserted AS i
        INNER JOIN dbo.Aulas AS a
            ON a.IdAula = i.IdAula
        WHERE i.Presente = 0
            AND a.Realizada = 1

        UNION ALL

        SELECT
            anterior.IdMatricula,
            a.IdTurmaDisciplina,
            -1 AS Variacao
        FROM deleted AS anterior
        INNER JOIN dbo.Aulas AS a
            ON a.IdAula = anterior.IdAula
        WHERE anterior.Presente = 0
            AND a.Realizada = 1
    ),

    Variacoes AS (
        SELECT
            IdMatricula,
            IdTurmaDisciplina,
            SUM(Variacao) AS TotalVariacao
        FROM Alteracoes
        GROUP BY
            IdMatricula,
            IdTurmaDisciplina
    )

    -- Notifica somente quando o limite é atingido.
    INSERT INTO dbo.Notificacoes (
        IdUsuario,
        Tipo,
        Titulo,
        Mensagem
    )
    SELECT
        al.IdUsuario,
        'Frequencia',
        N'Alerta de frequência',
        CONCAT(
            N'Você atingiu ',
            f.TotalFaltas,
            N' faltas na disciplina ',
            d.NomeDisciplina,
            N'. Verifique sua frequência.'
        )
    FROM FaltasAtuais AS f
    INNER JOIN Variacoes AS v
        ON v.IdMatricula = f.IdMatricula
        AND v.IdTurmaDisciplina = f.IdTurmaDisciplina
    INNER JOIN dbo.Matriculas AS m
        ON m.IdMatricula = f.IdMatricula
    INNER JOIN dbo.Alunos AS al
        ON al.IdAluno = m.IdAluno
    INNER JOIN dbo.Usuarios AS u
        ON u.IdUsuario = al.IdUsuario
    INNER JOIN dbo.TurmaDisciplinas AS td
        ON td.IdTurmaDisciplina = f.IdTurmaDisciplina
    INNER JOIN dbo.Disciplinas AS d
        ON d.IdDisciplina = td.IdDisciplina
    WHERE u.Ativo = 1
        AND f.TotalFaltas >= @LimiteFaltas
        AND (
            f.TotalFaltas - v.TotalVariacao
        ) < @LimiteFaltas;
END;
GO

CREATE   TRIGGER [dbo].[TR_Frequencias_Validar]
ON [dbo].[Frequencias]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar se o aluno frequentava
    -- a turma na data da aula
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Aulas a
            ON i.IdAula = a.IdAula
        INNER JOIN dbo.TurmaDisciplinas td
            ON a.IdTurmaDisciplina = td.IdTurmaDisciplina
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.HistoricoTurmas h
            WHERE h.IdMatricula = i.IdMatricula
              AND h.IdTurma = td.IdTurma
              AND a.DataAula >= h.DataEntrada
              AND (
                  h.DataSaida IS NULL
                  OR a.DataAula <= h.DataSaida
              )
        )
    )
    BEGIN
        THROW 50003,
            'A aula nao pertence ao historico escolar do aluno.',
            1;
    END;
END;
GO

CREATE   TRIGGER [dbo].[TR_HistoricoTurmas_Validar]
ON [dbo].[HistoricoTurmas]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Impedir periodos sobrepostos
    -- para a mesma matricula
    IF EXISTS (
        SELECT 1
        FROM dbo.HistoricoTurmas h1
        INNER JOIN dbo.HistoricoTurmas h2
            ON h1.IdMatricula = h2.IdMatricula
            AND h1.IdHistorico < h2.IdHistorico
            AND h1.DataEntrada <=
                ISNULL(h2.DataSaida, '99991231')
            AND h2.DataEntrada <=
                ISNULL(h1.DataSaida, '99991231')
        WHERE h1.IdMatricula IN (
            SELECT IdMatricula FROM inserted
        )
    )
    BEGIN
        THROW 50004,
            'A matricula possui periodos de turma sobrepostos.',
            1;
    END;
END;
GO

CREATE   TRIGGER dbo.TR_MateriaisAulas_Notificar
ON dbo.MateriaisAulas
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Notifica os alunos sobre novos materiais acadêmicos.
    INSERT INTO dbo.Notificacoes (
        IdUsuario,
        Tipo,
        Titulo,
        Mensagem
    )
    SELECT DISTINCT
        al.IdUsuario,
        'Material',
        N'Novo material disponível',
        CONCAT(
            d.NomeDisciplina,
            N': ',
            i.Titulo
        )
    FROM inserted AS i
    INNER JOIN dbo.Aulas AS a
        ON a.IdAula = i.IdAula
    INNER JOIN dbo.TurmaDisciplinas AS td
        ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
    INNER JOIN dbo.Disciplinas AS d
        ON d.IdDisciplina = td.IdDisciplina
    INNER JOIN dbo.Turmas AS t
        ON t.IdTurma = td.IdTurma
    INNER JOIN dbo.Matriculas AS m
        ON m.IdTurma = td.IdTurma
    INNER JOIN dbo.Alunos AS al
        ON al.IdAluno = m.IdAluno
    INNER JOIN dbo.Usuarios AS u
        ON u.IdUsuario = al.IdUsuario
    WHERE i.Ativo = 1
        AND m.Situacao = 'Ativa'
        AND u.Ativo = 1
        AND td.Ativa = 1
        AND d.Ativa = 1
        AND t.Ativa = 1;
END;
GO

CREATE   TRIGGER dbo.TR_Notas_Notificar
ON dbo.Notas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Notifica exclusivamente o aluno proprietário da nota.
    INSERT INTO dbo.Notificacoes (
        IdUsuario,
        Tipo,
        Titulo,
        Mensagem
    )
    SELECT
        al.IdUsuario,
        'Nota',
        CASE
            WHEN anterior.IdNota IS NULL
                THEN N'Nova nota lançada'
            ELSE N'Nota atualizada'
        END,
        CONCAT(
            d.NomeDisciplina,
            N' - ',
            av.Titulo,
            N': ',
            i.ValorNota
        )
    FROM inserted AS i
    LEFT JOIN deleted AS anterior
        ON anterior.IdNota = i.IdNota
    INNER JOIN dbo.Matriculas AS m
        ON m.IdMatricula = i.IdMatricula
    INNER JOIN dbo.Alunos AS al
        ON al.IdAluno = m.IdAluno
    INNER JOIN dbo.Usuarios AS u
        ON u.IdUsuario = al.IdUsuario
    INNER JOIN dbo.Avaliacoes AS av
        ON av.IdAvaliacao = i.IdAvaliacao
    INNER JOIN dbo.TurmaDisciplinas AS td
        ON td.IdTurmaDisciplina = av.IdTurmaDisciplina
    INNER JOIN dbo.Disciplinas AS d
        ON d.IdDisciplina = td.IdDisciplina
    WHERE u.Ativo = 1
        AND av.Ativa = 1
        AND (
            anterior.IdNota IS NULL
            OR anterior.ValorNota <> i.ValorNota
        );
END;
GO

CREATE   TRIGGER [dbo].[TR_Notas_Validar]
ON [dbo].[Notas]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar se a avaliacao pertence a uma
    -- turma frequentada pelo aluno na data
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Avaliacoes a
            ON i.IdAvaliacao = a.IdAvaliacao
        INNER JOIN dbo.TurmaDisciplinas td
            ON a.IdTurmaDisciplina = td.IdTurmaDisciplina
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.HistoricoTurmas h
            WHERE h.IdMatricula = i.IdMatricula
              AND h.IdTurma = td.IdTurma
              AND a.DataAvaliacao >= h.DataEntrada
              AND (
                  h.DataSaida IS NULL
                  OR a.DataAvaliacao <= h.DataSaida
              )
        )
    )
    BEGIN
        THROW 50001,
            'A avaliacao nao pertence ao historico escolar do aluno.',
            1;
    END;

    -- Nota nao pode ultrapassar o valor maximo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Avaliacoes a
            ON i.IdAvaliacao = a.IdAvaliacao
        WHERE i.ValorNota > a.ValorMaximo
    )
    BEGIN
        THROW 50002,
            'A nota ultrapassa o valor maximo da avaliacao.',
            1;
    END;
END;
GO

CREATE   TRIGGER dbo.TR_Quizzes_Notificar
ON dbo.Quizzes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Notifica os alunos sobre novos questionários.
    INSERT INTO dbo.Notificacoes (
        IdUsuario,
        Tipo,
        Titulo,
        Mensagem
    )
    SELECT DISTINCT
        al.IdUsuario,
        'Quiz',
        N'Novo quiz disponível',
        CONCAT(
            d.NomeDisciplina,
            N': ',
            i.Titulo,
            N' - ',
            i.XPMaximo,
            N' XP'
        )
    FROM inserted AS i
    INNER JOIN dbo.TurmaDisciplinas AS td
        ON td.IdTurmaDisciplina = i.IdTurmaDisciplina
    INNER JOIN dbo.Disciplinas AS d
        ON d.IdDisciplina = td.IdDisciplina
    INNER JOIN dbo.Turmas AS t
        ON t.IdTurma = td.IdTurma
    INNER JOIN dbo.Matriculas AS m
        ON m.IdTurma = td.IdTurma
    INNER JOIN dbo.Alunos AS al
        ON al.IdAluno = m.IdAluno
    INNER JOIN dbo.Usuarios AS u
        ON u.IdUsuario = al.IdUsuario
    WHERE i.Ativo = 1
        AND m.Situacao = 'Ativa'
        AND u.Ativo = 1
        AND td.Ativa = 1
        AND d.Ativa = 1
        AND t.Ativa = 1;
END;
GO
