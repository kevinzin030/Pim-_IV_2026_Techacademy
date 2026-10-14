
USE PIMIV_PainelADM;
GO

CREATE OR ALTER TRIGGER dbo.TR_Frequencias_Validar
ON dbo.Frequencias
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar se a aula pertence
    -- a turma da matricula
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Matriculas m
            ON i.IdMatricula = m.IdMatricula
        INNER JOIN dbo.Aulas a
            ON i.IdAula = a.IdAula
        INNER JOIN dbo.TurmaDisciplinas td
            ON a.IdTurmaDisciplina = td.IdTurmaDisciplina
        WHERE m.IdTurma <> td.IdTurma
    )
    BEGIN
        THROW 50003,
            'A aula nao pertence a turma do aluno.',
            1;
    END;
END;
GO
