using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    /// <summary>
    /// Gerencia a autenticação e a alteração de senha
    /// através da API REST do TechAcademy.
    /// </summary>
    public class AutenticacaoService
    {
        // Configura a interpretação das propriedades JSON.
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Mantém a compatibilidade temporária com o formulário antigo.
        /// </summary>
        public Usuario? Autenticar(string login, string senha)
        {
            return Task.Run(() => AutenticarAsync(login, senha))
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Autentica o usuário sem bloquear a interface gráfica.
        /// </summary>
        public async Task<Usuario?> AutenticarAsync(
            string login,
            string senha)
        {
            // Remove uma eventual sessão anterior.
            SessaoUsuario.Encerrar();

            // Valida o preenchimento das credenciais.
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrEmpty(senha))
            {
                return null;
            }

            // Identifica a plataforma responsável pela solicitação.
            var credenciais = new
            {
                login = login.Trim(),
                senha,
                plataforma = "desktop"
            };

            // Encaminha as credenciais para a API.
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.PostAsJsonAsync(
                    "auth/login",
                    credenciais);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            // Identifica credenciais inválidas.
            if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }

            // Trata erros de autenticação e autorização.
            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ObterMensagemErro(conteudo));
            }

            // Converte a resposta para o modelo de autenticação.
            RespostaAutenticacao? resultado =
                JsonSerializer.Deserialize<RespostaAutenticacao>(
                    conteudo,
                    OpcoesJson);

            if (resultado?.Usuario == null ||
                string.IsNullOrWhiteSpace(resultado.Token))
            {
                throw new InvalidOperationException(
                    "A API retornou uma resposta inválida.");
            }

            // Restringe o acesso aos perfis administrativos.
            if (!PerfilPermitido(resultado.Usuario.Perfil))
            {
                throw new UnauthorizedAccessException(
                    "Perfil não autorizado a acessar o painel.");
            }

            if (!resultado.Usuario.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "A conta está desativada.");
            }

            // Preenche o modelo utilizado pelo painel administrativo.
            Usuario usuario = new()
            {
                IdUsuario = resultado.Usuario.IdUsuario,
                NomeCompleto = resultado.Usuario.NomeCompleto,
                Login = resultado.Usuario.Login,
                IdPerfil = resultado.Usuario.IdPerfil,
                Ativo = resultado.Usuario.Ativo,
                DataCadastro = resultado.Usuario.DataCadastro,
                DeveAlterarSenha = resultado.Usuario.DeveAlterarSenha,
                SenhaHash = string.Empty
            };

            // Armazena o JWT para as próximas requisições.
            SessaoUsuario.Iniciar(resultado.Token);

            return usuario;
        }

        /// <summary>
        /// Solicita a alteração da senha do usuário autenticado.
        /// </summary>
        public async Task AlterarSenhaAsync(
            string senhaAtual,
            string novaSenha,
            string confirmarNovaSenha)
        {
            // Impede operações sem uma sessão local.
            if (!SessaoUsuario.EstaAutenticado)
            {
                throw new UnauthorizedAccessException(
                    "Usuário não autenticado.");
            }

            // Prepara os dados exigidos pelo endpoint.
            var dadosAlteracao = new
            {
                senhaAtual,
                novaSenha,
                confirmarNovaSenha
            };

            // Solicita a alteração diretamente à API.
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.PostAsJsonAsync(
                    "auth/alterar-senha",
                    dadosAlteracao);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            // A API é responsável pela validação e atualização da senha.
            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ObterMensagemErro(conteudo));
            }
        }

        /// <summary>
        /// Verifica se o perfil pode acessar o painel administrativo.
        /// </summary>
        private static bool PerfilPermitido(string perfil)
        {
            return string.Equals(
                       perfil,
                       "Administrador",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       perfil,
                       "Coordenador",
                       StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extrai a mensagem de erro retornada pelo servidor.
        /// </summary>
        private static string ObterMensagemErro(string conteudo)
        {
            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                if (documento.RootElement.TryGetProperty(
                    "mensagem",
                    out JsonElement mensagem))
                {
                    return mensagem.GetString()
                        ?? "Não foi possível concluir a operação.";
                }
            }
            catch (JsonException)
            {
                // Utiliza uma mensagem padrão para respostas inválidas.
            }

            return "Não foi possível concluir a operação.";
        }

        /// <summary>
        /// Representa a resposta do endpoint de login.
        /// </summary>
        private sealed class RespostaAutenticacao
        {
            public string Token { get; set; } = string.Empty;

            public DadosUsuario? Usuario { get; set; }
        }

        /// <summary>
        /// Representa os dados cadastrais fornecidos pela API.
        /// </summary>
        private sealed class DadosUsuario
        {
            public int IdUsuario { get; set; }

            public string NomeCompleto { get; set; } = string.Empty;

            public string Login { get; set; } = string.Empty;

            public int IdPerfil { get; set; }

            public string Perfil { get; set; } = string.Empty;

            public bool Ativo { get; set; }

            public bool DeveAlterarSenha { get; set; }

            public DateTime DataCadastro { get; set; }
        }
    }
}