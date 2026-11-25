import { conectarBanco } from "../Config/database.js";

// Verifica se o administrador inicial já foi cadastrado.
export async function consultarInicializacao(req, res) {
    try {
        const conexao = await conectarBanco();

        const resultado = await conexao.request()
            .query(`
                SELECT COUNT(1) AS Total
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                WHERE p.NomePerfil = N'Administrador';
            `);

        return res.status(200).json({
            administradorExiste:
                resultado.recordset[0].Total > 0
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar inicialização:",
            erro
        );

        return res.status(500).json({
            mensagem:
                "Não foi possível verificar a inicialização do sistema."
        });
    }
}