
USE PIMIV_PainelADM;
GO

CREATE OR ALTER TRIGGER dbo.TR_HistoricoTurmas_Validar
ON dbo.HistoricoTurmas
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



USE PIMIV_PainelADM;
GO

SELECT
    name AS NomeTrigger,
    OBJECT_NAME(parent_id) AS Tabela,
    is_disabled AS Desabilitado
FROM sys.triggers
WHERE name = 'TR_HistoricoTurmas_Validar';
GO
