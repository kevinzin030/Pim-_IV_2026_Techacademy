import sql from "mssql";

import { conectarBanco } from "../../Config/database.js";

const TIPOS_AVALIACAO = ["P1", "P2", "Trabalho"];

/**
 * Valida identificadores utilizados pelo SQL Server.
 */
function identificadorValido(valor) {
    return (
        Number.isInteger(valor) &&
        valor > 0 &&
        valor <= 2147483647
    );
}

/**
 * Confirma se o usuário possui o perfil Professor,
 * está ativo e não possui alteração de senha pendente.
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
                AND u.Ativo = 1
                AND u.DeveAlterarSenha = 0
                AND p.Ativo = 1
        `);

    const perfil = resultadoPerfil.recordset[0];

    return (
        typeof perfil?.NomePerfil === "string" &&
        perfil.NomePerfil.trim().toLowerCase() === "professor"
    );
}

/**
 * Autoriza o professor utilizando a identificação
 * presente no token JWT.
 */
async function autorizarProfessor(req, res, conexao) {
    const idUsuario = Number(req.usuario?.idUsuario);

    if (!identificadorValido(idUsuario)) {
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
            mensagem: "Acesso não autorizado. Verifique seu perfil e sua senha."
        });

        return null;
    }

    return idUsuario;
}

/**
 * Consulta as turmas e disciplinas vinculadas ao professor.
 *
 * Rota: GET /api/professores/me/turmas
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
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
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
 * Consulta os alunos matriculados nas turmas do professor.
 *
 * Rota: GET /api/professores/me/alunos
 *
 * O DISTINCT evita repetir alunos quando o professor
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
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
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
                    AND d.Ativa = 1
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
 * Consulta as notas dos alunos nas disciplinas do professor.
 *
 * Rota: GET /api/professores/me/notas
 *
 * Retorna os identificadores necessários para permitir
 * a edição das notas pela plataforma Web.
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
                    m.IdMatricula AS idMatricula,
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
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
 * Consulta presenças e faltas dos alunos.
 *
 * Rota: GET /api/professores/me/frequencias
 *
 * Considera somente aulas realizadas e matrículas ativas.
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
                    m.IdMatricula AS idMatricula,
                    td.IdTurmaDisciplina AS idTurmaDisciplina,
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

/**
 * Valida uma data no formato utilizado pelo banco.
 *
 * Retorna um objeto Date válido ou null.
 */
function converterDataAvaliacao(valor) {
    if (
        typeof valor !== "string" ||
        !/^\d{4}-\d{2}-\d{2}$/.test(valor)
    ) {
        return null;
    }

    const data = new Date(
        `${valor}T00:00:00.000Z`
    );

    if (
        Number.isNaN(data.getTime()) ||
        data.toISOString().slice(0, 10) !== valor ||
        data.getUTCFullYear() < 1
    ) {
        return null;
    }

    return data;
}

/**
 * Valida os dados necessários para lançar uma nota.
 */
function validarDadosNota(dados) {
    const {
        idMatricula,
        idTurmaDisciplina,
        tipoAvaliacao,
        valorNota,
        dataAvaliacao
    } = dados;

    if (
        !identificadorValido(idMatricula) ||
        !identificadorValido(idTurmaDisciplina)
    ) {
        return "Matrícula ou disciplina inválida.";
    }

    if (!TIPOS_AVALIACAO.includes(tipoAvaliacao)) {
        return "Selecione P1, P2 ou Trabalho.";
    }

    if (
        typeof valorNota !== "number" ||
        !Number.isFinite(valorNota) ||
        valorNota < 0 ||
        valorNota > 10 ||
        Math.abs(
            valorNota * 100 -
            Math.round(valorNota * 100)
        ) > 0.000001
    ) {
        return "A nota deve estar entre 0 e 10, com até duas casas decimais.";
    }

    if (!converterDataAvaliacao(dataAvaliacao)) {
        return "Informe uma data de avaliação válida.";
    }

    return null;
}

/**
 * Verifica se o professor possui acesso à disciplina
 * e se o aluno está matriculado na turma correspondente.
 */
async function verificarVinculoAcademico(
    transacao,
    idUsuario,
    idMatricula,
    idTurmaDisciplina
) {
    const resultado = await new sql.Request(transacao)
        .input("idUsuario", sql.Int, idUsuario)
        .input("idMatricula", sql.Int, idMatricula)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .query(`
            SELECT TOP (1)
                m.IdMatricula
            FROM dbo.Professores AS p
            INNER JOIN dbo.Usuarios AS u
                ON u.IdUsuario = p.IdUsuario
            INNER JOIN dbo.Perfis AS pe
                ON pe.IdPerfil = u.IdPerfil
            INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                ON ptd.IdProfessor = p.IdProfessor
            INNER JOIN dbo.TurmaDisciplinas AS td
                ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
            INNER JOIN dbo.Turmas AS t
                ON t.IdTurma = td.IdTurma
            INNER JOIN dbo.Disciplinas AS d
                ON d.IdDisciplina = td.IdDisciplina
            INNER JOIN dbo.Matriculas AS m
                ON m.IdTurma = t.IdTurma
            WHERE p.IdUsuario = @idUsuario
                AND td.IdTurmaDisciplina = @idTurmaDisciplina
                AND m.IdMatricula = @idMatricula
                AND u.Ativo = 1
                AND u.DeveAlterarSenha = 0
                AND pe.Ativo = 1
                AND LOWER(LTRIM(RTRIM(pe.NomePerfil))) = 'professor'
                AND ptd.Ativo = 1
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
                AND m.Situacao = 'Ativa'
        `);

    return resultado.recordset.length > 0;
}

/**
 * Consulta as avaliações existentes para determinada
 * turma, disciplina e tipo de avaliação.
 *
 * Os bloqueios evitam cadastros simultâneos duplicados.
 */
async function consultarAvaliacoes(
    transacao,
    idTurmaDisciplina,
    tipoAvaliacao
) {
    const resultado = await new sql.Request(transacao)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .input(
            "titulo",
            sql.NVarChar(150),
            tipoAvaliacao
        )
        .query(`
            SELECT
                IdAvaliacao,
                ValorMaximo,
                DataAvaliacao
            FROM dbo.Avaliacoes
                WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTurmaDisciplina = @idTurmaDisciplina
                AND Titulo = @titulo
                AND Ativa = 1
        `);

    return resultado.recordset;
}

/**
 * Cadastra uma avaliação quando ainda não existe
 * uma P1, P2 ou Trabalho para a disciplina.
 */
async function cadastrarAvaliacao(
    transacao,
    idTurmaDisciplina,
    tipoAvaliacao,
    dataAvaliacao
) {
    const resultado = await new sql.Request(transacao)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .input(
            "titulo",
            sql.NVarChar(150),
            tipoAvaliacao
        )
        .input(
            "dataAvaliacao",
            sql.Date,
            dataAvaliacao
        )
        .query(`
            INSERT INTO dbo.Avaliacoes (
                IdTurmaDisciplina,
                Titulo,
                DataAvaliacao,
                ValorMaximo,
                Ativa
            )
            OUTPUT
                INSERTED.IdAvaliacao,
                INSERTED.ValorMaximo,
                INSERTED.DataAvaliacao
            VALUES (
                @idTurmaDisciplina,
                @titulo,
                @dataAvaliacao,
                10,
                1
            )
        `);

    return resultado.recordset[0];
}

/**
 * Consulta notas já registradas para uma matrícula
 * e uma avaliação específica.
 */
async function consultarNotasExistentes(
    transacao,
    idMatricula,
    idAvaliacao
) {
    const resultado = await new sql.Request(transacao)
        .input("idMatricula", sql.Int, idMatricula)
        .input("idAvaliacao", sql.Int, idAvaliacao)
        .query(`
            SELECT
                IdNota
            FROM dbo.Notas
                WITH (UPDLOCK, HOLDLOCK)
            WHERE IdMatricula = @idMatricula
                AND IdAvaliacao = @idAvaliacao
        `);

    return resultado.recordset;
}

/**
 * Registra uma nova nota.
 *
 * Utiliza OUTPUT INTO para manter compatibilidade
 * com as triggers existentes na tabela Notas.
 */
async function cadastrarNota(
    transacao,
    idMatricula,
    idAvaliacao,
    valorNota
) {
    const resultado = await new sql.Request(transacao)
        .input("idMatricula", sql.Int, idMatricula)
        .input("idAvaliacao", sql.Int, idAvaliacao)
        .input("valorNota", sql.Decimal(5, 2), valorNota)
        .query(`
            DECLARE @NotasInseridas TABLE (
                IdNota INT
            );

            INSERT INTO dbo.Notas (
                IdMatricula,
                IdAvaliacao,
                ValorNota,
                DataLancamento
            )
            OUTPUT
                INSERTED.IdNota
            INTO @NotasInseridas
            VALUES (
                @idMatricula,
                @idAvaliacao,
                @valorNota,
                SYSUTCDATETIME()
            );

            SELECT
                IdNota
            FROM @NotasInseridas;
        `);

    return resultado.recordset[0].IdNota;
}

/**
 * Atualiza uma nota existente sem gerar duplicidade.
 */
async function atualizarNota(
    transacao,
    idNota,
    valorNota
) {
    await new sql.Request(transacao)
        .input("idNota", sql.Int, idNota)
        .input("valorNota", sql.Decimal(5, 2), valorNota)
        .query(`
            UPDATE dbo.Notas
            SET
                ValorNota = @valorNota,
                DataLancamento = SYSUTCDATETIME()
            WHERE IdNota = @idNota
        `);
}

/**
 * Registra ou atualiza a nota de um aluno.
 *
 * Rota: POST /api/professores/me/notas
 *
 * A transação garante a integridade entre a avaliação
 * e a nota registrada no banco de dados.
 */
export async function lancarNota(req, res) {
    let transacao = null;
    let transacaoAtiva = false;

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

        const dados = req.body ?? {};

        const erroValidacao = validarDadosNota(dados);

        if (erroValidacao) {
            return res.status(400).json({
                mensagem: erroValidacao
            });
        }

        const {
            idMatricula,
            idTurmaDisciplina,
            tipoAvaliacao,
            valorNota,
            dataAvaliacao
        } = dados;

        transacao = new sql.Transaction(conexao);

        await transacao.begin(
            sql.ISOLATION_LEVEL.SERIALIZABLE
        );

        transacaoAtiva = true;

        // Confirma o vínculo acadêmico antes da gravação.
        const vinculoValido = await verificarVinculoAcademico(
            transacao,
            idUsuario,
            idMatricula,
            idTurmaDisciplina
        );

        if (!vinculoValido) {
            await transacao.rollback();

            transacaoAtiva = false;

            return res.status(403).json({
                mensagem: "Você não possui acesso a esta matrícula."
            });
        }

        // Recupera a avaliação correspondente.
        const avaliacoes = await consultarAvaliacoes(
            transacao,
            idTurmaDisciplina,
            tipoAvaliacao
        );

        if (avaliacoes.length > 1) {
            await transacao.rollback();

            transacaoAtiva = false;

            return res.status(409).json({
                mensagem: "Existem avaliações duplicadas nesta disciplina."
            });
        }

        let avaliacao = avaliacoes[0];

        // Cria a avaliação somente quando necessário.
        if (!avaliacao) {
            avaliacao = await cadastrarAvaliacao(
                transacao,
                idTurmaDisciplina,
                tipoAvaliacao,
                converterDataAvaliacao(dataAvaliacao)
            );
        }

        // Garante a escala de notas utilizada pelo sistema.
        if (Number(avaliacao.ValorMaximo) !== 10) {
            await transacao.rollback();

            transacaoAtiva = false;

            return res.status(409).json({
                mensagem: "A avaliação existente não utiliza a escala de 0 a 10."
            });
        }

        const idAvaliacao = avaliacao.IdAvaliacao;

        const notasExistentes = await consultarNotasExistentes(
            transacao,
            idMatricula,
            idAvaliacao
        );

        if (notasExistentes.length > 1) {
            await transacao.rollback();

            transacaoAtiva = false;

            return res.status(409).json({
                mensagem: "Existem notas duplicadas para esta avaliação."
            });
        }

        const notaExistente = notasExistentes[0];

        let idNota;

        // Atualiza a nota quando já existe um registro.
        if (notaExistente) {
            idNota = notaExistente.IdNota;

            await atualizarNota(
                transacao,
                idNota,
                valorNota
            );

        } else {
            // Registra a primeira nota da avaliação.
            idNota = await cadastrarNota(
                transacao,
                idMatricula,
                idAvaliacao,
                valorNota
            );
        }

        await transacao.commit();

        transacaoAtiva = false;

        return res.status(
            notaExistente ? 200 : 201
        ).json({
            mensagem: notaExistente
                ? "Nota atualizada com sucesso."
                : "Nota lançada com sucesso.",

            idNota,
            idMatricula,
            idTurmaDisciplina,
            idAvaliacao,
            tipoAvaliacao,
            valorNota,
            dataAvaliacao: avaliacao.DataAvaliacao
        });

    } catch (erro) {
        if (transacao && transacaoAtiva) {
            try {
                await transacao.rollback();

            } catch (erroRollback) {
                console.error(
                    "Erro ao desfazer transação:",
                    erroRollback.message
                );
            }
        }

        console.error(
            "Erro ao lançar nota:",
            erro.message
        );

        // Trata violações de unicidade do SQL Server.
        if (
            erro.number === 2601 ||
            erro.number === 2627
        ) {
            return res.status(409).json({
                mensagem: "Já existe um registro correspondente."
            });
        }

        // Trata violações das regras acadêmicas do banco.
        if (
            erro.number === 50000 ||
            erro.number === 547
        ) {
            return res.status(422).json({
                mensagem: "A nota não atende às regras acadêmicas do banco de dados."
            });
        }

        return res.status(500).json({
            mensagem: "Não foi possível lançar a nota."
        });
    }
}