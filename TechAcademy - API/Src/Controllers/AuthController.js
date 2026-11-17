
import jwt from "jsonwebtoken";
import sql from "mssql";

import { conectarBanco } from "../Config/database.js";
import { verificarSenha } from "../Utils/senhaHelper.js";

// Autentica um usuário utilizando as credenciais armazenadas no Azure SQL.
export async function realizarLogin(req, res) {
    try {
        // Recupera e valida as credenciais enviadas na requisição.
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

        // Consulta o usuário por meio de um parâmetro SQL.
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

        // Retorna uma mensagem genérica para credenciais inválidas.
        if (!usuario) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        // Verifica a senha com o mesmo algoritmo utilizado pelo Desktop.
        const senhaValida = await verificarSenha(
            senha,
            usuario.SenhaHash
        );

        if (!senhaValida) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        // Impede o acesso de contas desativadas.
        if (!usuario.Ativo) {
            return res.status(403).json({
                mensagem: "Conta desativada. Entre em contato com o administrador."
            });
        }

        // Verifica se a chave de assinatura está configurada.
        const chaveJwt = process.env.JWT_SECRET;

        if (!chaveJwt || chaveJwt.length < 32) {
            console.error("JWT_SECRET não configurada corretamente.");

            return res.status(500).json({
                mensagem: "Erro interno de autenticação."
            });
        }

        // Gera um token com identificação e perfil do usuário.
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

        // Retorna o token e os dados públicos necessários ao aplicativo.
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
