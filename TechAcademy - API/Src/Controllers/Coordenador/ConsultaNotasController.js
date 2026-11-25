import sql from "mssql";
import { conectarBanco } from "../../Config/database.js";

// Verifica a autorização do coordenador.
async function possuiAutorizacao(conexao, idUsuario) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT TOP 1 1 AS Autorizado
            FROM dbo.Usuarios u
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            INNER JOIN dbo.PerfilPermissoes pp
                ON pp.IdPerfil = p.IdPerfil
            INNER JOIN dbo.Permissoes pm
                ON pm.IdPermissao = pp.IdPermissao
            WHERE u.IdUsuario = @idUsuario
                AND u.Ativo = 1
                AND u.DeveAlterarSenha = 0
                AND p.Ativo = 1
                AND p.NomePerfil = 'Coordenador'
                AND pm.Ativo = 1
                AND pm.Codigo = 'CONSULTAR_NOTAS';
        `);

    return resultado.recordset.length > 0;
}

// Valida os identificadores utilizados nos filtros.
function filtroValido(valor) {
    return Number.isInteger(valor)
        && valor >= 0
        && valor <= 2147483647;
}

// Disponibiliza os filtros acadêmicos.
export async function listarOpcoesConsultaNotas(req, res) {
    try {
        const conexao = await conectarBanco();

        const autorizado = await possuiAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        if (!autorizado) {
            return res.status(403).json({
                mensagem: "Você não possui permissão para consultar notas."
            });
        }

        const resultado = await conexao.request()
            .query(`
                SELECT
                    IdTurma,
                    NomeTurma
                FROM dbo.Turmas
                ORDER BY NomeTurma;

                SELECT
                    IdDisciplina,
                    NomeDisciplina
                FROM dbo.Disciplinas
                ORDER BY NomeDisciplina;

                SELECT
                    a.IdAluno,
                    u.NomeCompleto
                FROM dbo.Alunos a
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                ORDER BY u.NomeCompleto;
            `);

        return res.status(200).json({
            turmas: resultado.recordsets[0],
            disciplinas: resultado.recordsets[1],
            alunos: resultado.recordsets[2]
        });

    } catch (erro) {
        console.error(
            "Erro ao carregar filtros de notas:",
            erro
        );

        return res.status(500).json({
            mensagem: "Não foi possível carregar os filtros."
        });
    }
}

// Consulta as notas atuais e anteriores dos alunos.
export async function consultarNotasCoordenador(req, res) {
    try {
        const idTurma = Number(req.query.idTurma ?? 0);

        const idDisciplina = Number(
            req.query.idDisciplina ?? 0
        );

        const idAluno = Number(req.query.idAluno ?? 0);

        if (
            !filtroValido(idTurma) ||
            !filtroValido(idDisciplina) ||
            !filtroValido(idAluno)
        ) {
            return res.status(400).json({
                mensagem: "Filtros de consulta inválidos."
            });
        }

        const conexao = await conectarBanco();

        const autorizado = await possuiAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        if (!autorizado) {
            return res.status(403).json({
                mensagem: "Você não possui permissão para consultar notas."
            });
        }

        // Utiliza a turma da avaliação, preservando as notas
        // obtidas antes de eventuais transferências.
        const resultado = await conexao.request()
            .input("IdTurma", sql.Int, idTurma)
            .input("IdDisciplina", sql.Int, idDisciplina)
            .input("IdAluno", sql.Int, idAluno)
            .query(`
                SELECT
                    u.NomeCompleto AS Aluno,
                    a.MatriculaAluno AS RA,
                    t.NomeTurma AS Turma,
                    d.NomeDisciplina AS Disciplina,
                    av.Titulo AS Avaliacao,
                    n.ValorNota AS Nota,
                    av.ValorMaximo,
                    n.DataLancamento
                FROM dbo.Notas n
                INNER JOIN dbo.Matriculas m
                    ON m.IdMatricula = n.IdMatricula
                INNER JOIN dbo.Alunos a
                    ON a.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                INNER JOIN dbo.Avaliacoes av
                    ON av.IdAvaliacao = n.IdAvaliacao
                INNER JOIN dbo.TurmaDisciplinas td
                    ON td.IdTurmaDisciplina =
                        av.IdTurmaDisciplina
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE
                    (
                        @IdTurma = 0
                        OR t.IdTurma = @IdTurma
                    )
                    AND (
                        @IdDisciplina = 0
                        OR d.IdDisciplina = @IdDisciplina
                    )
                    AND (
                        @IdAluno = 0
                        OR a.IdAluno = @IdAluno
                    )
                ORDER BY
                    t.NomeTurma,
                    u.NomeCompleto,
                    d.NomeDisciplina,
                    av.DataAvaliacao;
            `);

        return res.status(200).json({
            notas: resultado.recordset,
            total: resultado.recordset.length
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar notas:",
            erro
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as notas."
        });
    }
}