import sql from "mssql";
import { randomUUID } from "node:crypto";

import { conectarBanco } from "../../Config/database.js";

// Configurações utilizadas na validação dos quizzes.
const XP_MINIMO = 10;
const XP_MAXIMO = 1000;
const INTERVALO_XP = 10;

// Configurações de progressão do aluno.
const XP_POR_NIVEL = 1000;
const NIVEL_MAXIMO = 50;
const XP_TOTAL_MAXIMO = (NIVEL_MAXIMO - 1) * XP_POR_NIVEL;

// Limites de perguntas e alternativas.
const MAXIMO_PERGUNTAS = 50;
const MINIMO_ALTERNATIVAS = 2;
const MAXIMO_ALTERNATIVAS = 6;

/**
 * Verifica se um valor representa um identificador válido
 * para uma coluna INT do SQL Server.
 */
function identificadorValido(valor) {
    const identificador = Number(valor);

    return (
        Number.isInteger(identificador) &&
        identificador > 0 &&
        identificador <= 2147483647
    );
}

/**
 * Identifica o usuário autenticado pelo token JWT.
 *
 * Confirma o perfil, a situação da conta e a necessidade
 * de alteração obrigatória da senha diretamente no banco.
 */
async function autorizarUsuario(req, res, conexao, nomePerfil) {
    const idUsuario = Number(req.usuario?.idUsuario);

    if (!identificadorValido(idUsuario)) {
        res.status(401).json({
            mensagem: "Usuário não autenticado."
        });

        return null;
    }

    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT
                u.IdUsuario,
                u.Ativo,
                u.DeveAlterarSenha,
                p.NomePerfil,
                p.Ativo AS PerfilAtivo
            FROM dbo.Usuarios AS u
            INNER JOIN dbo.Perfis AS p
                ON p.IdPerfil = u.IdPerfil
            WHERE u.IdUsuario = @idUsuario
        `);

    const usuario = resultado.recordset[0];

    if (
        !usuario ||
        !usuario.Ativo ||
        !usuario.PerfilAtivo
    ) {
        res.status(403).json({
            mensagem: "Usuário ou perfil inativo."
        });

        return null;
    }

    if (
        usuario.NomePerfil.trim().toLowerCase() !==
        nomePerfil.toLowerCase()
    ) {
        res.status(403).json({
            mensagem: "Perfil sem permissão para esta operação."
        });

        return null;
    }

    if (usuario.DeveAlterarSenha) {
        res.status(403).json({
            mensagem: "Altere sua senha antes de continuar.",
            deveAlterarSenha: true
        });

        return null;
    }

    return idUsuario;
}

/**
 * Verifica se o professor possui vínculo ativo
 * com a turma e a disciplina informadas.
 */
async function verificarVinculoProfessor(
    conexao,
    idUsuario,
    idTurmaDisciplina
) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .query(`
            SELECT
                p.IdProfessor
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
                AND td.IdTurmaDisciplina = @idTurmaDisciplina
                AND ptd.Ativo = 1
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
        `);

    return resultado.recordset[0]?.IdProfessor ?? null;
}

/**
 * Verifica se o aluno possui matrícula ativa
 * na turma relacionada ao quiz.
 */
async function verificarVinculoAluno(
    conexao,
    idUsuario,
    idTurmaDisciplina
) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .query(`
            SELECT
                al.IdAluno
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
                AND td.IdTurmaDisciplina = @idTurmaDisciplina
                AND m.Situacao = 'Ativa'
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
        `);

    return resultado.recordset[0]?.IdAluno ?? null;
}

/**
 * Valida os dados enviados pelo professor
 * antes de iniciar o cadastro do quiz.
 */
function validarDadosQuiz(dados) {
    if (!dados || typeof dados !== "object") {
        return "Dados do quiz não informados.";
    }

    const {
        idTurmaDisciplina,
        idAula,
        titulo,
        xpMaximo,
        perguntas
    } = dados;

    if (!identificadorValido(idTurmaDisciplina)) {
        return "Selecione uma turma e disciplina válidas.";
    }

    if (
        idAula !== null &&
        idAula !== undefined &&
        idAula !== "" &&
        !identificadorValido(idAula)
    ) {
        return "A aula informada é inválida.";
    }

    if (
        typeof titulo !== "string" ||
        titulo.trim().length === 0 ||
        titulo.trim().length > 300
    ) {
        return "O título deve possuir entre 1 e 300 caracteres.";
    }

    const quantidadeXP = Number(xpMaximo);

    if (
        !Number.isInteger(quantidadeXP) ||
        quantidadeXP < XP_MINIMO ||
        quantidadeXP > XP_MAXIMO ||
        quantidadeXP % INTERVALO_XP !== 0
    ) {
        return "O XP deve estar entre 10 e 1000, em múltiplos de 10.";
    }

    if (
        !Array.isArray(perguntas) ||
        perguntas.length === 0 ||
        perguntas.length > MAXIMO_PERGUNTAS
    ) {
        return "O quiz deve possuir entre 1 e 50 perguntas.";
    }

    for (const pergunta of perguntas) {
        if (
            typeof pergunta?.enunciado !== "string" ||
            pergunta.enunciado.trim().length === 0 ||
            pergunta.enunciado.length > 10000
        ) {
            return "Todas as perguntas devem possuir um enunciado válido.";
        }

        if (
            !Array.isArray(pergunta.alternativas) ||
            pergunta.alternativas.length < MINIMO_ALTERNATIVAS ||
            pergunta.alternativas.length > MAXIMO_ALTERNATIVAS
        ) {
            return "Cada pergunta deve possuir entre 2 e 6 alternativas.";
        }

        let quantidadeCorretas = 0;

        for (const alternativa of pergunta.alternativas) {
            if (
                typeof alternativa?.texto !== "string" ||
                alternativa.texto.trim().length === 0 ||
                alternativa.texto.length > 1000
            ) {
                return "Todas as alternativas devem possuir um texto válido.";
            }

            if (typeof alternativa.correta !== "boolean") {
                return "Informe se cada alternativa está correta ou incorreta.";
            }

            if (alternativa.correta === true) {
                quantidadeCorretas++;
            }
        }

        if (quantidadeCorretas !== 1) {
            return "Cada pergunta deve possuir exatamente uma alternativa correta.";
        }
    }

    return null;
}

/**
 * Consulta as informações principais de um quiz.
 */
async function buscarQuiz(conexao, idQuiz) {
    const resultado = await conexao.request()
        .input("idQuiz", sql.NVarChar(200), idQuiz)
        .query(`
            SELECT
                IdQuiz,
                IdTurmaDisciplina,
                IdProfessor,
                IdAula,
                Titulo,
                XPMaximo,
                Ativo
            FROM dbo.Quizzes
            WHERE IdQuiz = @idQuiz
        `);

    return resultado.recordset[0] ?? null;
}

/**
 * Cancela uma transação quando ocorre uma falha.
 *
 * O tratamento evita que uma transação já encerrada
 * gere um segundo erro durante o rollback.
 */
async function cancelarTransacao(transacao) {
    try {
        await transacao.rollback();
    } catch (erro) {
        console.error(
            "Não foi possível cancelar a transação:",
            erro.message
        );
    }
}

/**
 * Cadastra um novo quiz, suas perguntas e alternativas.
 *
 * Rota: POST /api/professores/me/quizzes
 *
 * Utiliza uma transação para garantir que todos os dados
 * sejam cadastrados juntos ou cancelados em caso de erro.
 */
export async function criarQuiz(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Professor"
        );

        if (idUsuario === null) {
            return;
        }

        const erroValidacao = validarDadosQuiz(req.body);

        if (erroValidacao) {
            return res.status(400).json({
                mensagem: erroValidacao
            });
        }

        const {
            idTurmaDisciplina,
            idAula,
            titulo,
            xpMaximo,
            perguntas
        } = req.body;

        const idProfessor = await verificarVinculoProfessor(
            conexao,
            idUsuario,
            Number(idTurmaDisciplina)
        );

        if (idProfessor === null) {
            return res.status(403).json({
                mensagem: "Professor não vinculado à turma e disciplina."
            });
        }

        // A associação com uma aula é opcional.
        const idAulaValidado = (
            idAula === null ||
            idAula === undefined ||
            idAula === ""
        )
            ? null
            : Number(idAula);

        // Confirma que a aula pertence à turma e disciplina.
        if (idAulaValidado !== null) {
            const resultadoAula = await conexao.request()
                .input("idAula", sql.Int, idAulaValidado)
                .input(
                    "idTurmaDisciplina",
                    sql.Int,
                    Number(idTurmaDisciplina)
                )
                .query(`
                    SELECT
                        IdAula
                    FROM dbo.Aulas
                    WHERE IdAula = @idAula
                        AND IdTurmaDisciplina = @idTurmaDisciplina
                `);

            if (resultadoAula.recordset.length === 0) {
                return res.status(400).json({
                    mensagem: "A aula não pertence à turma e disciplina."
                });
            }
        }

        // Gera um identificador único para o quiz.
        const idQuiz = randomUUID();

        const transacao = new sql.Transaction(conexao);

        await transacao.begin();

        try {
            // Cadastra as informações principais do quiz.
            await new sql.Request(transacao)
                .input("idQuiz", sql.NVarChar(200), idQuiz)
                .input(
                    "idTurmaDisciplina",
                    sql.Int,
                    Number(idTurmaDisciplina)
                )
                .input("idProfessor", sql.Int, idProfessor)
                .input("idAula", sql.Int, idAulaValidado)
                .input("titulo", sql.NVarChar(300), titulo.trim())
                .input("xpMaximo", sql.Int, Number(xpMaximo))
                .query(`
                    INSERT INTO dbo.Quizzes (
                        IdQuiz,
                        IdTurmaDisciplina,
                        IdProfessor,
                        IdAula,
                        Titulo,
                        XPMaximo,
                        Ativo
                    )
                    VALUES (
                        @idQuiz,
                        @idTurmaDisciplina,
                        @idProfessor,
                        @idAula,
                        @titulo,
                        @xpMaximo,
                        1
                    )
                `);

            // Cadastra cada pergunta respeitando sua ordem.
            for (const [indice, pergunta] of perguntas.entries()) {
                const resultadoPergunta = await new sql.Request(
                    transacao
                )
                    .input("idQuiz", sql.NVarChar(200), idQuiz)
                    .input(
                        "enunciado",
                        sql.NVarChar(sql.MAX),
                        pergunta.enunciado.trim()
                    )
                    .input("ordem", sql.Int, indice + 1)
                    .query(`
                        INSERT INTO dbo.QuizPerguntas (
                            IdQuiz,
                            Enunciado,
                            Ordem
                        )
                        OUTPUT INSERTED.IdPergunta
                        VALUES (
                            @idQuiz,
                            @enunciado,
                            @ordem
                        )
                    `);

                const idPergunta =
                    resultadoPergunta.recordset[0].IdPergunta;

                // Cadastra as alternativas da pergunta atual.
                for (
                    const [posicao, alternativa]
                    of pergunta.alternativas.entries()
                ) {
                    await new sql.Request(transacao)
                        .input("idPergunta", sql.Int, idPergunta)
                        .input(
                            "texto",
                            sql.NVarChar(1000),
                            alternativa.texto.trim()
                        )
                        .input(
                            "correta",
                            sql.Bit,
                            alternativa.correta
                        )
                        .input("ordem", sql.Int, posicao + 1)
                        .query(`
                            INSERT INTO dbo.QuizAlternativas (
                                IdPergunta,
                                Texto,
                                Correta,
                                Ordem
                            )
                            VALUES (
                                @idPergunta,
                                @texto,
                                @correta,
                                @ordem
                            )
                        `);
                }
            }

            // Confirma todas as operações realizadas.
            await transacao.commit();

            return res.status(201).json({
                mensagem: "Quiz cadastrado com sucesso.",
                idQuiz,
                titulo: titulo.trim(),
                xpMaximo: Number(xpMaximo),
                totalPerguntas: perguntas.length
            });

        } catch (erro) {
            await cancelarTransacao(transacao);

            throw erro;
        }

    } catch (erro) {
        console.error(
            "Erro ao cadastrar quiz:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível cadastrar o quiz."
        });
    }
}

/**
 * Consulta os quizzes cadastrados pelo professor autenticado.
 *
 * Rota: GET /api/professores/me/quizzes
 */
export async function listarQuizzesProfessor(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Professor"
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    q.IdQuiz AS idQuiz,
                    q.Titulo AS titulo,
                    q.XPMaximo AS xpMaximo,
                    q.IdTurmaDisciplina AS idTurmaDisciplina,
                    q.IdAula AS idAula,
                    q.Ativo AS ativo,
                    q.DataCriacao AS dataCriacao,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    (
                        SELECT COUNT(*)
                        FROM dbo.QuizPerguntas AS qp
                        WHERE qp.IdQuiz = q.IdQuiz
                    ) AS totalPerguntas,
                    (
                        SELECT COUNT(*)
                        FROM dbo.QuizResultados AS qr
                        WHERE qr.IdQuiz = q.IdQuiz
                    ) AS totalRespostas
                FROM dbo.Quizzes AS q
                INNER JOIN dbo.Professores AS p
                    ON p.IdProfessor = q.IdProfessor
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = q.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE p.IdUsuario = @idUsuario
                ORDER BY q.DataCriacao DESC
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            quizzes: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar quizzes do professor:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar os quizzes."
        });
    }
}

/**
 * Consulta os quizzes disponíveis para o aluno autenticado.
 *
 * Rota: GET /api/alunos/me/quizzes
 *
 * Retorna somente quizzes das turmas em que o aluno
 * possui matrícula ativa, incluindo resultados anteriores.
 */
export async function listarQuizzesAluno(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Aluno"
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT DISTINCT
                    q.IdQuiz AS idQuiz,
                    d.IdDisciplina AS idDisciplina,
                    t.IdTurma AS idTurma,
                    q.IdAula AS idAula,
                    q.Titulo AS titulo,
                    q.XPMaximo AS xpMaximo,
                    q.DataCriacao AS dataCriacao,
                    t.NomeTurma AS nomeTurma,
                    d.NomeDisciplina AS nomeDisciplina,
                    CASE
                        WHEN qr.IdResultado IS NULL
                            THEN CAST(0 AS BIT)
                        ELSE CAST(1 AS BIT)
                    END AS respondido,
                    qr.Acertos AS acertos,
                    qr.TotalPerguntas AS totalPerguntas,
                    qr.XPGanho AS xpGanho,
                    qr.DataResposta AS dataResposta
                FROM dbo.Alunos AS al
                INNER JOIN dbo.Matriculas AS m
                    ON m.IdAluno = al.IdAluno
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = m.IdTurma
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurma = t.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Quizzes AS q
                    ON q.IdTurmaDisciplina = td.IdTurmaDisciplina
                LEFT JOIN dbo.QuizResultados AS qr
                    ON qr.IdQuiz = q.IdQuiz
                    AND qr.IdAluno = al.IdAluno
                WHERE al.IdUsuario = @idUsuario
                    AND m.Situacao = 'Ativa'
                    AND t.Ativa = 1
                    AND td.Ativa = 1
                    AND d.Ativa = 1
                    AND q.Ativo = 1
                ORDER BY q.DataCriacao DESC
            `);

        return res.status(200).json({
            total: resultado.recordset.length,
            quizzes: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar quizzes do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar os quizzes disponíveis."
        });
    }
}

/**
 * Consulta as perguntas e alternativas de um quiz.
 *
 * Rota: GET /api/alunos/me/quizzes/:idQuiz
 *
 * O gabarito não é enviado ao aluno, garantindo
 * que a correção seja realizada exclusivamente pela API.
 */
export async function consultarQuizAluno(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Aluno"
        );

        if (idUsuario === null) {
            return;
        }

        const { idQuiz } = req.params;

        if (!idQuiz || idQuiz.length > 200) {
            return res.status(400).json({
                mensagem: "Identificador do quiz inválido."
            });
        }

        const quiz = await buscarQuiz(
            conexao,
            idQuiz
        );

        if (!quiz || !quiz.Ativo) {
            return res.status(404).json({
                mensagem: "Quiz não encontrado ou indisponível."
            });
        }

        const idAluno = await verificarVinculoAluno(
            conexao,
            idUsuario,
            quiz.IdTurmaDisciplina
        );

        if (idAluno === null) {
            return res.status(403).json({
                mensagem: "Aluno não matriculado nesta turma."
            });
        }

        const resultado = await conexao.request()
            .input("idQuiz", sql.NVarChar(200), idQuiz)
            .query(`
                SELECT
                    qp.IdPergunta AS idPergunta,
                    qp.Enunciado AS enunciado,
                    qp.Ordem AS ordemPergunta,
                    qa.IdAlternativa AS idAlternativa,
                    qa.Texto AS texto,
                    qa.Ordem AS ordemAlternativa
                FROM dbo.QuizPerguntas AS qp
                INNER JOIN dbo.QuizAlternativas AS qa
                    ON qa.IdPergunta = qp.IdPergunta
                WHERE qp.IdQuiz = @idQuiz
                ORDER BY
                    qp.Ordem,
                    qa.Ordem
            `);

        const perguntas = new Map();

        // Organiza os registros em perguntas e alternativas.
        for (const registro of resultado.recordset) {
            if (!perguntas.has(registro.idPergunta)) {
                perguntas.set(registro.idPergunta, {
                    idPergunta: registro.idPergunta,
                    enunciado: registro.enunciado,
                    ordem: registro.ordemPergunta,
                    alternativas: []
                });
            }

            const pergunta = perguntas.get(
                registro.idPergunta
            );

            pergunta.alternativas.push({
                idAlternativa: registro.idAlternativa,
                texto: registro.texto,
                ordem: registro.ordemAlternativa
            });
        }

        const resultadoAnterior = await conexao.request()
            .input("idAluno", sql.Int, idAluno)
            .input("idQuiz", sql.NVarChar(200), idQuiz)
            .query(`
                SELECT
                    Acertos AS acertos,
                    TotalPerguntas AS totalPerguntas,
                    XPGanho AS xpGanho,
                    DataResposta AS dataResposta
                FROM dbo.QuizResultados
                WHERE IdAluno = @idAluno
                    AND IdQuiz = @idQuiz
            `);

        return res.status(200).json({
            idQuiz: quiz.IdQuiz,
            titulo: quiz.Titulo,
            xpMaximo: quiz.XPMaximo,
            totalPerguntas: perguntas.size,
            respondido: resultadoAnterior.recordset.length > 0,
            resultado: resultadoAnterior.recordset[0] ?? null,
            perguntas: Array.from(perguntas.values())
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar perguntas do quiz:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar o quiz."
        });
    }
}

/**
 * Valida as respostas enviadas pelo aluno.
 *
 * Impede perguntas duplicadas e identificadores inválidos.
 */
function validarRespostas(respostas) {
    if (
        !Array.isArray(respostas) ||
        respostas.length === 0 ||
        respostas.length > MAXIMO_PERGUNTAS
    ) {
        return false;
    }

    const perguntasRespondidas = new Set();

    for (const resposta of respostas) {
        if (
            !identificadorValido(resposta?.idPergunta) ||
            !identificadorValido(resposta?.idAlternativa)
        ) {
            return false;
        }

        const idPergunta = Number(resposta.idPergunta);

        if (perguntasRespondidas.has(idPergunta)) {
            return false;
        }

        perguntasRespondidas.add(idPergunta);
    }

    return true;
}

/**
 * Calcula o nível atual e o progresso do aluno.
 *
 * O aluno começa no nível 1 e avança um nível
 * a cada 1000 pontos de experiência acumulados.
 *
 * O nível máximo é 50, atingido com 49000 XP.
 * Nesse nível, a barra de progresso permanece completa.
 */
function calcularProgresso(xpTotal) {
    const nivel = Math.min(
        Math.floor(xpTotal / XP_POR_NIVEL) + 1,
        NIVEL_MAXIMO
    );

    // Identifica se o aluno atingiu o último nível.
    const atingiuNivelMaximo = nivel === NIVEL_MAXIMO;

    // Mantém a barra de progresso completa no nível máximo.
    const xpNivelAtual = atingiuNivelMaximo
        ? XP_POR_NIVEL
        : xpTotal % XP_POR_NIVEL;

    // Não existe um próximo nível após o nível 50.
    const xpProximoNivel = atingiuNivelMaximo
        ? 0
        : XP_POR_NIVEL - xpNivelAtual;

    const percentualProgresso = atingiuNivelMaximo
        ? 100
        : Math.floor(
            (xpNivelAtual / XP_POR_NIVEL) * 100
        );

    return {
        xpTotal,
        nivel,
        nivelMaximo: NIVEL_MAXIMO,
        xpTotalMaximo: XP_TOTAL_MAXIMO,
        xpNivelAtual,
        xpProximoNivel,
        xpNecessarioPorNivel: XP_POR_NIVEL,
        percentualProgresso,
        atingiuNivelMaximo
    };
}

/**
 * Registra e corrige as respostas do aluno.
 *
 * Rota: POST /api/alunos/me/quizzes/:idQuiz/respostas
 *
 * Calcula os acertos e o XP diretamente no servidor.
 * Utiliza uma transação para impedir XP duplicado
 * e garantir a consistência dos dados.
 *
 * O XP acumulado não pode ultrapassar 49000 pontos.
 * O aluno pode continuar respondendo quizzes mesmo
 * após atingir o nível máximo.
 */
export async function responderQuiz(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Aluno"
        );

        if (idUsuario === null) {
            return;
        }

        const { idQuiz } = req.params;

        const respostas = req.body?.respostas;

        if (!idQuiz || idQuiz.length > 200) {
            return res.status(400).json({
                mensagem: "Identificador do quiz inválido."
            });
        }

        if (!validarRespostas(respostas)) {
            return res.status(400).json({
                mensagem: "As respostas enviadas são inválidas."
            });
        }

        const quiz = await buscarQuiz(
            conexao,
            idQuiz
        );

        if (!quiz || !quiz.Ativo) {
            return res.status(404).json({
                mensagem: "Quiz não encontrado ou indisponível."
            });
        }

        const idAluno = await verificarVinculoAluno(
            conexao,
            idUsuario,
            quiz.IdTurmaDisciplina
        );

        if (idAluno === null) {
            return res.status(403).json({
                mensagem: "Aluno não matriculado nesta turma."
            });
        }

        // Consulta o gabarito exclusivamente no servidor.
        const resultadoGabarito = await conexao.request()
            .input("idQuiz", sql.NVarChar(200), idQuiz)
            .query(`
                SELECT
                    qp.IdPergunta AS idPergunta,
                    qa.IdAlternativa AS idAlternativa,
                    qa.Correta AS correta
                FROM dbo.QuizPerguntas AS qp
                INNER JOIN dbo.QuizAlternativas AS qa
                    ON qa.IdPergunta = qp.IdPergunta
                WHERE qp.IdQuiz = @idQuiz
            `);

        const gabarito = new Map();

        // Agrupa as alternativas pelo identificador da pergunta.
        for (const registro of resultadoGabarito.recordset) {
            if (!gabarito.has(registro.idPergunta)) {
                gabarito.set(
                    registro.idPergunta,
                    []
                );
            }

            gabarito.get(registro.idPergunta).push({
                idAlternativa: registro.idAlternativa,
                correta: registro.correta
            });
        }

        const totalPerguntas = gabarito.size;

        if (totalPerguntas === 0) {
            return res.status(409).json({
                mensagem: "O quiz não possui perguntas disponíveis."
            });
        }

        // Verifica a integridade do gabarito.
        for (const alternativas of gabarito.values()) {
            const alternativasCorretas = alternativas.filter(
                alternativa => alternativa.correta === true
            );

            if (alternativasCorretas.length !== 1) {
                return res.status(409).json({
                    mensagem: "O quiz possui um gabarito inválido."
                });
            }
        }

        if (respostas.length !== totalPerguntas) {
            return res.status(400).json({
                mensagem: "Todas as perguntas devem ser respondidas."
            });
        }

        let acertos = 0;

        // Compara as respostas do aluno com o gabarito.
        for (const resposta of respostas) {
            const idPergunta = Number(
                resposta.idPergunta
            );

            const idAlternativa = Number(
                resposta.idAlternativa
            );

            const alternativas = gabarito.get(
                idPergunta
            );

            if (!alternativas) {
                return res.status(400).json({
                    mensagem: "A pergunta não pertence ao quiz."
                });
            }

            const alternativaSelecionada = alternativas.find(
                alternativa =>
                    alternativa.idAlternativa === idAlternativa
            );

            if (!alternativaSelecionada) {
                return res.status(400).json({
                    mensagem: "A alternativa não pertence à pergunta."
                });
            }

            if (alternativaSelecionada.correta === true) {
                acertos++;
            }
        }

        // Calcula o XP proporcional à quantidade de acertos.
        const xpCalculado = Math.round(
            (acertos / totalPerguntas) * quiz.XPMaximo
        );

        // Calcula o percentual de aproveitamento do aluno.
        const percentualAcertos = Math.round(
            (acertos / totalPerguntas) * 100
        );

        const transacao = new sql.Transaction(conexao);

        // Evita que solicitações simultâneas concedam XP duplicado.
        await transacao.begin(
            sql.ISOLATION_LEVEL.SERIALIZABLE
        );

        try {
            // Verifica se o aluno já respondeu este quiz.
            const resultadoExistente = await new sql.Request(
                transacao
            )
                .input("idAluno", sql.Int, idAluno)
                .input("idQuiz", sql.NVarChar(200), idQuiz)
                .query(`
                    SELECT
                        IdResultado,
                        Acertos,
                        TotalPerguntas,
                        XPGanho
                    FROM dbo.QuizResultados WITH (
                        UPDLOCK,
                        HOLDLOCK
                    )
                    WHERE IdAluno = @idAluno
                        AND IdQuiz = @idQuiz
                `);

            if (resultadoExistente.recordset.length > 0) {
                await transacao.rollback();

                return res.status(409).json({
                    mensagem: "Este quiz já foi respondido.",
                    xpDuplicado: false
                });
            }

            // Consulta e bloqueia o registro de XP do aluno.
            const resultadoXP = await new sql.Request(
                transacao
            )
                .input("idAluno", sql.Int, idAluno)
                .query(`
                    SELECT
                        XPTotal
                    FROM dbo.AlunoXP WITH (
                        UPDLOCK,
                        HOLDLOCK
                    )
                    WHERE IdAluno = @idAluno
                `);

            const xpAtual =
                resultadoXP.recordset[0]?.XPTotal ?? 0;

            // Calcula quanto XP ainda pode ser concedido.
            const xpDisponivel = Math.max(
                0,
                XP_TOTAL_MAXIMO - xpAtual
            );

            // Limita o XP concedido ao saldo disponível.
            const xpGanho = Math.min(
                xpCalculado,
                xpDisponivel
            );

            // Calcula o novo total de XP acumulado.
            const novoXPTotal = xpAtual + xpGanho;

            // Identifica se parte do XP foi limitada.
            const xpLimitado = xpGanho < xpCalculado;

            // Registra o resultado obtido pelo aluno.
            // Armazena somente o XP efetivamente concedido.
            await new sql.Request(transacao)
                .input("idAluno", sql.Int, idAluno)
                .input("idQuiz", sql.NVarChar(200), idQuiz)
                .input("acertos", sql.Int, acertos)
                .input("totalPerguntas", sql.Int, totalPerguntas)
                .input("xpGanho", sql.Int, xpGanho)
                .query(`
                    INSERT INTO dbo.QuizResultados (
                        IdAluno,
                        IdQuiz,
                        Acertos,
                        TotalPerguntas,
                        XPGanho,
                        DataResposta
                    )
                    VALUES (
                        @idAluno,
                        @idQuiz,
                        @acertos,
                        @totalPerguntas,
                        @xpGanho,
                        SYSUTCDATETIME()
                    )
                `);

            if (resultadoXP.recordset.length > 0) {
                // Atualiza o XP de um aluno já registrado.
                await new sql.Request(transacao)
                    .input("idAluno", sql.Int, idAluno)
                    .input("xpTotal", sql.Int, novoXPTotal)
                    .query(`
                        UPDATE dbo.AlunoXP
                        SET
                            XPTotal = @xpTotal,
                            DataAtualizacao = SYSUTCDATETIME()
                        WHERE IdAluno = @idAluno
                    `);

            } else {
                // Cria o registro de XP no primeiro quiz respondido.
                await new sql.Request(transacao)
                    .input("idAluno", sql.Int, idAluno)
                    .input("xpTotal", sql.Int, novoXPTotal)
                    .query(`
                        INSERT INTO dbo.AlunoXP (
                            IdAluno,
                            XPTotal,
                            DataAtualizacao
                        )
                        VALUES (
                            @idAluno,
                            @xpTotal,
                            SYSUTCDATETIME()
                        )
                    `);
            }

            // Calcula o progresso antes e depois do quiz.
            const progressoAnterior = calcularProgresso(
                xpAtual
            );

            const progressoAtual = calcularProgresso(
                novoXPTotal
            );

            // Identifica se o aluno avançou de nível.
            const subiuDeNivel =
                progressoAtual.nivel > progressoAnterior.nivel;

            // Calcula a quantidade de níveis conquistados.
            const niveisConquistados =
                progressoAtual.nivel - progressoAnterior.nivel;

            // Confirma o resultado e a atualização do XP.
            await transacao.commit();

            return res.status(200).json({
                mensagem: "Quiz respondido com sucesso.",
                idQuiz,
                acertos,
                totalPerguntas,
                percentualAcertos,
                xpCalculado,
                xpGanho,
                xpLimitado,
                nivelAnterior: progressoAnterior.nivel,
                subiuDeNivel,
                niveisConquistados,
                ...progressoAtual
            });

        } catch (erro) {
            await cancelarTransacao(transacao);

            const numeroErro =
                erro.number ??
                erro.originalError?.info?.number;

            // Trata violações de unicidade do SQL Server.
            if (
                numeroErro === 2601 ||
                numeroErro === 2627
            ) {
                return res.status(409).json({
                    mensagem: "Registro duplicado. Nenhum XP adicional foi concedido."
                });
            }

            throw erro;
        }

    } catch (erro) {
        console.error(
            "Erro ao registrar respostas do quiz:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível registrar as respostas."
        });
    }
}

/**
 * Consulta o XP acumulado e o nível do aluno autenticado.
 *
 * Rota: GET /api/alunos/me/xp
 *
 * Pode ser utilizada tanto pelo Web quanto pelo Mobile.
 * Retorna o nível atual, o progresso, o XP restante
 * e a indicação de nível máximo atingido.
 */
export async function consultarMeuXP(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Aluno"
        );

        if (idUsuario === null) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    al.IdAluno AS idAluno,
                    COALESCE(xp.XPTotal, 0) AS xpTotal,
                    xp.DataAtualizacao AS dataAtualizacao,
                    (
                        SELECT COUNT(*)
                        FROM dbo.QuizResultados AS qr
                        WHERE qr.IdAluno = al.IdAluno
                    ) AS totalQuizzesRespondidos
                FROM dbo.Alunos AS al
                LEFT JOIN dbo.AlunoXP AS xp
                    ON xp.IdAluno = al.IdAluno
                WHERE al.IdUsuario = @idUsuario
            `);

        const aluno = resultado.recordset[0];

        if (!aluno) {
            return res.status(404).json({
                mensagem: "Aluno não encontrado."
            });
        }

        const progresso = calcularProgresso(
            aluno.xpTotal
        );

        return res.status(200).json({
            idAluno: aluno.idAluno,
            totalQuizzesRespondidos: aluno.totalQuizzesRespondidos,
            dataAtualizacao: aluno.dataAtualizacao,
            ...progresso
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar XP do aluno:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar o XP do aluno."
        });
    }
}