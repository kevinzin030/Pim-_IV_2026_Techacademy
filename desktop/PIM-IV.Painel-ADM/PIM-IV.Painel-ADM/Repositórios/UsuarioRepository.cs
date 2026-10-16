
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Data;

namespace PIM_IV.Painel_ADM.Repositorios
{
    public class UsuarioRepository
    {
        public Usuario? BuscarPorLogin(string login)
        {
            const string sql = @"
                SELECT u.IdUsuario, u.NomeCompleto, u.Login,
                       u.SenhaHash, u.IdPerfil, u.Ativo,
                       u.DataCadastro
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.Login = @Login
                  AND p.NomePerfil IN
                      ('Administrador', 'Coordenador')
                  AND p.Ativo = 1";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@Login", SqlDbType.NVarChar, 100).Value = login;

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            if (!leitor.Read())
                return null;

            return new Usuario
            {
                IdUsuario = leitor.GetInt32(0),
                NomeCompleto = leitor.GetString(1),
                Login = leitor.GetString(2),
                SenhaHash = leitor.GetString(3),
                IdPerfil = leitor.GetInt32(4),
                Ativo = leitor.GetBoolean(5),
                DataCadastro = leitor.GetDateTime(6)
            };
        }

        public void Cadastrar(Usuario usuario, int idAdministrador)
        {
            using SqlConnection conexao = Conexao.CriarConexao();
            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                ValidarAdministrador(
                    conexao, transacao, idAdministrador);

                const string sqlPerfil = @"
                    SELECT NomePerfil
                    FROM dbo.Perfis
                    WHERE IdPerfil = @IdPerfil
                      AND Ativo = 1";

                using SqlCommand cmdPerfil =
                    new(sqlPerfil, conexao, transacao);

                cmdPerfil.Parameters.Add(
                    "@IdPerfil", SqlDbType.Int).Value =
                    usuario.IdPerfil;

                string? nomePerfil =
                    cmdPerfil.ExecuteScalar() as string;

                if (nomePerfil == null)
                    throw new ArgumentException(
                        "O perfil selecionado não está disponível.");

                bool automatico =
                    nomePerfil == "Aluno" ||
                    nomePerfil == "Professor" ||
                    nomePerfil == "Coordenador";

                string loginInicial = automatico
                    ? "TMP_" + Guid.NewGuid().ToString("N")
                    : usuario.Login;

                const string sqlInserir = @"
                    INSERT INTO dbo.Usuarios
                        (NomeCompleto, Login, SenhaHash,
                         IdPerfil, Ativo)
                    OUTPUT INSERTED.IdUsuario
                    VALUES
                        (@Nome, @Login, @Hash,
                         @Perfil, @Ativo)";

                using SqlCommand comando =
                    new(sqlInserir, conexao, transacao);

                comando.Parameters.Add(
                    "@Nome", SqlDbType.NVarChar, 150).Value =
                    usuario.NomeCompleto;

                comando.Parameters.Add(
                    "@Login", SqlDbType.NVarChar, 100).Value =
                    loginInicial;

                comando.Parameters.Add(
                    "@Hash", SqlDbType.NVarChar, 500).Value =
                    usuario.SenhaHash;

                comando.Parameters.Add(
                    "@Perfil", SqlDbType.Int).Value =
                    usuario.IdPerfil;

                comando.Parameters.Add(
                    "@Ativo", SqlDbType.Bit).Value =
                    usuario.Ativo;

                int idNovo =
                    Convert.ToInt32(comando.ExecuteScalar());

                if (automatico)
                {
                    // D3: minimo de tres digitos.
                    // Exemplo: 15 -> 015; 1000 -> 1000.
                    string identificador = nomePerfil switch
                    {
                        "Aluno" => $"RA{idNovo:D3}",
                        "Professor" => $"RP{idNovo:D3}",
                        "Coordenador" => $"RC{idNovo:D3}",
                        _ => throw new InvalidOperationException(
                            "Perfil inválido.")
                    };

                    const string sqlAtualizarLogin = @"
                        UPDATE dbo.Usuarios
                        SET Login = @Login
                        WHERE IdUsuario = @IdUsuario";

                    using SqlCommand cmdLogin =
                        new(sqlAtualizarLogin, conexao, transacao);

                    cmdLogin.Parameters.Add(
                        "@Login", SqlDbType.NVarChar, 100).Value =
                        identificador;

                    cmdLogin.Parameters.Add(
                        "@IdUsuario", SqlDbType.Int).Value =
                        idNovo;

                    cmdLogin.ExecuteNonQuery();

                    if (nomePerfil == "Aluno")
                    {
                        // IdAluno e IDENTITY no SQL Server.
                        const string sqlAluno = @"
                            INSERT INTO dbo.Alunos
                                (IdUsuario, MatriculaAluno)
                            VALUES
                                (@IdUsuario, @Registro)";

                        using SqlCommand cmdAluno =
                            new(sqlAluno, conexao, transacao);

                        cmdAluno.Parameters.Add(
                            "@IdUsuario", SqlDbType.Int).Value =
                            idNovo;

                        cmdAluno.Parameters.Add(
                            "@Registro", SqlDbType.NVarChar, 30)
                            .Value = identificador;

                        cmdAluno.ExecuteNonQuery();
                    }
                    else if (nomePerfil == "Professor")
                    {
                        const string sqlProfessor = @"
                            INSERT INTO dbo.Professores
                                (IdUsuario, RegistroProfessor)
                            VALUES
                                (@IdUsuario, @Registro)";

                        using SqlCommand cmdProfessor =
                            new(sqlProfessor, conexao, transacao);

                        cmdProfessor.Parameters.Add(
                            "@IdUsuario", SqlDbType.Int).Value =
                            idNovo;

                        cmdProfessor.Parameters.Add(
                            "@Registro", SqlDbType.NVarChar, 30)
                            .Value = identificador;

                        cmdProfessor.ExecuteNonQuery();
                    }

                    usuario.Login = identificador;
                }

                usuario.IdUsuario = idNovo;

                RegistrarAuditoria(
                    conexao,
                    transacao,
                    idAdministrador,
                    "CADASTRO_USUARIO",
                    $"Usuário ID {idNovo} cadastrado. " +
                    $"Perfil: {nomePerfil}.");

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        public bool LoginExiste(string login)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios
                WHERE Login = @Login";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@Login", SqlDbType.NVarChar, 100).Value = login;

            conexao.Open();

            return Convert.ToInt32(comando.ExecuteScalar()) > 0;
        }

        public DataTable ListarPerfis()
        {
            const string sql = @"
                SELECT IdPerfil, NomePerfil
                FROM dbo.Perfis
                WHERE Ativo = 1
                ORDER BY NomePerfil";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }

        public DataTable ListarTodos()
        {
            const string sql = @"
                SELECT u.IdUsuario, u.NomeCompleto,
                       u.Login, p.NomePerfil,
                       u.IdPerfil, u.Ativo,
                       u.DataCadastro
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                ORDER BY u.NomeCompleto";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor = comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }

        public void Editar(
            int idAdministrador,
            int idUsuario,
            string nome,
            string login,
            int idPerfil)
        {
            using SqlConnection conexao = Conexao.CriarConexao();
            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                ValidarAdministrador(
                    conexao, transacao, idAdministrador);

                const string sqlUsuario = @"
                    SELECT u.IdPerfil, u.Ativo,
                           p.NomePerfil, u.Login
                    FROM dbo.Usuarios u
                         WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Perfis p
                        ON u.IdPerfil = p.IdPerfil
                    WHERE u.IdUsuario = @Id";

                using SqlCommand consulta =
                    new(sqlUsuario, conexao, transacao);

                consulta.Parameters.Add(
                    "@Id", SqlDbType.Int).Value = idUsuario;

                int perfilAnterior;
                bool ativo;
                string nomePerfilAnterior;
                string loginAnterior;

                using (SqlDataReader leitor =
                    consulta.ExecuteReader())
                {
                    if (!leitor.Read())
                        throw new InvalidOperationException(
                            "Usuário não encontrado.");

                    perfilAnterior = leitor.GetInt32(0);
                    ativo = leitor.GetBoolean(1);
                    nomePerfilAnterior = leitor.GetString(2);
                    loginAnterior = leitor.GetString(3);
                }

                if (idUsuario == idAdministrador &&
                    perfilAnterior != idPerfil)
                {
                    throw new InvalidOperationException(
                        "Você não pode alterar seu próprio perfil.");
                }

                const string sqlPerfil = @"
                    SELECT NomePerfil
                    FROM dbo.Perfis
                    WHERE IdPerfil = @Perfil AND Ativo = 1";

                using SqlCommand cmdPerfil =
                    new(sqlPerfil, conexao, transacao);

                cmdPerfil.Parameters.Add(
                    "@Perfil", SqlDbType.Int).Value = idPerfil;

                string? novoPerfil =
                    cmdPerfil.ExecuteScalar() as string;

                if (novoPerfil == null)
                    throw new ArgumentException(
                        "O perfil selecionado não está disponível.");

                if (nomePerfilAnterior == "Administrador" &&
                    novoPerfil != "Administrador" && ativo)
                {
                    VerificarOutroAdministrador(
                        conexao, transacao, idUsuario);
                }

                if (perfilAnterior != idPerfil)
                {
                    throw new InvalidOperationException(
                        "A alteração de perfil ainda não está " +
                        "disponível para preservar os " +
                        "vínculos acadêmicos.");
                }

                if (novoPerfil != "Administrador" &&
                    login != loginAnterior)
                {
                    throw new InvalidOperationException(
                        "O identificador deste perfil é " +
                        "gerado automaticamente e não " +
                        "pode ser alterado.");
                }

                const string sqlEditar = @"
                    UPDATE dbo.Usuarios
                    SET NomeCompleto = @Nome,
                        Login = @Login,
                        IdPerfil = @Perfil
                    WHERE IdUsuario = @Id";

                using SqlCommand comando =
                    new(sqlEditar, conexao, transacao);

                comando.Parameters.Add(
                    "@Nome", SqlDbType.NVarChar, 150).Value = nome;

                comando.Parameters.Add(
                    "@Login", SqlDbType.NVarChar, 100).Value = login;

                comando.Parameters.Add(
                    "@Perfil", SqlDbType.Int).Value = idPerfil;

                comando.Parameters.Add(
                    "@Id", SqlDbType.Int).Value = idUsuario;

                comando.ExecuteNonQuery();

                RegistrarAuditoria(
                    conexao,
                    transacao,
                    idAdministrador,
                    "EDICAO_USUARIO",
                    $"Dados do usuário ID {idUsuario} atualizados.");

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        public void AlterarStatus(
            int idAdministrador,
            int idUsuario,
            bool novoStatus)
        {
            using SqlConnection conexao = Conexao.CriarConexao();
            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                ValidarAdministrador(
                    conexao, transacao, idAdministrador);

                if (idAdministrador == idUsuario && !novoStatus)
                    throw new InvalidOperationException(
                        "Você não pode desativar sua própria conta.");

                const string sqlConsultar = @"
                    SELECT u.Ativo, p.NomePerfil
                    FROM dbo.Usuarios u
                         WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Perfis p
                        ON u.IdPerfil = p.IdPerfil
                    WHERE u.IdUsuario = @Id";

                using SqlCommand consulta =
                    new(sqlConsultar, conexao, transacao);

                consulta.Parameters.Add(
                    "@Id", SqlDbType.Int).Value = idUsuario;

                bool ativoAnterior;
                string perfil;

                using (SqlDataReader leitor =
                    consulta.ExecuteReader())
                {
                    if (!leitor.Read())
                        throw new InvalidOperationException(
                            "Usuário não encontrado.");

                    ativoAnterior = leitor.GetBoolean(0);
                    perfil = leitor.GetString(1);
                }

                if (perfil == "Administrador" &&
                    ativoAnterior && !novoStatus)
                {
                    VerificarOutroAdministrador(
                        conexao, transacao, idUsuario);
                }

                const string sqlAtualizar = @"
                    UPDATE dbo.Usuarios
                    SET Ativo = @Ativo
                    WHERE IdUsuario = @Id";

                using SqlCommand comando =
                    new(sqlAtualizar, conexao, transacao);

                comando.Parameters.Add(
                    "@Ativo", SqlDbType.Bit).Value = novoStatus;

                comando.Parameters.Add(
                    "@Id", SqlDbType.Int).Value = idUsuario;

                comando.ExecuteNonQuery();

                RegistrarAuditoria(
                    conexao,
                    transacao,
                    idAdministrador,
                    novoStatus
                        ? "ATIVACAO_USUARIO"
                        : "DESATIVACAO_USUARIO",
                    $"Conta do usuário ID {idUsuario} " +
                    (novoStatus ? "ativada." : "desativada."));

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        private static void RegistrarAuditoria(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador,
            string acao,
            string descricao)
        {
            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                    (IdUsuario, Acao, Descricao)
                VALUES
                    (@Usuario, @Acao, @Descricao)";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Usuario", SqlDbType.Int).Value =
                idAdministrador;

            comando.Parameters.Add(
                "@Acao", SqlDbType.NVarChar, 100).Value = acao;

            comando.Parameters.Add(
                "@Descricao", SqlDbType.NVarChar, 500).Value =
                descricao;

            comando.ExecuteNonQuery();
        }

        private static void ValidarAdministrador(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                     WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.IdUsuario = @Id
                  AND u.Ativo = 1
                  AND p.NomePerfil = 'Administrador'
                  AND p.Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Id", SqlDbType.Int).Value = idAdministrador;

            if (Convert.ToInt32(comando.ExecuteScalar()) == 0)
            {
                throw new UnauthorizedAccessException(
                    "Sua conta não possui mais permissão " +
                    "de Administrador.");
            }
        }

        private static void VerificarOutroAdministrador(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                     WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE p.NomePerfil = 'Administrador'
                  AND p.Ativo = 1
                  AND u.Ativo = 1
                  AND u.IdUsuario <> @Id";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Id", SqlDbType.Int).Value = idUsuario;

            if (Convert.ToInt32(comando.ExecuteScalar()) == 0)
            {
                throw new InvalidOperationException(
                    "O sistema precisa manter pelo menos " +
                    "um Administrador ativo.");
            }
        }
    }
}
