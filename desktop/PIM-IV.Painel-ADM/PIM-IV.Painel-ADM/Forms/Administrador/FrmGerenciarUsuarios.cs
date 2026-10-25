using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    public partial class FrmGerenciarUsuarios : Form
    {
        private const int PerfilAdministrador = 1;

        private static readonly Color CorFundo =
            Color.FromArgb(244, 247, 251);

        private static readonly Color CorTitulo =
            Color.FromArgb(24, 39, 65);

        private static readonly Color CorAzul =
            Color.FromArgb(40, 92, 170);

        private static readonly Color CorCinza =
            Color.FromArgb(92, 105, 120);

        private readonly Usuario _usuarioLogado;
        private readonly UsuarioRepository _repositorio = new();
        private readonly UsuarioService _usuarioService = new();

        private DataGridView dgvUsuarios = null!;
        private TextBox txtPesquisa = null!;

        private Button btnNovo = null!;
        private Button btnEditar = null!;
        private Button btnStatus = null!;
        private Button btnAtualizar = null!;

        private Label lblQuantidade = null!;

        private DataTable? _dadosUsuarios;

        // Recebe o usuário autenticado e verifica sua autorização.
        public FrmGerenciarUsuarios(Usuario usuarioLogado)
        {
            InitializeComponent();

            _usuarioLogado = usuarioLogado
                ?? throw new ArgumentNullException(nameof(usuarioLogado));

            ValidarAcesso();

            ConfigurarFormulario();
            CriarLayout();
            CarregarUsuarios();
        }

        // Restringe o gerenciamento de usuários ao Administrador ativo.
        private void ValidarAcesso()
        {
            if (_usuarioLogado.IdPerfil != PerfilAdministrador ||
                !_usuarioLogado.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Acesso permitido somente ao Administrador.");
            }
        }

        // Define as propriedades gerais da janela.
        private void ConfigurarFormulario()
        {
            Text = "Gerenciamento de Usuários";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 600);

            BackColor = CorFundo;
        }

        // Organiza os controles utilizados no gerenciamento.
        private void CriarLayout()
        {
            Controls.Clear();

            Label titulo = CriarTitulo();
            Label subtitulo = CriarSubtitulo();

            txtPesquisa = CriarCampoPesquisa();

            btnNovo = CriarBotao(
                "+ Novo usuário",
                new Point(865, 125),
                new Size(190, 42),
                CorAzul);

            btnNovo.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;

            btnNovo.Click += BtnNovo_Click;

            dgvUsuarios = CriarGradeUsuarios();

            btnEditar = CriarBotao(
                "Editar usuário",
                new Point(30, 585),
                new Size(170, 42),
                CorAzul);

            btnEditar.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnEditar.Click += BtnEditar_Click;

            btnStatus = CriarBotao(
                "Ativar / Desativar",
                new Point(215, 585),
                new Size(180, 42),
                CorCinza);

            btnStatus.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnStatus.Click += BtnStatus_Click;

            btnAtualizar = CriarBotao(
                "Atualizar lista",
                new Point(410, 585),
                new Size(165, 42),
                CorCinza);

            btnAtualizar.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnAtualizar.Click += (_, _) => CarregarUsuarios();

            lblQuantidade = new Label
            {
                Text = "Usuários: 0",
                Location = new Point(790, 595),
                Size = new Size(265, 25),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Controls.AddRange(new Control[]
            {
                titulo,
                subtitulo,
                txtPesquisa,
                btnNovo,
                dgvUsuarios,
                btnEditar,
                btnStatus,
                btnAtualizar,
                lblQuantidade
            });
        }

        // Cria o título principal do formulário.
        private static Label CriarTitulo()
        {
            return new Label
            {
                Text = "Gerenciamento de Usuários",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = CorTitulo,
                Location = new Point(30, 25),
                AutoSize = true
            };
        }

        // Apresenta uma descrição das funcionalidades disponíveis.
        private static Label CriarSubtitulo()
        {
            return new Label
            {
                Text = "Cadastre, consulte e gerencie as contas do sistema.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.DimGray,
                Location = new Point(32, 75),
                AutoSize = true
            };
        }

        // Cria o campo utilizado para pesquisar usuários.
        private TextBox CriarCampoPesquisa()
        {
            TextBox campo = new()
            {
                Location = new Point(30, 130),
                Size = new Size(430, 34),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Pesquisar por nome, login ou perfil..."
            };

            campo.TextChanged += (_, _) => FiltrarUsuarios();

            return campo;
        }

        // Padroniza a aparência dos botões do formulário.
        private static Button CriarBotao(
            string texto,
            Point localizacao,
            Size tamanho,
            Color cor)
        {
            Button botao = new()
            {
                Text = texto,
                Location = localizacao,
                Size = tamanho,
                BackColor = cor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),
                UseVisualStyleBackColor = false
            };

            botao.FlatAppearance.BorderSize = 0;

            return botao;
        }

        // Configura a tabela responsável por apresentar os usuários.
        private static DataGridView CriarGradeUsuarios()
        {
            DataGridView grade = new()
            {
                Location = new Point(30, 195),
                Size = new Size(1025, 365),

                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Bottom |
                    AnchorStyles.Left |
                    AnchorStyles.Right,

                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,

                ReadOnly = true,

                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,

                MultiSelect = false,

                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,

                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,

                RowHeadersVisible = false,

                RowTemplate = { Height = 36 },

                Font = new Font("Segoe UI", 10)
            };

            grade.EnableHeadersVisualStyles = false;

            grade.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(32, 55, 88);

            grade.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            grade.ColumnHeadersHeight = 42;

            grade.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            return grade;
        }

        // Consulta os usuários no repositório e atualiza a tabela.
        private void CarregarUsuarios()
        {
            try
            {
                _dadosUsuarios = _repositorio.ListarTodos();

                dgvUsuarios.DataSource = _dadosUsuarios;

                ConfigurarColunas();
                FiltrarUsuarios();
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Não foi possível carregar os usuários.\n\n" +
                    ex.Message,
                    "Erro de consulta",
                    MessageBoxIcon.Error);
            }
        }

        // Define os títulos e tamanhos das colunas disponíveis.
        private void ConfigurarColunas()
        {
            ConfigurarColuna(
                "IdUsuario",
                "ID",
                35);

            ConfigurarColuna(
                "NomeCompleto",
                "Nome completo",
                160);

            ConfigurarColuna(
                "Login",
                "Login / Identificador",
                105);

            ConfigurarColuna(
                "NomePerfil",
                "Perfil",
                85);

            ConfigurarColuna(
                "Ativo",
                "Ativo",
                45);

            ConfigurarColuna(
                "DataCadastro",
                "Data do cadastro",
                110);

            if (dgvUsuarios.Columns.Contains("IdPerfil"))
            {
                dgvUsuarios.Columns["IdPerfil"]!.Visible = false;
            }

            if (dgvUsuarios.Columns.Contains("DataCadastro"))
            {
                dgvUsuarios.Columns["DataCadastro"]!
                    .DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        // Evita repetir a configuração individual de cada coluna.
        private void ConfigurarColuna(
            string nomeColuna,
            string titulo,
            float larguraRelativa)
        {
            if (!dgvUsuarios.Columns.Contains(nomeColuna))
            {
                return;
            }

            DataGridViewColumn coluna =
                dgvUsuarios.Columns[nomeColuna]!;

            coluna.HeaderText = titulo;
            coluna.FillWeight = larguraRelativa;
        }

        // Filtra os usuários pelo nome, login ou perfil.
        private void FiltrarUsuarios()
        {
            if (_dadosUsuarios == null)
            {
                return;
            }

            string pesquisa = txtPesquisa.Text.Trim();

            DataView visao = _dadosUsuarios.DefaultView;

            if (string.IsNullOrWhiteSpace(pesquisa))
            {
                visao.RowFilter = string.Empty;
            }
            else
            {
                string valor = EscaparTextoPesquisa(pesquisa);

                visao.RowFilter =
                    $"NomeCompleto LIKE '%{valor}%' OR " +
                    $"Login LIKE '%{valor}%' OR " +
                    $"NomePerfil LIKE '%{valor}%'";
            }

            lblQuantidade.Text = $"Usuários: {visao.Count}";
        }

        // Trata caracteres especiais utilizados pelo filtro do DataView.
        private static string EscaparTextoPesquisa(string texto)
        {
            return texto
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("]", "[]]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        // Obtém o identificador do usuário selecionado na tabela.
        private int? ObterUsuarioSelecionado()
        {
            DataGridViewRow? linha = dgvUsuarios.CurrentRow;

            if (linha == null || linha.IsNewRow)
            {
                ExibirMensagem(
                    "Selecione um usuário na lista.",
                    "Seleção necessária",
                    MessageBoxIcon.Information);

                return null;
            }

            object? valor = linha.Cells["IdUsuario"].Value;

            if (valor == null || valor == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(valor);
        }

        // Mantém a integração com FrmGerenciarUsuarios.Redefinicao.cs.
        private int? ObterSelecionado()
        {
            return ObterUsuarioSelecionado();
        }

        // Abre o cadastro de novos usuários.
        private void BtnNovo_Click(object? sender, EventArgs e)
        {
            try
            {
                using FrmCadastrarUsuario formulario =
                    new(_usuarioLogado);

                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    CarregarUsuarios();
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Não foi possível abrir o cadastro",
                    MessageBoxIcon.Error);
            }
        }

        // Inicia a edição do usuário selecionado.
        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            int? idUsuario = ObterUsuarioSelecionado();

            if (!idUsuario.HasValue)
            {
                return;
            }

            DataGridViewRow linha = dgvUsuarios.CurrentRow!;

            string nomeAtual = Convert.ToString(
                linha.Cells["NomeCompleto"].Value) ?? "";

            string loginAtual = Convert.ToString(
                linha.Cells["Login"].Value) ?? "";

            int perfilAtual = Convert.ToInt32(
                linha.Cells["IdPerfil"].Value);

            try
            {
                ExibirFormularioEdicao(
                    idUsuario.Value,
                    nomeAtual,
                    loginAtual,
                    perfilAtual);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Não foi possível editar o usuário",
                    MessageBoxIcon.Error);
            }
        }

        // Monta a janela de edição preservando as regras de perfil.
        private void ExibirFormularioEdicao(
            int idUsuario,
            string nomeAtual,
            string loginAtual,
            int perfilAtual)
        {
            using Form janela = new()
            {
                Text = "Editar usuário",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(480, 350),
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White
            };

            Label lblNome = CriarRotuloEdicao(
                "Nome completo",
                25);

            TextBox txtNome = CriarCampoEdicao(
                nomeAtual,
                50,
                150);

            Label lblLogin = CriarRotuloEdicao(
                "Login / Identificador",
                100);

            TextBox txtLogin = CriarCampoEdicao(
                loginAtual,
                125,
                100);

            // Identificadores de outros perfis não podem ser editados.
            txtLogin.ReadOnly =
                perfilAtual != PerfilAdministrador;

            Label lblPerfil = CriarRotuloEdicao(
                "Perfil de acesso",
                175);

            ComboBox cmbPerfil = new()
            {
                Location = new Point(25, 200),
                Width = 425,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                DisplayMember = "NomePerfil",
                ValueMember = "IdPerfil"
            };

            cmbPerfil.DataSource = _repositorio.ListarPerfis();
            cmbPerfil.SelectedValue = perfilAtual;

            Label lblAviso = new()
            {
                Text =
                    "A alteração de perfil está desativada " +
                    "para preservar os vínculos acadêmicos.",

                Location = new Point(25, 240),
                Size = new Size(425, 35),
                ForeColor = Color.DimGray
            };

            Button btnSalvar = new()
            {
                Text = "Salvar alterações",
                Location = new Point(275, 290),
                Size = new Size(175, 38),
                BackColor = CorAzul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnSalvar.FlatAppearance.BorderSize = 0;

            btnSalvar.Click += (_, _) =>
                SalvarEdicaoUsuario(
                    janela,
                    idUsuario,
                    perfilAtual,
                    txtNome.Text,
                    txtLogin.Text);

            janela.Controls.AddRange(new Control[]
            {
                lblNome,
                txtNome,
                lblLogin,
                txtLogin,
                lblPerfil,
                cmbPerfil,
                lblAviso,
                btnSalvar
            });

            janela.AcceptButton = btnSalvar;

            if (janela.ShowDialog(this) == DialogResult.OK)
            {
                CarregarUsuarios();
            }
        }

        // Padroniza os rótulos da janela de edição.
        private static Label CriarRotuloEdicao(
            string texto,
            int posicaoY)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(25, posicaoY),
                AutoSize = true
            };
        }

        // Padroniza os campos de texto utilizados na edição.
        private static TextBox CriarCampoEdicao(
            string valorInicial,
            int posicaoY,
            int limiteCaracteres)
        {
            return new TextBox
            {
                Text = valorInicial,
                Location = new Point(25, posicaoY),
                Width = 425,
                MaxLength = limiteCaracteres
            };
        }

        // Encaminha a atualização para a camada de serviços.
        private void SalvarEdicaoUsuario(
            Form janela,
            int idUsuario,
            int perfilAtual,
            string nome,
            string login)
        {
            try
            {
                _usuarioService.EditarUsuario(
                    _usuarioLogado,
                    idUsuario,
                    nome,
                    login,
                    perfilAtual);

                ExibirMensagem(
                    "Usuário atualizado com sucesso!",
                    "Edição concluída",
                    MessageBoxIcon.Information);

                janela.DialogResult = DialogResult.OK;
                janela.Close();
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Erro na edição",
                    MessageBoxIcon.Warning);
            }
        }

        // Solicita confirmação antes de ativar ou desativar uma conta.
        private void BtnStatus_Click(object? sender, EventArgs e)
        {
            int? idUsuario = ObterUsuarioSelecionado();

            if (!idUsuario.HasValue)
            {
                return;
            }

            DataGridViewRow linha = dgvUsuarios.CurrentRow!;

            string nome = Convert.ToString(
                linha.Cells["NomeCompleto"].Value) ?? "";

            bool statusAtual = Convert.ToBoolean(
                linha.Cells["Ativo"].Value);

            bool novoStatus = !statusAtual;

            string acao = novoStatus ? "ativar" : "desativar";

            DialogResult confirmacao = MessageBox.Show(
                $"Deseja realmente {acao} o usuário '{nome}'?",
                "Confirmar alteração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes)
            {
                return;
            }

            AlterarStatusUsuario(
                idUsuario.Value,
                novoStatus);
        }

        // Executa a alteração de status pela camada de serviços.
        private void AlterarStatusUsuario(
            int idUsuario,
            bool novoStatus)
        {
            try
            {
                _usuarioService.AlterarStatusUsuario(
                    _usuarioLogado,
                    idUsuario,
                    novoStatus);

                ExibirMensagem(
                    "Status atualizado com sucesso!",
                    "Operação concluída",
                    MessageBoxIcon.Information);

                CarregarUsuarios();
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Erro na alteração de status",
                    MessageBoxIcon.Error);
            }
        }

        // Centraliza a apresentação de mensagens ao usuário.
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