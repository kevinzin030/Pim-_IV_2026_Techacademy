using System;
using System.Data;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;

namespace PIM_IV.Painel_ADM.Repositorios
{
    // Responsável pelo acesso aos registros de auditoria.
    public class AuditoriaRepository
    {
        private const int TamanhoAcao = 100;
        private const int TamanhoDescricao = 500;
        private const int TamanhoPesquisa = 200;
        private const int TamanhoTexto = 450;

        // Registra uma atividade realizada por um usuário.
        public void Registrar(
            int idUsuario,
            string acao,
            string descricao)
        {
            ValidarRegistro(idUsuario, acao, descricao);

            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                (
                    IdUsuario,
                    Acao,
                    Descricao
                )
                VALUES
                (
                    @IdUsuario,
                    @Acao,
                    @Descricao
                )";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando =
                new(sql, conexao);

            AdicionarParametro(
                comando,
                "@IdUsuario",
                SqlDbType.Int,
                idUsuario);

            AdicionarParametroTexto(
                comando,
                "@Acao",
                acao,
                TamanhoAcao);

            AdicionarParametroTexto(
                comando,
                "@Descricao",
                descricao,
                TamanhoDescricao);

            conexao.Open();

            comando.ExecuteNonQuery();
        }

        // Valida os dados antes de registrar uma atividade.
        private static void ValidarRegistro(
            int idUsuario,
            string acao,
            string descricao)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "O identificador do usuário é inválido.",
                    nameof(idUsuario));
            }

            if (string.IsNullOrWhiteSpace(acao))
            {
                throw new ArgumentException(
                    "A ação da auditoria deve ser informada.",
                    nameof(acao));
            }

            if (acao.Length > TamanhoAcao)
            {
                throw new ArgumentException(
                    "A ação ultrapassa o tamanho permitido.",
                    nameof(acao));
            }

            if (descricao == null)
            {
                throw new ArgumentNullException(
                    nameof(descricao));
            }

            if (descricao.Length > TamanhoDescricao)
            {
                throw new ArgumentException(
                    "A descrição ultrapassa o tamanho permitido.",
                    nameof(descricao));
            }
        }

        // Consulta todos os registros de auditoria.
        public DataTable ListarTodos()
        {
            const string sql = @"
                SELECT
                    l.IdLog,
                    u.NomeCompleto AS Usuario,
                    l.Acao,
                    l.Descricao,
                    l.DataHora
                FROM dbo.LogsAtividades l
                LEFT JOIN dbo.Usuarios u
                    ON l.IdUsuario = u.IdUsuario
                ORDER BY
                    l.DataHora DESC,
                    l.IdLog DESC";

            return ExecutarConsulta(sql);
        }

        // Confirma se o usuário continua sendo administrador ativo.
        public bool AdministradorEstaAutorizado(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return false;
            }

            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.IdUsuario = @IdUsuario
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND p.NomePerfil = N'Administrador'";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando =
                new(sql, conexao);

            AdicionarParametro(
                comando,
                "@IdUsuario",
                SqlDbType.Int,
                idUsuario);

            conexao.Open();

            return Convert.ToInt32(
                comando.ExecuteScalar()) > 0;
        }

        // Consulta registros por período e texto pesquisado.
        public DataTable Pesquisar(
            DateTime dataInicio,
            DateTime dataFim,
            string pesquisa)
        {
            ValidarPeriodo(dataInicio, dataFim);

            pesquisa ??= string.Empty;

            pesquisa = pesquisa.Trim();

            if (pesquisa.Length > TamanhoPesquisa)
            {
                throw new ArgumentException(
                    "O texto de pesquisa ultrapassa " +
                    "o tamanho permitido.",
                    nameof(pesquisa));
            }

            const string sql = @"
                SELECT
                    l.IdLog,
                    l.DataHora,
                    u.Login,
                    u.NomeCompleto,
                    l.Acao,
                    l.Descricao
                FROM dbo.LogsAtividades l
                LEFT JOIN dbo.Usuarios u
                    ON l.IdUsuario = u.IdUsuario
                WHERE l.DataHora >= @DataInicio
                  AND l.DataHora < @DataFim
                  AND
                  (
                      @Pesquisa = N''
                      OR u.Login LIKE @Texto
                      OR u.NomeCompleto LIKE @Texto
                      OR l.Acao LIKE @Texto
                      OR l.Descricao LIKE @Texto
                  )
                ORDER BY
                    l.DataHora DESC,
                    l.IdLog DESC";

            return ExecutarConsulta(
                sql,
                comando =>
                {
                    AdicionarParametro(
                        comando,
                        "@DataInicio",
                        SqlDbType.DateTime2,
                        dataInicio.Date);

                    AdicionarParametro(
                        comando,
                        "@DataFim",
                        SqlDbType.DateTime2,
                        dataFim.Date.AddDays(1));

                    AdicionarParametroTexto(
                        comando,
                        "@Pesquisa",
                        pesquisa,
                        TamanhoPesquisa);

                    AdicionarParametroTexto(
                        comando,
                        "@Texto",
                        "%" + pesquisa + "%",
                        TamanhoTexto);
                });
        }

        // Verifica se o período informado é válido.
        private static void ValidarPeriodo(
            DateTime dataInicio,
            DateTime dataFim)
        {
            if (dataInicio.Date > dataFim.Date)
            {
                throw new ArgumentException(
                    "A data inicial não pode ser " +
                    "posterior à data final.");
            }

            if (dataFim.Date >= DateTime.MaxValue.Date)
            {
                throw new ArgumentException(
                    "A data final informada é inválida.");
            }
        }

        // Executa consultas e retorna os resultados em uma tabela.
        private static DataTable ExecutarConsulta(
            string sql,
            Action<SqlCommand>? configurarParametros = null)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando =
                new(sql, conexao);

            configurarParametros?.Invoke(comando);

            conexao.Open();

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();

            tabela.Load(leitor);

            return tabela;
        }

        // Adiciona parâmetros tipados às consultas SQL.
        private static void AdicionarParametro(
            SqlCommand comando,
            string nome,
            SqlDbType tipo,
            object valor)
        {
            comando.Parameters.Add(
                nome,
                tipo).Value = valor;
        }

        // Adiciona parâmetros de texto com tamanho definido.
        private static void AdicionarParametroTexto(
            SqlCommand comando,
            string nome,
            string valor,
            int tamanho)
        {
            comando.Parameters.Add(
                nome,
                SqlDbType.NVarChar,
                tamanho).Value = valor;
        }
    }
}