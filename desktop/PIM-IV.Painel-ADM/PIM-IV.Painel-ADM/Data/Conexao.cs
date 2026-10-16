
using Microsoft.Data.SqlClient;

namespace PIM_IV.Painel_ADM.Data
{
    internal static class Conexao
    {
        // Define o servidor e o banco de dados
        private const string StringConexao =
            @"Server=localhost\MSSQLSERVER02;" +
            "Database=PIMIV_PainelADM;" +
            "Integrated Security=True;" +
            "Encrypt=True;" +
            "TrustServerCertificate=True;";

        // Cria uma conexao com o SQL Server
        public static SqlConnection CriarConexao()
        {
            return new SqlConnection(StringConexao);
        }
    }
}
