
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Seguranca;
using System;
using System.Data;
using System.Linq;

namespace PIM_IV.Painel_ADM.Servicos
{
    public class UsuarioService
    {
        private readonly UsuarioRepository _repositorio = new();

        // Verifica se ja existe um administrador no sistema
        public bool ExisteAdministrador()
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p ON u.IdPerfil = p.IdPerfil
                WHERE p.NomePerfil = 'Administrador'";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        // Cadastro do primeiro administrador
        public void CadastrarAdministradorInicial(
            string nomeCompleto,
            string login,
            string senha)
        {
            ValidarDados(nomeCompleto, login, senha);

            string hash = SenhaHelper.GerarHash(senha);

            using SqlConnection conexao = Conexao.CriarConexao();
            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                const string sqlPerfil = @"
                    SELECT IdPerfil
                    FROM dbo.Perfis
                    WHERE NomePerfil = 'Administrador'
                      AND Ativo = 1";

                using SqlCommand cmdPerfil =
                    new(sqlPerfil, conexao, transacao);

                object? resultado = cmdPerfil.ExecuteScalar();

                if (resultado == null)
                    throw new InvalidOperationException(
                        "O perfil Administrador não foi encontrado.");

                int idPerfil = Convert.ToInt32(resultado);

                const string sqlVerificar = @"
                    SELECT COUNT(1)
                    FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdPerfil = @IdPerfil";

                using SqlCommand cmdVerificar =
                    new(sqlVerificar, conexao, transacao);

                cmdVerificar.Parameters.Add("@IdPerfil", SqlDbType.Int)
                    .Value = idPerfil;

                if (Convert.ToInt32(cmdVerificar.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        "O administrador inicial já foi cadastrado.");

                const string sqlInserir = @"
                    INSERT INTO dbo.Usuarios
                        (NomeCompleto, Login, SenhaHash, IdPerfil, Ativo)
                    VALUES
                        (@Nome, @Login, @Hash, @IdPerfil, 1)";

                using SqlCommand cmdInserir =
                    new(sqlInserir, conexao, transacao);

                cmdInserir.Parameters.Add(
                    "@Nome", SqlDbType.NVarChar, 150)
                    .Value = nomeCompleto.Trim();

                cmdInserir.Parameters.Add(
                    "@Login", SqlDbType.NVarChar, 100)
                    .Value = login.Trim();

                cmdInserir.Parameters.Add(
                    "@Hash", SqlDbType.NVarChar, 500)
                    .Value = hash;

                cmdInserir.Parameters.Add(
                    "@IdPerfil", SqlDbType.Int)
                    .Value = idPerfil;

                cmdInserir.ExecuteNonQuery();

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        // Administrador cadastra um usuario e define sua senha
        // Retorna o login definitivo, incluindo RA, RP ou RC.
        public string CadastrarUsuario(
            Usuario administrador,
            string nomeCompleto,
            string login,
            string senha,
            int idPerfil)
        {
            ValidarAdministrador(administrador);
            ValidarPerfil(idPerfil);

            if (string.IsNullOrWhiteSpace(nomeCompleto))
                throw new ArgumentException(
                    "Informe o nome completo.");

            if (nomeCompleto.Trim().Length > 150)
                throw new ArgumentException(
                    "O nome excede 150 caracteres.");

            ValidarSenha(senha);

            // Somente o Administrador define seu login.
            if (idPerfil == 1)
            {
                ValidarNomeLogin(nomeCompleto, login);

                if (_repositorio.LoginExiste(login.Trim()))
                    throw new ArgumentException(
                        "Este login já está cadastrado.");
            }

            Usuario novoUsuario = new()
            {
                NomeCompleto = nomeCompleto.Trim(),
                Login = idPerfil == 1
                    ? login.Trim()
                    : string.Empty,
                SenhaHash = SenhaHelper.GerarHash(senha),
                IdPerfil = idPerfil,
                Ativo = true
            };

            try
            {
                // O repositorio gera RA, RP ou RC
                // usando o IdUsuario do SQL Server.
                _repositorio.Cadastrar(
                    novoUsuario,
                    administrador.IdUsuario);

                return novoUsuario.Login;
            }
            catch (SqlException ex) when (
                ex.Number == 2601 || ex.Number == 2627)
            {
                throw new ArgumentException(
                    "O identificador já está cadastrado. " +
                    "Verifique os dados e tente novamente.");
            }
        }

        // Edita dados de usuario
        public void EditarUsuario(
            Usuario administrador,
            int idUsuario,
            string nomeCompleto,
            string login,
            int idPerfil)
        {
            ValidarAdministrador(administrador);
            ValidarNomeLogin(nomeCompleto, login);
            ValidarPerfil(idPerfil);

            try
            {
                _repositorio.Editar(
                    administrador.IdUsuario,
                    idUsuario,
                    nomeCompleto.Trim(),
                    login.Trim(),
                    idPerfil);
            }
            catch (SqlException ex) when (
                ex.Number == 2601 || ex.Number == 2627)
            {
                throw new ArgumentException(
                    "Este login já pertence a outro usuário.");
            }
        }

        // Ativa ou desativa uma conta
        public void AlterarStatusUsuario(
            Usuario administrador,
            int idUsuario,
            bool novoStatus)
        {
            ValidarAdministrador(administrador);

            _repositorio.AlterarStatus(
                administrador.IdUsuario,
                idUsuario,
                novoStatus);
        }

        // Regras obrigatorias para qualquer nova senha
        public static void ValidarSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha) ||
                senha.Length < 8)
            {
                throw new ArgumentException(
                    "A senha deve ter no mínimo 8 caracteres.");
            }

            if (senha.Length > 128)
            {
                throw new ArgumentException(
                    "A senha deve ter no máximo 128 caracteres.");
            }

            if (!senha.Any(char.IsUpper))
            {
                throw new ArgumentException(
                    "A senha deve conter pelo menos uma letra maiúscula.");
            }

            if (!senha.Any(char.IsLower))
            {
                throw new ArgumentException(
                    "A senha deve conter pelo menos uma letra minúscula.");
            }

            if (!senha.Any(char.IsDigit))
            {
                throw new ArgumentException(
                    "A senha deve conter pelo menos um número.");
            }

            if (!senha.Any(c => !char.IsLetterOrDigit(c)))
            {
                throw new ArgumentException(
                    "A senha deve conter pelo menos um caractere especial.");
            }
        }

        // Verifica se o usuario da sessao e Administrador
        private static void ValidarAdministrador(
            Usuario administrador)
        {
            if (administrador.IdPerfil != 1 ||
                !administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem realizar esta operação.");
            }
        }

        // Verifica se o perfil esta ativo
        private void ValidarPerfil(int idPerfil)
        {
            DataTable perfis = _repositorio.ListarPerfis();

            bool encontrado = perfis.AsEnumerable()
                .Any(linha =>
                    linha.Field<int>("IdPerfil") == idPerfil);

            if (!encontrado)
            {
                throw new ArgumentException(
                    "Selecione um perfil válido.");
            }
        }

        // Validacao de nome e login
        private static void ValidarNomeLogin(
            string nomeCompleto,
            string login)
        {
            if (string.IsNullOrWhiteSpace(nomeCompleto))
            {
                throw new ArgumentException(
                    "Informe o nome completo.");
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException(
                    "Informe o login.");
            }

            if (nomeCompleto.Trim().Length > 150 ||
                login.Trim().Length > 100)
            {
                throw new ArgumentException(
                    "Nome ou login excede o tamanho permitido.");
            }
        }

        // Validacao utilizada no cadastro inicial
        private static void ValidarDados(
            string nomeCompleto,
            string login,
            string senha)
        {
            ValidarNomeLogin(nomeCompleto, login);
            ValidarSenha(senha);
        }
    }
}
