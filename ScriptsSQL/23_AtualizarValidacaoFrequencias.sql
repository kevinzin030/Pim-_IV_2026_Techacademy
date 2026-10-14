
USE PIMIV_PainelADM;
GO

CREATE OR ALTER TRIGGER dbo.TR_Notas_Validar
ON dbo.Notas
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
