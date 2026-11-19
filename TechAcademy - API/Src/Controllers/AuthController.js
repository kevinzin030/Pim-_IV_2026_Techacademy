
import jwt from "jsonwebtoken";
import sql from "mssql";
import { conectarBanco } from "../Config/database.js";
import {
    verificarSenha,
    validarNovaSenha,
    gerarHashSenha
} from "../Utils/senhaHelper.js";

// Autentica o usuário utilizando as credenciais armazenadas no Azure SQL.
export async function realizarLogin(req, res) {
    try {
        const { login, senha } = req.body ?? {};

        if (
            typeof login !== "string" ||
            login.trim().length === 0 ||
            typeof senha !== "string" ||
            senha.length === 0
        ) {
            return res.status(400).json({
                mensagem: "Informe o login e a senha."
            });
        }

        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .input("login", sql.NVarChar(100), login.trim())
            .query(`
                SELECT
                    IdUsuario,
                    NomeCompleto,
                    Login,
                    SenhaHash,
                    IdPerfil,
                    Ativo,
                    DeveAlterarSenha
                FROM dbo.Usuarios
                WHERE Login = @login
            `);

        const usuario = resultado.recordset[0];

        if (!usuario) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        const senhaValida = await verificarSenha(
            senha,
            usuario.SenhaHash
        );

        if (!senhaValida) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        if (!usuario.Ativo) {
            return res.status(403).json({
                mensagem: "Conta desativada. Entre em contato com o administrador."
            });
        }

        const chaveJwt = process.env.JWT_SECRET;

        if (!chaveJwt || chaveJwt.length < 32) {
            console.error("JWT_SECRET não configurada corretamente.");

            return res.status(500).json({
                mensagem: "Erro interno de autenticação."
            });
        }

        const token = jwt.sign(
            {
                idUsuario: usuario.IdUsuario,
                idPerfil: usuario.IdPerfil
            },
            chaveJwt,
            {
                expiresIn: "2h",
                algorithm: "HS256"
            }
        );

        return res.status(200).json({
            mensagem: "Login realizado com sucesso.",
            token,
            usuario: {
                idUsuario: usuario.IdUsuario,
                nomeCompleto: usuario.NomeCompleto,
                login: usuario.Login,
                idPerfil: usuario.IdPerfil,
                deveAlterarSenha: Boolean(usuario.DeveAlterarSenha)
            }
        });
    } catch (erro) {
        console.error("Erro ao realizar login:", erro.message);

        return res.status(500).json({
            mensagem: "Não foi possível realizar o login."
        });
    }
}

// Altera a senha do usuário autenticado e atualiza o Azure SQL.
// O identificador do usuário é obtido do JWT validado pelo middleware.
export async function alterarSenha(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const {
            senhaAtual,
            novaSenha,
            confirmarNovaSenha
        } = req.body ?? {};

        if (
            typeof senhaAtual !== "string" ||
            typeof novaSenha !== "string" ||
            typeof confirmarNovaSenha !== "string" ||
            senhaAtual.length === 0
        ) {
            return res.status(400).json({
                mensagem: "Informe a senha atual, a nova senha e a confirmação."
            });
        }

        if (novaSenha !== confirmarNovaSenha) {
            return res.status(400).json({
                mensagem: "A confirmação não corresponde à nova senha."
            });
        }

        if (senhaAtual === novaSenha) {
            return res.status(400).json({
                mensagem: "A nova senha deve ser diferente da senha atual."
            });
        }

        const validacao = validarNovaSenha(novaSenha);

        if (!validacao.valida) {
            return res.status(400).json({
                mensagem: validacao.mensagem
            });
        }

        const conexao = await conectarBanco();

        const resultadoUsuario = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT IdUsuario, SenhaHash, Ativo
                FROM dbo.Usuarios
                WHERE IdUsuario = @idUsuario
            `);

        const usuario = resultadoUsuario.recordset[0];

        if (!usuario) {
            return res.status(404).json({
                mensagem: "Usuário não encontrado."
            });
        }

        if (!usuario.Ativo) {
            return res.status(403).json({
                mensagem: "Conta desativada. Entre em contato com o administrador."
            });
        }

        const senhaAtualValida = await verificarSenha(
            senhaAtual,
            usuario.SenhaHash
        );

        if (!senhaAtualValida) {
            return res.status(401).json({
                mensagem: "A senha atual está incorreta."
            });
        }

        const novoHash = await gerarHashSenha(novaSenha);

        // Atualiza o hash somente se a senha anterior não foi alterada
        // por outra operação durante o processamento.
        const resultadoAtualizacao = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .input("hashAnterior", sql.NVarChar(sql.MAX), usuario.SenhaHash)
            .input("novoHash", sql.NVarChar(sql.MAX), novoHash)
            .query(`
                UPDATE dbo.Usuarios
                SET
                    SenhaHash = @novoHash,
                    DeveAlterarSenha = 0
                WHERE
                    IdUsuario = @idUsuario
                    AND SenhaHash = @hashAnterior
                    AND Ativo = 1
            `);

        if (resultadoAtualizacao.rowsAffected[0] !== 1) {
            return res.status(409).json({
                mensagem: "A senha foi modificada durante a operação. Tente novamente."
            });
        }

        return res.status(200).json({
            mensagem: "Senha alterada com sucesso.",
            deveAlterarSenha: false
        });
    } catch (erro) {
        console.error("Erro ao alterar senha:", erro.message);

        return res.status(500).json({
            mensagem: "Não foi possível alterar a senha."
        });
    }
}
