
USE PIMIV_PainelADM;
GO

SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @IdPerfil INT;
DECLARE @IdUsuario INT;
DECLARE @IdAluno INT;
DECLARE @IdTurmaA INT;
DECLARE @IdTurmaB INT;
DECLARE @IdDisciplina INT;
DECLARE @IdMatricula INT;
DECLARE @IdTurmaDisciplina INT;
DECLARE @IdAula INT;

BEGIN TRY

    SELECT @IdPerfil = IdPerfil
    FROM dbo.Perfis
    WHERE NomePerfil = N'Aluno';

    IF @IdPerfil IS NULL
        THROW 50010, 'Perfil Aluno nao encontrado.', 1;

    BEGIN TRANSACTION;

    -- 1. Usuario temporario
    INSERT INTO dbo.Usuarios (
        NomeCompleto,
        Login,
        SenhaHash,
        IdPerfil,
        Ativo
    )
    VALUES (
        N'Aluno Teste Frequencia',
        N'teste_frequencia_999',
        N'HASH_FICTICIO_NAO_UTILIZAVEL',
        @IdPerfil,
        1
    );

    SET @IdUsuario = SCOPE_IDENTITY();

    -- 2. Aluno
    INSERT INTO dbo.Alunos (
        IdUsuario,
        MatriculaAluno
    )
    VALUES (
        @IdUsuario,
        N'TESTE-FREQ-999'
    );

    SET @IdAluno = SCOPE_IDENTITY();

    -- 3. Turma A
    INSERT INTO dbo.Turmas (
        CodigoTurma,
        NomeTurma,
        Periodo,
        AnoLetivo,
        Semestre
    )
    VALUES (
        N'TESTE-FA-999',
        N'Turma A Teste',
        N'Matutino',
        2026,
        2
    );

    SET @IdTurmaA = SCOPE_IDENTITY();

    -- 4. Turma B
    INSERT INTO dbo.Turmas (
        CodigoTurma,
        NomeTurma,
        Periodo,
        AnoLetivo,
        Semestre
    )
    VALUES (
        N'TESTE-FB-999',
        N'Turma B Teste',
        N'Matutino',
        2026,
        2
    );

    SET @IdTurmaB = SCOPE_IDENTITY();

    -- 5. Disciplina
    INSERT INTO dbo.Disciplinas (
        CodigoDisciplina,
        NomeDisciplina,
        CargaHoraria
    )
    VALUES (
        N'TESTE-FREQ-DISC-999',
        N'Matematica Teste',
        80
    );

    SET @IdDisciplina = SCOPE_IDENTITY();

    -- 6. Matricular aluno na Turma A
    INSERT INTO dbo.Matriculas (
        IdAluno,
        IdTurma
    )
    VALUES (
        @IdAluno,
        @IdTurmaA
    );

    SET @IdMatricula = SCOPE_IDENTITY();

  
-- 7. Associar disciplina a Turma A
INSERT INTO dbo.TurmaDisciplinas (
    IdTurma,
    IdDisciplina
)
VALUES (
    @IdTurmaA,
    @IdDisciplina
);


    SET @IdTurmaDisciplina = SCOPE_IDENTITY();

    -- 8. Criar aula na Turma B
    INSERT INTO dbo.Aulas (
        IdTurmaDisciplina,
        DataAula,
        Conteudo,
        Realizada
    )
    VALUES (
        @IdTurmaDisciplina,
        '2026-10-13',
        N'Aula de teste',
        1
    );

    SET @IdAula = SCOPE_IDENTITY();

    PRINT 'Dados de teste preparados.';

    -- 9. Tentar registrar frequencia incorreta
    INSERT INTO dbo.Frequencias (
        IdMatricula,
        IdAula,
        Presente
    )
    VALUES (
        @IdMatricula,
        @IdAula,
        1
    );

   PRINT 'SUCESSO: Frequencia valida aceita.';

END TRY
BEGIN CATCH

    IF ERROR_NUMBER() = 50003
        PRINT 'SUCESSO: Trigger bloqueou a frequencia de outra turma.';
    ELSE
        PRINT 'ERRO INESPERADO: Verifique a mensagem abaixo.';

    PRINT ERROR_MESSAGE();

END CATCH;

-- Encerrar a transacao, inclusive se estiver invalida
IF @@TRANCOUNT > 0
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'ROLLBACK executado.';
END;
GO
