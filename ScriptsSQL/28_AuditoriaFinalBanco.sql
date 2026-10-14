
USE PIMIV_PainelADM;
GO

-- 1. Contar as tabelas
SELECT COUNT(*) AS TotalTabelas
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo');

-- 2. Verificar os triggers
SELECT
    name AS NomeTrigger,
    OBJECT_NAME(parent_id) AS Tabela,
    is_disabled AS Desabilitado
FROM sys.triggers
WHERE name IN (
    'TR_Notas_Validar',
    'TR_Frequencias_Validar',
    'TR_HistoricoTurmas_Validar'
)
ORDER BY name;

-- 3. Verificar o procedimento
SELECT
    name AS NomeProcedimento,
    modify_date AS UltimaAlteracao
FROM sys.procedures
WHERE name = 'SP_TransferirAluno';

-- 4. Verificar transacoes
SELECT
    @@TRANCOUNT AS TransacoesAbertas,
    XACT_STATE() AS EstadoTransacao;
GO
