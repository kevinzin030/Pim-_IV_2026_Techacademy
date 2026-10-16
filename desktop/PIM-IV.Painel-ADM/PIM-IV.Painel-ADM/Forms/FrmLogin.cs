
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
        private readonly AutenticacaoService _autenticacaoService;
        private readonly UsuarioService _usuarioService;

        public FrmLogin()
        {
            InitializeComponent();

            _autenticacaoService = new AutenticacaoService();
            _usuarioService = new UsuarioService();

            txtSenha.UseSystemPasswordChar = true;
            AcceptButton = btnEntrar;

            btnEntrar.Click -= btnEntrar_Click;
            btnEntrar.Click += btnEntrar_Click;

            btnCadastroInicial.Click -= btnCadastroInicial_Click;
            btnCadastroInicial.Click += btnCadastroInicial_Click;

            Shown -= FrmLogin_Shown;
            Shown += FrmLogin_Shown;
        }

        private void FrmLogin_Shown(object? sender, EventArgs e)
        {
            btnCadastroInicial.Visible = false;

            try
            {
                btnCadastroInicial.Visible =
                    !_usuarioService.ExisteAdministrador();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível consultar o banco de dados.",
                    "Erro de conexão",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnCadastroInicial_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_usuarioService.ExisteAdministrador())
                {
                    btnCadastroInicial.Visible = false;

                    MessageBox.Show(
                        "O administrador já está cadastrado.",
                        "Configuração concluída",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return;
                }

                const string nome = "Administrador";
                const string login = "administrador";

                string? senha = SolicitarSenha();

                if (senha == null)
                    return;

                _usuarioService.CadastrarAdministradorInicial(
                    nome,
                    login,
                    senha
                );

                btnCadastroInicial.Visible = false;

                txtUsuario.Text = login;
                txtSenha.Clear();
                txtSenha.Focus();

                MessageBox.Show(
                    "Administrador cadastrado com sucesso! Realize o login.",
                    "Cadastro concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Dados inválidos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Cadastro não permitido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível concluir o cadastro inicial.",
                    "Erro no cadastro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private string? SolicitarSenha()
        {
            using Form janela = new Form
            {
                Text = "Configuração do Administrador",
                Width = 400,
                Height = 230,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblSenha = new Label
            {
                Text = "Senha (mínimo de 8 caracteres):",
                Left = 20,
                Top = 20,
                Width = 340
            };

            TextBox campoSenha = new TextBox
            {
                Left = 20,
                Top = 45,
                Width = 340,
                UseSystemPasswordChar = true
            };

            Label lblConfirmar = new Label
            {
                Text = "Confirmar senha:",
                Left = 20,
                Top = 80,
                Width = 340
            };

            TextBox campoConfirmar = new TextBox
            {
                Left = 20,
                Top = 105,
                Width = 340,
                UseSystemPasswordChar = true
            };

            Button btnConfirmar = new Button
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
                return null;

            if (campoSenha.Text != campoConfirmar.Text)
            {
                throw new ArgumentException(
                    "As senhas informadas não coincidem.");
            }

            return campoSenha.Text;
        }

        private void btnEntrar_Click(object? sender, EventArgs e)
        {
            string login = txtUsuario.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show(
                    "Informe o usuário e a senha.",
                    "Campos obrigatórios",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                Usuario? usuario =
                    _autenticacaoService.Autenticar(login, senha);

                if (usuario == null)
                {
                    MessageBox.Show(
                        "Usuário ou senha incorretos. Verifique suas credenciais e tente novamente.",
                        "Falha na autenticação",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    txtSenha.Clear();
                    txtSenha.Focus();

                    return;
                }

                // Verifica se o usuario precisa trocar
                // a senha antes de acessar o sistema.
                if (PrecisaAlterarSenha(usuario.IdUsuario))
                {
                    bool senhaAlterada =
                        ExibirTrocaObrigatoriaSenha(usuario, senha);

                    txtSenha.Clear();

                    if (!senhaAlterada)
                    {
                        MessageBox.Show(
                            "É necessário alterar a senha inicial para acessar o sistema.",
                            "Primeiro acesso",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtSenha.Focus();
                        return;
                    }

                    MessageBox.Show(
                        "Senha alterada com sucesso! Você já pode acessar o sistema.",
                        "Primeiro acesso concluído",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                txtSenha.Clear();

                using FrmPainelPrincipal painel =
                    new FrmPainelPrincipal(usuario);

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
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível realizar o login.\n\n" +
                    ex.Message,
                    "Erro de autenticação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // Consulta o indicador de primeiro acesso.
        private bool PrecisaAlterarSenha(int idUsuario)
        {
            const string sql = @"
                SELECT DeveAlterarSenha
                FROM dbo.Usuarios
                WHERE IdUsuario = @IdUsuario
                  AND Ativo = 1";

            using SqlConnection conexao = Conexao.CriarConexao();
            using SqlCommand comando = new SqlCommand(sql, conexao);

            comando.Parameters.Add("@IdUsuario", SqlDbType.Int)
                .Value = idUsuario;

            conexao.Open();

            object? resultado = comando.ExecuteScalar();

            if (resultado == null || resultado == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Usuário não encontrado ou desativado.");
            }

            return Convert.ToBoolean(resultado);
        }

        // Janela de alteracao obrigatoria da senha.
        private bool ExibirTrocaObrigatoriaSenha(
            Usuario usuario,
            string senhaInicial)
        {
            using Form janela = new Form
            {
                Text = "Primeiro acesso - Alteração de senha",
                Width = 460,
                Height = 350,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTitulo = new Label
            {
                Text = "ALTERAÇÃO OBRIGATÓRIA DE SENHA",
                Left = 20,
                Top = 20,
                Width = 405,
                Height = 25,
                Font = new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Bold
                )
            };

            Label lblDescricao = new Label
            {
                Text = "Este é seu primeiro acesso. Crie uma nova senha para continuar.",
                Left = 20,
                Top = 52,
                Width = 405,
                Height = 38
            };

            Label lblNovaSenha = new Label
            {
                Text = "Nova senha:",
                Left = 20,
                Top = 100,
                Width = 400
            };

            TextBox txtNovaSenha = new TextBox
            {
                Left = 20,
                Top = 125,
                Width = 405,
                UseSystemPasswordChar = true
            };

            Label lblConfirmarSenha = new Label
            {
                Text = "Confirmar nova senha:",
                Left = 20,
                Top = 165,
                Width = 400
            };

            TextBox txtConfirmarSenha = new TextBox
            {
                Left = 20,
                Top = 190,
                Width = 405,
                UseSystemPasswordChar = true
            };

            Label lblRequisitos = new Label
            {
                Text = "Mínimo de 8 caracteres, com maiúscula, minúscula, número e especial.",
                Left = 20,
                Top = 225,
                Width = 405,
                Height = 35,
                ForeColor = Color.DimGray
            };

            Button btnSalvar = new Button
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

            bool alterada = false;

            btnSalvar.Click += (sender, e) =>
            {
                try
                {
                    string novaSenha = txtNovaSenha.Text;
                    string confirmarSenha = txtConfirmarSenha.Text;

                    if (novaSenha != confirmarSenha)
                    {
                        throw new ArgumentException(
                            "As novas senhas não coincidem.");
                    }

                    UsuarioService.ValidarSenha(novaSenha);

                    if (novaSenha == senhaInicial)
                    {
                        throw new ArgumentException(
                            "A nova senha deve ser diferente da senha inicial.");
                    }

                    SalvarNovaSenha(
                        usuario.IdUsuario,
                        senhaInicial,
                        novaSenha
                    );

                    alterada = true;

                    janela.DialogResult = DialogResult.OK;
                    janela.Close();
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(
                        janela,
                        ex.Message,
                        "Senha inválida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        janela,
                        "Não foi possível alterar a senha.\n\n" +
                        ex.Message,
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };

            janela.ShowDialog(this);

            return alterada;
        }

        // Grava o novo hash e libera o primeiro acesso.
        private void SalvarNovaSenha(
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
                // Bloqueia a linha durante a alteracao
                // e confirma que a senha inicial continua valida.
                const string sqlConsultar = @"
                    SELECT SenhaHash
                    FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdUsuario = @IdUsuario
                      AND Ativo = 1
                      AND DeveAlterarSenha = 1";

                using SqlCommand consultar = new SqlCommand(
                    sqlConsultar,
                    conexao,
                    transacao
                );

                consultar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int
                ).Value = idUsuario;

                string? hashAtual =
                    consultar.ExecuteScalar() as string;

                if (hashAtual == null ||
                    !SenhaHelper.VerificarSenha(
                        senhaInicial,
                        hashAtual))
                {
                    throw new InvalidOperationException(
                        "A senha inicial não é mais válida. Faça o login novamente.");
                }

                const string sqlAtualizar = @"
                    UPDATE dbo.Usuarios
                    SET SenhaHash = @SenhaHash,
                        DeveAlterarSenha = 0
                    WHERE IdUsuario = @IdUsuario
                      AND Ativo = 1
                      AND DeveAlterarSenha = 1";

                using SqlCommand atualizar = new SqlCommand(
                    sqlAtualizar,
                    conexao,
                    transacao
                );

                atualizar.Parameters.Add(
                    "@SenhaHash",
                    SqlDbType.NVarChar,
                    500
                ).Value = novoHash;

                atualizar.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int
                ).Value = idUsuario;

                int linhas = atualizar.ExecuteNonQuery();

                if (linhas != 1)
                {
                    throw new InvalidOperationException(
                        "Não foi possível atualizar a senha.");
                }

                // Registra a troca sem armazenar
                // a senha antiga ou a nova no log.
                const string sqlAuditoria = @"
                    INSERT INTO dbo.LogsAtividades
                        (IdUsuario, Acao, Descricao)
                    VALUES
                        (@IdUsuario, @Acao, @Descricao)";

                using SqlCommand auditoria = new SqlCommand(
                    sqlAuditoria,
                    conexao,
                    transacao
                );

                auditoria.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int
                ).Value = idUsuario;

                auditoria.Parameters.Add(
                    "@Acao",
                    SqlDbType.NVarChar,
                    100
                ).Value = "ALTERACAO_SENHA_PRIMEIRO_ACESSO";

                auditoria.Parameters.Add(
                    "@Descricao",
                    SqlDbType.NVarChar,
                    500
                ).Value =
                    "Usuário alterou a senha inicial no primeiro acesso.";

                auditoria.ExecuteNonQuery();

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
