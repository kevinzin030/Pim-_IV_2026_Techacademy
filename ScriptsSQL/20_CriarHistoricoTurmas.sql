
USE PIMIV_PainelADM;
GO

SELECT
    name AS NomeTabela
FROM sys.tables
WHERE name = 'HistoricoTurmas';

SELECT
    name AS NomeIndice,
    is_unique AS EhUnico,
    has_filter AS PossuiFiltro,
    filter_definition AS Filtro
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.HistoricoTurmas')
  AND name = 'UX_HistoricoTurmas_Atual';
GO
