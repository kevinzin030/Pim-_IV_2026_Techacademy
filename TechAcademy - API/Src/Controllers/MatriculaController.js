import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

// Representa erros das regras de negócio.
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

// Situações acadêmicas permitidas.
const situacoesPermitidas = [
    "Ativa",
    "Trancada",
    "Concluida",
    "Cancelada"
];

// Valida identificadores recebidos pela API.
function validarId(valor) {
    const id = Number(valor);

    if (
        !Number.isInteger(id) ||
        id <= 0 ||
        id > 2147483647
    ) {
        throw new ErroOperacao(
            400,
            "Identificador inválido."
        );
    }

    return id;
}

// Verifica a autorização do coordenador.
async function exigirAutorizacao(origem, idUsuario) {
    const resultado = await new sql.Request(origem)
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
                AND pm.Codigo = 'GERENCIAR_MATRICULAS';
        `);

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui permissão para gerenciar matrículas."
        );
    }
}

// Executa operações acadêmicas dentro de uma transação.
async function executarTransacao(idUsuario, operacao) {
    const conexao = await conectarBanco();
    const transacao = new sql.Transaction(conexao);

    let revertida = false;

    transacao.on("rollback", () => {
        revertida = true;
    });

    await transacao.begin(
        sql.ISOLATION_LEVEL.SERIALIZABLE
    );

    try {
        await exigirAutorizacao(
            transacao,
            idUsuario
        );

        const resultado = await operacao(transacao);

        await transacao.commit();

        return resultado;

    } catch (erro) {
        if (!revertida) {
            try {
                await transacao.rollback();
            } catch (erroRollback) {
                console.error(
                    "Erro ao desfazer transação:",
                    erroRollback.message
                );
            }
        }

        throw erro;
    }
}

// Registra as operações na auditoria do sistema.
async function registrarAuditoria(
    transacao,
    idUsuario,
    acao,
    descricao
) {
    await new sql.Request(transacao)
        .input("idUsuario", sql.Int, idUsuario)
        .input("acao", sql.NVarChar(100), acao)
        .input("descricao", sql.NVarChar(500), descricao)
        .query(`
            INSERT INTO dbo.LogsAtividades (
                IdUsuario,
                Acao,
                Descricao
            )
            VALUES (
                @idUsuario,
                @acao,
                @descricao
            );
        `);
}

// Centraliza os erros da API e da procedure.
function responderErro(res, erro) {
    if (erro instanceof ErroOperacao) {
        return res.status(erro.status).json({
            mensagem: erro.message
        });
    }

    const errosProcedure = {
        50010: {
            status: 404,
            mensagem: "Matrícula não encontrada."
        },
        50011: {
            status: 400,
            mensagem: "Turma de destino inexistente ou inativa."
        },
        50012: {
            status: 409,
            mensagem: "O aluno já pertence a essa turma."
        },
        50013: {
            status: 409,
            mensagem: "Histórico atual da matrícula não encontrado."
        },
        50014: {
            status: 400,
            mensagem:
                "A transferência deve ocorrer após a data " +
                "de entrada na turma atual."
        },
        50015: {
            status: 409,
            mensagem:
                "O histórico e a matrícula possuem turmas diferentes."
        }
    };

    const erroProcedure = errosProcedure[erro.number];

    if (erroProcedure) {
        return res.status(erroProcedure.status).json({
            mensagem: erroProcedure.mensagem
        });
    }

    if (erro.number === 2601 || erro.number === 2627) {
        return res.status(409).json({
            mensagem: "Já existe uma matrícula para este aluno."
        });
    }

    console.error(
        "Erro no gerenciamento de matrículas:",
        erro
    );

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

// Lista alunos e turmas disponíveis para matrícula.
export async function listarOpcoesMatricula(req, res) {
    try {
        const conexao = await conectarBanco();

        await exigirAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        const resultado = await conexao.request()
            .query(`
                SELECT
                    a.IdAluno,
                    a.MatriculaAluno + N' - ' +
                        u.NomeCompleto AS NomeExibicao
                FROM dbo.Alunos a
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.Ativo = 1
                    AND p.Ativo = 1
                    AND p.NomePerfil = 'Aluno'
                ORDER BY u.NomeCompleto;

                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma AS NomeExibicao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma;

                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma +
                        CASE
                            WHEN Ativa = 0
                            THEN N' (Inativa)'
                            ELSE N''
                        END AS NomeExibicao
                FROM dbo.Turmas
                ORDER BY CodigoTurma;
            `);

        return res.status(200).json({
            alunos: resultado.recordsets[0],
            turmas: resultado.recordsets[1],
            filtroTurmas: resultado.recordsets[2]
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Consulta matrículas utilizando pesquisa e filtro por turma.
export async function listarMatriculas(req, res) {
    try {
        const idTurma = Number(req.query.idTurma ?? 0);
        const pesquisa = req.query.pesquisa ?? "";

        if (
            !Number.isInteger(idTurma) ||
            idTurma < 0 ||
            idTurma > 2147483647
        ) {
            throw new ErroOperacao(
                400,
                "Filtro de turma inválido."
            );
        }

        if (
            typeof pesquisa !== "string" ||
            pesquisa.length > 150
        ) {
            throw new ErroOperacao(
                400,
                "Pesquisa inválida."
            );
        }

        const conexao = await conectarBanco();

        await exigirAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        const resultado = await conexao.request()
            .input("idTurma", sql.Int, idTurma)
            .input(
                "pesquisa",
                sql.NVarChar(150),
                pesquisa.trim()
            )
            .query(`
                SELECT
                    m.IdMatricula,
                    a.IdAluno,
                    t.IdTurma,
                    a.MatriculaAluno AS RA,
                    u.NomeCompleto AS Aluno,
                    t.CodigoTurma AS Turma,
                    t.NomeTurma AS NomeTurma,
                    m.Situacao,
                    m.DataMatricula
                FROM dbo.Matriculas m
                INNER JOIN dbo.Alunos a
                    ON a.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = m.IdTurma
                WHERE
                    (
                        @idTurma = 0
                        OR t.IdTurma = @idTurma
                    )
                    AND (
                        @pesquisa = N''
                        OR u.NomeCompleto LIKE
                            N'%' + @pesquisa + N'%'
                        OR a.MatriculaAluno LIKE
                            N'%' + @pesquisa + N'%'
                        OR t.CodigoTurma LIKE
                            N'%' + @pesquisa + N'%'
                    )
                ORDER BY u.NomeCompleto;
            `);

        return res.status(200).json({
            matriculas: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Verifica se o aluno e a turma estão disponíveis.
async function validarAlunoETurma(
    transacao,
    idAluno,
    idTurma
) {
    const resultado = await new sql.Request(transacao)
        .input("idAluno", sql.Int, idAluno)
        .input("idTurma", sql.Int, idTurma)
        .query(`
            SELECT COUNT(1) AS Total
            FROM dbo.Alunos a
            INNER JOIN dbo.Usuarios u
                ON u.IdUsuario = a.IdUsuario
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            WHERE a.IdAluno = @idAluno
                AND u.Ativo = 1
                AND p.Ativo = 1
                AND p.NomePerfil = 'Aluno';

            SELECT COUNT(1) AS Total
            FROM dbo.Turmas
            WHERE IdTurma = @idTurma
                AND Ativa = 1;
        `);

    if (resultado.recordsets[0][0].Total === 0) {
        throw new ErroOperacao(
            400,
            "Aluno inexistente ou inativo."
        );
    }

    if (resultado.recordsets[1][0].Total === 0) {
        throw new ErroOperacao(
            400,
            "Turma inexistente ou inativa."
        );
    }
}

// Cria a matrícula e seu histórico inicial atomicamente.
export async function matricularAluno(req, res) {
    try {
        const idAluno = validarId(req.body?.idAluno);
        const idTurma = validarId(req.body?.idTurma);
        const idUsuario = req.usuario.idUsuario;

        const idMatricula = await executarTransacao(
            idUsuario,
            async transacao => {
                await validarAlunoETurma(
                    transacao,
                    idAluno,
                    idTurma
                );

                // Impede a criação de matrículas duplicadas.
                const existente = await new sql.Request(transacao)
                    .input("idAluno", sql.Int, idAluno)
                    .query(`
                        SELECT IdMatricula
                        FROM dbo.Matriculas
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdAluno = @idAluno;
                    `);

                if (existente.recordset.length > 0) {
                    throw new ErroOperacao(
                        409,
                        "Este aluno já possui matrícula. " +
                        "Utilize Transferir turma ou Alterar situação."
                    );
                }

                // Cria a matrícula e recupera seu identificador.
                const insercao = await new sql.Request(transacao)
                    .input("idAluno", sql.Int, idAluno)
                    .input("idTurma", sql.Int, idTurma)
                    .query(`
                        INSERT INTO dbo.Matriculas (
                            IdAluno,
                            IdTurma,
                            DataMatricula,
                            Situacao
                        )
                        OUTPUT INSERTED.IdMatricula
                        VALUES (
                            @idAluno,
                            @idTurma,
                            SYSDATETIME(),
                            N'Ativa'
                        );
                    `);

                const novaMatricula =
                    insercao.recordset[0].IdMatricula;

                // Cria o primeiro período do histórico acadêmico.
                await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        novaMatricula
                    )
                    .query(`
                        INSERT INTO dbo.HistoricoTurmas (
                            IdMatricula,
                            IdTurma,
                            DataEntrada
                        )
                        SELECT
                            IdMatricula,
                            IdTurma,
                            CAST(DataMatricula AS DATE)
                        FROM dbo.Matriculas
                        WHERE IdMatricula = @idMatricula;
                    `);

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "CRIAR_MATRICULA",
                    `Aluno ${idAluno} matriculado ` +
                    `na turma ${idTurma}. ` +
                    `Matrícula ${novaMatricula}.`
                );

                return novaMatricula;
            }
        );

        return res.status(201).json({
            mensagem: "Matrícula realizada com sucesso!",
            idMatricula
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Transfere uma matrícula ativa utilizando a stored procedure.
export async function transferirMatricula(req, res) {
    try {
        const idMatricula = validarId(
            req.params.idMatricula
        );

        const idNovaTurma = validarId(
            req.body?.idTurma
        );

        const idUsuario = req.usuario.idUsuario;

        await executarTransacao(
            idUsuario,
            async transacao => {
                const consulta = await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        idMatricula
                    )
                    .query(`
                        SELECT
                            IdMatricula,
                            IdTurma,
                            Situacao
                        FROM dbo.Matriculas
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdMatricula = @idMatricula;
                    `);

                const matricula = consulta.recordset[0];

                if (!matricula) {
                    throw new ErroOperacao(
                        404,
                        "Matrícula não encontrada."
                    );
                }

                if (matricula.Situacao !== "Ativa") {
                    throw new ErroOperacao(
                        409,
                        "Somente matrículas ativas podem ser transferidas."
                    );
                }

                if (matricula.IdTurma === idNovaTurma) {
                    throw new ErroOperacao(
                        409,
                        "O aluno já pertence a essa turma."
                    );
                }

                // A procedure atualiza a matrícula e seu histórico.
                await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        idMatricula
                    )
                    .input(
                        "idNovaTurma",
                        sql.Int,
                        idNovaTurma
                    )
                    .query(`
                        DECLARE @DataTransferencia DATE =
                            CAST(SYSDATETIME() AS DATE);

                        EXEC dbo.SP_TransferirAluno
                            @IdMatricula = @idMatricula,
                            @IdNovaTurma = @idNovaTurma,
                            @DataTransferencia = @DataTransferencia;
                    `);

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "TRANSFERIR_MATRICULA",
                    `Matrícula ${idMatricula} transferida ` +
                    `da turma ${matricula.IdTurma} ` +
                    `para a turma ${idNovaTurma}.`
                );
            }
        );

        return res.status(200).json({
            mensagem: "Transferência realizada com sucesso!"
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Altera a situação acadêmica sem excluir a matrícula.
export async function alterarSituacaoMatricula(req, res) {
    try {
        const idMatricula = validarId(
            req.params.idMatricula
        );

        const situacao = req.body?.situacao;
        const idUsuario = req.usuario.idUsuario;

        if (!situacoesPermitidas.includes(situacao)) {
            throw new ErroOperacao(
                400,
                "Situação acadêmica inválida."
            );
        }

        await executarTransacao(
            idUsuario,
            async transacao => {
                const consulta = await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        idMatricula
                    )
                    .query(`
                        SELECT
                            IdMatricula,
                            Situacao
                        FROM dbo.Matriculas
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdMatricula = @idMatricula;
                    `);

                const matricula = consulta.recordset[0];

                if (!matricula) {
                    throw new ErroOperacao(
                        404,
                        "Matrícula não encontrada."
                    );
                }

                if (matricula.Situacao === situacao) {
                    throw new ErroOperacao(
                        409,
                        "A matrícula já possui essa situação."
                    );
                }

                await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        idMatricula
                    )
                    .input(
                        "situacao",
                        sql.NVarChar(30),
                        situacao
                    )
                    .query(`
                        UPDATE dbo.Matriculas
                        SET Situacao = @situacao
                        WHERE IdMatricula = @idMatricula;
                    `);

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "ALTERAR_SITUACAO_MATRICULA",
                    `Matrícula ${idMatricula} alterada ` +
                    `de ${matricula.Situacao} para ${situacao}.`
                );
            }
        );

        return res.status(200).json({
            mensagem: "Situação atualizada com sucesso!"
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}