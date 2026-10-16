
using Microsoft.Data.SqlClient;

namespace PIM_IV.Painel_ADM.Data
{
    internal static class TesteConexao
    {
        // Verifica se o banco de dados esta acessivel
        public static bool Verificar()
        {
            try
            {
                // Cria e abre a conexao
                using SqlConnection conexao = Conexao.CriarConexao();
                conexao.Open();

                return true;
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }
}
