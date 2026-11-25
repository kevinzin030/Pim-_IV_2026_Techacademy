using System;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    /// <summary>
    /// Gerencia a consulta das permissões dos usuários
    /// e perfis através da API REST do TechAcademy.
    /// </summary>
    public class PermissaoService
    {
        private const int TamanhoCodigoPermissao = 100;

        // Configura a interpretação dos dados JSON retornados pela API.
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Verifica se o usuário autenticado possui determinada permissão.
        /// </summary>
        public bool UsuarioTemPermissao(
            Usuario usuario,
            string codigoPermissao)
        {
            // Valida os dados do usuário e da permissão solicitada.
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

            // Consulta as permissões vinculadas à sessão autenticada.
            DataTable permissoes = ExecutarConsulta(
                "permissoes/me");

            // Verifica se a permissão solicitada está disponível.
            foreach (DataRow permissao in permissoes.Rows)
            {
                string codigo = Convert.ToString(
                    permissao["Codigo"]) ?? string.Empty;

                if (string.Equals(
                    codigo,
                    codigoPermissao,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Confirma se o usuário está ativo e possui uma sessão local.
        /// </summary>
        private static bool UsuarioValido(Usuario? usuario)
        {
            return usuario != null &&
                   usuario.IdUsuario > 0 &&
                   usuario.Ativo &&
                   SessaoUsuario.EstaAutenticado;
        }

        /// <summary>
        /// Lista todas as permissões ativas cadastradas no sistema.
        /// </summary>
        public DataTable ListarPermissoes()
        {
            return ExecutarConsulta("permissoes");
        }

        /// <summary>
        /// Lista as permissões ativas atribuídas ao perfil informado.
        /// </summary>
        public DataTable ListarPermissoesDoPerfil(int idPerfil)
        {
            // Impede consultas com identificadores inválidos.
            if (idPerfil <= 0)
            {
                throw new ArgumentException(
                    "O identificador do perfil é inválido.",
                    nameof(idPerfil));
            }

            return ExecutarConsulta(
                $"perfis/{idPerfil}/permissoes");
        }

        /// <summary>
        /// Mantém a compatibilidade com os formulários existentes,
        /// que utilizam métodos síncronos e objetos DataTable.
        /// </summary>
        private static DataTable ExecutarConsulta(string endpoint)
        {
            return Task.Run(
                () => ExecutarConsultaAsync(endpoint))
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Realiza uma consulta HTTP autenticada e converte
        /// os dados recebidos para uma tabela.
        /// </summary>
        private static async Task<DataTable> ExecutarConsultaAsync(
            string endpoint)
        {
            // Impede consultas sem uma sessão autenticada.
            if (!SessaoUsuario.EstaAutenticado)
            {
                throw new UnauthorizedAccessException(
                    "Usuário não autenticado.");
            }

            // Envia a requisição utilizando o cliente HTTP centralizado.
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.GetAsync(endpoint);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            // Trata erros de comunicação e autorização.
            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ObterMensagemErro(conteudo));
            }

            // Converte a resposta JSON em objetos C#.
            RespostaPermissoes? resultado =
                JsonSerializer.Deserialize<RespostaPermissoes>(
                    conteudo,
                    OpcoesJson);

            if (resultado?.Permissoes == null)
            {
                throw new InvalidOperationException(
                    "A API retornou uma resposta de permissões inválida.");
            }

            // Preserva a estrutura esperada pelos formulários.
            DataTable tabela = CriarTabelaPermissoes();

            foreach (DadosPermissao permissao in resultado.Permissoes)
            {
                tabela.Rows.Add(
                    permissao.IdPermissao,
                    permissao.Codigo,
                    permissao.Descricao,
                    permissao.Plataforma);
            }

            return tabela;
        }

        /// <summary>
        /// Cria uma tabela compatível com a estrutura original
        /// utilizada pelo painel administrativo.
        /// </summary>
        private static DataTable CriarTabelaPermissoes()
        {
            DataTable tabela = new();

            tabela.Columns.Add(
                "IdPermissao", typeof(int));

            tabela.Columns.Add(
                "Codigo", typeof(string));

            tabela.Columns.Add(
                "Descricao", typeof(string));

            tabela.Columns.Add(
                "Plataforma", typeof(string));

            return tabela;
        }

        /// <summary>
        /// Extrai a mensagem de erro retornada pela API.
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
                        ?? "Não foi possível consultar as permissões.";
                }
            }
            catch (JsonException)
            {
                // Utiliza a mensagem padrão quando o retorno
                // do servidor não possui um JSON válido.
            }

            return "Não foi possível consultar as permissões.";
        }

        /// <summary>
        /// Representa a resposta dos endpoints de permissões.
        /// </summary>
        private sealed class RespostaPermissoes
        {
            public DadosPermissao[]? Permissoes { get; set; }
        }

        /// <summary>
        /// Representa uma permissão retornada pela API.
        /// </summary>
        private sealed class DadosPermissao
        {
            public int IdPermissao { get; set; }

            public string Codigo { get; set; } = string.Empty;

            public string Descricao { get; set; } = string.Empty;

            public string Plataforma { get; set; } = string.Empty;
        }
    }
}