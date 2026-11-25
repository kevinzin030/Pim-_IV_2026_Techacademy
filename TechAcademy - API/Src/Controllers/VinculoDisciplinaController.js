import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

// Representa erros de negócio e autorização.
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

// Valida os identificadores recebidos.
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

// Confirma a autorização do coordenador.
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
                AND pm.Plataforma = 'Desktop'
                AND pm.Codigo = 'GERENCIAR_DISCIPLINAS'
        `);

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui autorização para gerenciar vínculos."
        );
    }
}

// Executa operações de gravação em uma transação.
async function executarTransacao(operacao) {
    const conexao = await conectarBanco();
    const transacao = new sql.Transaction(conexao);

    await transacao.begin(
        sql.ISOLATION_LEVEL.SERIALIZABLE
    );

    try {
        const resultado = await operacao(transacao);

        await transacao.commit();

        return resultado;

    } catch (erro) {
        try {
            await transacao.rollback();
        } catch (erroRollback) {
            console.error(
                "Erro ao desfazer transação:",
                erroRollback.message
            );
        }

        throw erro;
    }
}

// Registra a auditoria na mesma transação do vínculo.
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
            )
        `);
}

// Padroniza as respostas de erro.
function responderErro(res, erro) {
    if (erro instanceof ErroOperacao) {
        return res.status(erro.status).json({
            mensagem: erro.message
        });
    }

    if (erro.number === 2601 || erro.number === 2627) {
        return res.status(409).json({
            mensagem: "Este vínculo já existe."
        });
    }

    console.error(
        "Erro na vinculação de disciplinas:",
        erro
    );

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

// Lista as turmas e disciplinas disponíveis para vinculação.
export async function listarOpcoesVinculo(req, res) {
    try {
        const conexao = await conectarBanco();

        await exigirAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        const resultado = await conexao.request()
            .query(`
                SELECT
                    IdTurma,
                    CodigoTurma + ' - ' + NomeTurma
                        AS NomeExibicao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma;

                SELECT
                    IdDisciplina,
                    CodigoDisciplina + ' - ' + NomeDisciplina
                        AS NomeExibicao
                FROM dbo.Disciplinas
                WHERE Ativa = 1
                ORDER BY CodigoDisciplina;
            `);

        return res.status(200).json({
            turmas: resultado.recordsets[0],
            disciplinas: resultado.recordsets[1]
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Lista as disciplinas vinculadas à turma selecionada.
export async function listarVinculosTurma(req, res) {
    try {
        const idTurma = validarId(req.params.idTurma);
        const conexao = await conectarBanco();

        await exigirAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        const resultado = await conexao.request()
            .input("idTurma", sql.Int, idTurma)
            .query(`
                SELECT
                    td.IdTurmaDisciplina,
                    d.CodigoDisciplina AS Codigo,
                    d.NomeDisciplina AS Disciplina,
                    d.CargaHoraria AS CargaHoraria
                FROM dbo.TurmaDisciplinas td
                INNER JOIN dbo.Disciplinas d
                    ON td.IdDisciplina = d.IdDisciplina
                WHERE td.IdTurma = @idTurma
                    AND td.Ativa = 1
                ORDER BY d.CodigoDisciplina;
            `);

        return res.status(200).json({
            vinculos: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Confirma que a turma e a disciplina estão ativas.
async function validarTurmaEDisciplina(
    transacao,
    idTurma,
    idDisciplina
) {
    const resultado = await new sql.Request(transacao)
        .input("idTurma", sql.Int, idTurma)
        .input("idDisciplina", sql.Int, idDisciplina)
        .query(`
            SELECT
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Turmas
                    WHERE IdTurma = @idTurma
                        AND Ativa = 1
                )
                AND EXISTS (
                    SELECT 1
                    FROM dbo.Disciplinas
                    WHERE IdDisciplina = @idDisciplina
                        AND Ativa = 1
                )
                THEN 1 ELSE 0 END AS Valido;
        `);

    if (resultado.recordset[0].Valido !== 1) {
        throw new ErroOperacao(
            400,
            "A turma e a disciplina devem estar ativas."
        );
    }
}

// Cria ou reativa um vínculo acadêmico.
export async function vincularDisciplina(req, res) {
    try {
        const idTurma = validarId(req.params.idTurma);

        const idDisciplina = validarId(
            req.body?.idDisciplina
        );

        const idUsuario = req.usuario.idUsuario;

        const reativado = await executarTransacao(
            async transacao => {
                await exigirAutorizacao(
                    transacao,
                    idUsuario
                );

                await validarTurmaEDisciplina(
                    transacao,
                    idTurma,
                    idDisciplina
                );

                // Bloqueia alterações concorrentes no vínculo.
                const consulta = await new sql.Request(transacao)
                    .input("idTurma", sql.Int, idTurma)
                    .input("idDisciplina", sql.Int, idDisciplina)
                    .query(`
                        SELECT
                            IdTurmaDisciplina,
                            Ativa
                        FROM dbo.TurmaDisciplinas
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdTurma = @idTurma
                            AND IdDisciplina = @idDisciplina;
                    `);

                const vinculo = consulta.recordset[0];

                if (vinculo?.Ativa) {
                    throw new ErroOperacao(
                        409,
                        "Esta disciplina já está vinculada à turma."
                    );
                }

                if (vinculo) {
                    // Reativa o vínculo sem excluir o histórico.
                    await new sql.Request(transacao)
                        .input(
                            "idVinculo",
                            sql.Int,
                            vinculo.IdTurmaDisciplina
                        )
                        .query(`
                            UPDATE dbo.TurmaDisciplinas
                            SET
                                Ativa = 1,
                                DataVinculo = SYSDATETIME()
                            WHERE IdTurmaDisciplina = @idVinculo;
                        `);

                } else {
                    // Cria um vínculo quando ainda não existe.
                    await new sql.Request(transacao)
                        .input("idTurma", sql.Int, idTurma)
                        .input("idDisciplina", sql.Int, idDisciplina)
                        .query(`
                            INSERT INTO dbo.TurmaDisciplinas (
                                IdTurma,
                                IdDisciplina,
                                Ativa,
                                DataVinculo
                            )
                            VALUES (
                                @idTurma,
                                @idDisciplina,
                                1,
                                SYSDATETIME()
                            );
                        `);
                }

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "VINCULAR_DISCIPLINA",
                    `Disciplina ${idDisciplina} vinculada ` +
                    `à turma ${idTurma}.`
                );

                return Boolean(vinculo);
            }
        );

        return res.status(reativado ? 200 : 201).json({
            mensagem: reativado
                ? "Vínculo reativado com sucesso!"
                : "Disciplina vinculada com sucesso!",
            reativado
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Desativa um vínculo sem excluir seus registros.
export async function desvincularDisciplina(req, res) {
    try {
        const idTurma = validarId(req.params.idTurma);
        const idVinculo = validarId(req.params.idVinculo);
        const idUsuario = req.usuario.idUsuario;

        await executarTransacao(async transacao => {
            await exigirAutorizacao(
                transacao,
                idUsuario
            );

            const resultado = await new sql.Request(transacao)
                .input("idTurma", sql.Int, idTurma)
                .input("idVinculo", sql.Int, idVinculo)
                .query(`
                    UPDATE dbo.TurmaDisciplinas
                    SET Ativa = 0
                    WHERE IdTurmaDisciplina = @idVinculo
                        AND IdTurma = @idTurma
                        AND Ativa = 1;
                `);

            if (resultado.rowsAffected[0] === 0) {
                throw new ErroOperacao(
                    404,
                    "Vínculo ativo não encontrado."
                );
            }

            await registrarAuditoria(
                transacao,
                idUsuario,
                "DESVINCULAR_DISCIPLINA",
                `Vínculo acadêmico ${idVinculo} desativado.`
            );
        });

        return res.status(200).json({
            mensagem: "Disciplina desvinculada com sucesso!"
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}