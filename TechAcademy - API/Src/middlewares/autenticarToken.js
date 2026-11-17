
import jwt from "jsonwebtoken";
import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

// Valida o token JWT e verifica se o usuário permanece autorizado.
export async function autenticarToken(req, res, next) {
    // Recupera o token enviado no cabeçalho Authorization.
    const autorizacao = req.headers.authorization;

    if (
        typeof autorizacao !== "string" ||
        !autorizacao.startsWith("Bearer ")
    ) {
        return res.status(401).json({
            mensagem: "Token de autenticação não informado."
        });
    }

    // Extrai o token removendo o prefixo Bearer.
    const token = autorizacao.slice(7);

    if (!token || token.trim() !== token) {
        return res.status(401).json({
            mensagem: "Token de autenticação inválido."
        });
    }

    // Recupera a chave utilizada para validar o JWT.
    const chaveJwt = process.env.JWT_SECRET;

    if (!chaveJwt || chaveJwt.length < 32) {
        console.error("JWT_SECRET não configurada corretamente.");

        return res.status(500).json({
            mensagem: "Erro interno de autenticação."
        });
    }

    let dadosUsuario;

    try {
        // Verifica a assinatura, o algoritmo e a expiração do token.
        dadosUsuario = jwt.verify(token, chaveJwt, {
            algorithms: ["HS256"]
        });

    } catch (erro) {
        if (erro.name === "TokenExpiredError") {
            return res.status(401).json({
                mensagem: "Sessão expirada. Realize o login novamente."
            });
        }

        return res.status(401).json({
            mensagem: "Token de autenticação inválido."
        });
    }

    // Confirma que o token contém uma identificação válida.
    if (
        typeof dadosUsuario !== "object" ||
        !Number.isInteger(dadosUsuario.idUsuario) ||
        dadosUsuario.idUsuario <= 0 ||
        !Number.isInteger(dadosUsuario.idPerfil) ||
        dadosUsuario.idPerfil <= 0
    ) {
        return res.status(401).json({
            mensagem: "Token de autenticação inválido."
        });
    }

    try {
        // Consulta os dados atuais do usuário no Azure SQL.
        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .input(
                "idUsuario",
                sql.Int,
                dadosUsuario.idUsuario
            )
            .query(`
                SELECT
                    IdUsuario,
                    IdPerfil,
                    Ativo,
                    DeveAlterarSenha
                FROM dbo.Usuarios
                WHERE IdUsuario = @idUsuario
            `);

        const usuario = resultado.recordset[0];

        // Impede o acesso de usuários removidos do sistema.
        if (!usuario) {
            return res.status(401).json({
                mensagem: "Usuário não encontrado. Realize o login novamente."
            });
        }

        // Impede o acesso de contas desativadas.
        if (!usuario.Ativo) {
            return res.status(403).json({
                mensagem: "Conta desativada. Acesso negado."
            });
        }

        // Invalida tokens emitidos antes de uma mudança de perfil.
        if (usuario.IdPerfil !== dadosUsuario.idPerfil) {
            return res.status(401).json({
                mensagem: "Permissões alteradas. Realize o login novamente."
            });
        }

        // Disponibiliza os dados atualizados para as próximas rotas.
        req.usuario = {
            idUsuario: usuario.IdUsuario,
            idPerfil: usuario.IdPerfil,
            deveAlterarSenha: Boolean(usuario.DeveAlterarSenha)
        };

        // Permite que a requisição continue.
        return next();

    } catch (erro) {
        console.error(
            "Erro ao validar usuário no Azure SQL:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível validar a sessão."
        });
    }
}
