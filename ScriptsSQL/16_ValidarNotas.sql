
USE PIMIV_PainelADM;
GO

CREATE OR ALTER TRIGGER dbo.TR_Notas_Validar
ON dbo.Notas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar se a avaliacao pertence
    -- a turma da matricula
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Matriculas m
            ON i.IdMatricula = m.IdMatricula
        INNER JOIN dbo.Avaliacoes a
            ON i.IdAvaliacao = a.IdAvaliacao
        INNER JOIN dbo.TurmaDisciplinas td
            ON a.IdTurmaDisciplina = td.IdTurmaDisciplina
        WHERE m.IdTurma <> td.IdTurma
    )
    BEGIN
        THROW 50001,
            'A avaliacao nao pertence a turma do aluno.',
            1;
    END;

    -- Verificar se a nota ultrapassa
    -- o valor maximo da avaliacao
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
