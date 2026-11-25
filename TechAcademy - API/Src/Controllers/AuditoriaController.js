import sql from "mssql";
import { conectarBanco } from "../Config/database.js";

// Verifica a autorização para consultar registros de auditoria.
async function administradorAutorizado(conexao, idUsuario) {
    const resultado = await conexao.request()
        .input("idUsuario", sql.Int, idUsuario)
        .query(`
            SELECT TOP 1 1 AS Autorizado
            FROM dbo.Usuarios u
            INNER JOIN dbo.Perfis p
                ON p.IdPerfil = u.IdPerfil
            INNER JOIN dbo.PerfilPermissoes pp
                ON pp.IdPerfil = p.IdPerfil
            INNER JOIN dbo.Permissoes pm
                ON pm.IdPermissao = pp.IdPermissao
            WHERE u.IdUsuario = @idUsuario
                AND u.Ativo = 1
                AND u.DeveAlterarSenha = 0
                AND p.Ativo = 1
                AND p.NomePerfil = 'Administrador'
                AND pm.Ativo = 1
                AND pm.Codigo = 'CONSULTAR_AUDITORIA'
        `);

    return resultado.recordset.length > 0;
}

// Consulta a autorização do administrador autenticado.
export async function consultarAutorizacaoAuditoria(req, res) {
    try {
        const conexao = await conectarBanco();

        const autorizado = await administradorAutorizado(
            conexao,
            req.usuario.idUsuario
        );

        return res.status(200).json({
            autorizado
        });

    } catch (erro) {
        console.error("Erro ao consultar autorização:", erro);

        return res.status(500).json({
            mensagem: "Não foi possível verificar a autorização."
        });
    }
}

// Valida uma data no formato YYYY-MM-DD.
function dataValida(valor) {
    if (
        typeof valor !== "string" ||
        !/^\d{4}-\d{2}-\d{2}$/.test(valor)
    ) {
        return false;
    }

    const data = new Date(`${valor}T00:00:00Z`);

    return !Number.isNaN(data.getTime()) &&
        data.toISOString().slice(0, 10) === valor &&
        valor >= "0001-01-01" &&
        valor < "9999-12-31";
}

// Consulta os registros de auditoria com filtros opcionais.
export async function listarAuditoria(req, res) {
    try {
        const conexao = await conectarBanco();

        const autorizado = await administradorAutorizado(
            conexao,
            req.usuario.idUsuario
        );

        if (!autorizado) {
            return res.status(403).json({
                mensagem: "Você não possui permissão para consultar a auditoria."
            });
        }

        const {
            dataInicio,
            dataFim,
            pesquisa = ""
        } = req.query;

        if (
            typeof pesquisa !== "string" ||
            pesquisa.trim().length > 200
        ) {
            return res.status(400).json({
                mensagem: "Texto de pesquisa inválido."
            });
        }

        const possuiPeriodo =
            dataInicio !== undefined ||
            dataFim !== undefined;

        if (possuiPeriodo) {
            if (
                !dataValida(dataInicio) ||
                !dataValida(dataFim) ||
                dataInicio > dataFim
            ) {
                return res.status(400).json({
                    mensagem: "Período de pesquisa inválido."
                });
            }
        }

        const requisicao = conexao.request();

        requisicao.input(
            "pesquisa",
            sql.NVarChar(200),
            pesquisa.trim()
        );

        requisicao.input(
            "texto",
            sql.NVarChar(450),
            `%${pesquisa.trim()}%`
        );

        // Monta a consulta preservando as colunas originais.
        let consulta = `
            SELECT
                l.IdLog,
                l.DataHora,
                u.Login,
                u.NomeCompleto,
                u.NomeCompleto AS Usuario,
                l.Acao,
                l.Descricao
            FROM dbo.LogsAtividades l
            LEFT JOIN dbo.Usuarios u
                ON l.IdUsuario = u.IdUsuario
            WHERE
            (
                @pesquisa = N''
                OR u.Login LIKE @texto
                OR u.NomeCompleto LIKE @texto
                OR l.Acao LIKE @texto
                OR l.Descricao LIKE @texto
            )
        `;

        // Aplica o período quando informado.
        if (possuiPeriodo) {
            requisicao.input(
                "dataInicio",
                sql.Date,
                new Date(`${dataInicio}T00:00:00Z`)
            );

            requisicao.input(
                "dataFim",
                sql.Date,
                new Date(`${dataFim}T00:00:00Z`)
            );

            consulta += `
                AND l.DataHora >= @dataInicio
                AND l.DataHora < DATEADD(DAY, 1, @dataFim)
            `;
        }

        consulta += `
            ORDER BY
                l.DataHora DESC,
                l.IdLog DESC
        `;

        const resultado = await requisicao.query(consulta);

        return res.status(200).json({
            registros: resultado.recordset
        });

    } catch (erro) {
        console.error("Erro ao consultar auditoria:", erro);

        return res.status(500).json({
            mensagem: "Não foi possível consultar a auditoria."
        });
    }
}