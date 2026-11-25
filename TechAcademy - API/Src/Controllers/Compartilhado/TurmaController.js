
import { conectarBanco } from "../../Config/database.js";

// Consulta as turmas ativas cadastradas no Azure SQL.
export async function listarTurmas(req, res) {
    try {
        // Estabelece conexão com o banco de dados.
        const conexao = await conectarBanco();

        // Recupera as turmas ativas em ordem alfabética.
        const resultado = await conexao.request().query(`
            SELECT
                IdTurma AS idTurma,
                CodigoTurma AS codigoTurma,
                NomeTurma AS nomeTurma,
                Periodo AS periodo,
                AnoLetivo AS anoLetivo,
                Semestre AS semestre
            FROM dbo.Turmas
            WHERE Ativa = 1
            ORDER BY NomeTurma ASC
        `);

        // Retorna a quantidade e os dados das turmas.
        return res.status(200).json({
            total: resultado.recordset.length,
            turmas: resultado.recordset
        });

    } catch (erro) {
        // Registra o erro sem expor detalhes internos ao cliente.
        console.error(
            "Erro ao consultar turmas:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as turmas."
        });
    }
}
