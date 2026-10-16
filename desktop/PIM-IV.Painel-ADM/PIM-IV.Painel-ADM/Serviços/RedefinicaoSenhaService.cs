
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    public class RedefinicaoSenhaService
    {
        public void RedefinirSenha(
            Usuario administrador,
            int idUsuario,
            string novaSenha)
        {
            if (administrador == null)
                throw new UnauthorizedAccessException(
                    "Administrador não identificado.");

            if (administrador.IdPerfil != 1 ||
                !administrador.Ativo)
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem redefinir senhas.");

            // Utiliza as mesmas regras do cadastro.
            UsuarioService.ValidarSenha(novaSenha);

            using SqlConnection conexao = Conexao.CriarConexao();
            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                const string sqlAdministrador = @"
                    SELECT COUNT(1)
                    FROM dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Perfis p
                        ON p.IdPerfil = u.IdPerfil
                    WHERE u.IdUsuario = @Administrador
                      AND u.Ativo = 1
                      AND p.NomePerfil = 'Administrador'
                      AND p.Ativo = 1";

                using SqlCommand cmdAdministrador =
                    new SqlCommand(
                        sqlAdministrador,
                        conexao,
                        transacao);

                cmdAdministrador.Parameters.Add(
                    "@Administrador",
                    SqlDbType.Int
                ).Value = administrador.IdUsuario;

                int autorizado = Convert.ToInt32(
                    cmdAdministrador.ExecuteScalar());

                if (autorizado != 1)
                    throw new UnauthorizedAccessException(
                        "Sua conta não possui mais permissão de Administrador.");

                // Confirma que a conta de destino existe
                // e bloqueia a linha durante a alteração.
                const string sqlConsultar = @"
                    SELECT SenhaHash
                    FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdUsuario = @IdUsuario
                      AND Ativo = 1";

                using SqlCommand cmdConsultar =
                    new SqlCommand(
                        sqlConsultar,
                        conexao,
                        transacao);

                cmdConsultar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int
                ).Value = idUsuario;

                string? hashAtual =
                    cmdConsultar.ExecuteScalar() as string;

                if (hashAtual == null)
                    throw new InvalidOperationException(
                        "Usuário não encontrado ou desativado.");

                if (SenhaHelper.VerificarSenha(
                    novaSenha,
                    hashAtual))
                    throw new ArgumentException(
                        "A senha provisória deve ser diferente da senha atual.");

                string novoHash =
                    SenhaHelper.GerarHash(novaSenha);

                // Atualiza a senha e exige que o usuário
                // escolha outra no próximo acesso.
                const string sqlAtualizar = @"
                    UPDATE dbo.Usuarios
                    SET SenhaHash = @SenhaHash,
                        DeveAlterarSenha = 1
                    WHERE IdUsuario = @IdUsuario
                      AND Ativo = 1";

                using SqlCommand cmdAtualizar =
                    new SqlCommand(
                        sqlAtualizar,
                        conexao,
                        transacao);

                cmdAtualizar.Parameters.Add(
                    "@SenhaHash",
                    SqlDbType.NVarChar,
                    500
                ).Value = novoHash;

                cmdAtualizar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int
                ).Value = idUsuario;

                if (cmdAtualizar.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException(
                        "Não foi possível redefinir a senha.");

                // Registra a operação sem armazenar
                // a senha provisória no histórico.
                const string sqlAuditoria = @"
                    INSERT INTO dbo.LogsAtividades
                        (IdUsuario, Acao, Descricao)
                    VALUES
                        (@Administrador, @Acao, @Descricao)";

                using SqlCommand cmdAuditoria =
                    new SqlCommand(
                        sqlAuditoria,
                        conexao,
                        transacao);

                cmdAuditoria.Parameters.Add(
                    "@Administrador",
                    SqlDbType.Int
                ).Value = administrador.IdUsuario;

                cmdAuditoria.Parameters.Add(
                    "@Acao",
                    SqlDbType.NVarChar,
                    100
                ).Value = "REDEFINICAO_SENHA";

                cmdAuditoria.Parameters.Add(
                    "@Descricao",
                    SqlDbType.NVarChar,
                    500
                ).Value =
                    $"Administrador redefiniu a senha provisória " +
                    $"do usuário ID {idUsuario}. " +
                    $"Troca obrigatória no próximo acesso.";

                cmdAuditoria.ExecuteNonQuery();

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }
    }
}
