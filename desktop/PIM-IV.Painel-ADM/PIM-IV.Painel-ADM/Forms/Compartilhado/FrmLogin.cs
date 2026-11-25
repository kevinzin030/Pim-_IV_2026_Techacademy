using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Seguranca;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM
{
    /// <summary>
    /// Gerencia a autenticação e o primeiro acesso
    /// ao painel administrativo do TechAcademy.
    /// </summary>
    public partial class FrmLogin : Form
    {
        private const string NomeAdministradorInicial = "Administrador";
        private const string LoginAdministradorInicial = "administrador";

        private readonly AutenticacaoService _autenticacaoService;
        private readonly UsuarioService _usuarioService;

        // Inicializa os serviços e configura o formulário.
        public FrmLogin()
        {
            InitializeComponent();

            _autenticacaoService = new AutenticacaoService();
            _usuarioService = new UsuarioService();

            ConfigurarFormulario();
            ConfigurarEventos();
        }

        // Define o comportamento inicial dos componentes.
        private void ConfigurarFormulario()
        {
            txtSenha.UseSystemPasswordChar = true;
            AcceptButton = btnEntrar;
        }

        // Configura os eventos sem permitir inscrições duplicadas.
        private void ConfigurarEventos()
        {
            btnEntrar.Click -= btnEntrar_Click;
            btnEntrar.Click += btnEntrar_Click;

            btnCadastroInicial.Click -= btnCadastroInicial_Click;
            btnCadastroInicial.Click += btnCadastroInicial_Click;

            Shown -= FrmLogin_Shown;
            Shown += FrmLogin_Shown;
        }

        // Verifica a disponibilidade do cadastro inicial.
        private void FrmLogin_Shown(object? sender, EventArgs e)
        {
            AtualizarDisponibilidadeCadastroInicial();
        }

        // Mantém temporariamente a consulta pelo serviço existente.
        // Esta operação será migrada posteriormente para a API.
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
                // Uma falha no cadastro inicial não deve impedir
                // a tentativa de autenticação através da API.
                btnCadastroInicial.Visible = false;
            }
        }

        // Inicia o cadastro do primeiro administrador.
        private void btnCadastroInicial_Click(
            object? sender,
            EventArgs e)
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

                string? senhaInicial =
                    SolicitarSenhaAdministrador();

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
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Não foi possível concluir o cadastro inicial.\n\n" +
                    ex.Message,
                    "Erro no cadastro",
                    MessageBoxIcon.Error);
            }
        }

        // Encaminha o cadastro inicial ao serviço responsável.
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

        // Solicita a senha do administrador inicial.
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
                20, 20, 340);

            TextBox campoSenha = CriarCampoSenha(
                20, 45, 340);

            Label lblConfirmar = CriarRotulo(
                "Confirmar senha:",
                20, 80, 340);

            TextBox campoConfirmar = CriarCampoSenha(
                20, 105, 340);

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

        /// <summary>
        /// Autentica o usuário através da API sem bloquear
        /// a interface durante a requisição HTTP.
        /// </summary>
        private async void btnEntrar_Click(
            object? sender,
            EventArgs e)
        {
            string login = txtUsuario.Text.Trim();
            string senha = txtSenha.Text;

            // Valida os campos obrigatórios.
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
                // Impede solicitações simultâneas de autenticação.
                btnEntrar.Enabled = false;
                btnCadastroInicial.Enabled = false;
                txtUsuario.Enabled = false;
                txtSenha.Enabled = false;

                // Encaminha as credenciais para a API REST.
                Usuario? usuario =
                    await _autenticacaoService.AutenticarAsync(
                        login,
                        senha);

                if (usuario == null)
                {
                    TratarCredenciaisInvalidas();
                    return;
                }

                // Utiliza a informação retornada pela API
                // para verificar a alteração obrigatória de senha.
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
            finally
            {
                // Encerra a sessão após falhas ou saída do painel.
                SessaoUsuario.Encerrar();

                // Restaura os componentes do formulário.
                btnEntrar.Enabled = true;
                btnCadastroInicial.Enabled = true;
                txtUsuario.Enabled = true;
                txtSenha.Enabled = true;

                txtSenha.Clear();
                txtSenha.Focus();
            }
        }

        // Informa que as credenciais não foram reconhecidas.
        private void TratarCredenciaisInvalidas()
        {
            ExibirMensagem(
                "Usuário ou senha incorretos. " +
                "Verifique suas credenciais e tente novamente.",
                "Falha na autenticação",
                MessageBoxIcon.Warning);
        }

        // Impede o acesso enquanto a senha provisória estiver ativa.
        private bool VerificarTrocaObrigatoriaSenha(
            Usuario usuario,
            string senhaInicial)
        {
            // A informação agora é recebida diretamente da API.
            if (!usuario.DeveAlterarSenha)
            {
                return true;
            }

            bool senhaAlterada =
                ExibirTrocaObrigatoriaSenha(
                    usuario,
                    senhaInicial);

            txtSenha.Clear();

            if (!senhaAlterada)
            {
                ExibirMensagem(
                    "É necessário alterar a senha inicial " +
                    "para acessar o sistema.",
                    "Primeiro acesso",
                    MessageBoxIcon.Warning);

                return false;
            }

            ExibirMensagem(
                "Senha alterada com sucesso! " +
                "Você já pode acessar o sistema.",
                "Primeiro acesso concluído",
                MessageBoxIcon.Information);

            return true;
        }

        // Abre o painel administrativo durante a sessão autenticada.
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
                // Remove o token JWT ao encerrar o painel.
                SessaoUsuario.Encerrar();

                Show();

                txtSenha.Clear();
                txtUsuario.Focus();
            }
        }

        // Apresenta a janela de alteração obrigatória de senha.
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
                20, 100, 400);

            TextBox txtNovaSenha = CriarCampoSenha(
                20, 125, 405);

            Label lblConfirmarSenha = CriarRotulo(
                "Confirmar nova senha:",
                20, 165, 400);

            TextBox txtConfirmarSenha = CriarCampoSenha(
                20, 190, 405);

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
            bool alteracaoEmAndamento = false;

            // Impede o fechamento durante uma requisição.
            janela.FormClosing += (_, evento) =>
            {
                if (alteracaoEmAndamento)
                {
                    evento.Cancel = true;
                }
            };

            // Solicita a alteração de senha através da API.
            btnSalvar.Click += async (_, _) =>
            {
                alteracaoEmAndamento = true;

                try
                {
                    senhaAlterada =
                        await ProcessarTrocaObrigatoriaSenhaAsync(
                            janela,
                            btnSalvar,
                            senhaInicial,
                            txtNovaSenha,
                            txtConfirmarSenha);
                }
                finally
                {
                    alteracaoEmAndamento = false;
                }

                if (senhaAlterada)
                {
                    usuario.DeveAlterarSenha = false;

                    janela.DialogResult = DialogResult.OK;
                    janela.Close();
                }
            };

            janela.ShowDialog(this);

            return senhaAlterada;
        }

        /// <summary>
        /// Valida a nova senha e solicita sua atualização
        /// através do serviço de autenticação.
        /// </summary>
        private async Task<bool> ProcessarTrocaObrigatoriaSenhaAsync(
            Form janela,
            Button btnSalvar,
            string senhaInicial,
            TextBox txtNovaSenha,
            TextBox txtConfirmarSenha)
        {
            try
            {
                string novaSenha = txtNovaSenha.Text;
                string confirmacao = txtConfirmarSenha.Text;

                // Verifica se as senhas informadas coincidem.
                if (novaSenha != confirmacao)
                {
                    throw new ArgumentException(
                        "As novas senhas não coincidem.");
                }

                // Reutiliza a regra de complexidade existente.
                UsuarioService.ValidarSenha(novaSenha);

                // Impede a reutilização da senha provisória.
                if (novaSenha == senhaInicial)
                {
                    throw new ArgumentException(
                        "A nova senha deve ser diferente da senha inicial.");
                }

                btnSalvar.Enabled = false;
                txtNovaSenha.Enabled = false;
                txtConfirmarSenha.Enabled = false;

                // A API valida a senha atual, gera o novo hash
                // e atualiza os dados no Azure SQL.
                await _autenticacaoService.AlterarSenhaAsync(
                    senhaInicial,
                    novaSenha,
                    confirmacao);

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
                txtNovaSenha.Enabled = true;
                txtConfirmarSenha.Enabled = true;
            }
        }

        // Cria rótulos padronizados para as janelas auxiliares.
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

        // Centraliza as mensagens do formulário principal.
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

        // Centraliza as mensagens das janelas auxiliares.
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