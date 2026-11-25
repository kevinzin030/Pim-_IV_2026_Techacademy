import sql from "mssql";
import { conectarBanco } from "../../Config/database.js";

// Define os perfis reconhecidos pelo sistema.
const perfisPermitidos = [
    "Administrador",
    "Coordenador",
    "Professor",
    "Aluno"
];

const permissaoAdministrativa = "GERENCIAR_ACESSOS";

/**
 * Identifica a plataforma correspondente ao perfil.
 */
function obterPlataforma(nomePerfil) {
    return ["Administrador", "Coordenador"].includes(nomePerfil)
        ? "Desktop"
        : "Web/Mobile";
}

/**
 * Representa um erro de validação ou autorização.
 */
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

/**
 * Verifica a autorização administrativa diretamente no banco.
 */
async function validarAdministrador(origem, idUsuario) {
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
                AND p.NomePerfil = 'Administrador'
                AND pm.Ativo = 1
                AND pm.Codigo = 'GERENCIAR_ACESSOS'
        `);

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui permissão para gerenciar acessos."
        );
    }
}

/**
 * Padroniza o tratamento dos erros do controller.
 */
function responderErro(res, erro) {
    if (erro instanceof ErroOperacao) {
        return res.status(erro.status).json({
            mensagem: erro.message
        });
    }

    console.error("Erro no gerenciamento de perfis:", erro);

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

/**
 * Lista os perfis cadastrados no sistema.
 */
export async function listarPerfis(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            throw new ErroOperacao(
                401,
                "Usuário não autenticado."
            );
        }

        const conexao = await conectarBanco();

        await validarAdministrador(conexao, idUsuario);

        const resultado = await conexao.request().query(`
            SELECT
                IdPerfil,
                NomePerfil,
                Descricao,
                Ativo
            FROM dbo.Perfis
            WHERE NomePerfil IN (
                'Administrador',
                'Coordenador',
                'Professor',
                'Aluno'
            )
            ORDER BY IdPerfil
        `);

        return res.status(200).json({
            perfis: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Atualiza os dados e as permissões de um perfil.
 * Todas as alterações são executadas atomicamente.
 */
export async function atualizarPerfil(req, res) {
    let transacao;
    let transacaoAtiva = false;

    try {
        const idUsuario = req.usuario?.idUsuario;
        const idPerfil = Number(req.params.idPerfil);

        const {
            descricao,
            ativo,
            permissoes
        } = req.body ?? {};

        // Valida a sessão do usuário.
        if (!idUsuario) {
            throw new ErroOperacao(
                401,
                "Usuário não autenticado."
            );
        }

        // Valida o identificador do perfil.
        if (
            !Number.isInteger(idPerfil) ||
            idPerfil <= 0 ||
            idPerfil > 2147483647
        ) {
            throw new ErroOperacao(
                400,
                "Identificador do perfil inválido."
            );
        }

        // Valida os dados cadastrais recebidos.
        if (
            typeof descricao !== "string" ||
            descricao.trim().length === 0 ||
            descricao.trim().length > 200
        ) {
            throw new ErroOperacao(
                400,
                "Informe uma descrição válida para o perfil."
            );
        }

        if (typeof ativo !== "boolean") {
            throw new ErroOperacao(
                400,
                "Situação do perfil inválida."
            );
        }

        // Valida os identificadores das permissões.
        if (
            !Array.isArray(permissoes) ||
            permissoes.some(id =>
                !Number.isInteger(id) ||
                id <= 0 ||
                id > 2147483647
            )
        ) {
            throw new ErroOperacao(
                400,
                "Lista de permissões inválida."
            );
        }

        // Remove identificadores duplicados.
        const permissoesSelecionadas = new Set(permissoes);

        const conexao = await conectarBanco();

        // Inicia uma transação com isolamento serializável.
        transacao = new sql.Transaction(conexao);

        await transacao.begin(
            sql.ISOLATION_LEVEL.SERIALIZABLE
        );

        transacaoAtiva = true;

        // Confirma a autorização dentro da própria transação.
        await validarAdministrador(transacao, idUsuario);

        // Consulta o perfil que será atualizado.
        const resultadoPerfil = await new sql.Request(transacao)
            .input("idPerfil", sql.Int, idPerfil)
            .query(`
                SELECT
                    IdPerfil,
                    NomePerfil,
                    Ativo
                FROM dbo.Perfis WITH (UPDLOCK, HOLDLOCK)
                WHERE IdPerfil = @idPerfil
            `);

        const perfil = resultadoPerfil.recordset[0];

        if (
            !perfil ||
            !perfisPermitidos.includes(perfil.NomePerfil)
        ) {
            throw new ErroOperacao(
                404,
                "Perfil não encontrado."
            );
        }

        const nomePerfil = perfil.NomePerfil;
        const plataforma = obterPlataforma(nomePerfil);

        // Impede a desativação do perfil Administrador.
        if (nomePerfil === "Administrador" && !ativo) {
            throw new ErroOperacao(
                400,
                "O perfil Administrador deve permanecer ativo."
            );
        }

        // Impede a desativação de perfis com usuários vinculados.
        if (!ativo) {
            const resultadoUsuarios =
                await new sql.Request(transacao)
                    .input("idPerfil", sql.Int, idPerfil)
                    .query(`
                        SELECT COUNT(1) AS Total
                        FROM dbo.Usuarios
                        WHERE IdPerfil = @idPerfil
                    `);

            if (resultadoUsuarios.recordset[0].Total > 0) {
                throw new ErroOperacao(
                    400,
                    "Não é possível desativar um perfil que possui usuários vinculados."
                );
            }
        }

        // Recupera as permissões disponíveis para validação.
        const resultadoPermissoes =
            await new sql.Request(transacao).query(`
                SELECT
                    IdPermissao,
                    Codigo,
                    Plataforma
                FROM dbo.Permissoes WITH (HOLDLOCK)
                WHERE Ativo = 1
            `);

        const permissoesValidas = new Map(
            resultadoPermissoes.recordset.map(permissao => [
                permissao.IdPermissao,
                permissao
            ])
        );

        // Valida cada permissão selecionada.
        for (const idPermissao of permissoesSelecionadas) {
            const permissao = permissoesValidas.get(idPermissao);

            if (!permissao) {
                throw new ErroOperacao(
                    400,
                    "Uma das permissões selecionadas não está mais disponível."
                );
            }

            // Impede permissões incompatíveis com a plataforma.
            if (
                permissao.Plataforma.toLowerCase() !==
                plataforma.toLowerCase()
            ) {
                throw new ErroOperacao(
                    400,
                    "Permissão incompatível com a plataforma do perfil."
                );
            }

            // Restringe o gerenciamento de acessos ao Administrador.
            if (
                permissao.Codigo === permissaoAdministrativa &&
                nomePerfil !== "Administrador"
            ) {
                throw new ErroOperacao(
                    400,
                    "O gerenciamento de acessos é exclusivo do Administrador."
                );
            }
        }

        // Garante que o Administrador mantenha acesso ao gerenciamento.
        if (nomePerfil === "Administrador") {
            const permissaoObrigatoria =
                resultadoPermissoes.recordset.find(permissao =>
                    permissao.Codigo === permissaoAdministrativa &&
                    permissao.Plataforma.toLowerCase() === "desktop"
                );

            if (!permissaoObrigatoria) {
                throw new ErroOperacao(
                    409,
                    "A permissão GERENCIAR_ACESSOS não está disponível."
                );
            }

            permissoesSelecionadas.add(
                permissaoObrigatoria.IdPermissao
            );
        }

        // Atualiza a descrição e a situação do perfil.
        await new sql.Request(transacao)
            .input("idPerfil", sql.Int, idPerfil)
            .input("descricao", sql.NVarChar(200), descricao.trim())
            .input("ativo", sql.Bit, ativo)
            .query(`
                UPDATE dbo.Perfis
                SET
                    Descricao = @descricao,
                    Ativo = @ativo
                WHERE IdPerfil = @idPerfil
            `);

        // Remove somente os vínculos da plataforma correspondente.
        await new sql.Request(transacao)
            .input("idPerfil", sql.Int, idPerfil)
            .input("plataforma", sql.NVarChar(50), plataforma)
            .query(`
                DELETE pp
                FROM dbo.PerfilPermissoes pp
                INNER JOIN dbo.Permissoes pm
                    ON pm.IdPermissao = pp.IdPermissao
                WHERE pp.IdPerfil = @idPerfil
                    AND pm.Plataforma = @plataforma
            `);

        // Registra os novos vínculos de permissões.
        for (const idPermissao of permissoesSelecionadas) {
            await new sql.Request(transacao)
                .input("idPerfil", sql.Int, idPerfil)
                .input("idPermissao", sql.Int, idPermissao)
                .query(`
                    INSERT INTO dbo.PerfilPermissoes (
                        IdPerfil,
                        IdPermissao
                    )
                    VALUES (
                        @idPerfil,
                        @idPermissao
                    )
                `);
        }

        // Registra a operação para fins de auditoria.
        const descricaoAuditoria =
            `Alterou o perfil ${nomePerfil} (ID ${idPerfil}). ` +
            `Plataforma: ${plataforma}. ` +
            `Permissões selecionadas: ${permissoesSelecionadas.size}. ` +
            `Ativo: ${ativo}.`;

        await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .input(
                "acao",
                sql.NVarChar(100),
                "ALTERACAO_PERMISSOES"
            )
            .input(
                "descricao",
                sql.NVarChar(500),
                descricaoAuditoria
            )
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

        // Confirma todas as alterações realizadas.
        await transacao.commit();

        transacaoAtiva = false;

        return res.status(200).json({
            mensagem: "Permissões atualizadas com sucesso!"
        });

    } catch (erro) {
        // Desfaz as alterações caso alguma operação falhe.
        if (transacaoAtiva && transacao) {
            try {
                await transacao.rollback();
            } catch (erroRollback) {
                console.error(
                    "Erro ao desfazer transação:",
                    erroRollback.message
                );
            }
        }

        return responderErro(res, erro);
    }
}