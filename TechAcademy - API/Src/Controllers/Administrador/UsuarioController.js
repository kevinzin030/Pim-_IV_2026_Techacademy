import sql from "mssql";
import { conectarBanco } from "../../Config/database.js";
import {
    validarNovaSenha,
    gerarHashSenha
} from "../../Utils/senhaHelper.js";

// Define os prefixos dos identificadores automáticos.
const prefixosPorPerfil = {
    Aluno: "RA",
    Professor: "RP",
    Coordenador: "RC"
};

// Representa erros de validação das operações administrativas.
class ErroOperacao extends Error {
    constructor(status, mensagem) {
        super(mensagem);
        this.status = status;
    }
}

// Valida identificadores numéricos recebidos pela API.
function validarIdentificador(valor) {
    const identificador = Number(valor);

    if (
        !Number.isInteger(identificador) ||
        identificador <= 0 ||
        identificador > 2147483647
    ) {
        throw new ErroOperacao(
            400,
            "Identificador inválido."
        );
    }

    return identificador;
}

// Valida o nome completo do usuário.
function validarNome(nome) {
    if (
        typeof nome !== "string" ||
        !nome.trim() ||
        nome.trim().length > 150
    ) {
        throw new ErroOperacao(
            400,
            "Informe um nome completo válido."
        );
    }

    return nome.trim();
}

// Valida o login manual dos administradores.
function validarLogin(login) {
    if (
        typeof login !== "string" ||
        !login.trim() ||
        login.trim().length > 100 ||
        /\s/.test(login.trim())
    ) {
        throw new ErroOperacao(
            400,
            "Informe um login válido, sem espaços."
        );
    }

    return login.trim();
}

// Impede que administradores utilizem identificadores reservados.
function validarLoginAdministrador(login) {
    const loginValidado = validarLogin(login);

    if (/^(RA|RP|RC)\d+$/i.test(loginValidado)) {
        throw new ErroOperacao(
            400,
            "Este formato de login é reservado aos identificadores acadêmicos."
        );
    }

    return loginValidado;
}

// Padroniza as respostas de erro.
function responderErro(res, erro) {
    if (erro instanceof ErroOperacao) {
        return res.status(erro.status).json({
            mensagem: erro.message
        });
    }

    // Trata violações de unicidade do SQL Server.
    if (erro.number === 2601 || erro.number === 2627) {
        return res.status(409).json({
            mensagem: "Este login ou identificador já está cadastrado."
        });
    }

    console.error(
        "Erro no gerenciamento de usuários:",
        erro
    );

    return res.status(500).json({
        mensagem: "Não foi possível concluir a operação."
    });
}

// Valida o administrador autenticado e sua autorização.
async function validarAdministrador(origem, idAdministrador) {
    const resultado = await new sql.Request(origem)
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

    if (resultado.recordset.length === 0) {
        throw new ErroOperacao(
            403,
            "Você não possui permissão para gerenciar usuários."
        );
    }
}

// Executa operações administrativas em uma transação.
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

// Consulta um perfil ativo pelo identificador.
async function consultarPerfilAtivo(origem, idPerfil) {
    const resultado = await new sql.Request(origem)
        .input("idPerfil", sql.Int, idPerfil)
        .query(`
            SELECT IdPerfil, NomePerfil
            FROM dbo.Perfis
            WHERE IdPerfil = @idPerfil
                AND Ativo = 1
        `);

    const perfil = resultado.recordset[0];

    if (!perfil) {
        throw new ErroOperacao(
            400,
            "Perfil inválido ou inativo."
        );
    }

    return perfil;
}

// Consulta os dados atuais de um usuário.
async function consultarUsuario(origem, idUsuario) {
    const resultado = await new sql.Request(origem)
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT
                u.IdUsuario,
                u.NomeCompleto,
                u.Login,
                u.IdPerfil,
                u.Ativo,
                p.NomePerfil
            FROM dbo.Usuarios u
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            WHERE u.IdUsuario = @idUsuario
        `);

    const usuario = resultado.recordset[0];

    if (!usuario) {
        throw new ErroOperacao(
            404,
            "Usuário não encontrado."
        );
    }

    return usuario;
}

// Verifica se determinado login já está cadastrado.
async function verificarLoginExistente(
    origem,
    login,
    idUsuarioIgnorado = 0
) {
    const resultado = await new sql.Request(origem)
        .input("login", sql.NVarChar(100), login)
        .input("idUsuario", sql.Int, idUsuarioIgnorado)
        .query(`
            SELECT COUNT(1) AS Total
            FROM dbo.Usuarios
            WHERE Login = @login
                AND IdUsuario <> @idUsuario
        `);

    return resultado.recordset[0].Total > 0;
}

// Gera RA, RP ou RC utilizando a sequência do banco.
async function gerarIdentificador(transacao, prefixo) {
    const resultado = await new sql.Request(transacao)
        .input("Prefixo", sql.Char(2), prefixo)
        .output("Identificador", sql.NVarChar(30))
        .execute("dbo.SP_GerarIdentificador");

    const identificador = resultado.output.Identificador;

    if (!identificador) {
        throw new ErroOperacao(
            500,
            "Não foi possível gerar o identificador."
        );
    }

    return identificador;
}

// Cria os registros acadêmicos associados ao usuário.
async function criarRegistroAcademico(
    transacao,
    idUsuario,
    nomePerfil,
    identificador
) {
    if (nomePerfil === "Aluno") {
        await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .input(
                "identificador",
                sql.NVarChar(30),
                identificador
            )
            .query(`
                INSERT INTO dbo.Alunos (
                    IdUsuario,
                    MatriculaAluno
                )
                VALUES (
                    @idUsuario,
                    @identificador
                )
            `);
    }

    if (nomePerfil === "Professor") {
        await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .input(
                "identificador",
                sql.NVarChar(30),
                identificador
            )
            .query(`
                INSERT INTO dbo.Professores (
                    IdUsuario,
                    RegistroProfessor
                )
                VALUES (
                    @idUsuario,
                    @identificador
                )
            `);
    }

    // Coordenadores utilizam o RC diretamente no login.
}

// Registra a auditoria dentro da transação administrativa.
async function registrarAuditoria(
    transacao,
    idAdministrador,
    acao,
    descricao
) {
    await new sql.Request(transacao)
        .input("idUsuario", sql.Int, idAdministrador)
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

// Garante a existência de outro administrador ativo.
async function verificarOutroAdministrador(
    transacao,
    idUsuario
) {
    const resultado = await new sql.Request(transacao)
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT COUNT(1) AS Total
            FROM dbo.Usuarios u
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            WHERE p.NomePerfil = 'Administrador'
                AND p.Ativo = 1
                AND u.Ativo = 1
                AND u.IdUsuario <> @idUsuario
        `);

    if (resultado.recordset[0].Total === 0) {
        throw new ErroOperacao(
            400,
            "O sistema deve possuir pelo menos um administrador ativo."
        );
    }
}

/**
 * Lista todos os usuários cadastrados.
 * Nunca retorna senhas ou hashes.
 */
export async function listarUsuarios(req, res) {
    try {
        const idAdministrador = req.usuario?.idUsuario;

        const conexao = await conectarBanco();

        await validarAdministrador(
            conexao,
            idAdministrador
        );

        const resultado = await conexao.request().query(`
            SELECT
                u.IdUsuario,
                u.NomeCompleto,
                u.Login,
                p.NomePerfil,
                u.IdPerfil,
                u.Ativo,
                u.DataCadastro
            FROM dbo.Usuarios u
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            ORDER BY u.NomeCompleto
        `);

        return res.status(200).json({
            usuarios: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Lista os perfis ativos disponíveis para cadastro.
 */
export async function listarPerfisCadastro(req, res) {
    try {
        const conexao = await conectarBanco();

        await validarAdministrador(
            conexao,
            req.usuario?.idUsuario
        );

        const resultado = await conexao.request().query(`
            SELECT
                IdPerfil,
                NomePerfil
            FROM dbo.Perfis
            WHERE Ativo = 1
            ORDER BY NomePerfil
        `);

        return res.status(200).json({
            perfis: resultado.recordset
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Verifica a disponibilidade de um login.
 */
export async function verificarLogin(req, res) {
    try {
        const login = validarLogin(req.query.login);

        const conexao = await conectarBanco();

        await validarAdministrador(
            conexao,
            req.usuario?.idUsuario
        );

        const existe = await verificarLoginExistente(
            conexao,
            login
        );

        return res.status(200).json({
            existe
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Cadastra um usuário e seus registros acadêmicos.
 * Os identificadores são gerados sequencialmente.
 */
export async function cadastrarUsuario(req, res) {
    try {
        const idAdministrador = req.usuario?.idUsuario;

        const {
            nomeCompleto,
            login,
            senha,
            idPerfil
        } = req.body ?? {};

        const nomeValidado = validarNome(nomeCompleto);

        const perfilValidado = validarIdentificador(
            idPerfil
        );

        // Verifica a autorização antes de processar a senha.
        const conexao = await conectarBanco();

        await validarAdministrador(
            conexao,
            idAdministrador
        );

        // Valida os requisitos de segurança da senha.
        const validacaoSenha = validarNovaSenha(senha);

        if (!validacaoSenha.valida) {
            throw new ErroOperacao(
                400,
                validacaoSenha.mensagem
            );
        }

        // Gera o hash PBKDF2 compatível com as três plataformas.
        const senhaHash = await gerarHashSenha(senha);

        const usuario = await executarTransacao(
            async transacao => {
                await validarAdministrador(
                    transacao,
                    idAdministrador
                );

                const perfil = await consultarPerfilAtivo(
                    transacao,
                    perfilValidado
                );

                const nomePerfil = perfil.NomePerfil;

                let loginFinal;

                // Administradores possuem login manual.
                if (nomePerfil === "Administrador") {
                    loginFinal =
                        validarLoginAdministrador(login);

                } else {
                    // Alunos, professores e coordenadores
                    // possuem sequências independentes.
                    const prefixo =
                        prefixosPorPerfil[nomePerfil];

                    if (!prefixo) {
                        throw new ErroOperacao(
                            400,
                            "Perfil não reconhecido."
                        );
                    }

                    loginFinal = await gerarIdentificador(
                        transacao,
                        prefixo
                    );
                }

                const loginExiste =
                    await verificarLoginExistente(
                        transacao,
                        loginFinal
                    );

                if (loginExiste) {
                    throw new ErroOperacao(
                        409,
                        "Este identificador já está cadastrado."
                    );
                }

                // Insere o usuário com troca obrigatória de senha.
                const resultado =
                    await new sql.Request(transacao)
                        .input(
                            "nome",
                            sql.NVarChar(150),
                            nomeValidado
                        )
                        .input(
                            "login",
                            sql.NVarChar(100),
                            loginFinal
                        )
                        .input(
                            "senhaHash",
                            sql.NVarChar(500),
                            senhaHash
                        )
                        .input(
                            "idPerfil",
                            sql.Int,
                            perfilValidado
                        )
                        .query(`
                            INSERT INTO dbo.Usuarios (
                                NomeCompleto,
                                Login,
                                SenhaHash,
                                IdPerfil,
                                Ativo,
                                DeveAlterarSenha
                            )
                            OUTPUT INSERTED.IdUsuario
                            VALUES (
                                @nome,
                                @login,
                                @senhaHash,
                                @idPerfil,
                                1,
                                1
                            )
                        `);

                const idUsuario =
                    resultado.recordset[0].IdUsuario;

                // Cria o registro acadêmico quando necessário.
                await criarRegistroAcademico(
                    transacao,
                    idUsuario,
                    nomePerfil,
                    loginFinal
                );

                // Registra a criação do usuário.
                await registrarAuditoria(
                    transacao,
                    idAdministrador,
                    "CADASTRO_USUARIO",
                    `Usuário ID ${idUsuario} cadastrado. ` +
                    `Perfil: ${nomePerfil}. ` +
                    `Login: ${loginFinal}.`
                );

                return {
                    idUsuario,
                    nomeCompleto: nomeValidado,
                    login: loginFinal,
                    idPerfil: perfilValidado,
                    perfil: nomePerfil,
                    ativo: true,
                    deveAlterarSenha: true
                };
            }
        );

        return res.status(201).json({
            mensagem: "Usuário cadastrado com sucesso!",
            usuario
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Atualiza os dados cadastrais de um usuário.
 * Preserva os identificadores e vínculos acadêmicos.
 */
export async function editarUsuario(req, res) {
    try {
        const idAdministrador = req.usuario?.idUsuario;

        const idUsuario = validarIdentificador(
            req.params.idUsuario
        );

        const {
            nomeCompleto,
            login,
            idPerfil
        } = req.body ?? {};

        const nomeValidado = validarNome(nomeCompleto);

        const loginValidado = validarLogin(login);

        const perfilValidado = validarIdentificador(
            idPerfil
        );

        await executarTransacao(async transacao => {
            await validarAdministrador(
                transacao,
                idAdministrador
            );

            const usuario = await consultarUsuario(
                transacao,
                idUsuario
            );

            // Preserva os vínculos acadêmicos existentes.
            if (usuario.IdPerfil !== perfilValidado) {
                throw new ErroOperacao(
                    400,
                    "A alteração de perfil não está disponível."
                );
            }

            await consultarPerfilAtivo(
                transacao,
                perfilValidado
            );

            // Impede alterações manuais de RA, RP e RC.
            if (
                usuario.NomePerfil !== "Administrador" &&
                usuario.Login !== loginValidado
            ) {
                throw new ErroOperacao(
                    400,
                    "O identificador deste perfil não pode ser alterado."
                );
            }

            if (usuario.NomePerfil === "Administrador") {
                // Permite manter um login legado já existente.
                if (usuario.Login !== loginValidado) {
                    validarLoginAdministrador(loginValidado);
                }
            }

            const loginExiste =
                await verificarLoginExistente(
                    transacao,
                    loginValidado,
                    idUsuario
                );

            if (loginExiste) {
                throw new ErroOperacao(
                    409,
                    "Este login já pertence a outro usuário."
                );
            }

            // Atualiza os dados cadastrais.
            await new sql.Request(transacao)
                .input("idUsuario", sql.Int, idUsuario)
                .input(
                    "nome",
                    sql.NVarChar(150),
                    nomeValidado
                )
                .input(
                    "login",
                    sql.NVarChar(100),
                    loginValidado
                )
                .query(`
                    UPDATE dbo.Usuarios
                    SET
                        NomeCompleto = @nome,
                        Login = @login
                    WHERE IdUsuario = @idUsuario
                `);

            // Registra a alteração na auditoria.
            await registrarAuditoria(
                transacao,
                idAdministrador,
                "EDICAO_USUARIO",
                `Dados do usuário ID ${idUsuario} atualizados.`
            );
        });

        return res.status(200).json({
            mensagem: "Usuário atualizado com sucesso!"
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}

/**
 * Ativa ou desativa uma conta.
 * Protege a conta atual e o último administrador ativo.
 */
export async function alterarStatusUsuario(req, res) {
    try {
        const idAdministrador = req.usuario?.idUsuario;

        const idUsuario = validarIdentificador(
            req.params.idUsuario
        );

        const { ativo } = req.body ?? {};

        if (typeof ativo !== "boolean") {
            throw new ErroOperacao(
                400,
                "Status do usuário inválido."
            );
        }

        await executarTransacao(async transacao => {
            await validarAdministrador(
                transacao,
                idAdministrador
            );

            const usuario = await consultarUsuario(
                transacao,
                idUsuario
            );

            // Impede a desativação da própria conta.
            if (
                idUsuario === idAdministrador &&
                !ativo
            ) {
                throw new ErroOperacao(
                    400,
                    "Você não pode desativar sua própria conta."
                );
            }

            // Impede a desativação do último administrador.
            if (
                usuario.NomePerfil === "Administrador" &&
                usuario.Ativo &&
                !ativo
            ) {
                await verificarOutroAdministrador(
                    transacao,
                    idUsuario
                );
            }

            // Valida o perfil antes de reativar a conta.
            if (ativo) {
                await consultarPerfilAtivo(
                    transacao,
                    usuario.IdPerfil
                );
            }

            // Evita atualizações desnecessárias.
            if (usuario.Ativo === ativo) {
                return;
            }

            await new sql.Request(transacao)
                .input("idUsuario", sql.Int, idUsuario)
                .input("ativo", sql.Bit, ativo)
                .query(`
                    UPDATE dbo.Usuarios
                    SET Ativo = @ativo
                    WHERE IdUsuario = @idUsuario
                `);

            // Registra a ativação ou desativação.
            await registrarAuditoria(
                transacao,
                idAdministrador,
                ativo
                    ? "ATIVACAO_USUARIO"
                    : "DESATIVACAO_USUARIO",
                `Usuário ID ${idUsuario} ` +
                `${ativo ? "ativado" : "desativado"}.`
            );
        });

        return res.status(200).json({
            mensagem: ativo
                ? "Usuário ativado com sucesso!"
                : "Usuário desativado com sucesso!"
        });

    } catch (erro) {
        return responderErro(res, erro);
    }
}