
import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

// Consulta exclusivamente as notas do aluno autenticado.
export async function consultarMinhasNotas(req, res) {
    try {
        // Recupera a identificação do usuário validada pelo JWT.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Confirma que o usuário pertence ao perfil Aluno.
        const resultadoPerfil = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    u.IdUsuario,
                    p.NomePerfil
                FROM dbo.Usuarios AS u
                INNER JOIN dbo.Perfis AS p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @idUsuario
            `);

        const perfil = resultadoPerfil.recordset[0];

        if (
            !perfil ||
            perfil.NomePerfil.trim().toLowerCase() !== "aluno"
        ) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Localiza as matrículas e notas vinculadas ao usuário autenticado.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    n.IdNota AS idNota,
                    n.ValorNota AS valorNota,
                    n.DataLancamento AS dataLancamento,
                    a.Titulo AS avaliacao,
                    a.DataAvaliacao AS dataAvaliacao,
                    a.ValorMaximo AS valorMaximo,
                    d.NomeDisciplina AS disciplina,
                    t.NomeTurma AS turma,
                    m.Situacao AS situacaoMatricula
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Notas AS n
                    ON n.IdMatricula = m.IdMatricula
                INNER JOIN dbo.Avaliacoes AS a
                    ON a.IdAvaliacao = n.IdAvaliacao
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
                    AND td.IdTurma = m.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = m.IdTurma
                WHERE al.IdUsuario = @idUsuario
                ORDER BY
                    d.NomeDisciplina ASC,
                    a.DataAvaliacao DESC
            `);

        // Retorna somente as notas pertencentes ao aluno autenticado.
        return res.status(200).json({
            total: resultado.recordset.length,
            notas: resultado.recordset
        });

    } catch (erro) {
        // Registra o erro sem expor informações internas do banco.
        console.error(
            "Erro ao consultar notas do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as notas."
        });
    }
}

/**
 * Consulta as frequências do aluno autenticado.
 * Retorna os registros de presença e ausência vinculados às suas matrículas.
 */
export async function consultarMinhasFrequencias(req, res) {
    try {
        // Recupera a identificação do usuário autenticado.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Verifica se o usuário possui o perfil Aluno.
        const resultadoPerfil = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    p.NomePerfil
                FROM dbo.Usuarios AS u
                INNER JOIN dbo.Perfis AS p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @idUsuario
            `);

        const perfil = resultadoPerfil.recordset[0];

        if (
            !perfil ||
            perfil.NomePerfil.trim().toLowerCase() !== "aluno"
        ) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Consulta as frequências vinculadas às matrículas do aluno.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    f.IdFrequencia AS idFrequencia,
                    f.IdAula AS idAula,
                    f.Presente AS presente,
                    f.DataRegistro AS dataRegistro,
                    t.NomeTurma AS turma,
                    m.Situacao AS situacaoMatricula
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Frequencias AS f
                    ON f.IdMatricula = m.IdMatricula
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = m.IdTurma
                WHERE al.IdUsuario = @idUsuario
                ORDER BY f.DataRegistro DESC
            `);

        // Calcula os totais de presenças e faltas.
        const frequencias = resultado.recordset;

        const totalPresencas = frequencias.filter(
            frequencia => frequencia.presente === true
        ).length;

        const totalFaltas = frequencias.filter(
            frequencia => frequencia.presente === false
        ).length;

        // Retorna somente os registros pertencentes ao aluno.
        return res.status(200).json({
            totalRegistros: frequencias.length,
            totalPresencas,
            totalFaltas,
            frequencias
        });

    } catch (erro) {
        // Registra o erro sem expor detalhes internos do banco.
        console.error(
            "Erro ao consultar frequências do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as frequências."
        });
    }
}
