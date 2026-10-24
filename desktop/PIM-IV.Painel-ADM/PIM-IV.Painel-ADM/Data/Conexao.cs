using Microsoft.Data.SqlClient;

namespace PIM_IV.Painel_ADM.Data
{
    // Centraliza a configuração das conexões com o SQL Server.
    internal static class Conexao
    {
        private const string Servidor =
            @"localhost\MSSQLSERVER02";

        private const string BancoDados =
            "PIMIV_PainelADM";

        // Configura os parâmetros de conexão.
        private static readonly string StringConexao =
            CriarStringConexao();

        // Monta a string de conexão de forma organizada.
        private static string CriarStringConexao()
        {
            SqlConnectionStringBuilder configuracao = new()
            {
                DataSource = Servidor,
                InitialCatalog = BancoDados,
                IntegratedSecurity = true,
                Encrypt = SqlConnectionEncryptOption.Mandatory,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            return configuracao.ConnectionString;
        }

        // Cria uma conexão que será aberta por quem a utilizar.
        public static SqlConnection CriarConexao()
        {
            return new SqlConnection(StringConexao);
        }
    }
}