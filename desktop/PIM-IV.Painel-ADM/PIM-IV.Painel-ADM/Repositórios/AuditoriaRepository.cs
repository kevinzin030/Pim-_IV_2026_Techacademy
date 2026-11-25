using System;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Repositorios
{
    // Responsável pela consulta dos registros de auditoria pela API.
    public class AuditoriaRepository
    {
        // Executa consultas autenticadas na API.
        private static JsonElement ConsultarApi(string rota)
        {
            return Task.Run(async () =>
            {
                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.GetAsync(rota);

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                if (!resposta.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        ExtrairMensagem(conteudo));
                }

                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                return documento.RootElement.Clone();

            }).GetAwaiter().GetResult();
        }

        // Recupera a mensagem de erro retornada pela API.
        private static string ExtrairMensagem(string conteudo)
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
                        ?? "Não foi possível consultar a auditoria.";
                }
            }
            catch (JsonException)
            {
                // Utiliza a mensagem padrão.
            }

            return "Não foi possível consultar a auditoria.";
        }

        // Preserva a assinatura original do serviço.
        // Os registros são gerados diretamente pelos controllers.
        public void Registrar(
            int idUsuario,
            string acao,
            string descricao)
        {
            throw new NotSupportedException(
                "Os registros de auditoria devem ser gerados pela API.");
        }

        // Consulta todos os registros de auditoria.
        public DataTable ListarTodos()
        {
            JsonElement resposta = ConsultarApi("auditoria");

            return ConverterRegistros(
                resposta,
                pesquisa: false);
        }

        // Confirma a autorização do administrador autenticado.
        public bool AdministradorEstaAutorizado(int idUsuario)
        {
            if (idUsuario <= 0 ||
                !SessaoUsuario.EstaAutenticado)
            {
                return false;
            }

            JsonElement resposta =
                ConsultarApi("auditoria/autorizacao");

            return resposta
                .GetProperty("autorizado")
                .GetBoolean();
        }

        // Consulta registros por período e texto pesquisado.
        public DataTable Pesquisar(
            DateTime dataInicio,
            DateTime dataFim,
            string pesquisa)
        {
            if (dataInicio.Date > dataFim.Date)
            {
                throw new ArgumentException(
                    "A data inicial não pode ser posterior à data final.");
            }

            if (dataFim.Date == DateTime.MaxValue.Date)
            {
                throw new ArgumentException(
                    "A data final informada é inválida.");
            }

            pesquisa = (pesquisa ?? string.Empty).Trim();

            if (pesquisa.Length > 200)
            {
                throw new ArgumentException(
                    "O texto de pesquisa ultrapassa o tamanho permitido.");
            }

            string textoCodificado =
                Uri.EscapeDataString(pesquisa);

            string rota =
                $"auditoria?dataInicio={dataInicio:yyyy-MM-dd}" +
                $"&dataFim={dataFim:yyyy-MM-dd}" +
                $"&pesquisa={textoCodificado}";

            JsonElement resposta = ConsultarApi(rota);

            return ConverterRegistros(
                resposta,
                pesquisa: true);
        }

        // Converte os registros mantendo a estrutura original.
        private static DataTable ConverterRegistros(
            JsonElement resposta,
            bool pesquisa)
        {
            DataTable tabela = new();

            tabela.Columns.Add("IdLog", typeof(int));

            if (pesquisa)
            {
                tabela.Columns.Add("DataHora", typeof(DateTime));
                tabela.Columns.Add("Login", typeof(string));
                tabela.Columns.Add("NomeCompleto", typeof(string));
                tabela.Columns.Add("Acao", typeof(string));
                tabela.Columns.Add("Descricao", typeof(string));
            }
            else
            {
                tabela.Columns.Add("Usuario", typeof(string));
                tabela.Columns.Add("Acao", typeof(string));
                tabela.Columns.Add("Descricao", typeof(string));
                tabela.Columns.Add("DataHora", typeof(DateTime));
            }

            foreach (JsonElement registro in
                resposta.GetProperty("registros").EnumerateArray())
            {
                int idLog =
                    registro.GetProperty("IdLog").GetInt32();

                DateTime dataHora =
                    registro.GetProperty("DataHora").GetDateTime();

                string acao =
                    ObterTexto(registro, "Acao");

                string descricao =
                    ObterTexto(registro, "Descricao");

                if (pesquisa)
                {
                    tabela.Rows.Add(
                        idLog,
                        dataHora,
                        ObterTexto(registro, "Login"),
                        ObterTexto(registro, "NomeCompleto"),
                        acao,
                        descricao);
                }
                else
                {
                    tabela.Rows.Add(
                        idLog,
                        ObterTexto(registro, "Usuario"),
                        acao,
                        descricao,
                        dataHora);
                }
            }

            return tabela;
        }

        // Trata campos textuais que podem retornar nulos.
        private static string ObterTexto(
            JsonElement registro,
            string propriedade)
        {
            if (registro.TryGetProperty(
                propriedade,
                out JsonElement valor) &&
                valor.ValueKind == JsonValueKind.String)
            {
                return valor.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}