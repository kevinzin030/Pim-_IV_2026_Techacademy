using System;
using System.Data;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;

namespace PIM_IV.Painel_ADM.Servicos
{
    // Gerencia a consulta das permissões dos usuários e perfis.
    public class PermissaoService
    {
        private const int TamanhoCodigoPermissao = 100;

        // Verifica se um usuário possui determinada permissão.
        public bool UsuarioTemPermissao(
            Usuario usuario,
            string codigoPermissao)
        {
            if (!UsuarioValido(usuario) ||
                string.IsNullOrWhiteSpace(codigoPermissao))
            {
                return false;
            }

            codigoPermissao = codigoPermissao.Trim();

            if (codigoPermissao.Length > TamanhoCodigoPermissao)
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

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando =
                new(sql, conexao);

            AdicionarParametroInteiro(
                comando,
                "@IdUsuario",
                usuario.IdUsuario);

            AdicionarParametroTexto(
                comando,
                "@Codigo",
                codigoPermissao,
                TamanhoCodigoPermissao);

            conexao.Open();

            return Convert.ToInt32(
                comando.ExecuteScalar()) > 0;
        }

        // Confirma se o objeto representa um usuário ativo.
        private static bool UsuarioValido(Usuario? usuario)
        {
            return usuario != null &&
                   usuario.IdUsuario > 0 &&
                   usuario.Ativo;
        }

        // Lista todas as permissões ativas do sistema.
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
                ORDER BY
                    Plataforma,
                    Descricao";

            return ExecutarConsulta(sql);
        }

        // Lista as permissões ativas atribuídas a um perfil.
        public DataTable ListarPermissoesDoPerfil(int idPerfil)
        {
            if (idPerfil <= 0)
            {
                throw new ArgumentException(
                    "O identificador do perfil é inválido.",
                    nameof(idPerfil));
            }

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
                ORDER BY
                    pm.Plataforma,
                    pm.Descricao";

            return ExecutarConsulta(
                sql,
                comando =>
                {
                    AdicionarParametroInteiro(
                        comando,
                        "@IdPerfil",
                        idPerfil);
                });
        }

        // Executa consultas SQL e retorna os resultados.
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

        // Adiciona um parâmetro numérico à consulta.
        private static void AdicionarParametroInteiro(
            SqlCommand comando,
            string nome,
            int valor)
        {
            comando.Parameters.Add(
                nome,
                SqlDbType.Int).Value = valor;
        }

        // Adiciona um parâmetro de texto à consulta.
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