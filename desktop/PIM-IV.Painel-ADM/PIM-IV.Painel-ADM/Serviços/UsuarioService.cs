using System;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Seguranca;

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

        // Inicializa o serviço com o repositório padrão.
        public UsuarioService()
            : this(new UsuarioRepository())
        {
        }

        // Permite receber uma instância do repositório.
        public UsuarioService(UsuarioRepository repositorio)
        {
            _repositorio = repositorio
                ?? throw new ArgumentNullException(nameof(repositorio));
        }

        // Verifica pela API se existe um administrador cadastrado.
        public bool ExisteAdministrador()
        {
            return Task.Run(async () =>
            {
                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.GetAsync(
                        "inicializacao/status");

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                JsonElement resultado =
                    documento.RootElement;

                if (!resposta.IsSuccessStatusCode)
                {
                    string mensagem =
                        "Não foi possível verificar a inicialização.";

                    if (resultado.TryGetProperty(
                        "mensagem",
                        out JsonElement mensagemApi))
                    {
                        mensagem =
                            mensagemApi.GetString() ?? mensagem;
                    }

                    throw new InvalidOperationException(mensagem);
                }

                return resultado
                    .GetProperty("administradorExiste")
                    .GetBoolean();

            }).GetAwaiter().GetResult();
        }

        // Mantém a compatibilidade com o formulário de login.
        // O provisionamento inicial deve ocorrer no servidor.
        public void CadastrarAdministradorInicial(
            string nomeCompleto,
            string login,
            string senha)
        {
            ValidarDados(nomeCompleto, login, senha);

            if (ExisteAdministrador())
            {
                throw new InvalidOperationException(
                    "O administrador inicial já foi cadastrado.");
            }

            throw new InvalidOperationException(
                "O administrador inicial deve ser provisionado " +
                "no servidor por um responsável autorizado.");
        }

        // Cadastra um usuário pela API.
        // Retorna o login definitivo gerado pelo servidor.
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

            // Apenas administradores possuem login manual.
            if (idPerfil == PerfilAdministrador)
            {
                ValidarLogin(login);

                if (_repositorio.LoginExiste(login.Trim()))
                {
                    throw new ArgumentException(
                        "Este login já está cadastrado.");
                }
            }

            Usuario novoUsuario = CriarUsuario(
                nomeCompleto,
                login,
                idPerfil);

            // A API gera o hash e o identificador sequencial.
            _repositorio.Cadastrar(
                novoUsuario,
                administrador.IdUsuario,
                senha);

            // Retorna RA, RP, RC ou login manual.
            return novoUsuario.Login;
        }

        // Cria o objeto encaminhado ao repositório.
        private static Usuario CriarUsuario(
            string nomeCompleto,
            string login,
            int idPerfil)
        {
            return new Usuario
            {
                NomeCompleto = nomeCompleto.Trim(),

                Login = idPerfil == PerfilAdministrador
                    ? login.Trim()
                    : string.Empty,

                SenhaHash = string.Empty,

                IdPerfil = idPerfil,

                Ativo = true,

                DeveAlterarSenha = true
            };
        }

        // Atualiza os dados cadastrais pela API.
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

            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "Usuário inválido.");
            }

            _repositorio.Editar(
                administrador.IdUsuario,
                idUsuario,
                nomeCompleto.Trim(),
                login.Trim(),
                idPerfil);
        }

        // Ativa ou desativa uma conta pela API.
        public void AlterarStatusUsuario(
            Usuario administrador,
            int idUsuario,
            bool novoStatus)
        {
            ValidarAdministrador(administrador);

            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "Usuário inválido.");
            }

            if (administrador.IdUsuario == idUsuario &&
                !novoStatus)
            {
                throw new InvalidOperationException(
                    "Você não pode desativar sua própria conta.");
            }

            _repositorio.AlterarStatus(
                administrador.IdUsuario,
                idUsuario,
                novoStatus);
        }

        // Valida os requisitos obrigatórios da senha.
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

        // Verifica se o administrador possui uma sessão válida.
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

            if (!SessaoUsuario.EstaAutenticado)
            {
                throw new UnauthorizedAccessException(
                    "Sessão inválida. Realize o login novamente.");
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

        // Valida o nome completo.
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