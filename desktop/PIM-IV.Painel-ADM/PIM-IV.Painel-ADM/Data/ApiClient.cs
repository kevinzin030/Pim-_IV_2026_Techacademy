using System;
using System.Net.Http;

namespace PIM_IV.Painel_ADM.Data
{
    /// <summary>
    /// Centraliza a comunicação HTTP entre o painel
    /// administrativo e a API REST do TechAcademy.
    /// </summary>
    internal static class ApiClient
    {
        // Endereço base da API no ambiente de desenvolvimento.
        // Será substituído pelo endereço de produção após a publicação.
        private const string UrlBaseApi = "http://localhost:3000/api/";

        // Mantém uma única instância do HttpClient durante
        // a execução da aplicação, evitando conexões desnecessárias.
        public static HttpClient Cliente { get; } = new HttpClient
        {
            BaseAddress = new Uri(UrlBaseApi),

            // Define o tempo máximo de espera por uma resposta da API.
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}