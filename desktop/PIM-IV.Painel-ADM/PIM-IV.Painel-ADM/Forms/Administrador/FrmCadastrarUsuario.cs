using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia o cadastro de novos usuários do sistema.
    public partial class FrmCadastrarUsuario : Form
    {
        private const int PerfilAdministrador = 1;

        private readonly Usuario _administrador;
        private readonly UsuarioService _usuarioService = new();
        private readonly UsuarioRepository _usuarioRepository = new();

        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);
        private readonly Color _corTitulo = Color.FromArgb(24, 39, 65);
        private readonly Color _corBotao = Color.FromArgb(40, 92, 170);

        private TextBox txtNome = null!;
        private TextBox txtLogin = null!;
        private TextBox txtSenha = null!;
        private TextBox txtConfirmarSenha = null!;

        private ComboBox cmbPerfil = null!;

        private Button btnSalvar = null!;

        private Label lblLogin = null!;
        private Label lblOrientacao = null!;

        // Inicializa o formulário e valida o administrador autenticado.
        public FrmCadastrarUsuario(Usuario administrador)
        {
            InitializeComponent();

            _administrador = administrador
                ?? throw new ArgumentNullException(nameof(administrador));

            ValidarAdministrador();
            ConfigurarFormulario();
            CriarLayout();
            CarregarPerfis();
        }

        // Restringe o cadastro aos administradores ativos.
        private void ValidarAdministrador()
        {
            if (_administrador.IdPerfil != PerfilAdministrador ||
                !_administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Acesso permitido somente ao Administrador.");
            }
        }

        // Define as propriedades visuais do formulário.
        private void ConfigurarFormulario()
        {
            Text = "Cadastrar Usuário";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            ClientSize = new Size(540, 590);

            MaximizeBox = false;
            MinimizeBox = false;

            BackColor = _corFundo;
        }

        // Cria e organiza os campos de cadastro.
        private void CriarLayout()
        {
            Controls.Clear();

            Label lblTitulo = CriarLabel(
                "Cadastrar novo usuário", 30, 20);

            lblTitulo.Font = new Font(
                "Segoe UI", 20, FontStyle.Bold);

            lblTitulo.ForeColor = _corTitulo;

            Label lblNome = CriarLabel(
                "Nome completo", 30, 85);

            txtNome = CriarTextBox(30, 108, 150);

            Label lblPerfil = CriarLabel(
                "Perfil de acesso", 30, 155);

            cmbPerfil = CriarComboBoxPerfil();

            lblLogin = CriarLabel(
                "Login / Identificador", 30, 225);

            txtLogin = CriarTextBox(30, 248, 100);

            Label lblSenha = CriarLabel(
                "Senha inicial", 30, 295);

            txtSenha = CriarTextBox(30, 318, 128);
            txtSenha.UseSystemPasswordChar = true;

            Label lblConfirmarSenha = CriarLabel(
                "Confirmar senha", 30, 365);

            txtConfirmarSenha = CriarTextBox(30, 388, 128);
            txtConfirmarSenha.UseSystemPasswordChar = true;

            lblOrientacao = CriarOrientacaoSenha();

            btnSalvar = CriarBotaoSalvar();

            Controls.AddRange(new Control[]
            {
                lblTitulo,
                lblNome,
                txtNome,
                lblPerfil,
                cmbPerfil,
                lblLogin,
                txtLogin,
                lblSenha,
                txtSenha,
                lblConfirmarSenha,
                txtConfirmarSenha,
                lblOrientacao,
                btnSalvar
            });

            AcceptButton = btnSalvar;
        }

        // Cria o campo de seleção do perfil.
        private ComboBox CriarComboBoxPerfil()
        {
            ComboBox comboBox = new()
            {
                Location = new Point(30, 178),
                Width = 470,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11)
            };

            comboBox.SelectedIndexChanged +=
                (_, _) => AtualizarCamposPerfil();

            return comboBox;
        }

        // Exibe as orientações de segurança da senha.
        private static Label CriarOrientacaoSenha()
        {
            return new Label
            {
                Text =
                    "A senha deve conter no mínimo 8 caracteres, " +
                    "incluindo maiúscula, minúscula, número e símbolo.",

                Location = new Point(30, 440),
                Size = new Size(470, 55),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9)
            };
        }

        // Cria o botão responsável pelo cadastro.
        private Button CriarBotaoSalvar()
        {
            Button botao = new()
            {
                Text = "Cadastrar usuário",
                Location = new Point(300, 515),
                Size = new Size(200, 42),
                BackColor = _corBotao,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            botao.FlatAppearance.BorderSize = 0;
            botao.Click += BtnSalvar_Click;

            return botao;
        }

        // Cria rótulos padronizados.
        private static Label CriarLabel(
            string texto,
            int posicaoX,
            int posicaoY)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(posicaoX, posicaoY),
                AutoSize = true
            };
        }

        // Cria campos de texto padronizados.
        private static TextBox CriarTextBox(
            int posicaoX,
            int posicaoY,
            int tamanhoMaximo)
        {
            return new TextBox
            {
                Location = new Point(posicaoX, posicaoY),
                Width = 470,
                Font = new Font("Segoe UI", 11),
                MaxLength = tamanhoMaximo
            };
        }

        // Consulta os perfis cadastrados no banco.
        private void CarregarPerfis()
        {
            try
            {
                DataTable perfis = _usuarioRepository.ListarPerfis();

                cmbPerfil.DisplayMember = "NomePerfil";
                cmbPerfil.ValueMember = "IdPerfil";
                cmbPerfil.DataSource = perfis;

                cmbPerfil.SelectedIndex = -1;

                AtualizarCamposPerfil();
            }
            catch (Exception ex)
            {
                btnSalvar.Enabled = false;

                ExibirMensagem(
                    "Não foi possível carregar os perfis.\n\n" +
                    ex.Message,
                    "Erro de consulta",
                    MessageBoxIcon.Error);
            }
        }

        // Ajusta o campo de login conforme o perfil escolhido.
        private void AtualizarCamposPerfil()
        {
            if (cmbPerfil.SelectedItem is not DataRowView linha)
            {
                LimparConfiguracaoLogin();
                return;
            }

            string nomePerfil =
                Convert.ToString(linha["NomePerfil"]) ?? "";

            txtLogin.Clear();

            if (nomePerfil == "Administrador")
            {
                ConfigurarLoginAdministrador();
            }
            else
            {
                ConfigurarLoginAutomatico(nomePerfil);
            }
        }

        // Limpa o campo quando nenhum perfil está selecionado.
        private void LimparConfiguracaoLogin()
        {
            txtLogin.Enabled = false;
            txtLogin.Clear();
            txtLogin.PlaceholderText = "";

            lblLogin.Text = "Login / Identificador";
        }

        // Permite um login personalizado para administradores.
        private void ConfigurarLoginAdministrador()
        {
            txtLogin.Enabled = true;
            txtLogin.PlaceholderText = "Exemplo: admin.max";

            lblLogin.Text = "Login do Administrador";
        }

        // Exibe o identificador automático de cada perfil.
        private void ConfigurarLoginAutomatico(string nomePerfil)
        {
            txtLogin.Enabled = false;

            string prefixo = nomePerfil switch
            {
                "Aluno" => "RA",
                "Professor" => "RP",
                "Coordenador" => "RC",
                _ => ""
            };

            lblLogin.Text = prefixo + " automático";
            txtLogin.PlaceholderText = "Gerado após o cadastro";
        }

        // Valida os campos e solicita o cadastro ao serviço.
        private void BtnSalvar_Click(
            object? sender,
            EventArgs e)
        {
            if (!ValidarCampos(out int idPerfil))
            {
                return;
            }

            try
            {
                // A complexidade da senha é validada pelo serviço.
                UsuarioService.ValidarSenha(txtSenha.Text);

                btnSalvar.Enabled = false;

                string loginGerado =
                    _usuarioService.CadastrarUsuario(
                        _administrador,
                        txtNome.Text,
                        txtLogin.Text,
                        txtSenha.Text,
                        idPerfil);

                ExibirMensagem(
                    "Usuário cadastrado com sucesso!\n\n" +
                    "Login / Identificador: " + loginGerado +
                    "\n\nA senha foi definida pelo Administrador.",
                    "Cadastro concluído",
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Dados inválidos",
                    MessageBoxIcon.Warning);
            }
            catch (UnauthorizedAccessException ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Acesso negado",
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Não foi possível cadastrar o usuário.\n\n" +
                    "Detalhes técnicos:\n" + ex,
                    "Erro no cadastro",
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }

        // Verifica os campos obrigatórios antes do cadastro.
        private bool ValidarCampos(out int idPerfil)
        {
            idPerfil = 0;

            if (cmbPerfil.SelectedValue is not int perfilSelecionado)
            {
                ExibirMensagem(
                    "Selecione um perfil de acesso.",
                    "Validação",
                    MessageBoxIcon.Warning);

                cmbPerfil.Focus();
                return false;
            }

            idPerfil = perfilSelecionado;

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                ExibirMensagem(
                    "Informe o nome completo.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtNome.Focus();
                return false;
            }

            if (idPerfil == PerfilAdministrador &&
                string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                ExibirMensagem(
                    "Informe o login do Administrador.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtLogin.Focus();
                return false;
            }

            if (txtSenha.Text != txtConfirmarSenha.Text)
            {
                ExibirMensagem(
                    "As senhas informadas não coincidem.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtConfirmarSenha.Focus();
                return false;
            }

            return true;
        }

        // Exibe mensagens padronizadas ao usuário.
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
    }
}