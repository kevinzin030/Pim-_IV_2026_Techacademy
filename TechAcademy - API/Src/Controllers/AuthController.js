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
        const { login, senha, plataforma } = req.body ?? {};

        // Valida as credenciais recebidas na requisição.
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

        // Consulta os dados cadastrais e o perfil do usuário.
        const resultado = await conexao.request()
            .input("login", sql.NVarChar(100), login.trim())
            .query(`
                SELECT
                    u.IdUsuario,
                    u.NomeCompleto,
                    u.Login,
                    u.SenhaHash,
                    u.IdPerfil,
                    u.Ativo,
                    u.DeveAlterarSenha,
                    u.DataCadastro,
                    p.NomePerfil,
                    p.Ativo AS PerfilAtivo
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.Login = @login
            `);

        const usuario = resultado.recordset[0];

        // Impede a autenticação de usuários inexistentes.
        if (!usuario) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        // Compara a senha informada com o hash armazenado.
        const senhaValida = await verificarSenha(
            senha,
            usuario.SenhaHash
        );

        if (!senhaValida) {
            return res.status(401).json({
                mensagem: "Login ou senha inválidos."
            });
        }

        // Impede o acesso de contas ou perfis desativados.
        if (!usuario.Ativo || !usuario.PerfilAtivo) {
            return res.status(403).json({
                mensagem: "Conta desativada. Entre em contato com o administrador."
            });
        }

        // Restringe o acesso solicitado pelo painel administrativo.
        // Os endpoints administrativos também devem validar
        // as permissões do usuário diretamente no servidor.
        if (
            plataforma === "desktop" &&
            !["Administrador", "Coordenador"].includes(usuario.NomePerfil)
        ) {
            return res.status(403).json({
                mensagem: "Perfil não autorizado a acessar o painel administrativo."
            });
        }

        // Obtém a chave secreta utilizada na assinatura do JWT.
        const chaveJwt = process.env.JWT_SECRET;

        if (!chaveJwt || chaveJwt.length < 32) {
            console.error("JWT_SECRET não configurada corretamente.");

            return res.status(500).json({
                mensagem: "Erro interno de autenticação."
            });
        }

        // Gera o token de autenticação com validade de duas horas.
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

        // Retorna os dados necessários para as plataformas,
        // sem expor o hash da senha.
        return res.status(200).json({
            mensagem: "Login realizado com sucesso.",
            token,
            usuario: {
                idUsuario: usuario.IdUsuario,
                nomeCompleto: usuario.NomeCompleto,
                login: usuario.Login,
                idPerfil: usuario.IdPerfil,
                perfil: usuario.NomePerfil,
                ativo: Boolean(usuario.Ativo),
                dataCadastro: usuario.DataCadastro,
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

        // Verifica se existe um usuário autenticado.
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

        // Valida o preenchimento dos campos obrigatórios.
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

        // Verifica se a confirmação corresponde à nova senha.
        if (novaSenha !== confirmarNovaSenha) {
            return res.status(400).json({
                mensagem: "A confirmação não corresponde à nova senha."
            });
        }

        // Impede a reutilização da senha atual.
        if (senhaAtual === novaSenha) {
            return res.status(400).json({
                mensagem: "A nova senha deve ser diferente da senha atual."
            });
        }

        // Verifica os requisitos de segurança da nova senha.
        const validacao = validarNovaSenha(novaSenha);

        if (!validacao.valida) {
            return res.status(400).json({
                mensagem: validacao.mensagem
            });
        }

        const conexao = await conectarBanco();

        // Consulta o usuário responsável pela solicitação.
        const resultadoUsuario = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    IdUsuario,
                    SenhaHash,
                    Ativo
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

        // Confirma a senha atual antes de permitir a alteração.
        const senhaAtualValida = await verificarSenha(
            senhaAtual,
            usuario.SenhaHash
        );

        if (!senhaAtualValida) {
            return res.status(401).json({
                mensagem: "A senha atual está incorreta."
            });
        }

        // Gera um novo hash utilizando o padrão de segurança do sistema.
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

        // Evita sobrescrever alterações simultâneas de senha.
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

/**
 * Consulta os dados cadastrais do usuário autenticado.
 * O identificador é obtido exclusivamente do JWT.
 */
export async function consultarPerfil(req, res) {
    try {
        const idUsuario = req.usuario?.idUsuario;

        if (!idUsuario) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        const conexao = await conectarBanco();

        // Consulta os dados do perfil e os registros acadêmicos
        // associados ao usuário autenticado.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    u.IdUsuario AS idUsuario,
                    u.NomeCompleto AS nomeCompleto,
                    u.Login AS login,
                    u.IdPerfil AS idPerfil,
                    p.NomePerfil AS perfil,
                    u.Ativo AS ativo,
                    p.Ativo AS perfilAtivo,
                    u.DeveAlterarSenha AS deveAlterarSenha,
                    al.MatriculaAluno AS matriculaAluno,
                    pr.RegistroProfessor AS registroProfessor
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                LEFT JOIN dbo.Alunos al
                    ON al.IdUsuario = u.IdUsuario
                LEFT JOIN dbo.Professores pr
                    ON pr.IdUsuario = u.IdUsuario
                WHERE u.IdUsuario = @idUsuario
            `);

        const usuario = resultado.recordset[0];

        if (!usuario) {
            return res.status(404).json({
                mensagem: "Usuário não encontrado."
            });
        }

        // Impede consultas de contas ou perfis desativados.
        if (!usuario.ativo || !usuario.perfilAtivo) {
            return res.status(403).json({
                mensagem: "Conta ou perfil desativado."
            });
        }

        // Exige a alteração obrigatória de senha antes da consulta.
        if (usuario.deveAlterarSenha) {
            return res.status(403).json({
                mensagem: "É necessário alterar a senha.",
                deveAlterarSenha: true
            });
        }

        // Retorna somente os dados necessários para a plataforma.
        return res.status(200).json({
            usuario: {
                idUsuario: usuario.idUsuario,
                nomeCompleto: usuario.nomeCompleto,
                login: usuario.login,
                idPerfil: usuario.idPerfil,
                perfil: usuario.perfil,
                ativo: Boolean(usuario.ativo),
                matriculaAluno: usuario.matriculaAluno,
                registroProfessor: usuario.registroProfessor
            }
        });

    } catch (erro) {
        console.error("Erro ao consultar perfil:", erro.message);

        return res.status(500).json({
            mensagem: "Não foi possível consultar o perfil."
        });
    }
}