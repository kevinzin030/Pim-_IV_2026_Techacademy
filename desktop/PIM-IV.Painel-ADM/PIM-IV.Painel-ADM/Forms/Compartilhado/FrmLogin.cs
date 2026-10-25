using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM
{
    public partial class FrmLogin : Form
    {
        private const string NomeAdministradorInicial = "Administrador";
        private const string LoginAdministradorInicial = "administrador";

        private const string AcaoAuditoriaTrocaSenha =
            "ALTERACAO_SENHA_PRIMEIRO_ACESSO";

        private readonly AutenticacaoService _autenticacaoService;
        private readonly UsuarioService _usuarioService;

        // Inicializa os serviços e configura os eventos do formulário.
        public FrmLogin()
        {
            InitializeComponent();

            _autenticacaoService = new AutenticacaoService();
            _usuarioService = new UsuarioService();

            ConfigurarFormulario();
            ConfigurarEventos();
        }

        // Define o comportamento inicial dos campos e botões.
        private void ConfigurarFormulario()
        {
            txtSenha.UseSystemPasswordChar = true;

            AcceptButton = btnEntrar;
        }

        // Evita inscrições duplicadas nos eventos do formulário.
        private void ConfigurarEventos()
        {
            btnEntrar.Click -= btnEntrar_Click;
            btnEntrar.Click += btnEntrar_Click;

            btnCadastroInicial.Click -= btnCadastroInicial_Click;
            btnCadastroInicial.Click += btnCadastroInicial_Click;

            Shown -= FrmLogin_Shown;
            Shown += FrmLogin_Shown;
        }

        // Verifica se o sistema ainda precisa cadastrar o administrador.
        private void FrmLogin_Shown(object? sender, EventArgs e)
        {
            AtualizarDisponibilidadeCadastroInicial();
        }

        // Exibe o cadastro inicial somente quando não existe administrador.
        private void AtualizarDisponibilidadeCadastroInicial()
        {
            btnCadastroInicial.Visible = false;

            try
            {
                btnCadastroInicial.Visible =
                    !_usuarioService.ExisteAdministrador();
            }
            catch (Exception)
            {
                ExibirMensagem(
                    "Não foi possível consultar o banco de dados.",
                    "Erro de conexão",
                    MessageBoxIcon.Error);
            }
        }

        // Inicia o cadastro do primeiro administrador do sistema.
        private void btnCadastroInicial_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_usuarioService.ExisteAdministrador())
                {
                    btnCadastroInicial.Visible = false;

                    ExibirMensagem(
                        "O administrador já está cadastrado.",
                        "Configuração concluída",
                        MessageBoxIcon.Information);

                    return;
                }

                string? senhaInicial = SolicitarSenhaAdministrador();

                if (senhaInicial == null)
                {
                    return;
                }

                CadastrarAdministradorInicial(senhaInicial);
            }
            catch (ArgumentException ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Dados inválidos",
                    MessageBoxIcon.Warning);
            }
            catch (InvalidOperationException ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Cadastro não permitido",
                    MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                ExibirMensagem(
                    "Não foi possível concluir o cadastro inicial.",
                    "Erro no cadastro",
                    MessageBoxIcon.Error);
            }
        }

        // Encaminha o cadastro para o serviço responsável.
        private void CadastrarAdministradorInicial(string senha)
        {
            _usuarioService.CadastrarAdministradorInicial(
                NomeAdministradorInicial,
                LoginAdministradorInicial,
                senha);

            btnCadastroInicial.Visible = false;

            txtUsuario.Text = LoginAdministradorInicial;
            txtSenha.Clear();
            txtSenha.Focus();

            ExibirMensagem(
                "Administrador cadastrado com sucesso! Realize o login.",
                "Cadastro concluído",
                MessageBoxIcon.Information);
        }

        // Solicita e confirma a senha do administrador inicial.
        private string? SolicitarSenhaAdministrador()
        {
            using Form janela = new()
            {
                Text = "Configuração do Administrador",
                Width = 400,
                Height = 230,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblSenha = CriarRotulo(
                "Senha (mínimo de 8 caracteres):",
                20,
                20,
                340);

            TextBox campoSenha = CriarCampoSenha(
                20,
                45,
                340);

            Label lblConfirmar = CriarRotulo(
                "Confirmar senha:",
                20,
                80,
                340);

            TextBox campoConfirmar = CriarCampoSenha(
                20,
                105,
                340);

            Button btnConfirmar = new()
            {
                Text = "Confirmar",
                Left = 245,
                Top = 140,
                Width = 115,
                DialogResult = DialogResult.OK
            };

            janela.Controls.AddRange(new Control[]
            {
                lblSenha,
                campoSenha,
                lblConfirmar,
                campoConfirmar,
                btnConfirmar
            });

            janela.AcceptButton = btnConfirmar;

            if (janela.ShowDialog(this) != DialogResult.OK)
            {
                return null;
            }

            if (campoSenha.Text != campoConfirmar.Text)
            {
                throw new ArgumentException(
                    "As senhas informadas não coincidem.");
            }

            return campoSenha.Text;
        }

        // Executa a autenticação das credenciais informadas.
        private void btnEntrar_Click(object? sender, EventArgs e)
        {
            string login = txtUsuario.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(senha))
            {
                ExibirMensagem(
                    "Informe o usuário e a senha.",
                    "Campos obrigatórios",
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                Usuario? usuario =
                    _autenticacaoService.Autenticar(login, senha);

                if (usuario == null)
                {
                    TratarCredenciaisInvalidas();
                    return;
                }

                if (!VerificarTrocaObrigatoriaSenha(usuario, senha))
                {
                    return;
                }

                AbrirPainelPrincipal(usuario);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Não foi possível realizar o login.\n\n" +
                    ex.Message,
                    "Erro de autenticação",
                    MessageBoxIcon.Error);
            }
        }

        // Informa a falha de autenticação sem revelar dados da conta.
        private void TratarCredenciaisInvalidas()
        {
            ExibirMensagem(
                "Usuário ou senha incorretos. " +
                "Verifique suas credenciais e tente novamente.",
                "Falha na autenticação",
                MessageBoxIcon.Warning);

            txtSenha.Clear();
            txtSenha.Focus();
        }

        // Impede o acesso enquanto a senha provisória não for alterada.
        private bool VerificarTrocaObrigatoriaSenha(
            Usuario usuario,
            string senhaInicial)
        {
            if (!PrecisaAlterarSenha(usuario.IdUsuario))
            {
                return true;
            }

            bool senhaAlterada =
                ExibirTrocaObrigatoriaSenha(usuario, senhaInicial);

            txtSenha.Clear();

            if (!senhaAlterada)
            {
                ExibirMensagem(
                    "É necessário alterar a senha inicial " +
                    "para acessar o sistema.",
                    "Primeiro acesso",
                    MessageBoxIcon.Warning);

                txtSenha.Focus();

                return false;
            }

            ExibirMensagem(
                "Senha alterada com sucesso! " +
                "Você já pode acessar o sistema.",
                "Primeiro acesso concluído",
                MessageBoxIcon.Information);

            return true;
        }

        // Abre o painel e restaura a tela de login ao encerrá-lo.
        private void AbrirPainelPrincipal(Usuario usuario)
        {
            txtSenha.Clear();

            using FrmPainelPrincipal painel = new(usuario);

            Hide();

            try
            {
                painel.ShowDialog(this);
            }
            finally
            {
                Show();

                txtSenha.Clear();
                txtUsuario.Focus();
            }
        }

        // Consulta no banco se o usuário deve trocar a senha.
        private static bool PrecisaAlterarSenha(int idUsuario)
        {
            const string sql = @"
                SELECT DeveAlterarSenha
                FROM dbo.Usuarios
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1";

            using SqlConnection conexao = Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            conexao.Open();

            object? resultado = comando.ExecuteScalar();

            if (resultado == null || resultado == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Usuário não encontrado ou desativado.");
            }

            return Convert.ToBoolean(resultado);
        }

        // Apresenta o formulário de alteração obrigatória de senha.
        private bool ExibirTrocaObrigatoriaSenha(
            Usuario usuario,
            string senhaInicial)
        {
            using Form janela = new()
            {
                Text = "Primeiro acesso - Alteração de senha",
                Width = 460,
                Height = 350,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTitulo = new()
            {
                Text = "ALTERAÇÃO OBRIGATÓRIA DE SENHA",
                Left = 20,
                Top = 20,
                Width = 405,
                Height = 25,
                Font = new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Bold)
            };

            Label lblDescricao = new()
            {
                Text =
                    "Este é seu primeiro acesso. " +
                    "Crie uma nova senha para continuar.",

                Left = 20,
                Top = 52,
                Width = 405,
                Height = 38
            };

            Label lblNovaSenha = CriarRotulo(
                "Nova senha:",
                20,
                100,
                400);

            TextBox txtNovaSenha = CriarCampoSenha(
                20,
                125,
                405);

            Label lblConfirmarSenha = CriarRotulo(
                "Confirmar nova senha:",
                20,
                165,
                400);

            TextBox txtConfirmarSenha = CriarCampoSenha(
                20,
                190,
                405);

            Label lblRequisitos = new()
            {
                Text =
                    "Mínimo de 8 caracteres, com maiúscula, " +
                    "minúscula, número e especial.",

                Left = 20,
                Top = 225,
                Width = 405,
                Height = 35,
                ForeColor = Color.DimGray
            };

            Button btnSalvar = new()
            {
                Text = "Alterar senha",
                Left = 285,
                Top = 265,
                Width = 140,
                Height = 32
            };

            janela.Controls.AddRange(new Control[]
            {
                lblTitulo,
                lblDescricao,
                lblNovaSenha,
                txtNovaSenha,
                lblConfirmarSenha,
                txtConfirmarSenha,
                lblRequisitos,
                btnSalvar
            });

            janela.AcceptButton = btnSalvar;

            bool senhaAlterada = false;

            // Mantém a janela aberta caso a validação falhe.
            btnSalvar.Click += (_, _) =>
            {
                senhaAlterada = ProcessarTrocaObrigatoriaSenha(
                    janela,
                    btnSalvar,
                    usuario,
                    senhaInicial,
                    txtNovaSenha,
                    txtConfirmarSenha);

                if (senhaAlterada)
                {
                    janela.DialogResult = DialogResult.OK;
                    janela.Close();
                }
            };

            janela.ShowDialog(this);

            return senhaAlterada;
        }

        // Valida a nova senha antes de solicitar a atualização.
        private static bool ProcessarTrocaObrigatoriaSenha(
            Form janela,
            Button btnSalvar,
            Usuario usuario,
            string senhaInicial,
            TextBox txtNovaSenha,
            TextBox txtConfirmarSenha)
        {
            try
            {
                string novaSenha = txtNovaSenha.Text;
                string confirmacao = txtConfirmarSenha.Text;

                if (novaSenha != confirmacao)
                {
                    throw new ArgumentException(
                        "As novas senhas não coincidem.");
                }

                // Reutiliza a regra de complexidade do serviço.
                UsuarioService.ValidarSenha(novaSenha);

                if (novaSenha == senhaInicial)
                {
                    throw new ArgumentException(
                        "A nova senha deve ser diferente da senha inicial.");
                }

                btnSalvar.Enabled = false;

                SalvarNovaSenha(
                    usuario.IdUsuario,
                    senhaInicial,
                    novaSenha);

                return true;
            }
            catch (ArgumentException ex)
            {
                ExibirMensagem(
                    janela,
                    ex.Message,
                    "Senha inválida",
                    MessageBoxIcon.Warning);

                return false;
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    janela,
                    "Não foi possível alterar a senha.\n\n" +
                    ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);

                return false;
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }

        // Atualiza o hash da senha e registra a operação em uma transação.
        private static void SalvarNovaSenha(
            int idUsuario,
            string senhaInicial,
            string novaSenha)
        {
            string novoHash = SenhaHelper.GerarHash(novaSenha);

            using SqlConnection conexao = Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction();

            try
            {
                string hashAtual = ConsultarHashSenhaInicial(
                    conexao,
                    transacao,
                    idUsuario);

                // Confirma que a senha autenticada ainda é válida.
                if (!SenhaHelper.VerificarSenha(
                    senhaInicial,
                    hashAtual))
                {
                    throw new InvalidOperationException(
                        "A senha inicial não é mais válida. " +
                        "Faça o login novamente.");
                }

                AtualizarSenhaUsuario(
                    conexao,
                    transacao,
                    idUsuario,
                    novoHash);

                RegistrarAuditoriaTrocaSenha(
                    conexao,
                    transacao,
                    idUsuario);

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        // Bloqueia o registro durante a validação da senha inicial.
        private static string ConsultarHashSenhaInicial(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                SELECT SenhaHash
                FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1
                  AND DeveAlterarSenha = 1";

            using SqlCommand comando = new(
                sql,
                conexao,
                transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            string? hashAtual =
                comando.ExecuteScalar() as string;

            if (hashAtual == null)
            {
                throw new InvalidOperationException(
                    "A senha inicial não é mais válida. " +
                    "Faça o login novamente.");
            }

            return hashAtual;
        }

        // Grava o novo hash e libera o acesso sem senha provisória.
        private static void AtualizarSenhaUsuario(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario,
            string novoHash)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET SenhaHash = @SenhaHash,
                    DeveAlterarSenha = 0
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1
                  AND DeveAlterarSenha = 1";

            using SqlCommand comando = new(
                sql,
                conexao,
                transacao);

            comando.Parameters.Add(
                "@SenhaHash",
                SqlDbType.NVarChar,
                500).Value = novoHash;

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            int linhasAfetadas = comando.ExecuteNonQuery();

            if (linhasAfetadas != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível atualizar a senha.");
            }
        }

        // Registra a troca sem armazenar senhas ou hashes no histórico.
        private static void RegistrarAuditoriaTrocaSenha(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idUsuario)
        {
            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                    (IdUsuario, Acao, Descricao)
                VALUES
                    (@IdUsuario, @Acao, @Descricao)";

            using SqlCommand comando = new(
                sql,
                conexao,
                transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = idUsuario;

            comando.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                100).Value = AcaoAuditoriaTrocaSenha;

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                500).Value =
                    "Usuário alterou a senha inicial no primeiro acesso.";

            comando.ExecuteNonQuery();
        }

        // Cria rótulos padronizados para as janelas de senha.
        private static Label CriarRotulo(
            string texto,
            int posicaoX,
            int posicaoY,
            int largura)
        {
            return new Label
            {
                Text = texto,
                Left = posicaoX,
                Top = posicaoY,
                Width = largura
            };
        }

        // Cria campos protegidos para entrada de senhas.
        private static TextBox CriarCampoSenha(
            int posicaoX,
            int posicaoY,
            int largura)
        {
            return new TextBox
            {
                Left = posicaoX,
                Top = posicaoY,
                Width = largura,
                UseSystemPasswordChar = true,
                MaxLength = 128
            };
        }

        // Centraliza as mensagens apresentadas pelo formulário.
        private static void ExibirMensagem(
            string mensagem,
            string titulo,
            MessageBoxIcon icone)
        {
            MessageBox.Show(
                mensagem,
                titulo,
                MessageBoxButtons.OK,
                icone);
        }

        // Exibe mensagens vinculadas a uma janela secundária.
        private static void ExibirMensagem(
            Form janela,
            string mensagem,
            string titulo,
            MessageBoxIcon icone)
        {
            MessageBox.Show(
                janela,
                mensagem,
                titulo,
                MessageBoxButtons.OK,
                icone);
        }
    }
}