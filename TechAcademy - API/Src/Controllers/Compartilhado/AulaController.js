import sql from "mssql";
import multer from "multer";
import { conectarBanco } from "../../Config/database.js";

const TAMANHO_MAXIMO_PDF = 5 * 1024 * 1024;

/**
 * Configuração do recebimento de materiais acadêmicos.
 *
 * O arquivo permanece temporariamente na memória
 * até ser armazenado no banco de dados.
 */
const upload = multer({
    storage: multer.memoryStorage(),

    limits: {
        fileSize: TAMANHO_MAXIMO_PDF,
        files: 1,
        fields: 2
    },

    fileFilter: (req, arquivo, callback) => {
        if (arquivo.mimetype !== "application/pdf") {
            return callback(
                new Error("Somente arquivos PDF são permitidos.")
            );
        }

        callback(null, true);
    }
});

/**
 * Processa o recebimento de um arquivo PDF.
 */
export function receberMaterial(req, res, next) {
    upload.single("arquivo")(req, res, erro => {
        if (erro) {
            const mensagem =
                erro.code === "LIMIT_FILE_SIZE"
                    ? "O arquivo deve possuir no máximo 5 MB."
                    : erro.message;

            return res.status(400).json({
                mensagem
            });
        }

        next();
    });
}

/**
 * Valida identificadores utilizados pelo SQL Server.
 *
 * @param {*} valor Identificador recebido.
 * @returns {boolean} Resultado da validação.
 */
function identificadorValido(valor) {
    const numero = Number(valor);

    return (
        Number.isInteger(numero) &&
        numero > 0 &&
        numero <= 2147483647
    );
}

/**
 * Valida uma data no formato YYYY-MM-DD.
 *
 * @param {*} valor Data recebida.
 * @returns {boolean} Resultado da validação.
 */
function dataValida(valor) {
    if (
        typeof valor !== "string" ||
        !/^\d{4}-\d{2}-\d{2}$/.test(valor)
    ) {
        return false;
    }

    const data = new Date(
        `${valor}T00:00:00.000Z`
    );

    return (
        !Number.isNaN(data.getTime()) &&
        data.toISOString().slice(0, 10) === valor
    );
}

/**
 * Verifica a identidade e o perfil do usuário.
 */
async function autorizarUsuario(
    req,
    res,
    conexao,
    perfilEsperado
) {
    const idUsuario = req.usuario?.idUsuario;

    if (!identificadorValido(idUsuario)) {
        res.status(401).json({
            mensagem: "Usuário não autenticado."
        });

        return null;
    }

    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT
                u.IdUsuario,
                u.Ativo,
                u.DeveAlterarSenha,
                p.NomePerfil,
                p.Ativo AS PerfilAtivo
            FROM dbo.Usuarios AS u
            INNER JOIN dbo.Perfis AS p
                ON p.IdPerfil = u.IdPerfil
            WHERE u.IdUsuario = @idUsuario
        `);

    const usuario = resultado.recordset[0];

    if (
        !usuario ||
        !usuario.Ativo ||
        !usuario.PerfilAtivo ||
        usuario.NomePerfil.trim().toLowerCase() !==
        perfilEsperado.toLowerCase()
    ) {
        res.status(403).json({
            mensagem: "Acesso não autorizado."
        });

        return null;
    }

    if (usuario.DeveAlterarSenha) {
        res.status(403).json({
            mensagem: "Altere sua senha antes de continuar.",
            deveAlterarSenha: true
        });

        return null;
    }

    return usuario.IdUsuario;
}

/**
 * Verifica se o professor possui vínculo ativo
 * com a disciplina e a turma informadas.
 */
async function verificarVinculoProfessor(
    conexao,
    idUsuario,
    idTurmaDisciplina
) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .query(`
            SELECT TOP (1)
                p.IdProfessor
            FROM dbo.Professores AS p
            INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                ON ptd.IdProfessor = p.IdProfessor
            INNER JOIN dbo.TurmaDisciplinas AS td
                ON td.IdTurmaDisciplina = ptd.IdTurmaDisciplina
            INNER JOIN dbo.Turmas AS t
                ON t.IdTurma = td.IdTurma
            INNER JOIN dbo.Disciplinas AS d
                ON d.IdDisciplina = td.IdDisciplina
            WHERE p.IdUsuario = @idUsuario
                AND td.IdTurmaDisciplina = @idTurmaDisciplina
                AND ptd.Ativo = 1
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
        `);

    return resultado.recordset.length > 0;
}

/**
 * Verifica se o aluno possui matrícula ativa
 * na turma correspondente à disciplina.
 */
async function verificarVinculoAluno(
    conexao,
    idUsuario,
    idTurmaDisciplina
) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .input(
            "idTurmaDisciplina",
            sql.Int,
            idTurmaDisciplina
        )
        .query(`
            SELECT TOP (1)
                al.IdAluno
            FROM dbo.Alunos AS al
            INNER JOIN dbo.Matriculas AS m
                ON m.IdAluno = al.IdAluno
            INNER JOIN dbo.Turmas AS t
                ON t.IdTurma = m.IdTurma
            INNER JOIN dbo.TurmaDisciplinas AS td
                ON td.IdTurma = t.IdTurma
            INNER JOIN dbo.Disciplinas AS d
                ON d.IdDisciplina = td.IdDisciplina
            WHERE al.IdUsuario = @idUsuario
                AND td.IdTurmaDisciplina = @idTurmaDisciplina
                AND m.Situacao = 'Ativa'
                AND td.Ativa = 1
                AND t.Ativa = 1
                AND d.Ativa = 1
        `);

    return resultado.recordset.length > 0;
}

/**
 * Consulta uma aula pelo identificador.
 */
async function buscarAula(conexao, idAula) {
    const resultado = await conexao.request()
        .input("idAula", sql.Int, idAula)
        .query(`
            SELECT
                IdAula,
                IdTurmaDisciplina,
                DataAula,
                Conteudo,
                Realizada
            FROM dbo.Aulas
            WHERE IdAula = @idAula
        `);

    return resultado.recordset[0] ?? null;
}

/**
 * Registra uma falha interna sem expor informações
 * sensíveis do banco de dados ao usuário.
 */
function responderErro(res, operacao, erro) {
    console.error(
        `Erro ao ${operacao}:`,
        erro
    );

    return res.status(500).json({
        mensagem: `Não foi possível ${operacao}.`
    });
}

/**
 * Cadastra uma aula vinculada à turma e disciplina.
 *
 * Utiliza OUTPUT INTO para permitir a execução
 * da trigger de notificações do Azure SQL.
 *
 * POST /professores/me/aulas
 */
export async function cadastrarAula(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Professor"
        );

        if (!idUsuario) {
            return;
        }

        const {
            idTurmaDisciplina,
            dataAula,
            conteudo,
            realizada = false
        } = req.body ?? {};

        if (!identificadorValido(idTurmaDisciplina)) {
            return res.status(400).json({
                mensagem: "Selecione uma turma e disciplina válidas."
            });
        }

        if (!dataValida(dataAula)) {
            return res.status(400).json({
                mensagem: "Informe uma data de aula válida."
            });
        }

        if (
            typeof conteudo !== "string" ||
            conteudo.trim().length === 0 ||
            conteudo.trim().length > 300
        ) {
            return res.status(400).json({
                mensagem: "O conteúdo deve possuir de 1 a 300 caracteres."
            });
        }

        if (typeof realizada !== "boolean") {
            return res.status(400).json({
                mensagem: "A situação da aula é inválida."
            });
        }

        const possuiVinculo =
            await verificarVinculoProfessor(
                conexao,
                idUsuario,
                idTurmaDisciplina
            );

        if (!possuiVinculo) {
            return res.status(403).json({
                mensagem: "Você não possui vínculo com esta disciplina."
            });
        }

        const resultado = await conexao.request()
            .input(
                "idTurmaDisciplina",
                sql.Int,
                idTurmaDisciplina
            )
            .input(
                "dataAula",
                sql.Date,
                new Date(`${dataAula}T00:00:00.000Z`)
            )
            .input(
                "conteudo",
                sql.NVarChar(300),
                conteudo.trim()
            )
            .input("realizada", sql.Bit, realizada)
            .query(`
                -- Armazena os dados retornados pelo cadastro.
                DECLARE @AulaCadastrada TABLE (
                    idAula INT,
                    idTurmaDisciplina INT,
                    dataAula DATE,
                    conteudo NVARCHAR(300),
                    realizada BIT
                );

                -- Cadastra a aula e permite a execução da trigger.
                INSERT INTO dbo.Aulas (
                    IdTurmaDisciplina,
                    DataAula,
                    Conteudo,
                    Realizada
                )
                OUTPUT
                    INSERTED.IdAula,
                    INSERTED.IdTurmaDisciplina,
                    INSERTED.DataAula,
                    INSERTED.Conteudo,
                    INSERTED.Realizada
                INTO @AulaCadastrada
                VALUES (
                    @idTurmaDisciplina,
                    @dataAula,
                    @conteudo,
                    @realizada
                );

                -- Retorna os dados da aula cadastrada.
                SELECT
                    idAula,
                    idTurmaDisciplina,
                    dataAula,
                    conteudo,
                    realizada
                FROM @AulaCadastrada;
            `);

        return res.status(201).json({
            mensagem: "Aula cadastrada com sucesso.",
            aula: resultado.recordset[0]
        });

    } catch (erro) {
        return responderErro(
            res,
            "cadastrar a aula",
            erro
        );
    }
}

/**
 * Consulta as aulas vinculadas ao professor.
 *
 * GET /professores/me/aulas
 */
export async function listarAulasProfessor(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Professor"
        );

        if (!idUsuario) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    a.IdAula AS idAula,
                    a.IdTurmaDisciplina AS idTurmaDisciplina,
                    a.DataAula AS dataAula,
                    a.Conteudo AS conteudo,
                    a.Realizada AS realizada,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    (
                        SELECT COUNT(*)
                        FROM dbo.MateriaisAulas AS ma
                        WHERE ma.IdAula = a.IdAula
                            AND ma.Ativo = 1
                    ) AS totalMateriais
                FROM dbo.Aulas AS a
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                    AND EXISTS (
                        SELECT 1
                        FROM dbo.ProfessorTurmaDisciplinas AS ptd
                        INNER JOIN dbo.Professores AS p
                            ON p.IdProfessor = ptd.IdProfessor
                        WHERE ptd.IdTurmaDisciplina = td.IdTurmaDisciplina
                            AND p.IdUsuario = @idUsuario
                            AND ptd.Ativo = 1
                    )
                ORDER BY
                    a.DataAula DESC,
                    a.IdAula DESC
            `);

        return res.json({
            total: resultado.recordset.length,
            aulas: resultado.recordset
        });

    } catch (erro) {
        return responderErro(
            res,
            "consultar as aulas",
            erro
        );
    }
}

/**
 * Consulta as aulas das disciplinas do aluno.
 *
 * GET /alunos/me/aulas
 */
export async function listarAulasAluno(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Aluno"
        );

        if (!idUsuario) {
            return;
        }

        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    a.IdAula AS idAula,
                    a.IdTurmaDisciplina AS idTurmaDisciplina,
                    a.DataAula AS dataAula,
                    a.Conteudo AS conteudo,
                    a.Realizada AS realizada,
                    t.IdTurma AS idTurma,
                    t.NomeTurma AS nomeTurma,
                    d.IdDisciplina AS idDisciplina,
                    d.NomeDisciplina AS nomeDisciplina,
                    (
                        SELECT COUNT(*)
                        FROM dbo.MateriaisAulas AS ma
                        WHERE ma.IdAula = a.IdAula
                            AND ma.Ativo = 1
                    ) AS totalMateriais
                FROM dbo.Aulas AS a
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                    AND EXISTS (
                        SELECT 1
                        FROM dbo.Matriculas AS m
                        INNER JOIN dbo.Alunos AS al
                            ON al.IdAluno = m.IdAluno
                        WHERE m.IdTurma = t.IdTurma
                            AND al.IdUsuario = @idUsuario
                            AND m.Situacao = 'Ativa'
                    )
                ORDER BY
                    a.DataAula DESC,
                    a.IdAula DESC
            `);

        return res.json({
            total: resultado.recordset.length,
            aulas: resultado.recordset
        });

    } catch (erro) {
        return responderErro(
            res,
            "consultar as aulas",
            erro
        );
    }
}

/**
 * Publica um material PDF em uma aula.
 *
 * Utiliza OUTPUT INTO para permitir a execução
 * da trigger de notificações de novos materiais.
 *
 * POST /professores/me/aulas/:idAula/materiais
 */
export async function publicarMaterial(req, res) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            "Professor"
        );

        if (!idUsuario) {
            return;
        }

        const { idAula } = req.params;
        const { titulo } = req.body ?? {};
        const arquivo = req.file;

        if (!identificadorValido(idAula)) {
            return res.status(400).json({
                mensagem: "Identificador de aula inválido."
            });
        }

        if (
            typeof titulo !== "string" ||
            titulo.trim().length === 0 ||
            titulo.trim().length > 150
        ) {
            return res.status(400).json({
                mensagem: "O título deve possuir de 1 a 150 caracteres."
            });
        }

        if (!arquivo || !arquivo.buffer) {
            return res.status(400).json({
                mensagem: "Selecione um arquivo PDF."
            });
        }

        if (
            arquivo.size <= 0 ||
            arquivo.size > TAMANHO_MAXIMO_PDF
        ) {
            return res.status(400).json({
                mensagem: "O tamanho do arquivo é inválido."
            });
        }

        // Verifica a assinatura inicial do formato PDF.
        const assinatura = arquivo.buffer
            .subarray(0, 5)
            .toString("ascii");

        if (
            arquivo.mimetype !== "application/pdf" ||
            assinatura !== "%PDF-"
        ) {
            return res.status(400).json({
                mensagem: "O arquivo enviado não possui uma assinatura PDF válida."
            });
        }

        const aula = await buscarAula(
            conexao,
            idAula
        );

        if (!aula) {
            return res.status(404).json({
                mensagem: "Aula não encontrada."
            });
        }

        const possuiVinculo =
            await verificarVinculoProfessor(
                conexao,
                idUsuario,
                aula.IdTurmaDisciplina
            );

        if (!possuiVinculo) {
            return res.status(403).json({
                mensagem: "Você não possui acesso a esta aula."
            });
        }

        // Remove caminhos e caracteres de controle.
        const nomeArquivo = arquivo.originalname
            .split(/[\\/]/)
            .pop()
            .replace(/[\x00-\x1F\x7F]/g, "");

        if (
            nomeArquivo.length === 0 ||
            nomeArquivo.length > 255 ||
            !nomeArquivo.toLowerCase().endsWith(".pdf")
        ) {
            return res.status(400).json({
                mensagem: "Nome de arquivo PDF inválido."
            });
        }

        const resultado = await conexao.request()
            .input("idAula", sql.Int, idAula)
            .input(
                "titulo",
                sql.NVarChar(150),
                titulo.trim()
            )
            .input(
                "nomeArquivo",
                sql.NVarChar(255),
                nomeArquivo
            )
            .input(
                "tipoArquivo",
                sql.NVarChar(100),
                "application/pdf"
            )
            .input(
                "arquivo",
                sql.VarBinary(sql.MAX),
                arquivo.buffer
            )
            .query(`
                -- Armazena os dados retornados pela publicação.
                DECLARE @MaterialPublicado TABLE (
                    idMaterial INT,
                    idAula INT,
                    titulo NVARCHAR(150),
                    nomeArquivo NVARCHAR(255),
                    dataPublicacao DATETIME2
                );

                -- Publica o material e permite a execução da trigger.
                INSERT INTO dbo.MateriaisAulas (
                    IdAula,
                    Titulo,
                    NomeArquivo,
                    TipoArquivo,
                    Arquivo
                )
                OUTPUT
                    INSERTED.IdMaterial,
                    INSERTED.IdAula,
                    INSERTED.Titulo,
                    INSERTED.NomeArquivo,
                    INSERTED.DataPublicacao
                INTO @MaterialPublicado
                VALUES (
                    @idAula,
                    @titulo,
                    @nomeArquivo,
                    @tipoArquivo,
                    @arquivo
                );

                -- Retorna os dados do material publicado.
                SELECT
                    idMaterial,
                    idAula,
                    titulo,
                    nomeArquivo,
                    dataPublicacao
                FROM @MaterialPublicado;
            `);

        return res.status(201).json({
            mensagem: "Material publicado com sucesso.",
            material: resultado.recordset[0]
        });

    } catch (erro) {
        return responderErro(
            res,
            "publicar o material",
            erro
        );
    }
}

/**
 * Consulta os materiais ativos de uma aula.
 *
 * Não retorna o conteúdo binário dos arquivos,
 * evitando transferências desnecessárias.
 */
async function consultarMateriais(
    req,
    res,
    perfil
) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            perfil
        );

        if (!idUsuario) {
            return;
        }

        const { idAula } = req.params;

        if (!identificadorValido(idAula)) {
            return res.status(400).json({
                mensagem: "Identificador de aula inválido."
            });
        }

        const aula = await buscarAula(
            conexao,
            idAula
        );

        if (!aula) {
            return res.status(404).json({
                mensagem: "Aula não encontrada."
            });
        }

        const verificarVinculo =
            perfil === "Professor"
                ? verificarVinculoProfessor
                : verificarVinculoAluno;

        const possuiVinculo = await verificarVinculo(
            conexao,
            idUsuario,
            aula.IdTurmaDisciplina
        );

        if (!possuiVinculo) {
            return res.status(403).json({
                mensagem: "Você não possui acesso a esta aula."
            });
        }

        const resultado = await conexao.request()
            .input("idAula", sql.Int, idAula)
            .query(`
                SELECT
                    IdMaterial AS idMaterial,
                    IdAula AS idAula,
                    Titulo AS titulo,
                    NomeArquivo AS nomeArquivo,
                    TipoArquivo AS tipoArquivo,
                    DATALENGTH(Arquivo) AS tamanhoBytes,
                    DataPublicacao AS dataPublicacao
                FROM dbo.MateriaisAulas
                WHERE IdAula = @idAula
                    AND Ativo = 1
                ORDER BY
                    DataPublicacao DESC,
                    IdMaterial DESC
            `);

        return res.json({
            total: resultado.recordset.length,
            materiais: resultado.recordset
        });

    } catch (erro) {
        return responderErro(
            res,
            "consultar os materiais",
            erro
        );
    }
}

/**
 * Consulta os materiais disponíveis ao professor.
 */
export async function listarMateriaisProfessor(req, res) {
    return consultarMateriais(
        req,
        res,
        "Professor"
    );
}

/**
 * Consulta os materiais disponíveis ao aluno.
 */
export async function listarMateriaisAluno(req, res) {
    return consultarMateriais(
        req,
        res,
        "Aluno"
    );
}

/**
 * Recupera um PDF após validar o vínculo acadêmico.
 */
async function baixarMaterial(
    req,
    res,
    perfil
) {
    try {
        const conexao = await conectarBanco();

        const idUsuario = await autorizarUsuario(
            req,
            res,
            conexao,
            perfil
        );

        if (!idUsuario) {
            return;
        }

        const { idMaterial } = req.params;

        if (!identificadorValido(idMaterial)) {
            return res.status(400).json({
                mensagem: "Identificador de material inválido."
            });
        }

        const resultadoMaterial = await conexao.request()
            .input("idMaterial", sql.Int, idMaterial)
            .query(`
                SELECT
                    ma.IdMaterial,
                    a.IdTurmaDisciplina
                FROM dbo.MateriaisAulas AS ma
                INNER JOIN dbo.Aulas AS a
                    ON a.IdAula = ma.IdAula
                WHERE ma.IdMaterial = @idMaterial
                    AND ma.Ativo = 1
            `);

        const material =
            resultadoMaterial.recordset[0];

        if (!material) {
            return res.status(404).json({
                mensagem: "Material não encontrado."
            });
        }

        const verificarVinculo =
            perfil === "Professor"
                ? verificarVinculoProfessor
                : verificarVinculoAluno;

        const possuiVinculo = await verificarVinculo(
            conexao,
            idUsuario,
            material.IdTurmaDisciplina
        );

        if (!possuiVinculo) {
            return res.status(403).json({
                mensagem: "Você não possui acesso a este material."
            });
        }

        const resultadoArquivo = await conexao.request()
            .input("idMaterial", sql.Int, idMaterial)
            .query(`
                SELECT
                    NomeArquivo,
                    TipoArquivo,
                    Arquivo
                FROM dbo.MateriaisAulas
                WHERE IdMaterial = @idMaterial
                    AND Ativo = 1
            `);

        const arquivo = resultadoArquivo.recordset[0];

        if (!arquivo) {
            return res.status(404).json({
                mensagem: "Arquivo não encontrado."
            });
        }

        res.attachment(arquivo.NomeArquivo);

        res.setHeader(
            "Content-Type",
            "application/pdf"
        );

        res.setHeader(
            "X-Content-Type-Options",
            "nosniff"
        );

        res.setHeader(
            "Cache-Control",
            "private, no-store"
        );

        return res.send(arquivo.Arquivo);

    } catch (erro) {
        return responderErro(
            res,
            "baixar o material",
            erro
        );
    }
}

/**
 * Disponibiliza o PDF ao professor autorizado.
 */
export async function baixarMaterialProfessor(req, res) {
    return baixarMaterial(
        req,
        res,
        "Professor"
    );
}

/**
 * Disponibiliza o PDF ao aluno autorizado.
 */
export async function baixarMaterialAluno(req, res) {
    return baixarMaterial(
        req,
        res,
        "Aluno"
    );
}