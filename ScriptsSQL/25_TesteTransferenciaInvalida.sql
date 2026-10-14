
USE PIMIV_PainelADM;
GO

BEGIN TRY
    EXEC dbo.SP_TransferirAluno
        @IdMatricula = -1,
        @IdNovaTurma = -1,
        @DataTransferencia = '2026-10-13';

    PRINT 'FALHA: transferencia indevida aceita.';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50010
        PRINT 'SUCESSO: matricula inexistente bloqueada.';
    ELSE
    BEGIN
        PRINT 'ERRO INESPERADO:';
        PRINT ERROR_MESSAGE();
    END;
END CATCH;
GO

SELECT
    @@TRANCOUNT AS TransacoesAbertas,
    XACT_STATE() AS EstadoTransacao;
GO
