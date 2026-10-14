
USE PIMIV_PainelADM;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdTurmaA INT;
    DECLARE @IdTurmaB INT;
    DECLARE @IdUsuario INT;
    DECLARE @IdAluno INT;
    DECLARE @IdMatricula INT;
    DECLARE @IdPerfilAluno INT;

    SELECT @IdPerfilAluno = IdPerfil
    FROM dbo.Perfis
    WHERE NomePerfil = 'Aluno';

    -- Criar duas turmas ficticias
    INSERT INTO dbo.Turmas
        (CodigoTurma, NomeTurma, Periodo,
         AnoLetivo, Semestre, Ativa)
    VALUES
        ('TESTE-TRANS-A', 'Turma Teste A',
         'Manha', 2026, 1, 1);

    SET @IdTurmaA = SCOPE_IDENTITY();

    INSERT INTO dbo.Turmas
        (CodigoTurma, NomeTurma, Periodo,
         AnoLetivo, Semestre, Ativa)
    VALUES
        ('TESTE-TRANS-B', 'Turma Teste B',
         'Manha', 2026, 1, 1);

    SET @IdTurmaB = SCOPE_IDENTITY();

    -- Criar usuario ficticio
    INSERT INTO dbo.Usuarios
        (NomeCompleto, Login, SenhaHash,
         IdPerfil, Ativo)
    VALUES
        ('Aluno Teste Transferencia',
         'aluno_teste_transferencia',
         'HASH_APENAS_PARA_TESTE',
         @IdPerfilAluno, 1);

    SET @IdUsuario = SCOPE_IDENTITY();

    -- Criar aluno
    INSERT INTO dbo.Alunos
        (IdUsuario, MatriculaAluno)
    VALUES
        (@IdUsuario, 'TESTE-TRANS-001');

    SET @IdAluno = SCOPE_IDENTITY();

    -- Criar matricula unica
    INSERT INTO dbo.Matriculas
        (IdAluno, IdTurma, DataMatricula, Situacao)
    VALUES
        (@IdAluno, @IdTurmaA,
         '2026-02-05', 'Ativa');

    SET @IdMatricula = SCOPE_IDENTITY();

    -- Registrar turma inicial
    INSERT INTO dbo.HistoricoTurmas
        (IdMatricula, IdTurma, DataEntrada)
    VALUES
        (@IdMatricula, @IdTurmaA, '2026-02-05');

    -- Executar transferencia
    EXEC dbo.SP_TransferirAluno
        @IdMatricula = @IdMatricula,
        @IdNovaTurma = @IdTurmaB,
        @DataTransferencia = '2026-04-11';

    -- Conferir turma atual
    SELECT
        m.IdMatricula,
        m.IdAluno,
        m.IdTurma AS TurmaAtual
    FROM dbo.Matriculas m
    WHERE m.IdMatricula = @IdMatricula;

    -- Conferir historico
    SELECT
        h.IdMatricula,
        h.IdTurma,
        h.DataEntrada,
        h.DataSaida,
        h.MotivoSaida
    FROM dbo.HistoricoTurmas h
    WHERE h.IdMatricula = @IdMatricula
    ORDER BY h.DataEntrada;

    ROLLBACK TRANSACTION;

    PRINT 'SUCESSO: teste concluido e dados desfeitos.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERRO NO TESTE:';
    PRINT ERROR_MESSAGE();
END CATCH;
GO

SELECT
    @@TRANCOUNT AS TransacoesAbertas,
    XACT_STATE() AS EstadoTransacao;
GO
