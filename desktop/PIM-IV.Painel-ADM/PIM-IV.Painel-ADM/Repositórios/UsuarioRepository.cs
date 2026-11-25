using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PIM_IV.Painel_ADM.Repositorios
{
    // Responsável pela comunicação com a API de usuários.
    public class UsuarioRepository
    {
        // Executa uma requisição e valida a resposta da API.
        private static JsonElement ExecutarRequisicao(
            HttpMethod metodo,
            string rota,
            object? dados = null)
        {
            return Task.Run(async () =>
            {
                using HttpRequestMessage requisicao =
                    new(metodo, rota);

                if (dados != null)
                {
                    requisicao.Content =
                        JsonContent.Create(dados);
                }

                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.SendAsync(requisicao);

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

        // Extrai a mensagem de erro retornada pela API.
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
                        ?? "Não foi possível concluir a operação.";
                }
            }
            catch (JsonException)
            {
                // Utiliza a mensagem padrão.
            }

            return "Não foi possível concluir a operação.";
        }

        // Cadastra o usuário pela API.
        // A senha é protegida pelo servidor antes da persistência.
        public void Cadastrar(
            Usuario usuario,
            int idAdministrador,
            string senha)
        {
            ArgumentNullException.ThrowIfNull(usuario);

            JsonElement resposta = ExecutarRequisicao(
                HttpMethod.Post,
                "usuarios",
                new
                {
                    nomeCompleto = usuario.NomeCompleto,
                    login = usuario.Login,
                    senha,
                    idPerfil = usuario.IdPerfil
                });

            JsonElement usuarioCadastrado =
                resposta.GetProperty("usuario");

            // Recupera o identificador definitivo gerado pelo banco.
            usuario.IdUsuario =
                usuarioCadastrado
                    .GetProperty("idUsuario")
                    .GetInt32();

            usuario.Login =
                usuarioCadastrado
                    .GetProperty("login")
                    .GetString() ?? string.Empty;

            usuario.Ativo =
                usuarioCadastrado
                    .GetProperty("ativo")
                    .GetBoolean();

            usuario.DeveAlterarSenha =
                usuarioCadastrado
                    .GetProperty("deveAlterarSenha")
                    .GetBoolean();
        }

        // Verifica se determinado login já está cadastrado.
        public bool LoginExiste(string login)
        {
            string loginCodificado =
                Uri.EscapeDataString(login);

            JsonElement resposta = ExecutarRequisicao(
                HttpMethod.Get,
                $"usuarios/verificar-login?login={loginCodificado}");

            return resposta
                .GetProperty("existe")
                .GetBoolean();
        }

        // Lista os perfis ativos disponíveis para cadastro.
        public DataTable ListarPerfis()
        {
            JsonElement resposta = ExecutarRequisicao(
                HttpMethod.Get,
                "usuarios/perfis");

            DataTable tabela = new();

            tabela.Columns.Add("IdPerfil", typeof(int));
            tabela.Columns.Add("NomePerfil", typeof(string));

            foreach (JsonElement perfil in
                resposta.GetProperty("perfis").EnumerateArray())
            {
                tabela.Rows.Add(
                    perfil.GetProperty("IdPerfil").GetInt32(),
                    perfil.GetProperty("NomePerfil").GetString()
                );
            }

            return tabela;
        }

        // Lista todos os usuários cadastrados.
        // Mantém as colunas utilizadas pelos formulários existentes.
        public DataTable ListarTodos()
        {
            JsonElement resposta = ExecutarRequisicao(
                HttpMethod.Get,
                "usuarios");

            DataTable tabela = new();

            tabela.Columns.Add("IdUsuario", typeof(int));
            tabela.Columns.Add("NomeCompleto", typeof(string));
            tabela.Columns.Add("Login", typeof(string));
            tabela.Columns.Add("NomePerfil", typeof(string));
            tabela.Columns.Add("IdPerfil", typeof(int));
            tabela.Columns.Add("Ativo", typeof(bool));
            tabela.Columns.Add("DataCadastro", typeof(DateTime));

            foreach (JsonElement usuario in
                resposta.GetProperty("usuarios").EnumerateArray())
            {
                tabela.Rows.Add(
                    usuario.GetProperty("IdUsuario").GetInt32(),
                    usuario.GetProperty("NomeCompleto").GetString(),
                    usuario.GetProperty("Login").GetString(),
                    usuario.GetProperty("NomePerfil").GetString(),
                    usuario.GetProperty("IdPerfil").GetInt32(),
                    usuario.GetProperty("Ativo").GetBoolean(),
                    usuario.GetProperty("DataCadastro").GetDateTime()
                );
            }

            return tabela;
        }

        // Atualiza os dados cadastrais de um usuário.
        public void Editar(
            int idAdministrador,
            int idUsuario,
            string nome,
            string login,
            int idPerfil)
        {
            ExecutarRequisicao(
                HttpMethod.Put,
                $"usuarios/{idUsuario}",
                new
                {
                    nomeCompleto = nome,
                    login,
                    idPerfil
                });
        }

        // Ativa ou desativa uma conta.
        public void AlterarStatus(
            int idAdministrador,
            int idUsuario,
            bool novoStatus)
        {
            ExecutarRequisicao(
                HttpMethod.Patch,
                $"usuarios/{idUsuario}/status",
                new
                {
                    ativo = novoStatus
                });
        }
    }
}