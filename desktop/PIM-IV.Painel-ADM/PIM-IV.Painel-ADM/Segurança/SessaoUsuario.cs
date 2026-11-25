using System;
using System.Net.Http.Headers;
using PIM_IV.Painel_ADM.Data;

namespace PIM_IV.Painel_ADM.Seguranca
{
    /// <summary>
    /// Gerencia a sessão do usuário autenticado no painel
    /// administrativo por meio do token JWT fornecido pela API.
    /// </summary>
    internal static class SessaoUsuario
    {
        // Armazena o token JWT durante a execução da aplicação.
        public static string Token { get; private set; } = string.Empty;

        // Indica se existe uma sessão autenticada localmente.
        public static bool EstaAutenticado =>
            !string.IsNullOrWhiteSpace(Token);

        /// <summary>
        /// Inicia a sessão do usuário e configura o token
        /// de autenticação nas requisições HTTP.
        /// </summary>
        public static void Iniciar(string token)
        {
            // Impede a criação de uma sessão sem um token válido.
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "O token de autenticação não pode ser vazio.",
                    nameof(token)
                );
            }

            Token = token;

            // Adiciona o token JWT ao cabeçalho Authorization,
            // permitindo o acesso aos endpoints protegidos da API.
            ApiClient.Cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);
        }

        /// <summary>
        /// Encerra a sessão local e remove as credenciais
        /// utilizadas nas requisições HTTP.
        /// </summary>
        public static void Encerrar()
        {
            // Remove o token armazenado na memória.
            Token = string.Empty;

            // Remove o cabeçalho de autenticação das requisições.
            ApiClient.Cliente.DefaultRequestHeaders.Authorization = null;
        }
    }
}