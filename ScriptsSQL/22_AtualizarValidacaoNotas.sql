
USE PIMIV_PainelADM;
GO

SELECT
    name AS NomeTrigger,
    OBJECT_NAME(parent_id) AS Tabela,
    is_disabled AS Desabilitado
FROM sys.triggers
WHERE name IN (
    'TR_Notas_Validar',
    'TR_Frequencias_Validar'
)
ORDER BY name;
GO
