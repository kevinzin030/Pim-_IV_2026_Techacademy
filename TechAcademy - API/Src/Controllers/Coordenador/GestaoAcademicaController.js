import sql from "mssql";
import { conectarBanco } from "../../Config/database.js";

// Define as entidades acadêmicas permitidas.
const entidades = {
    disciplinas: {
        tabela: "Disciplinas",
        id: "IdDisciplina",
        codigo: "CodigoDisciplina",
        nome: "NomeDisciplina",
        tamanhoNome: 150,
        permissao: "GERENCIAR_DISCIPLINAS",
        acao: "DISCIPLINA",
        campos: ["CargaHoraria"]
    },

    turmas: {
        tabela: "Turmas",
        id: "IdTurma",
        codigo: "CodigoTurma",
        nome: "NomeTurma",
        tamanhoNome: 100,
        permissao: "GERENCIAR_TURMAS",
        acao: "TURMA",
        campos: ["Periodo", "AnoLetivo", "Semestre"]
    }
};

// Representa erros de validação e autorização.
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

// Identifica a entidade solicitada.
function obterEntidade(req) {
    const nome = req.params.entidade;

    if (!Object.hasOwn(entidades, nome)) {
        throw new ErroOperacao(
            404,
            "Entidade acadêmica não encontrada."
        );
    }

    return entidades[nome];
}

// Valida o identificador recebido.
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

// Valida os campos textuais.
function validarTexto(valor, tamanho, campo) {
    if (
        typeof valor !== "string" ||
        !valor.trim() ||
        valor.trim().length > tamanho
    ) {
        throw new ErroOperacao(
            400,
            `${campo} inválido.`
        );
    }

    return valor.trim();
}

// Confirma a autorização do coordenador.
async function exigirAutorizacao(
    origem,
    idUsuario,
    permissao
) {
    const resultado = await new sql.Request(origem)
        .input("idUsuario", sql.Int, idUsuario)
        .input("permissao", sql.NVarChar(100), permissao)
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
                AND pm.Codigo = @permissao
        `);

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui autorização para esta operação."
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

// Registra a operação na auditoria.
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
            mensagem: "Já existe um registro com esses dados."
        });
    }

    console.error("Erro na gestão acadêmica:", erro);

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

// Lista disciplinas ou turmas, incluindo registros inativos.
export async function listarRegistrosAcademicos(req, res) {
    try {
        const entidade = obterEntidade(req);

        const pesquisa = req.query.pesquisa ?? "";

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
            req.usuario.idUsuario,
            entidade.permissao
        );

        const colunas = [
            entidade.id,
            entidade.codigo,
            entidade.nome,
            ...entidade.campos,
            "Ativa"
        ].join(", ");

        // Os nomes das tabelas e colunas são definidos
        // exclusivamente pela configuração interna.
        const resultado = await conexao.request()
            .input(
                "pesquisa",
                sql.NVarChar(152),
                `%${pesquisa.trim()}%`
            )
            .query(`
                SELECT ${colunas}
                FROM dbo.${entidade.tabela}
                WHERE ${entidade.codigo} LIKE @pesquisa
                    OR ${entidade.nome} LIKE @pesquisa
                ORDER BY ${entidade.nome}
            `);

        return res.status(200).json({
            registros: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Valida os dados específicos de cada entidade.
function validarDados(entidade, corpo) {
    const dados = {
        codigo: validarTexto(
            corpo.codigo,
            30,
            "Código"
        ),

        nome: validarTexto(
            corpo.nome,
            entidade.tamanhoNome,
            "Nome"
        )
    };

    if (entidade.tabela === "Disciplinas") {
        const cargaHoraria = Number(corpo.cargaHoraria);

        if (
            !Number.isInteger(cargaHoraria) ||
            cargaHoraria <= 0 ||
            cargaHoraria > 2147483647
        ) {
            throw new ErroOperacao(
                400,
                "Carga horária inválida."
            );
        }

        dados.cargaHoraria = cargaHoraria;

    } else {
        const periodos = [
            "Matutino",
            "Vespertino",
            "Noturno",
            "Integral"
        ];

        const anoLetivo = Number(corpo.anoLetivo);
        const semestre = Number(corpo.semestre);

        if (!periodos.includes(corpo.periodo)) {
            throw new ErroOperacao(
                400,
                "Período inválido."
            );
        }

        if (
            !Number.isInteger(anoLetivo) ||
            anoLetivo < 2000 ||
            anoLetivo > 2100
        ) {
            throw new ErroOperacao(
                400,
                "Ano letivo inválido."
            );
        }

        // Valida os quatro semestres do curso.
        if (![1, 2, 3, 4].includes(semestre)) {
            throw new ErroOperacao(
                400,
                "O semestre deve estar entre 1 e 4."
            );
        }

        dados.periodo = corpo.periodo;
        dados.anoLetivo = anoLetivo;
        dados.semestre = semestre;
    }

    return dados;
}

// Impede a duplicidade de códigos e nomes.
async function validarDuplicidade(
    transacao,
    entidade,
    dados,
    id
) {
    const resultado = await new sql.Request(transacao)
        .input("id", sql.Int, id ?? 0)
        .input("codigo", sql.NVarChar(30), dados.codigo)
        .input(
            "nome",
            sql.NVarChar(entidade.tamanhoNome),
            dados.nome
        )
        .query(`
            SELECT TOP 1 ${entidade.id}
            FROM dbo.${entidade.tabela}
                WITH (UPDLOCK, HOLDLOCK)
            WHERE ${entidade.id} <> @id
                AND (
                    ${entidade.codigo} = @codigo
                    OR UPPER(LTRIM(RTRIM(${entidade.nome})))
                        = UPPER(@nome)
                )
        `);

    if (resultado.recordset.length > 0) {
        throw new ErroOperacao(
            409,
            "Já existe um registro com este código ou nome."
        );
    }
}

// Prepara os parâmetros utilizados na gravação.
function prepararRequisicao(
    transacao,
    entidade,
    dados,
    id
) {
    const requisicao = new sql.Request(transacao)
        .input("id", sql.Int, id ?? 0)
        .input("codigo", sql.NVarChar(30), dados.codigo)
        .input(
            "nome",
            sql.NVarChar(entidade.tamanhoNome),
            dados.nome
        );

    if (entidade.tabela === "Disciplinas") {
        requisicao.input(
            "CargaHoraria",
            sql.Int,
            dados.cargaHoraria
        );

    } else {
        requisicao.input(
            "Periodo",
            sql.NVarChar(30),
            dados.periodo
        );

        requisicao.input(
            "AnoLetivo",
            sql.Int,
            dados.anoLetivo
        );

        requisicao.input(
            "Semestre",
            sql.Int,
            dados.semestre
        );
    }

    return requisicao;
}

// Cadastra ou atualiza uma entidade acadêmica.
export async function salvarRegistroAcademico(req, res) {
    try {
        const entidade = obterEntidade(req);

        const id = req.params.id
            ? validarId(req.params.id)
            : null;

        const dados = validarDados(
            entidade,
            req.body ?? {}
        );

        const idUsuario = req.usuario.idUsuario;

        const idRegistrado = await executarTransacao(
            async transacao => {
                await exigirAutorizacao(
                    transacao,
                    idUsuario,
                    entidade.permissao
                );

                await validarDuplicidade(
                    transacao,
                    entidade,
                    dados,
                    id
                );

                const requisicao = prepararRequisicao(
                    transacao,
                    entidade,
                    dados,
                    id
                );

                let consulta;

                if (id !== null) {
                    const atualizacoes = [
                        `${entidade.codigo} = @codigo`,
                        `${entidade.nome} = @nome`,
                        ...entidade.campos.map(
                            campo => `${campo} = @${campo}`
                        )
                    ].join(", ");

                    consulta = `
                        UPDATE dbo.${entidade.tabela}
                        SET ${atualizacoes}
                        OUTPUT INSERTED.${entidade.id} AS Id
                        WHERE ${entidade.id} = @id
                    `;

                } else {
                    const colunas = [
                        entidade.codigo,
                        entidade.nome,
                        ...entidade.campos,
                        "Ativa"
                    ].join(", ");

                    const valores = [
                        "@codigo",
                        "@nome",
                        ...entidade.campos.map(
                            campo => `@${campo}`
                        ),
                        "1"
                    ].join(", ");

                    consulta = `
                        INSERT INTO dbo.${entidade.tabela}
                            (${colunas})
                        OUTPUT INSERTED.${entidade.id} AS Id
                        VALUES (${valores})
                    `;
                }

                const resultado =
                    await requisicao.query(consulta);

                const registro = resultado.recordset[0];

                if (!registro) {
                    throw new ErroOperacao(
                        404,
                        "Registro não encontrado."
                    );
                }

                const acao = id !== null
                    ? `ALTERACAO_${entidade.acao}`
                    : `CADASTRO_${entidade.acao}`;

                await registrarAuditoria(
                    transacao,
                    idUsuario,
                    acao,
                    `${id !== null ? "Alterou" : "Cadastrou"} ` +
                    `${dados.codigo} - ${dados.nome} ` +
                    `(ID ${registro.Id}).`
                );

                return registro.Id;
            }
        );

        return res.status(id !== null ? 200 : 201).json({
            mensagem: id !== null
                ? "Registro atualizado com sucesso!"
                : "Registro cadastrado com sucesso!",
            id: idRegistrado
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

// Ativa ou desativa uma entidade acadêmica.
export async function alterarStatusAcademico(req, res) {
    try {
        const entidade = obterEntidade(req);

        const id = validarId(req.params.id);

        const { ativa } = req.body ?? {};

        if (typeof ativa !== "boolean") {
            throw new ErroOperacao(
                400,
                "Status inválido."
            );
        }

        const idUsuario = req.usuario.idUsuario;

        await executarTransacao(async transacao => {
            await exigirAutorizacao(
                transacao,
                idUsuario,
                entidade.permissao
            );

            const resultado = await new sql.Request(transacao)
                .input("id", sql.Int, id)
                .input("ativa", sql.Bit, ativa)
                .query(`
                    UPDATE dbo.${entidade.tabela}
                    SET Ativa = @ativa
                    OUTPUT INSERTED.Ativa
                    WHERE ${entidade.id} = @id
                `);

            if (resultado.recordset.length === 0) {
                throw new ErroOperacao(
                    404,
                    "Registro não encontrado."
                );
            }

            const acao = ativa
                ? `ATIVACAO_${entidade.acao}`
                : `DESATIVACAO_${entidade.acao}`;

            await registrarAuditoria(
                transacao,
                idUsuario,
                acao,
                `Registro ID ${id} ` +
                `${ativa ? "ativado" : "desativado"}.`
            );
        });

        return res.status(200).json({
            mensagem: ativa
                ? "Registro ativado com sucesso!"
                : "Registro desativado com sucesso!",
            ativa
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}