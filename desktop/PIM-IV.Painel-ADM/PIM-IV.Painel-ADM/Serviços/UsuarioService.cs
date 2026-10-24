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
    // Aplica as regras de negócio relacionadas aos usuários.
    public class UsuarioService
    {
        private const int PerfilAdministrador = 1;
        private const int TamanhoMaximoNome = 150;
        private const int TamanhoMaximoLogin = 100;
        private const int TamanhoMinimoSenha = 8;
        private const int TamanhoMaximoSenha = 128;

        private readonly UsuarioRepository _repositorio;

        // Permite utilizar o repositório padrão.
        public UsuarioService()
            : this(new UsuarioRepository())
        {
        }

        // Recebe o repositório utilizado pelo serviço.
        public UsuarioService(UsuarioRepository repositorio)
        {
            _repositorio = repositorio
                ?? throw new ArgumentNullException(nameof(repositorio));
        }

        // Verifica se já existe um administrador cadastrado.
        public bool ExisteAdministrador()
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE p.NomePerfil = N'Administrador'";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            return Convert.ToInt32(
                comando.ExecuteScalar()) > 0;
        }

        // Cadastra o primeiro administrador do sistema.
        public void CadastrarAdministradorInicial(
            string nomeCompleto,
            string login,
            string senha)
        {
            ValidarDados(nomeCompleto, login, senha);

            string nomeValidado = nomeCompleto.Trim();
            string loginValidado = login.Trim();

            string hash = SenhaHelper.GerarHash(senha);

            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                int idPerfil = ConsultarPerfilAdministrador(
                    conexao,
                    transacao);

                VerificarAdministradorInicial(
                    conexao,
                    transacao,
                    idPerfil);

                InserirAdministradorInicial(
                    conexao,
                    transacao,
                    nomeValidado,
                    loginValidado,
                    hash,
                    idPerfil);

                transacao.Commit();
            }
            catch
            {
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Consulta o identificador do perfil Administrador.
        private static int ConsultarPerfilAdministrador(
            SqlConnection conexao,
            SqlTransaction transacao)
        {
            const string sql = @"
                SELECT IdPerfil
                FROM dbo.Perfis
                WHERE NomePerfil = N'Administrador'
                  AND Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            object? resultado = comando.ExecuteScalar();

            if (resultado == null || resultado == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "O perfil Administrador não foi encontrado.");
            }

            return Convert.ToInt32(resultado);
        }

        // Impede o cadastro duplicado do administrador inicial.
        private static void VerificarAdministradorInicial(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                WHERE IdPerfil = @IdPerfil";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = idPerfil;

            bool administradorExiste =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (administradorExiste)
            {
                throw new InvalidOperationException(
                    "O administrador inicial já foi cadastrado.");
            }
        }

        // Insere os dados do primeiro administrador.
        private static void InserirAdministradorInicial(
            SqlConnection conexao,
            SqlTransaction transacao,
            string nomeCompleto,
            string login,
            string hash,
            int idPerfil)
        {
            const string sql = @"
                INSERT INTO dbo.Usuarios
                (
                    NomeCompleto,
                    Login,
                    SenhaHash,
                    IdPerfil,
                    Ativo
                )
                VALUES
                (
                    @Nome,
                    @Login,
                    @Hash,
                    @IdPerfil,
                    1
                )";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                TamanhoMaximoNome).Value = nomeCompleto;

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                TamanhoMaximoLogin).Value = login;

            comando.Parameters.Add(
                "@Hash",
                SqlDbType.NVarChar,
                500).Value = hash;

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = idPerfil;

            comando.ExecuteNonQuery();
        }

        // Cadastra um usuário e retorna seu login definitivo.
        public string CadastrarUsuario(
            Usuario administrador,
            string nomeCompleto,
            string login,
            string senha,
            int idPerfil)
        {
            ValidarAdministrador(administrador);
            ValidarPerfil(idPerfil);
            ValidarNome(nomeCompleto);
            ValidarSenha(senha);

            bool administradorNovo =
                idPerfil == PerfilAdministrador;

            if (administradorNovo)
            {
                ValidarNomeLogin(nomeCompleto, login);

                if (_repositorio.LoginExiste(login.Trim()))
                {
                    throw new ArgumentException(
                        "Este login já está cadastrado.");
                }
            }

            Usuario novoUsuario = CriarUsuario(
                nomeCompleto,
                login,
                senha,
                idPerfil);

            try
            {
                _repositorio.Cadastrar(
                    novoUsuario,
                    administrador.IdUsuario);

                return novoUsuario.Login;
            }
            catch (SqlException ex) when (LoginDuplicado(ex))
            {
                throw new ArgumentException(
                    "O identificador já está cadastrado. " +
                    "Verifique os dados e tente novamente.",
                    ex);
            }
        }

        // Cria o objeto que será enviado ao repositório.
        private static Usuario CriarUsuario(
            string nomeCompleto,
            string login,
            string senha,
            int idPerfil)
        {
            return new Usuario
            {
                NomeCompleto = nomeCompleto.Trim(),

                Login = idPerfil == PerfilAdministrador
                    ? login.Trim()
                    : string.Empty,

                SenhaHash = SenhaHelper.GerarHash(senha),

                IdPerfil = idPerfil,

                Ativo = true
            };
        }

        // Atualiza os dados cadastrais de um usuário.
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
            catch (SqlException ex) when (LoginDuplicado(ex))
            {
                throw new ArgumentException(
                    "Este login já pertence a outro usuário.",
                    ex);
            }
        }

        // Ativa ou desativa uma conta de usuário.
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

        // Identifica violações de unicidade no SQL Server.
        private static bool LoginDuplicado(SqlException ex)
        {
            return ex.Number == 2601 || ex.Number == 2627;
        }

        // Valida as regras obrigatórias de segurança da senha.
        public static void ValidarSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha) ||
                senha.Length < TamanhoMinimoSenha)
            {
                throw new ArgumentException(
                    "A senha deve ter no mínimo 8 caracteres.");
            }

            if (senha.Length > TamanhoMaximoSenha)
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

        // Verifica se o usuário da sessão é administrador.
        private static void ValidarAdministrador(
            Usuario administrador)
        {
            if (administrador == null ||
                administrador.IdPerfil != PerfilAdministrador ||
                !administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem realizar esta operação.");
            }
        }

        // Confirma se o perfil selecionado está ativo.
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

        // Valida o nome completo do usuário.
        private static void ValidarNome(string nomeCompleto)
        {
            if (string.IsNullOrWhiteSpace(nomeCompleto))
            {
                throw new ArgumentException(
                    "Informe o nome completo.");
            }

            if (nomeCompleto.Trim().Length > TamanhoMaximoNome)
            {
                throw new ArgumentException(
                    "O nome excede 150 caracteres.");
            }
        }

        // Valida o login informado.
        private static void ValidarLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException(
                    "Informe o login.");
            }

            if (login.Trim().Length > TamanhoMaximoLogin)
            {
                throw new ArgumentException(
                    "O login excede 100 caracteres.");
            }
        }

        // Valida o nome e o login.
        private static void ValidarNomeLogin(
            string nomeCompleto,
            string login)
        {
            ValidarNome(nomeCompleto);
            ValidarLogin(login);
        }

        // Valida os dados do cadastro inicial.
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