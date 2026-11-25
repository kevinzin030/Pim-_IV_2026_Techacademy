import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

/**
 * Verifica se o usuário autenticado possui autorização
 * para gerenciar os perfis e permissões do sistema.
 */
async function possuiPermissaoAdministrativa(conexao, idUsuario) {
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
                AND p.Ativo = 1
                AND p.NomePerfil = 'Administrador'
                AND pm.Ativo = 1
                AND pm.Codigo = 'GERENCIAR_ACESSOS'
        `);

    return resultado.recordset.length > 0;
}

/**
 * Consulta as permissões ativas do usuário autenticado.
 * A identificação é obtida exclusivamente pelo JWT.
 */
export async function consultarMinhasPermissoes(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        // Consulta as permissões vinculadas ao perfil atual.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT DISTINCT
                    pm.IdPermissao,
                    pm.Codigo,
                    pm.Descricao,
                    pm.Plataforma
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                INNER JOIN dbo.PerfilPermissoes pp
                    ON p.IdPerfil = pp.IdPerfil
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE u.IdUsuario = @idUsuario
                    AND u.Ativo = 1
                    AND p.Ativo = 1
                    AND pm.Ativo = 1
                ORDER BY
                    pm.Plataforma,
                    pm.Descricao
            `);

        return res.status(200).json({
            permissoes: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar permissões:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as permissões."
        });
    }
}

/**
 * Lista todas as permissões ativas cadastradas no sistema.
 * A operação é restrita aos administradores autorizados.
 */
export async function listarPermissoes(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        // Valida a autorização diretamente no banco de dados.
        const autorizado = await possuiPermissaoAdministrativa(
            conexao,
            idUsuario
        );

        if (!autorizado) {
            return res.status(403).json({
                mensagem: "Você não possui permissão para gerenciar acessos."
            });
        }

        // Recupera todas as permissões disponíveis.
        const resultado = await conexao.request().query(`
            SELECT
                IdPermissao,
                Codigo,
                Descricao,
                Plataforma
            FROM dbo.Permissoes
            WHERE Ativo = 1
            ORDER BY
                Plataforma,
                Descricao
        `);

        return res.status(200).json({
            permissoes: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao listar permissões:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível listar as permissões."
        });
    }
}

/**
 * Consulta as permissões ativas atribuídas a um perfil.
 * Preserva as regras do PermissaoService original do Desktop.
 */
export async function listarPermissoesDoPerfil(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;
        const idPerfil = Number(req.params.idPerfil);

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        // Valida o identificador recebido pela rota.
        if (
            !Number.isInteger(idPerfil) ||
            idPerfil <= 0 ||
            idPerfil > 2147483647
        ) {
            return res.status(400).json({
                mensagem: "Identificador do perfil inválido."
            });
        }

        const conexao = await conectarBanco();

        // Impede consultas administrativas não autorizadas.
        const autorizado = await possuiPermissaoAdministrativa(
            conexao,
            idUsuario
        );

        if (!autorizado) {
            return res.status(403).json({
                mensagem: "Você não possui permissão para gerenciar acessos."
            });
        }

        // Verifica se o perfil solicitado existe.
        const resultadoPerfil = await conexao.request()
            .input("idPerfil", sql.Int, idPerfil)
            .query(`
                SELECT IdPerfil
                FROM dbo.Perfis
                WHERE IdPerfil = @idPerfil
            `);

        if (resultadoPerfil.recordset.length === 0) {
            return res.status(404).json({
                mensagem: "Perfil não encontrado."
            });
        }

        // Consulta as permissões associadas ao perfil.
        const resultado = await conexao.request()
            .input("idPerfil", sql.Int, idPerfil)
            .query(`
                SELECT
                    pm.IdPermissao,
                    pm.Codigo,
                    pm.Descricao,
                    pm.Plataforma
                FROM dbo.PerfilPermissoes pp
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE pp.IdPerfil = @idPerfil
                    AND pm.Ativo = 1
                ORDER BY
                    pm.Plataforma,
                    pm.Descricao
            `);

        return res.status(200).json({
            permissoes: resultado.recordset
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar permissões do perfil:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as permissões do perfil."
        });
    }
}