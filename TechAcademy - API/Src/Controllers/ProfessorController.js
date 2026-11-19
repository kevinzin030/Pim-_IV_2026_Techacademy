
import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

/**
 * Confirma se o usuário autenticado possui o perfil Professor.
 * A identificação é obtida pelo JWT, sem aceitar um ID enviado pelo aplicativo.
 */
async function verificarPerfilProfessor(conexao, idUsuario) {
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

    return (
        typeof perfil?.NomePerfil === "string" &&
        perfil.NomePerfil.trim().toLowerCase() === "professor"
    );
}

/**
 * Retorna um erro de acesso quando o usuário não é Professor.
 */
async function autorizarProfessor(req, res, conexao) {
    const idUsuario = Number(req.usuario?.idUsuario);

    if (!Number.isInteger(idUsuario) || idUsuario <= 0) {
        res.status(401).json({
            mensagem: "Usuário não autenticado."
        });

        return null;
    }

    const autorizado = await verificarPerfilProfessor(
        conexao,
        idUsuario
    );

    if (!autorizado) {
        res.status(403).json({
            mensagem: "Acesso permitido somente para professores."
        });

        return null;
    }

    return idUsuario;
}

/**
 * Consulta as turmas e disciplinas vinculadas ao Professor.
 *
 * Rota: GET /api/professores/me/turmas
 *
 * Mantém o formato de resposta já utilizado pelo Mobile.
 */
export async function consultarMinhasTurmas(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarProfessor(
            req,
            res,
            conexao
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    t.IdTurma AS idTurma,
                    t.CodigoTurma AS codigoTurma,
                    t.NomeTurma AS nomeTurma,
                    t.Periodo AS periodo,
                    t.AnoLetivo AS anoLetivo,
                    t.Semestre AS semestre,
                    d.IdDisciplina AS idDisciplina,
                    d.CodigoDisciplina AS codigoDisciplina,
                    d.NomeDisciplina AS nomeDisciplina
                FROM dbo.Professores AS p
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdProfessor = p.IdProfessor
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE p.IdUsuario = @idUsuario
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                ORDER BY
                    t.NomeTurma ASC,
                    d.NomeDisciplina ASC
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            turmas: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar turmas do professor:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as turmas do professor."
        });
    }
}

/**
 * Consulta os alunos matriculados nas turmas do Professor.
 *
 * Rota: GET /api/professores/me/alunos
 *
 * O DISTINCT evita repetir um aluno quando o Professor
 * leciona mais de uma disciplina na mesma turma.
 */
export async function consultarMeusAlunos(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarProfessor(
            req,
            res,
            conexao
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT DISTINCT
                    al.IdAluno AS idAluno,
                    al.MatriculaAluno AS matriculaAluno,
                    u.NomeCompleto AS nomeCompleto,
                    m.IdMatricula AS idMatricula,
                    m.Situacao AS situacaoMatricula,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma,
                    t.CodigoTurma AS codigoTurma
                FROM dbo.Professores AS p
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdProfessor = p.IdProfessor
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdTurma = t.IdTurma
                INNER JOIN dbo.Alunos AS al
                    ON al.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios AS u
                    ON u.IdUsuario = al.IdUsuario
                WHERE p.IdUsuario = @idUsuario
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND m.Situacao = 'Ativa'
                ORDER BY
                    t.NomeTurma,
                    u.NomeCompleto
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            alunos: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar alunos do professor:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar os alunos do professor."
        });
    }
}

/**
 * Consulta as notas dos alunos nas disciplinas do Professor.
 *
 * Rota: GET /api/professores/me/notas
 *
 * A consulta usa a matrícula e a avaliação para garantir
 * que cada nota pertence à turma e disciplina corretas.
 */
export async function consultarNotasDosAlunos(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarProfessor(
            req,
            res,
            conexao
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    n.IdNota AS idNota,
                    n.ValorNota AS valorNota,
                    n.DataLancamento AS dataLancamento,
                    av.IdAvaliacao AS idAvaliacao,
                    av.Titulo AS tituloAvaliacao,
                    av.DataAvaliacao AS dataAvaliacao,
                    av.ValorMaximo AS valorMaximo,
                    al.IdAluno AS idAluno,
                    al.MatriculaAluno AS matriculaAluno,
                    u.NomeCompleto AS nomeAluno,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma
                FROM dbo.Professores AS p
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdProfessor = p.IdProfessor
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Avaliacoes AS av
                    ON av.IdTurmaDisciplina = td.IdTurmaDisciplina
                INNER JOIN dbo.Notas AS n
                    ON n.IdAvaliacao = av.IdAvaliacao
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdMatricula = n.IdMatricula
                    AND m.IdTurma = t.IdTurma
                INNER JOIN dbo.Alunos AS al
                    ON al.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios AS u
                    ON u.IdUsuario = al.IdUsuario
                WHERE p.IdUsuario = @idUsuario
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                    AND av.Ativa = 1
                    AND m.Situacao = 'Ativa'
                ORDER BY
                    d.NomeDisciplina,
                    u.NomeCompleto,
                    av.DataAvaliacao DESC
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            notas: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar notas dos alunos:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as notas dos alunos."
        });
    }
}

/**
 * Consulta presenças e faltas dos alunos nas disciplinas do Professor.
 *
 * Rota: GET /api/professores/me/frequencias
 *
 * Cada registro informa se o aluno esteve presente
 * em uma aula específica.
 */
export async function consultarFrequenciasDosAlunos(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarProfessor(
            req,
            res,
            conexao
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    f.IdFrequencia AS idFrequencia,
                    f.Presente AS presente,
                    f.DataRegistro AS dataRegistro,
                    a.IdAula AS idAula,
                    a.DataAula AS dataAula,
                    a.Conteudo AS conteudoAula,
                    al.IdAluno AS idAluno,
                    al.MatriculaAluno AS matriculaAluno,
                    u.NomeCompleto AS nomeAluno,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma
                FROM dbo.Professores AS p
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdProfessor = p.IdProfessor
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Aulas AS a
                    ON a.IdTurmaDisciplina = td.IdTurmaDisciplina
                INNER JOIN dbo.Frequencias AS f
                    ON f.IdAula = a.IdAula
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdMatricula = f.IdMatricula
                    AND m.IdTurma = t.IdTurma
                INNER JOIN dbo.Alunos AS al
                    ON al.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios AS u
                    ON u.IdUsuario = al.IdUsuario
                WHERE p.IdUsuario = @idUsuario
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                    AND a.Realizada = 1
                    AND m.Situacao = 'Ativa'
                ORDER BY
                    a.DataAula DESC,
                    u.NomeCompleto
            `);

        const frequencias = resultado.recordset;

        return res.status(200).json({
            total: frequencias.length,
            totalPresencas: frequencias.filter(
                registro => registro.presente === true
            ).length,
            totalFaltas: frequencias.filter(
                registro => registro.presente === false
            ).length,
            frequencias
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar frequências dos alunos:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as frequências dos alunos."
        });
    }
}
