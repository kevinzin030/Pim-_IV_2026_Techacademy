import sql from "mssql";
import { conectarBanco } from "../../Config/database.js";
import {
    validarNovaSenha,
    gerarHashSenha,
    verificarSenha
} from "../../Utils/senhaHelper.js";

// Redefine administrativamente a senha de um usuário.
export async function redefinirSenha(req, res) {
    const idAdministrador = req.usuario?.idUsuario;
    const idUsuario = Number(req.params.idUsuario);
    const { novaSenha } = req.body ?? {};

    // Valida o usuário de destino.
    if (
        !Number.isInteger(idUsuario) ||
        idUsuario <= 0 ||
        idUsuario > 2147483647
    ) {
        return res.status(400).json({
            mensagem: "Usuário inválido."
        });
    }

    // Aplica os requisitos de segurança da senha.
    const validacao = validarNovaSenha(novaSenha);

    if (!validacao.valida) {
        return res.status(400).json({
            mensagem: validacao.mensagem
        });
    }

    let transacao;
    let transacaoIniciada = false;

    try {
        const conexao = await conectarBanco();

        transacao = new sql.Transaction(conexao);

        await transacao.begin(
            sql.ISOLATION_LEVEL.SERIALIZABLE
        );

        transacaoIniciada = true;

        // Confirma a autorização administrativa.
        const autorizacao = await new sql.Request(transacao)
            .input("idAdministrador", sql.Int, idAdministrador)
            .query(`
                SELECT TOP 1 1 AS Autorizado
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                INNER JOIN dbo.PerfilPermissoes pp
                    ON pp.IdPerfil = p.IdPerfil
                INNER JOIN dbo.Permissoes pm
                    ON pm.IdPermissao = pp.IdPermissao
                WHERE u.IdUsuario = @idAdministrador
                    AND u.Ativo = 1
                    AND u.DeveAlterarSenha = 0
                    AND p.Ativo = 1
                    AND p.NomePerfil = 'Administrador'
                    AND pm.Ativo = 1
                    AND pm.Codigo = 'GERENCIAR_USUARIOS'
            `);

        if (autorizacao.recordset.length === 0) {
            await transacao.rollback();
            transacaoIniciada = false;

            return res.status(403).json({
                mensagem: "Você não possui autorização para redefinir senhas."
            });
        }

        // Consulta e bloqueia o registro durante a operação.
        const resultado = await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT SenhaHash
                FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                WHERE IdUsuario = @idUsuario
                    AND Ativo = 1
            `);

        const usuario = resultado.recordset[0];

        if (!usuario) {
            await transacao.rollback();
            transacaoIniciada = false;

            return res.status(404).json({
                mensagem: "Usuário não encontrado ou desativado."
            });
        }

        // Impede a reutilização da senha atual.
        const senhaReutilizada = await verificarSenha(
            novaSenha,
            usuario.SenhaHash
        );

        if (senhaReutilizada) {
            await transacao.rollback();
            transacaoIniciada = false;

            return res.status(400).json({
                mensagem: "A senha provisória deve ser diferente da senha atual."
            });
        }

        // Gera um novo hash PBKDF2-SHA256.
        const novoHash = await gerarHashSenha(novaSenha);

        // Atualiza a senha e exige sua alteração no próximo acesso.
        const atualizacao = await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .input("senhaHash", sql.NVarChar(500), novoHash)
            .query(`
                UPDATE dbo.Usuarios
                SET
                    SenhaHash = @senhaHash,
                    DeveAlterarSenha = 1
                WHERE IdUsuario = @idUsuario
                    AND Ativo = 1
            `);

        if (atualizacao.rowsAffected[0] !== 1) {
            throw new Error(
                "Não foi possível atualizar a senha."
            );
        }

        // Registra a operação sem armazenar a senha provisória.
        await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idAdministrador)
            .input(
                "acao",
                sql.NVarChar(100),
                "REDEFINICAO_SENHA"
            )
            .input(
                "descricao",
                sql.NVarChar(500),
                `Administrador redefiniu a senha provisória do usuário ID ${idUsuario}. Troca obrigatória no próximo acesso.`
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

        await transacao.commit();
        transacaoIniciada = false;

        return res.status(200).json({
            mensagem: "Senha redefinida com sucesso!"
        });

    } catch (erro) {
        if (transacaoIniciada) {
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
            "Erro ao redefinir senha:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível redefinir a senha."
        });
    }
}