
USE PIMIV_PainelADM;
GO

CREATE OR ALTER PROCEDURE dbo.SP_TransferirAluno
    @IdMatricula INT,
    @IdNovaTurma INT,
    @DataTransferencia DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdTurmaAtual INT;
        DECLARE @IdHistoricoAtual INT;
        DECLARE @DataEntradaAtual DATE;

        SELECT @IdTurmaAtual = IdTurma
        FROM dbo.Matriculas WITH (UPDLOCK, HOLDLOCK)
        WHERE IdMatricula = @IdMatricula;

        IF @IdTurmaAtual IS NULL
            THROW 50010, 'Matricula nao encontrada.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Turmas
            WHERE IdTurma = @IdNovaTurma
              AND Ativa = 1
        )
            THROW 50011, 'Turma de destino inexistente ou inativa.', 1;

        IF @IdTurmaAtual = @IdNovaTurma
            THROW 50012, 'O aluno ja pertence a essa turma.', 1;

        SELECT
            @IdHistoricoAtual = IdHistorico,
            @DataEntradaAtual = DataEntrada
        FROM dbo.HistoricoTurmas
        WHERE IdMatricula = @IdMatricula
          AND DataSaida IS NULL;

        IF @IdHistoricoAtual IS NULL
            THROW 50013, 'Historico atual nao encontrado.', 1;

        IF @DataTransferencia <= @DataEntradaAtual
            THROW 50014, 'Data de transferencia invalida.', 1;

        -- Encerrar o periodo anterior
        UPDATE dbo.HistoricoTurmas
        SET DataSaida = DATEADD(DAY, -1, @DataTransferencia),
            MotivoSaida = 'Transferencia de turma'
        WHERE IdHistorico = @IdHistoricoAtual;

        -- Atualizar a turma da matricula
        UPDATE dbo.Matriculas
        SET IdTurma = @IdNovaTurma
        WHERE IdMatricula = @IdMatricula;

        -- Registrar o novo periodo
        INSERT INTO dbo.HistoricoTurmas
            (IdMatricula, IdTurma, DataEntrada)
        VALUES
            (@IdMatricula, @IdNovaTurma, @DataTransferencia);

        COMMIT TRANSACTION;

        PRINT 'Transferencia realizada com sucesso.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO


USE PIMIV_PainelADM;
GO

SELECT
    name AS NomeProcedimento,
    type_desc AS Tipo
FROM sys.procedures
WHERE name = 'SP_TransferirAluno';
GO

