import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

// Representa erros relacionados às regras de negócio.
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

// Valida os identificadores recebidos pela API.
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

// Verifica a autorização do coordenador autenticado.
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
                AND pm.Codigo = 'ALOCAR_PROFESSORES';
        `);

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui permissão para alocar professores."
        );
    }
}

// Executa operações acadêmicas de forma transacional.
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

// Registra a operação na auditoria do sistema.
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

// Centraliza o tratamento de erros.
function responderErro(res, erro) {
    if (erro instanceof ErroOperacao) {
        return res.status(erro.status).json({
            mensagem: erro.message
        });
    }

    if (erro.number === 2601 || erro.number === 2627) {
        return res.status(409).json({
            mensagem: "Já existe um vínculo para este professor."
        });
    }

    console.error(
        "Erro no gerenciamento de alocações:",
        erro
    );

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

// Lista as turmas e os professores disponíveis.
export async function listarOpcoesAlocacao(req, res) {
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
                    CodigoTurma + N' - ' +
                        NomeTurma AS Descricao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma;

                SELECT
                    p.IdProfessor,
                    p.RegistroProfessor + N' - ' +
                        u.NomeCompleto AS Descricao
                FROM dbo.Professores p
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = p.IdUsuario
                INNER JOIN dbo.Perfis pf
                    ON pf.IdPerfil = u.IdPerfil
                WHERE u.Ativo = 1
                    AND pf.Ativo = 1
                    AND pf.NomePerfil = 'Professor'
                ORDER BY u.NomeCompleto;

                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma +
                        CASE
                            WHEN Ativa = 0
                            THEN N' (Inativa)'
                            ELSE N''
                        END AS Descricao
                FROM dbo.Turmas
                ORDER BY CodigoTurma;
            `);

        return res.status(200).json({
            turmas: resultado.recordsets[0],
            professores: resultado.recordsets[1],
            filtroTurmas: resultado.recordsets[2]
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Lista as disciplinas ativas vinculadas à turma.
export async function listarDisciplinasAlocacao(req, res) {
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
                    d.CodigoDisciplina + N' - ' +
                        d.NomeDisciplina AS Descricao
                FROM dbo.TurmaDisciplinas td
                INNER JOIN dbo.Disciplinas d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = td.IdTurma
                WHERE td.IdTurma = @idTurma
                    AND td.Ativa = 1
                    AND d.Ativa = 1
                    AND t.Ativa = 1
                ORDER BY d.NomeDisciplina;
            `);

        return res.status(200).json({
            disciplinas: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Consulta o professor atualmente responsável pela disciplina.
export async function consultarProfessorResponsavel(req, res) {
    try {
        const idVinculo = validarId(req.params.idVinculo);

        const conexao = await conectarBanco();

        await exigirAutorizacao(
            conexao,
            req.usuario.idUsuario
        );

        const resultado = await conexao.request()
            .input("idVinculo", sql.Int, idVinculo)
            .query(`
                SELECT
                    ptd.IdVinculo AS IdAlocacao,
                    p.IdProfessor,
                    p.RegistroProfessor + N' - ' +
                        u.NomeCompleto AS Responsavel
                FROM dbo.ProfessorTurmaDisciplinas ptd
                INNER JOIN dbo.Professores p
                    ON p.IdProfessor = ptd.IdProfessor
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = p.IdUsuario
                WHERE ptd.IdTurmaDisciplina = @idVinculo
                    AND ptd.Ativo = 1;
            `);

        if (resultado.recordset.length > 1) {
            throw new ErroOperacao(
                409,
                "Esta disciplina possui mais de um professor ativo."
            );
        }

        const alocacao = resultado.recordset[0];

        return res.status(200).json({
            responsavel: alocacao?.Responsavel ?? null,
            idProfessor: alocacao?.IdProfessor ?? null,
            idAlocacao: alocacao?.IdAlocacao ?? null
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Lista as alocações ativas conforme os filtros informados.
export async function listarAlocacoes(req, res) {
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
                    ptd.IdVinculo AS IdAlocacao,
                    ptd.IdProfessor,
                    ptd.IdTurmaDisciplina,
                    td.IdTurma,
                    t.CodigoTurma AS Turma,
                    d.NomeDisciplina AS Disciplina,
                    p.RegistroProfessor AS RP,
                    u.NomeCompleto AS Professor,
                    ptd.DataVinculo AS DataAlocacao
                FROM dbo.ProfessorTurmaDisciplinas ptd
                INNER JOIN dbo.TurmaDisciplinas td
                    ON td.IdTurmaDisciplina =
                        ptd.IdTurmaDisciplina
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Professores p
                    ON p.IdProfessor = ptd.IdProfessor
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = p.IdUsuario
                WHERE ptd.Ativo = 1
                    AND (
                        @idTurma = 0
                        OR td.IdTurma = @idTurma
                    )
                    AND (
                        @pesquisa = N''
                        OR t.CodigoTurma LIKE
                            N'%' + @pesquisa + N'%'
                        OR d.NomeDisciplina LIKE
                            N'%' + @pesquisa + N'%'
                        OR u.NomeCompleto LIKE
                            N'%' + @pesquisa + N'%'
                        OR p.RegistroProfessor LIKE
                            N'%' + @pesquisa + N'%'
                    )
                ORDER BY
                    t.CodigoTurma,
                    d.NomeDisciplina;
            `);

        return res.status(200).json({
            alocacoes: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Valida a situação acadêmica dos registros envolvidos.
async function validarDadosAlocacao(
    transacao,
    idVinculo,
    idProfessor
) {
    const resultado = await new sql.Request(transacao)
        .input("idVinculo", sql.Int, idVinculo)
        .input("idProfessor", sql.Int, idProfessor)
        .query(`
            SELECT COUNT(1) AS Total
            FROM dbo.TurmaDisciplinas td
                WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.Turmas t
                ON t.IdTurma = td.IdTurma
            INNER JOIN dbo.Disciplinas d
                ON d.IdDisciplina = td.IdDisciplina
            INNER JOIN dbo.Professores p
                ON p.IdProfessor = @idProfessor
            INNER JOIN dbo.Usuarios u
                ON u.IdUsuario = p.IdUsuario
            INNER JOIN dbo.Perfis pf
                ON pf.IdPerfil = u.IdPerfil
            WHERE td.IdTurmaDisciplina = @idVinculo
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
                AND u.Ativo = 1
                AND pf.Ativo = 1
                AND pf.NomePerfil = 'Professor';
        `);

    if (resultado.recordset[0].Total === 0) {
        throw new ErroOperacao(
            400,
            "O professor, a turma e a disciplina devem estar ativos."
        );
    }
}

// Consulta o vínculo ativo com bloqueio transacional.
async function consultarAlocacaoAtual(
    transacao,
    idVinculo
) {
    const resultado = await new sql.Request(transacao)
        .input("idVinculo", sql.Int, idVinculo)
        .query(`
            SELECT
                IdVinculo AS IdAlocacao,
                IdProfessor,
                DataVinculo AS DataAlocacao
            FROM dbo.ProfessorTurmaDisciplinas
                WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTurmaDisciplina = @idVinculo
                AND Ativo = 1;
        `);

    if (resultado.recordset.length > 1) {
        throw new ErroOperacao(
            409,
            "Existem múltiplos professores ativos nesta disciplina."
        );
    }

    return resultado.recordset[0] ?? null;
}

// Cria um vínculo ou reativa um vínculo anteriormente desativado.
async function ativarVinculoProfessor(
    transacao,
    idVinculo,
    idProfessor
) {
    const resultado = await new sql.Request(transacao)
        .input("idVinculo", sql.Int, idVinculo)
        .input("idProfessor", sql.Int, idProfessor)
        .query(`
            SELECT
                IdVinculo,
                Ativo
            FROM dbo.ProfessorTurmaDisciplinas
                WITH (UPDLOCK, HOLDLOCK)
            WHERE IdProfessor = @idProfessor
                AND IdTurmaDisciplina = @idVinculo;
        `);

    const vinculoExistente = resultado.recordset[0];

    if (vinculoExistente?.Ativo) {
        throw new ErroOperacao(
            409,
            "O professor já está vinculado a esta disciplina."
        );
    }

    // Reativa o registro para respeitar a restrição de unicidade.
    if (vinculoExistente) {
        await new sql.Request(transacao)
            .input(
                "idAlocacao",
                sql.Int,
                vinculoExistente.IdVinculo
            )
            .query(`
                UPDATE dbo.ProfessorTurmaDisciplinas
                SET
                    Ativo = 1,
                    DataVinculo = SYSDATETIME()
                WHERE IdVinculo = @idAlocacao;
            `);

        return {
            idAlocacao: vinculoExistente.IdVinculo,
            reativado: true
        };
    }

    // Cria um novo registro quando não existe vínculo anterior.
    const insercao = await new sql.Request(transacao)
        .input("idVinculo", sql.Int, idVinculo)
        .input("idProfessor", sql.Int, idProfessor)
        .query(`
            INSERT INTO dbo.ProfessorTurmaDisciplinas (
                IdProfessor,
                IdTurmaDisciplina,
                DataVinculo,
                Ativo
            )
            OUTPUT INSERTED.IdVinculo
            VALUES (
                @idProfessor,
                @idVinculo,
                SYSDATETIME(),
                1
            );
        `);

    return {
        idAlocacao: insercao.recordset[0].IdVinculo,
        reativado: false
    };
}

// Aloca um professor a uma disciplina sem responsável.
export async function alocarProfessor(req, res) {
    try {
        const idVinculo = validarId(req.body?.idVinculo);
        const idProfessor = validarId(req.body?.idProfessor);

        const idUsuario = req.usuario.idUsuario;

        const resultado = await executarTransacao(
            async transacao => {
                await exigirAutorizacao(
                    transacao,
                    idUsuario
                );

                await validarDadosAlocacao(
                    transacao,
                    idVinculo,
                    idProfessor
                );

                const alocacaoAtual =
                    await consultarAlocacaoAtual(
                        transacao,
                        idVinculo
                    );

                if (alocacaoAtual) {
                    throw new ErroOperacao(
                        409,
                        "Esta disciplina já possui professor responsável. " +
                        "Utilize Substituir professor."
                    );
                }

                const alocacao =
                    await ativarVinculoProfessor(
                        transacao,
                        idVinculo,
                        idProfessor
                    );

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "ALOCAR_PROFESSOR",
                    `Professor ${idProfessor} alocado ` +
                    `ao vínculo acadêmico ${idVinculo}. ` +
                    `Alocação ${alocacao.idAlocacao}.`
                );

                return alocacao;
            }
        );

        return res.status(
            resultado.reativado ? 200 : 201
        ).json({
            mensagem: resultado.reativado
                ? "Alocação reativada com sucesso!"
                : "Professor alocado com sucesso!",
            idAlocacao: resultado.idAlocacao,
            reativado: resultado.reativado
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Substitui o responsável sem excluir o vínculo anterior.
export async function substituirProfessor(req, res) {
    try {
        const idVinculo = validarId(req.params.idVinculo);

        const idProfessorNovo = validarId(
            req.body?.idProfessor
        );

        const idUsuario = req.usuario.idUsuario;

        const resultado = await executarTransacao(
            async transacao => {
                await exigirAutorizacao(
                    transacao,
                    idUsuario
                );

                await validarDadosAlocacao(
                    transacao,
                    idVinculo,
                    idProfessorNovo
                );

                const alocacaoAnterior =
                    await consultarAlocacaoAtual(
                        transacao,
                        idVinculo
                    );

                if (!alocacaoAnterior) {
                    throw new ErroOperacao(
                        404,
                        "Esta disciplina não possui professor responsável."
                    );
                }

                if (
                    alocacaoAnterior.IdProfessor ===
                    idProfessorNovo
                ) {
                    throw new ErroOperacao(
                        409,
                        "O professor selecionado já é o responsável."
                    );
                }

                // Desativa o vínculo anterior, preservando o registro.
                const desativacao = await new sql.Request(transacao)
                    .input(
                        "idAlocacao",
                        sql.Int,
                        alocacaoAnterior.IdAlocacao
                    )
                    .query(`
                        UPDATE dbo.ProfessorTurmaDisciplinas
                        SET Ativo = 0
                        WHERE IdVinculo = @idAlocacao
                            AND Ativo = 1;
                    `);

                if (desativacao.rowsAffected[0] !== 1) {
                    throw new ErroOperacao(
                        409,
                        "Não foi possível desativar o vínculo anterior."
                    );
                }

                // Cria ou reativa o vínculo do novo responsável.
                const novaAlocacao =
                    await ativarVinculoProfessor(
                        transacao,
                        idVinculo,
                        idProfessorNovo
                    );

                // Registra a substituição na auditoria.
                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    "SUBSTITUIR_PROFESSOR",
                    `Vínculo acadêmico ${idVinculo}. ` +
                    `Professor anterior: ${alocacaoAnterior.IdProfessor}. ` +
                    `Novo professor: ${idProfessorNovo}. ` +
                    `Alocação anterior: ${alocacaoAnterior.IdAlocacao}. ` +
                    `Data anterior: ${alocacaoAnterior.DataAlocacao.toISOString()}. ` +
                    `Nova alocação: ${novaAlocacao.idAlocacao}.`
                );

                return novaAlocacao;
            }
        );

        return res.status(200).json({
            mensagem: "Professor substituído com sucesso!",
            idAlocacao: resultado.idAlocacao,
            reativado: resultado.reativado
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}