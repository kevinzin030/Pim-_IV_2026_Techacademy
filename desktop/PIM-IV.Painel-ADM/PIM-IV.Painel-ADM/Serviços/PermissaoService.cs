
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using System.Data;

namespace PIM_IV.Painel_ADM.Servicos
{
    public class PermissaoService
    {
        // Verifica se um usuario possui determinada permissao
        public bool UsuarioTemPermissao(
            Usuario usuario,
            string codigoPermissao)
        {
            if (usuario == null ||
                !usuario.Ativo ||
                string.IsNullOrWhiteSpace(codigoPermissao))
            {
                return false;
            }

            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                INNER JOIN dbo.PerfilPermissoes pp
                    ON p.IdPerfil = pp.IdPerfil
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE u.IdUsuario = @IdUsuario
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND pm.Ativo = 1
                  AND pm.Codigo = @Codigo";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@IdUsuario", SqlDbType.Int
            ).Value = usuario.IdUsuario;

            comando.Parameters.Add(
                "@Codigo", SqlDbType.NVarChar, 100
            ).Value = codigoPermissao;

            conexao.Open();

            return Convert.ToInt32(
                comando.ExecuteScalar()) > 0;
        }

        // Lista todas as permissoes cadastradas
        public DataTable ListarPermissoes()
        {
            const string sql = @"
                SELECT
                    IdPermissao,
                    Codigo,
                    Descricao,
                    Plataforma
                FROM dbo.Permissoes
                WHERE Ativo = 1
                ORDER BY Plataforma, Descricao";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }

        // Lista as permissoes atribuidas a um perfil
        public DataTable ListarPermissoesDoPerfil(int idPerfil)
        {
            const string sql = @"
                SELECT
                    pm.IdPermissao,
                    pm.Codigo,
                    pm.Descricao,
                    pm.Plataforma
                FROM dbo.PerfilPermissoes pp
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE pp.IdPerfil = @IdPerfil
                  AND pm.Ativo = 1
                ORDER BY pm.Plataforma, pm.Descricao";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@IdPerfil", SqlDbType.Int
            ).Value = idPerfil;

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }
    }
}
