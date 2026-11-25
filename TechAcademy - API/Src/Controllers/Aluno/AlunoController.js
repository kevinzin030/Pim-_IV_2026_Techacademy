
import sql from "mssql";

import { conectarBanco } from "../../Config/database.js";

// Confirma se o usuário autenticado possui o perfil Aluno.
async function verificarPerfilAluno(conexao, idUsuario) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT
                p.NomePerfil
            FROM dbo.Usuarios AS u
            INNER JOIN dbo.Perfis AS p
                ON p.IdPerfil = u.IdPerfil
            WHERE u.IdUsuario = @idUsuario
        `);

    const perfil = resultado.recordset[0];

    return Boolean(
        perfil &&
        typeof perfil.NomePerfil === "string" &&
        perfil.NomePerfil.trim().toLowerCase() === "aluno"
    );
}

// Consulta exclusivamente as notas do aluno autenticado.
export async function consultarMinhasNotas(req, res) {
    try {
        // Obtém a identificação validada pelo middleware JWT.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece a conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Impede que outros perfis consultem informações de alunos.
        const alunoAutorizado = await verificarPerfilAluno(
            conexao,
            idUsuario
        );

        if (!alunoAutorizado) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Recupera as notas vinculadas às matrículas do aluno.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                              SELECT
                    n.IdNota AS idNota,
                    n.ValorNota AS valorNota,
                    n.DataLancamento AS dataLancamento,
                    a.IdAvaliacao AS idAvaliacao,
                    a.Titulo AS avaliacao,
                    a.DataAvaliacao AS dataAvaliacao,
                    a.ValorMaximo AS valorMaximo,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS disciplina,
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
                    t.IdTurma AS idTurma,
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
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                WHERE al.IdUsuario = @idUsuario
                    AND a.Ativa = 1
                ORDER BY
                    d.NomeDisciplina ASC,
                    a.DataAvaliacao DESC
            `);

        // Mantém o formato de resposta utilizado pelo Mobile.
        return res.status(200).json({
            total: resultado.recordset.length,
            notas: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar notas do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as notas."
        });
    }
}

// Consulta os registros de frequência do aluno autenticado.
export async function consultarMinhasFrequencias(req, res) {
    try {
        // Recupera o identificador do usuário autenticado.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o banco de dados.
        const conexao = await conectarBanco();

        // Verifica se o usuário pertence ao perfil Aluno.
        const alunoAutorizado = await verificarPerfilAluno(
            conexao,
            idUsuario
        );

        if (!alunoAutorizado) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Consulta os registros das matrículas pertencentes ao aluno.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    f.IdFrequencia AS idFrequencia,
                    f.IdAula AS idAula,
                    f.Presente AS presente,
                    f.DataRegistro AS dataRegistro,
                    a.DataAula AS dataAula,
                    a.Conteudo AS conteudo,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS disciplina,
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS turma,
                    m.Situacao AS situacaoMatricula
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Frequencias AS f
                    ON f.IdMatricula = m.IdMatricula
                INNER JOIN dbo.Aulas AS a
                    ON a.IdAula = f.IdAula
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                WHERE al.IdUsuario = @idUsuario
                    AND a.Realizada = 1
                ORDER BY
                    d.NomeDisciplina ASC,
                    a.DataAula DESC
            `);

        // Calcula os totais a partir dos registros retornados.
        const frequencias = resultado.recordset;

        const totalPresencas = frequencias.filter(
            frequencia => frequencia.presente === true
        ).length;

        const totalFaltas = frequencias.filter(
            frequencia => frequencia.presente === false
        ).length;

        // Retorna os registros e o resumo da frequência.
        return res.status(200).json({
            totalRegistros: frequencias.length,
            totalPresencas,
            totalFaltas,
            frequencias
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar frequências do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as frequências."
        });
    }
}

// Consulta as disciplinas vinculadas às matrículas do aluno autenticado.
export async function consultarMinhasDisciplinas(req, res) {
    try {
        // Utiliza a identificação do token, nunca um ID enviado pelo cliente.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Restringe o acesso ao perfil Aluno.
        const alunoAutorizado = await verificarPerfilAluno(
            conexao,
            idUsuario
        );

        if (!alunoAutorizado) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Relaciona aluno, matrícula, turma e disciplinas da turma.
        // DISTINCT evita registros repetidos do mesmo vínculo acadêmico.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT DISTINCT
                    d.IdDisciplina AS idDisciplina,
                    d.CodigoDisciplina AS codigoDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    d.CargaHoraria AS cargaHoraria,
                    t.IdTurma AS idTurma,
                    t.CodigoTurma AS codigoTurma,
                    t.NomeTurma AS nomeTurma,
                    m.Situacao AS situacaoMatricula
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = m.IdTurma
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurma = t.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE al.IdUsuario = @idUsuario
                    AND t.Ativa = 1
                    AND td.Ativa = 1
                    AND d.Ativa = 1
                ORDER BY
                    d.NomeDisciplina ASC,
                    t.NomeTurma ASC
            `);

        // Retorna apenas disciplinas vinculadas ao aluno autenticado.
        return res.status(200).json({
            total: resultado.recordset.length,
            disciplinas: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar disciplinas do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as disciplinas do aluno."
        });
    }
}

// Consulta as turmas vinculadas às matrículas do aluno autenticado.
export async function consultarMinhaTurma(req, res) {
    try {
        // Recupera a identificação do usuário autenticado.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Impede a consulta por usuários de outros perfis.
        const alunoAutorizado = await verificarPerfilAluno(
            conexao,
            idUsuario
        );

        if (!alunoAutorizado) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para alunos."
            });
        }

        // Consulta as turmas das matrículas pertencentes ao aluno.
        // A situação da matrícula é mantida para distinguir vínculos.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    m.IdMatricula AS idMatricula,
                    m.Situacao AS situacaoMatricula,
                    t.IdTurma AS idTurma,
                    t.CodigoTurma AS codigoTurma,
                    t.NomeTurma AS nomeTurma,
                    t.Periodo AS periodo,
                    t.AnoLetivo AS anoLetivo,
                    t.Semestre AS semestre
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = m.IdTurma
                WHERE al.IdUsuario = @idUsuario
                ORDER BY
                    t.AnoLetivo DESC,
                    t.Semestre DESC,
                    t.NomeTurma ASC
            `);

        // Retorna todas as matrículas encontradas, inclusive históricas.
        // O Mobile poderá identificar a situação de cada vínculo.
        return res.status(200).json({
            total: resultado.recordset.length,
            turmas: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar turmas do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as turmas do aluno."
        });
    }
}
