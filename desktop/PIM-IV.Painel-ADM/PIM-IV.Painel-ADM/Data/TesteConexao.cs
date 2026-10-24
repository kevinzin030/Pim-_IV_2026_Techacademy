using Microsoft.Data.SqlClient;

namespace PIM_IV.Painel_ADM.Data
{
    // Verifica a disponibilidade da conexão com o SQL Server.
    internal static class TesteConexao
    {
        // Retorna true quando a conexão é estabelecida.
        public static bool Verificar()
        {
            try
            {
                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                return conexao.State ==
                    System.Data.ConnectionState.Open;
            }
            catch (SqlException)
            {
                // Retorna false quando ocorre um erro do SQL Server.
                return false;
            }
            catch (InvalidOperationException)
            {
                // Retorna false quando a conexão está inválida.
                return false;
            }
        }
    }
}