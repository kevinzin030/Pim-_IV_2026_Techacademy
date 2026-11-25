using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    // Gerencia a redefinição administrativa de senhas pela API.
    public class RedefinicaoSenhaService
    {
        // Redefine a senha e exige sua alteração no próximo acesso.
        public void RedefinirSenha(
            Usuario administrador,
            int idUsuario,
            string novaSenha)
        {
            ValidarAdministrador(administrador);
            ValidarUsuarioDestino(idUsuario);

            // Aplica as regras de segurança utilizadas no cadastro.
            UsuarioService.ValidarSenha(novaSenha);

            // Mantém a compatibilidade com os formulários existentes.
            Task.Run(async () =>
            {
                await RedefinirSenhaAsync(
                    idUsuario,
                    novaSenha);

            }).GetAwaiter().GetResult();
        }

        // Encaminha a solicitação de redefinição para a API.
        private static async Task RedefinirSenhaAsync(
            int idUsuario,
            string novaSenha)
        {
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.PostAsJsonAsync(
                    $"usuarios/{idUsuario}/redefinir-senha",
                    new
                    {
                        novaSenha
                    });

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ExtrairMensagem(conteudo));
            }
        }

        // Valida o administrador autenticado.
        private static void ValidarAdministrador(
            Usuario administrador)
        {
            if (administrador == null ||
                administrador.IdPerfil != 1 ||
                !administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem redefinir senhas.");
            }

            if (!SessaoUsuario.EstaAutenticado)
            {
                throw new UnauthorizedAccessException(
                    "Sessão inválida. Realize o login novamente.");
            }
        }

        // Valida o identificador do usuário de destino.
        private static void ValidarUsuarioDestino(
            int idUsuario)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "O identificador do usuário é inválido.",
                    nameof(idUsuario));
            }
        }

        // Recupera a mensagem retornada pela API.
        private static string ExtrairMensagem(
            string conteudo)
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
                        ?? "Não foi possível redefinir a senha.";
                }
            }
            catch (JsonException)
            {
                // Utiliza a mensagem padrão.
            }

            return "Não foi possível redefinir a senha.";
        }
    }
}