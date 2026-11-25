using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PIM_IV.Painel_ADM.Data
{
    // Verifica a disponibilidade do banco de dados por meio da API.
    internal static class TesteConexao
    {
        // Mantém a compatibilidade com chamadas síncronas.
        public static bool Verificar()
        {
            return Task.Run(VerificarAsync)
                .GetAwaiter()
                .GetResult();
        }

        // Consulta o estado da conexão entre a API e o banco.
        public static async Task<bool> VerificarAsync()
        {
            try
            {
                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.GetAsync("status-banco");

                if (!resposta.IsSuccessStatusCode)
                {
                    return false;
                }

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                return documento.RootElement.TryGetProperty(
                    "status",
                    out JsonElement status)
                    && status.GetString() == "conectado";
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}