import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

/**
 * Valida identificadores utilizados pelo SQL Server.
 */
function identificadorValido(valor) {
    return (
        Number.isInteger(valor) &&
        valor > 0 &&
        valor <= 2147483647
    );
}

/**
 * Registra ou atualiza a frequência de um aluno.
 *
 * Valida a autorização do professor, o vínculo
 * acadêmico e o histórico escolar do aluno.
 */
export async function lancarFrequencia(req, res) {
    let transacao = null;
    let transacaoIniciada = false;

    try {
        const idUsuario = Number(
            req.usuario?.idUsuario
        );

        const {
            idMatricula,
            idAula,
            presente
        } = req.body ?? {};

        if (!identificadorValido(idUsuario)) {
            return res.status(401).json({
                mensagem: "Usuário não autenticado."
            });
        }

        if (
            !identificadorValido(idMatricula) ||
            !identificadorValido(idAula) ||
            typeof presente !== "boolean"
        ) {
            return res.status(400).json({
                mensagem: "Dados da frequência inválidos."
            });
        }

        const conexao = await conectarBanco();

        // Inicia uma transação para garantir a integridade
        // dos dados durante o lançamento da frequência.
        transacao = new sql.Transaction(conexao);

        await transacao.begin(
            sql.ISOLATION_LEVEL.SERIALIZABLE
        );

        transacaoIniciada = true;

        // Verifica se o usuário possui perfil de professor.
        const autorizacao = await new sql.Request(transacao)
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    p.IdProfessor
                FROM dbo.Professores AS p
                INNER JOIN dbo.Usuarios AS u
                    ON u.IdUsuario = p.IdUsuario
                INNER JOIN dbo.Perfis AS pe
                    ON pe.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @idUsuario
                    AND u.Ativo = 1
                    AND u.DeveAlterarSenha = 0
                    AND pe.Ativo = 1
                    AND LOWER(LTRIM(RTRIM(pe.NomePerfil))) = 'professor'
            `);

        if (!autorizacao.recordset.length) {
            await transacao.rollback();

            transacaoIniciada = false;

            return res.status(403).json({
                mensagem: "Acesso não autorizado."
            });
        }

        const idProfessor =
            autorizacao.recordset[0].IdProfessor;

        // Verifica se a aula pertence a uma disciplina
        // vinculada ao professor autenticado.
        const consultaAula = await new sql.Request(transacao)
            .input("idProfessor", sql.Int, idProfessor)
            .input("idAula", sql.Int, idAula)
            .query(`
                SELECT
                    a.IdAula,
                    a.DataAula,
                    a.Realizada,
                    td.IdTurma,
                    td.IdTurmaDisciplina
                FROM dbo.Aulas AS a
                INNER JOIN dbo.TurmaDisciplinas AS td
                    ON td.IdTurmaDisciplina = a.IdTurmaDisciplina
                INNER JOIN dbo.ProfessorTurmaDisciplinas AS ptd
                    ON ptd.IdTurmaDisciplina = td.IdTurmaDisciplina
                INNER JOIN dbo.Turmas AS t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas AS d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE a.IdAula = @idAula
                    AND ptd.IdProfessor = @idProfessor
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
            `);

        if (!consultaAula.recordset.length) {
            await transacao.rollback();

            transacaoIniciada = false;

            return res.status(403).json({
                mensagem: "Aula não vinculada ao professor."
            });
        }

        const aula = consultaAula.recordset[0];

        // Impede o lançamento em aulas não realizadas.
        if (!aula.Realizada) {
            await transacao.rollback();

            transacaoIniciada = false;

            return res.status(400).json({
                mensagem: "A aula ainda não foi realizada."
            });
        }

        // Verifica se o aluno pertencia à turma
        // na data em que a aula foi realizada.
        const consultaMatricula =
            await new sql.Request(transacao)
                .input(
                    "idMatricula",
                    sql.Int,
                    idMatricula
                )
                .input(
                    "idTurma",
                    sql.Int,
                    aula.IdTurma
                )
                .input(
                    "dataAula",
                    sql.Date,
                    aula.DataAula
                )
                .query(`
                    SELECT
                        m.IdMatricula,
                        m.IdAluno
                    FROM dbo.Matriculas AS m
                    WHERE m.IdMatricula = @idMatricula
                        AND EXISTS (
                            SELECT 1
                            FROM dbo.HistoricoTurmas AS h
                            WHERE h.IdMatricula = m.IdMatricula
                                AND h.IdTurma = @idTurma
                                AND @dataAula >= h.DataEntrada
                                AND (
                                    h.DataSaida IS NULL
                                    OR @dataAula <= h.DataSaida
                                )
                        )
                `);

        if (!consultaMatricula.recordset.length) {
            await transacao.rollback();

            transacaoIniciada = false;

            return res.status(400).json({
                mensagem:
                    "A aula não pertence ao histórico escolar do aluno."
            });
        }

        // Verifica se a frequência já foi registrada.
        // Os bloqueios evitam lançamentos concorrentes.
        const consultaFrequencia =
            await new sql.Request(transacao)
                .input(
                    "idMatricula",
                    sql.Int,
                    idMatricula
                )
                .input(
                    "idAula",
                    sql.Int,
                    idAula
                )
                .query(`
                    SELECT
                        IdFrequencia
                    FROM dbo.Frequencias
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdMatricula = @idMatricula
                        AND IdAula = @idAula
                `);

        if (consultaFrequencia.recordset.length > 1) {
            await transacao.rollback();

            transacaoIniciada = false;

            return res.status(409).json({
                mensagem:
                    "Foram encontradas frequências duplicadas."
            });
        }

        const frequenciaExistente =
            consultaFrequencia.recordset[0];

        let idFrequencia;

        if (frequenciaExistente) {
            idFrequencia =
                frequenciaExistente.IdFrequencia;

            // Atualiza uma frequência já cadastrada.
            await new sql.Request(transacao)
                .input(
                    "idFrequencia",
                    sql.Int,
                    idFrequencia
                )
                .input(
                    "presente",
                    sql.Bit,
                    presente
                )
                .query(`
                    UPDATE dbo.Frequencias
                    SET
                        Presente = @presente,
                        DataRegistro = SYSUTCDATETIME()
                    WHERE IdFrequencia = @idFrequencia
                `);

        } else {
            // Registra uma nova frequência.
            const insercao =
                await new sql.Request(transacao)
                    .input(
                        "idMatricula",
                        sql.Int,
                        idMatricula
                    )
                    .input(
                        "idAula",
                        sql.Int,
                        idAula
                    )
                    .input(
                        "presente",
                        sql.Bit,
                        presente
                    )
                    .query(`
                        DECLARE @Registro TABLE (
                            IdFrequencia INT
                        );

                        INSERT INTO dbo.Frequencias (
                            IdMatricula,
                            IdAula,
                            Presente,
                            DataRegistro
                        )
                        OUTPUT
                            INSERTED.IdFrequencia
                        INTO @Registro
                        VALUES (
                            @idMatricula,
                            @idAula,
                            @presente,
                            SYSUTCDATETIME()
                        );

                        SELECT
                            IdFrequencia
                        FROM @Registro;
                    `);

            idFrequencia =
                insercao.recordset[0].IdFrequencia;
        }

        // Confirma todas as operações realizadas.
        await transacao.commit();

        transacaoIniciada = false;

        return res.status(
            frequenciaExistente ? 200 : 201
        ).json({
            mensagem: frequenciaExistente
                ? "Frequência atualizada com sucesso."
                : "Frequência registrada com sucesso.",

            idFrequencia,
            idMatricula,
            idAula,

            idTurmaDisciplina:
                aula.IdTurmaDisciplina,

            presente
        });

    } catch (erro) {
        if (transacao && transacaoIniciada) {
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
            "Erro ao lançar frequência:",
            erro.message
        );

        // Trata a validação realizada pela trigger.
        if (erro.number === 50003) {
            return res.status(400).json({
                mensagem:
                    "A aula não pertence ao histórico escolar do aluno."
            });
        }

        // Trata possíveis violações de unicidade.
        if (
            erro.number === 2601 ||
            erro.number === 2627
        ) {
            return res.status(409).json({
                mensagem:
                    "Já existe uma frequência para esta aula."
            });
        }

        return res.status(500).json({
            mensagem:
                "Não foi possível registrar a frequência."
        });
    }
}