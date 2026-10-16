
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using System.Data;

namespace PIM_IV.Painel_ADM.Servicos
{
    public class AuditoriaService
    {
        // Registra uma atividade no banco de dados
        public void Registrar(
            int idUsuario,
            string acao,
            string descricao)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "O usuário responsável pela ação é inválido.");
            }

            if (string.IsNullOrWhiteSpace(acao) ||
                string.IsNullOrWhiteSpace(descricao))
            {
                throw new ArgumentException(
                    "A ação e a descrição são obrigatórias.");
            }

            if (acao.Length > 100 || descricao.Length > 500)
            {
                throw new ArgumentException(
                    "Os dados da auditoria excedem o tamanho permitido.");
            }

            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                    (IdUsuario, Acao, Descricao)
                VALUES
                    (@IdUsuario, @Acao, @Descricao)";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add("@IdUsuario", SqlDbType.Int)
                .Value = idUsuario;

            comando.Parameters.Add("@Acao", SqlDbType.NVarChar, 100)
                .Value = acao;

            comando.Parameters.Add("@Descricao", SqlDbType.NVarChar, 500)
                .Value = descricao;

            conexao.Open();
            comando.ExecuteNonQuery();
        }

        // Consulta os registros de atividades
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
                ORDER BY l.DataHora DESC, l.IdLog DESC";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }
    }
}
