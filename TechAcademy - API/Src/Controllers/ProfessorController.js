
import sql from "mssql";

import { conectarBanco } from "../Config/database.js";

// Consulta as turmas e disciplinas vinculadas ao professor autenticado.
export async function consultarMinhasTurmas(req, res) {
    try {
        // Recupera a identificação do usuário validada pelo JWT.
        const idUsuario = req.usuario.idUsuario;

        // Estabelece conexão com o Azure SQL.
        const conexao = await conectarBanco();

        // Confirma que o usuário pertence ao perfil Professor.
        const resultadoPerfil = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    p.NomePerfil
                FROM dbo.Usuarios AS u
                INNER JOIN dbo.Perfis AS p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @idUsuario
            `);

        const perfil = resultadoPerfil.recordset[0];

        if (
            !perfil ||
            perfil.NomePerfil.trim().toLowerCase() !== "professor"
        ) {
            return res.status(403).json({
                mensagem: "Acesso permitido somente para professores."
            });
        }

        // Consulta somente os vínculos ativos do professor autenticado.
        const resultado = await conexao.request()
            .input("idUsuario", sql.Int, idUsuario)
            .query(`
                SELECT
                    t.IdTurma AS idTurma,
                    t.CodigoTurma AS codigoTurma,
                    t.NomeTurma AS nomeTurma,
                    t.Periodo AS periodo,
                    t.AnoLetivo AS anoLetivo,
                    t.Semestre AS semestre,
                    d.IdDisciplina AS idDisciplina,
                    d.CodigoDisciplina AS codigoDisciplina,
                    d.NomeDisciplina AS nomeDisciplina
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
                    AND ptd.Ativo = 1
                    AND td.Ativa = 1
                    AND t.Ativa = 1
                    AND d.Ativa = 1
                ORDER BY
                    t.NomeTurma ASC,
                    d.NomeDisciplina ASC
            `);

        // Retorna as turmas e disciplinas vinculadas ao professor.
        return res.status(200).json({
            total: resultado.recordset.length,
            turmas: resultado.recordset
        });

    } catch (erro) {
        // Registra o erro sem expor informações internas do banco.
        console.error(
            "Erro ao consultar turmas do professor:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as turmas do professor."
        });
    }
}
