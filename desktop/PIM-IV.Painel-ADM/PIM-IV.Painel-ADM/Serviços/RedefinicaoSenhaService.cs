using System;
using System.Data;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    // Gerencia a redefinição administrativa de senhas.
    public class RedefinicaoSenhaService
    {
        private const int TamanhoHash = 500;
        private const int TamanhoAcao = 100;
        private const int TamanhoDescricao = 500;

        // Redefine a senha e exige sua alteração no próximo acesso.
        public void RedefinirSenha(
            Usuario administrador,
            int idUsuario,
            string novaSenha)
        {
            ValidarAdministrador(administrador);
            ValidarUsuarioDestino(idUsuario);

            // Aplica as mesmas regras utilizadas no cadastro.
            UsuarioService.ValidarSenha(novaSenha);

            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                ConfirmarAdministradorNoBanco(
                    conexao,
                    transacao,
                    administrador.IdUsuario);

                string hashAtual = ConsultarHashAtual(
                    conexao,
                    transacao,
                    idUsuario);

                ValidarSenhaDiferente(
                    novaSenha,
                    hashAtual);

                string novoHash =
                    SenhaHelper.GerarHash(novaSenha);

                AtualizarSenha(
                    conexao,
                    transacao,
                    idUsuario,
                    novoHash);

                RegistrarAuditoria(
                    conexao,
                    transacao,
                    administrador.IdUsuario,
                    idUsuario);

                transacao.Commit();
            }
            catch
            {
                // Desfaz a alteração caso alguma etapa falhe.
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Valida os dados do administrador da sessão.
        private static void ValidarAdministrador(
            Usuario administrador)
        {
            if (administrador == null)
            {
                throw new UnauthorizedAccessException(
                    "Administrador não identificado.");
            }

            if (administrador.IdPerfil != 1 ||
                !administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem redefinir senhas.");
            }
        }

        // Verifica se o identificador do usuário é válido.
        private static void ValidarUsuarioDestino(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "O identificador do usuário é inválido.",
                    nameof(idUsuario));
            }
        }

        // Confirma a autorização diretamente no banco de dados.
        private static void ConfirmarAdministradorNoBanco(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                WHERE u.IdUsuario = @Administrador
                  AND u.Ativo = 1
                  AND p.NomePerfil = N'Administrador'
                  AND p.Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Administrador",
                SqlDbType.Int).Value = idAdministrador;

            int autorizado = Convert.ToInt32(
                comando.ExecuteScalar());

            if (autorizado != 1)
            {
                throw new UnauthorizedAccessException(
                    "Sua conta não possui mais permissão " +
                    "de Administrador.");
            }
        }

        // Consulta a senha atual e bloqueia o registro.
        private static string ConsultarHashAtual(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                SELECT SenhaHash
                FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            string? hashAtual =
                comando.ExecuteScalar() as string;

            if (hashAtual == null)
            {
                throw new InvalidOperationException(
                    "Usuário não encontrado ou desativado.");
            }

            return hashAtual;
        }

        // Impede a reutilização da senha atual.
        private static void ValidarSenhaDiferente(
            string novaSenha,
            string hashAtual)
        {
            bool senhaReutilizada =
                SenhaHelper.VerificarSenha(
                    novaSenha,
                    hashAtual);

            if (senhaReutilizada)
            {
                throw new ArgumentException(
                    "A senha provisória deve ser " +
                    "diferente da senha atual.");
            }
        }

        // Atualiza o hash e marca a troca como obrigatória.
        private static void AtualizarSenha(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string novoHash)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET
                    SenhaHash = @SenhaHash,
                    DeveAlterarSenha = 1
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@SenhaHash",
                SqlDbType.NVarChar,
                TamanhoHash).Value = novoHash;

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            int registrosAlterados =
                comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível redefinir a senha.");
            }
        }

        // Registra a operação sem armazenar a senha provisória.
        private static void RegistrarAuditoria(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador,
            int idUsuario)
        {
            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                (
                    IdUsuario,
                    Acao,
                    Descricao
                )
                VALUES
                (
                    @Administrador,
                    @Acao,
                    @Descricao
                )";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Administrador",
                SqlDbType.Int).Value = idAdministrador;

            comando.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                TamanhoAcao).Value = "REDEFINICAO_SENHA";

            string descricao =
                $"Administrador redefiniu a senha provisória " +
                $"do usuário ID {idUsuario}. " +
                $"Troca obrigatória no próximo acesso.";

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                TamanhoDescricao).Value = descricao;

            comando.ExecuteNonQuery();
        }
    }
}