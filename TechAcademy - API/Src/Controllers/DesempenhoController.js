import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

/**
 * Consulta o desempenho individual dos alunos nos quizzes.
 *
 * Considera somente quizzes ativos e disciplinas
 * vinculadas ao professor autenticado.
 */
export async function consultarDesempenhoQuizzes(req, res) {
    try {
        const idUsuario = Number(req.usuario?.idUsuario);

        if (
            !Number.isInteger(idUsuario) ||
            idUsuario <= 0 ||
            idUsuario > 2147483647
        ) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        // Confirma a autorização do professor.
        const autorizacao = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    p.IdProfessor
                FROM dbo.Professores AS p
                INNER JOIN dbo.Usuarios AS u
                    ON u.IdUsuario = p.IdUsuario
                INNER JOIN dbo.Perfis AS pe
                    ON pe.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @idUsuario
                    AND u.Ativo = 1
                    AND u.DeveAlterarSenha = 0
                    AND pe.Ativo = 1
                    AND LOWER(LTRIM(RTRIM(pe.NomePerfil))) = 'professor'
            `);

        if (!autorizacao.recordset.length) {
            return res.status(403).json({
                mensagem: "Acesso não autorizado."
            });
        }

        const idProfessor =
            autorizacao.recordset[0].IdProfessor;

        // Agrupa os resultados por aluno e disciplina.
        const resultado = await conexao.request()
            .input("idProfessor", sql.Int, idProfessor)
            .query(`
                SELECT
                    al.IdAluno AS idAluno,
                    m.IdMatricula AS idMatricula,
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
                    COUNT(DISTINCT qr.IdQuiz) AS totalQuizzes,
                    SUM(qr.Acertos) AS totalAcertos,
                    SUM(qr.TotalPerguntas) AS totalPerguntas,
                    CAST(
                        SUM(qr.Acertos) * 10.0 /
                        NULLIF(SUM(qr.TotalPerguntas), 0)
                        AS DECIMAL(5, 2)
                    ) AS notaQuizzes
                FROM dbo.QuizResultados AS qr
                INNER JOIN dbo.Quizzes AS q
                    ON q.IdQuiz = qr.IdQuiz
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = q.IdTurmaDisciplina
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdTurmaDisciplina = td.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Alunos AS al
                    ON al.IdAluno = qr.IdAluno
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                    AND m.IdTurma = td.IdTurma
                WHERE ptd.IdProfessor = @idProfessor
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                    AND q.Ativo = 1
                    AND m.Situacao = 'Ativa'
                GROUP BY
                    al.IdAluno,
                    m.IdMatricula,
                    td.IdTurmaDisciplina
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            desempenhos: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar desempenho dos quizzes:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar o desempenho."
        });
    }
}