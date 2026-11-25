
import { conectarBanco } from "../../Config/database.js";

// Consulta as disciplinas ativas cadastradas no Azure SQL.
export async function listarDisciplinas(req, res) {
    try {
        // Estabelece conexão com o banco de dados.
        const conexao = await conectarBanco();

        // Recupera somente as disciplinas ativas.
        const resultado = await conexao.request().query(`
            SELECT
                IdDisciplina AS idDisciplina,
                CodigoDisciplina AS codigoDisciplina,
                NomeDisciplina AS nomeDisciplina,
                CargaHoraria AS cargaHoraria
            FROM dbo.Disciplinas
            WHERE Ativa = 1
            ORDER BY NomeDisciplina ASC
        `);

        // Retorna as disciplinas encontradas.
        return res.status(200).json({
            total: resultado.recordset.length,
            disciplinas: resultado.recordset
        });

    } catch (erro) {
        // Registra o erro sem expor detalhes internos ao cliente.
        console.error(
            "Erro ao consultar disciplinas:",
            erro.message
        );

        return res.status(500).json({
            mensagem: "Não foi possível consultar as disciplinas."
        });
    }
}

