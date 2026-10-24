using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Data;

namespace PIM_IV.Painel_ADM.Repositorios
{
    // Responsável pelo acesso aos dados dos usuários.
    public class UsuarioRepository
    {
        // Localiza um usuário autorizado a acessar o painel desktop.
        public Usuario? BuscarPorLogin(string login)
        {
            const string sql = @"
                SELECT
                    u.IdUsuario,
                    u.NomeCompleto,
                    u.Login,
                    u.SenhaHash,
                    u.IdPerfil,
                    u.Ativo,
                    u.DataCadastro
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.Login = @Login
                  AND p.NomePerfil IN
                      ('Administrador', 'Coordenador')
                  AND p.Ativo = 1";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                100).Value = login;

            conexao.Open();

            using SqlDataReader leitor =
                comando.ExecuteReader();

            if (!leitor.Read())
            {
                return null;
            }

            return MapearUsuario(leitor);
        }

        // Converte os dados do banco em um objeto Usuario.
        private static Usuario MapearUsuario(SqlDataReader leitor)
        {
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

        // Cadastra um usuário e seus registros acadêmicos.
        public void Cadastrar(
            Usuario usuario,
            int idAdministrador)
        {
            ArgumentNullException.ThrowIfNull(usuario);

            (int idNovo, string loginFinal) resultado =
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        ValidarAdministrador(
                            conexao,
                            transacao,
                            idAdministrador);

                        string nomePerfil = ConsultarNomePerfil(
                            conexao,
                            transacao,
                            usuario.IdPerfil);

                        bool identificadorAutomatico =
                            PossuiIdentificadorAutomatico(nomePerfil);

                        string loginInicial = identificadorAutomatico
                            ? "TMP_" + Guid.NewGuid().ToString("N")
                            : usuario.Login;

                        int idNovo = InserirUsuario(
                            conexao,
                            transacao,
                            usuario,
                            loginInicial);

                        string loginFinal = loginInicial;

                        if (identificadorAutomatico)
                        {
                            loginFinal = GerarIdentificador(
                                nomePerfil,
                                idNovo);

                            AtualizarLogin(
                                conexao,
                                transacao,
                                idNovo,
                                loginFinal);

                            CriarRegistroAcademico(
                                conexao,
                                transacao,
                                idNovo,
                                nomePerfil,
                                loginFinal);
                        }

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            idAdministrador,
                            "CADASTRO_USUARIO",
                            $"Usuário ID {idNovo} cadastrado. " +
                            $"Perfil: {nomePerfil}.");

                        return (idNovo, loginFinal);
                    });

            usuario.IdUsuario = resultado.idNovo;
            usuario.Login = resultado.loginFinal;
        }

        // Consulta o nome de um perfil ativo.
        private static string ConsultarNomePerfil(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil)
        {
            const string sql = @"
                SELECT NomePerfil
                FROM dbo.Perfis
                WHERE IdPerfil = @IdPerfil
                  AND Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = idPerfil;

            string? nomePerfil =
                comando.ExecuteScalar() as string;

            if (nomePerfil == null)
            {
                throw new ArgumentException(
                    "O perfil selecionado não está disponível.");
            }

            return nomePerfil;
        }

        // Identifica os perfis com login automático.
        private static bool PossuiIdentificadorAutomatico(
            string nomePerfil)
        {
            return nomePerfil == "Aluno" ||
                   nomePerfil == "Professor" ||
                   nomePerfil == "Coordenador";
        }

        // Gera RA, RP ou RC com no mínimo três dígitos.
        private static string GerarIdentificador(
            string nomePerfil,
            int idUsuario)
        {
            return nomePerfil switch
            {
                "Aluno" => $"RA{idUsuario:D3}",
                "Professor" => $"RP{idUsuario:D3}",
                "Coordenador" => $"RC{idUsuario:D3}",

                _ => throw new InvalidOperationException(
                    "O perfil não utiliza identificador automático.")
            };
        }

        // Insere o usuário e retorna seu identificador.
        private static int InserirUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            Usuario usuario,
            string loginInicial)
        {
            const string sql = @"
                INSERT INTO dbo.Usuarios
                (
                    NomeCompleto,
                    Login,
                    SenhaHash,
                    IdPerfil,
                    Ativo
                )
                OUTPUT INSERTED.IdUsuario
                VALUES
                (
                    @Nome,
                    @Login,
                    @Hash,
                    @Perfil,
                    @Ativo
                )";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                150).Value = usuario.NomeCompleto;

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                100).Value = loginInicial;

            comando.Parameters.Add(
                "@Hash",
                SqlDbType.NVarChar,
                500).Value = usuario.SenhaHash;

            comando.Parameters.Add(
                "@Perfil",
                SqlDbType.Int).Value = usuario.IdPerfil;

            comando.Parameters.Add(
                "@Ativo",
                SqlDbType.Bit).Value = usuario.Ativo;

            return Convert.ToInt32(comando.ExecuteScalar());
        }

        // Substitui o login temporário pelo identificador definitivo.
        private static void AtualizarLogin(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string login)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET Login = @Login
                WHERE IdUsuario = @IdUsuario";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                100).Value = login;

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            int registrosAlterados =
                comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível definir o login do usuário.");
            }
        }

        // Cria o registro correspondente ao perfil acadêmico.
        private static void CriarRegistroAcademico(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string nomePerfil,
            string identificador)
        {
            if (nomePerfil == "Aluno")
            {
                InserirRegistroAcademico(
                    conexao,
                    transacao,
                    idUsuario,
                    identificador,
                    "Aluno");

                return;
            }

            if (nomePerfil == "Professor")
            {
                InserirRegistroAcademico(
                    conexao,
                    transacao,
                    idUsuario,
                    identificador,
                    "Professor");
            }

            // Coordenadores utilizam o RC no login.
        }

        // Insere o registro acadêmico do aluno ou professor.
        private static void InserirRegistroAcademico(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string identificador,
            string tipoRegistro)
        {
            string sql = tipoRegistro switch
            {
                "Aluno" => @"
                    INSERT INTO dbo.Alunos
                    (
                        IdUsuario,
                        MatriculaAluno
                    )
                    VALUES
                    (
                        @IdUsuario,
                        @Registro
                    )",

                "Professor" => @"
                    INSERT INTO dbo.Professores
                    (
                        IdUsuario,
                        RegistroProfessor
                    )
                    VALUES
                    (
                        @IdUsuario,
                        @Registro
                    )",

                _ => throw new ArgumentException(
                    "Tipo de registro acadêmico inválido.")
            };

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            comando.Parameters.Add(
                "@Registro",
                SqlDbType.NVarChar,
                30).Value = identificador;

            comando.ExecuteNonQuery();
        }

        // Verifica se determinado login já existe.
        public bool LoginExiste(string login)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios
                WHERE Login = @Login";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                100).Value = login;

            conexao.Open();

            return Convert.ToInt32(
                comando.ExecuteScalar()) > 0;
        }

        // Lista os perfis disponíveis para cadastro.
        public DataTable ListarPerfis()
        {
            const string sql = @"
                SELECT
                    IdPerfil,
                    NomePerfil
                FROM dbo.Perfis
                WHERE Ativo = 1
                ORDER BY NomePerfil";

            return ConsultarTabela(sql);
        }

        // Lista todos os usuários cadastrados.
        public DataTable ListarTodos()
        {
            const string sql = @"
                SELECT
                    u.IdUsuario,
                    u.NomeCompleto,
                    u.Login,
                    p.NomePerfil,
                    u.IdPerfil,
                    u.Ativo,
                    u.DataCadastro
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                ORDER BY u.NomeCompleto";

            return ConsultarTabela(sql);
        }

        // Executa consultas que retornam uma tabela.
        private static DataTable ConsultarTabela(string sql)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

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
            ExecutarTransacao(
                (conexao, transacao) =>
                {
                    ValidarAdministrador(
                        conexao,
                        transacao,
                        idAdministrador);

                    DadosUsuarioAnterior anterior =
                        ConsultarUsuarioParaEdicao(
                            conexao,
                            transacao,
                            idUsuario);

                    string novoPerfil = ConsultarNomePerfil(
                        conexao,
                        transacao,
                        idPerfil);

                    ValidarAlteracaoUsuario(
                        conexao,
                        transacao,
                        idAdministrador,
                        idUsuario,
                        idPerfil,
                        login,
                        novoPerfil,
                        anterior);

                    AtualizarUsuario(
                        conexao,
                        transacao,
                        idUsuario,
                        nome,
                        login,
                        idPerfil);

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        idAdministrador,
                        "EDICAO_USUARIO",
                        $"Dados do usuário ID {idUsuario} atualizados.");

                    return true;
                });
        }

        // Consulta os dados atuais do usuário antes da edição.
        private static DadosUsuarioAnterior ConsultarUsuarioParaEdicao(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                SELECT
                    u.IdPerfil,
                    u.Ativo,
                    p.NomePerfil,
                    u.Login
                FROM dbo.Usuarios u
                    WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.IdUsuario = @Id";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Id",
                SqlDbType.Int).Value = idUsuario;

            using SqlDataReader leitor =
                comando.ExecuteReader();

            if (!leitor.Read())
            {
                throw new InvalidOperationException(
                    "Usuário não encontrado.");
            }

            return new DadosUsuarioAnterior(
                leitor.GetInt32(0),
                leitor.GetBoolean(1),
                leitor.GetString(2),
                leitor.GetString(3));
        }

        // Valida as regras de alteração dos usuários.
        private static void ValidarAlteracaoUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador,
            int idUsuario,
            int idPerfil,
            string login,
            string novoPerfil,
            DadosUsuarioAnterior anterior)
        {
            if (idUsuario == idAdministrador &&
                anterior.IdPerfil != idPerfil)
            {
                throw new InvalidOperationException(
                    "Você não pode alterar seu próprio perfil.");
            }

            if (anterior.NomePerfil == "Administrador" &&
                novoPerfil != "Administrador" &&
                anterior.Ativo)
            {
                VerificarOutroAdministrador(
                    conexao,
                    transacao,
                    idUsuario);
            }

            if (anterior.IdPerfil != idPerfil)
            {
                throw new InvalidOperationException(
                    "A alteração de perfil ainda não está " +
                    "disponível para preservar os vínculos acadêmicos.");
            }

            if (novoPerfil != "Administrador" &&
                login != anterior.Login)
            {
                throw new InvalidOperationException(
                    "O identificador deste perfil é gerado " +
                    "automaticamente e não pode ser alterado.");
            }
        }

        // Executa a atualização dos dados cadastrais.
        private static void AtualizarUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string nome,
            string login,
            int idPerfil)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET
                    NomeCompleto = @Nome,
                    Login = @Login,
                    IdPerfil = @Perfil
                WHERE IdUsuario = @Id";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                150).Value = nome;

            comando.Parameters.Add(
                "@Login",
                SqlDbType.NVarChar,
                100).Value = login;

            comando.Parameters.Add(
                "@Perfil",
                SqlDbType.Int).Value = idPerfil;

            comando.Parameters.Add(
                "@Id",
                SqlDbType.Int).Value = idUsuario;

            int registrosAlterados =
                comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível atualizar o usuário.");
            }
        }

        // Ativa ou desativa uma conta de usuário.
        public void AlterarStatus(
            int idAdministrador,
            int idUsuario,
            bool novoStatus)
        {
            ExecutarTransacao(
                (conexao, transacao) =>
                {
                    ValidarAdministrador(
                        conexao,
                        transacao,
                        idAdministrador);

                    if (idAdministrador == idUsuario && !novoStatus)
                    {
                        throw new InvalidOperationException(
                            "Você não pode desativar sua própria conta.");
                    }

                    DadosStatusAnterior anterior =
                        ConsultarStatusUsuario(
                            conexao,
                            transacao,
                            idUsuario);

                    if (anterior.NomePerfil == "Administrador" &&
                        anterior.Ativo &&
                        !novoStatus)
                    {
                        VerificarOutroAdministrador(
                            conexao,
                            transacao,
                            idUsuario);
                    }

                    AtualizarStatusUsuario(
                        conexao,
                        transacao,
                        idUsuario,
                        novoStatus);

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        idAdministrador,
                        novoStatus
                            ? "ATIVACAO_USUARIO"
                            : "DESATIVACAO_USUARIO",
                        $"Conta do usuário ID {idUsuario} " +
                        (novoStatus ? "ativada." : "desativada."));

                    return true;
                });
        }

        // Consulta o estado atual da conta.
        private static DadosStatusAnterior ConsultarStatusUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                SELECT
                    u.Ativo,
                    p.NomePerfil
                FROM dbo.Usuarios u
                    WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                WHERE u.IdUsuario = @Id";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Id",
                SqlDbType.Int).Value = idUsuario;

            using SqlDataReader leitor =
                comando.ExecuteReader();

            if (!leitor.Read())
            {
                throw new InvalidOperationException(
                    "Usuário não encontrado.");
            }

            return new DadosStatusAnterior(
                leitor.GetBoolean(0),
                leitor.GetString(1));
        }

        // Atualiza o estado de ativação da conta.
        private static void AtualizarStatusUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            bool novoStatus)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET Ativo = @Ativo
                WHERE IdUsuario = @Id";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Ativo",
                SqlDbType.Bit).Value = novoStatus;

            comando.Parameters.Add(
                "@Id",
                SqlDbType.Int).Value = idUsuario;

            int registrosAlterados =
                comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível alterar o status do usuário.");
            }
        }

        // Executa uma operação dentro de uma transação SQL.
        private static T ExecutarTransacao<T>(
            Func<SqlConnection, SqlTransaction, T> operacao)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                T resultado = operacao(conexao, transacao);

                transacao.Commit();

                return resultado;
            }
            catch
            {
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Registra as operações administrativas.
        private static void RegistrarAuditoria(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAdministrador,
            string acao,
            string descricao)
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
                    @Usuario,
                    @Acao,
                    @Descricao
                )";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Usuario",
                SqlDbType.Int).Value = idAdministrador;

            comando.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                100).Value = acao;

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                500).Value = descricao;

            comando.ExecuteNonQuery();
        }

        // Confirma se o responsável continua sendo administrador ativo.
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
                "@Id",
                SqlDbType.Int).Value = idAdministrador;

            bool autorizado =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!autorizado)
            {
                throw new UnauthorizedAccessException(
                    "Sua conta não possui mais permissão " +
                    "de Administrador.");
            }
        }

        // Impede a remoção do último administrador ativo.
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
                "@Id",
                SqlDbType.Int).Value = idUsuario;

            bool existeOutroAdministrador =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!existeOutroAdministrador)
            {
                throw new InvalidOperationException(
                    "O sistema precisa manter pelo menos " +
                    "um Administrador ativo.");
            }
        }

        // Armazena os dados necessários para validar uma edição.
        private sealed class DadosUsuarioAnterior
        {
            public int IdPerfil { get; }
            public bool Ativo { get; }
            public string NomePerfil { get; }
            public string Login { get; }

            public DadosUsuarioAnterior(
                int idPerfil,
                bool ativo,
                string nomePerfil,
                string login)
            {
                IdPerfil = idPerfil;
                Ativo = ativo;
                NomePerfil = nomePerfil;
                Login = login;
            }
        }

        // Armazena os dados necessários para alterar o status.
        private sealed class DadosStatusAnterior
        {
            public bool Ativo { get; }
            public string NomePerfil { get; }

            public DadosStatusAnterior(
                bool ativo,
                string nomePerfil)
            {
                Ativo = ativo;
                NomePerfil = nomePerfil;
            }
        }
    }
}