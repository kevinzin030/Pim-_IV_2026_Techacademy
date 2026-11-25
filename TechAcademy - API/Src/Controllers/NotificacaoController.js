import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

/**
 * Consulta as notificações do usuário autenticado.
 * Retorna as 50 notificações mais recentes e o total
 * de notificações ainda não lidas.
 */
export async function listarNotificacoes(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT TOP (50)
                    IdNotificacao AS idNotificacao,
                    Tipo AS tipo,
                    Titulo AS titulo,
                    Mensagem AS mensagem,
                    Lida AS lida,
                    DataCriacao AS dataCriacao
                FROM dbo.Notificacoes
                WHERE IdUsuario = @idUsuario
                ORDER BY
                    DataCriacao DESC,
                    IdNotificacao DESC;

                SELECT
                    COUNT(*) AS totalNaoLidas
                FROM dbo.Notificacoes
                WHERE
                    IdUsuario = @idUsuario
                    AND Lida = 0;
            `);

        return res.status(200).json({
            total: resultado.recordsets[0].length,
            totalNaoLidas:
                resultado.recordsets[1][0].totalNaoLidas,
            notificacoes: resultado.recordsets[0]
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar notificações:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as notificações."
        });
    }
}

/**
 * Marca uma notificação como lida.
 * Impede que um usuário altere notificações de terceiros.
 */
export async function marcarNotificacaoComoLida(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        const idNotificacao = Number(
            req.params.idNotificacao
        );

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        if (
            !Number.isInteger(idNotificacao) ||
            idNotificacao <= 0 ||
            idNotificacao > 2147483647
        ) {
            return res.status(400).json({
                mensagem: "Identificador de notificação inválido."
            });
        }

        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .input("idNotificacao", sql.Int, idNotificacao)
            .query(`
                UPDATE dbo.Notificacoes
                SET Lida = 1
                WHERE
                    IdNotificacao = @idNotificacao
                    AND IdUsuario = @idUsuario;
            `);

        if (resultado.rowsAffected[0] === 0) {
            return res.status(404).json({
                mensagem: "Notificação não encontrada."
            });
        }

        return res.status(200).json({
            mensagem: "Notificação marcada como lida."
        });

    } catch (erro) {
        console.error(
            "Erro ao atualizar notificação:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível atualizar a notificação."
        });
    }
}

/**
 * Marca todas as notificações do usuário como lidas.
 */
export async function marcarTodasComoLidas(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                UPDATE dbo.Notificacoes
                SET Lida = 1
                WHERE
                    IdUsuario = @idUsuario
                    AND Lida = 0;
            `);

        return res.status(200).json({
            mensagem: "Notificações marcadas como lidas.",
            totalAtualizadas: resultado.rowsAffected[0]
        });

    } catch (erro) {
        console.error(
            "Erro ao atualizar notificações:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível atualizar as notificações."
        });
    }
}