
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

        public FrmGerenciarUsuarios(Usuario usuarioLogado)
        {
            InitializeComponent();

            _usuarioLogado = usuarioLogado;

            if (_usuarioLogado.IdPerfil != 1 ||
                !_usuarioLogado.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Acesso permitido somente ao Administrador.");
            }

            Text = "Gerenciamento de Usuários";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 600);
            BackColor = Color.FromArgb(244, 247, 251);

            CriarLayout();
            CarregarUsuarios();
        }

        private void CriarLayout()
        {
            Controls.Clear();

            Label titulo = new()
            {
                Text = "Gerenciamento de Usuários",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 39, 65),
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label subtitulo = new()
            {
                Text = "Cadastre, consulte e gerencie as contas do sistema.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.DimGray,
                Location = new Point(32, 75),
                AutoSize = true
            };

            txtPesquisa = new TextBox
            {
                Location = new Point(30, 130),
                Size = new Size(430, 34),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Pesquisar por nome, login ou perfil..."
            };

            txtPesquisa.TextChanged +=
                (s, e) => FiltrarUsuarios();

            btnNovo = CriarBotao(
                "+ Novo usuário",
                new Point(865, 125),
                new Size(190, 42),
                Color.FromArgb(40, 92, 170));

            btnNovo.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;

            btnNovo.Click += BtnNovo_Click;

            dgvUsuarios = new DataGridView
            {
                Location = new Point(30, 195),
                Size = new Size(1025, 365),
                Anchor = AnchorStyles.Top |
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

            dgvUsuarios.EnableHeadersVisualStyles = false;

            dgvUsuarios.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(32, 55, 88);

            dgvUsuarios.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvUsuarios.ColumnHeadersHeight = 42;

            dgvUsuarios.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            btnEditar = CriarBotao(
                "Editar usuário",
                new Point(30, 585),
                new Size(170, 42),
                Color.FromArgb(40, 92, 170));

            btnEditar.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnEditar.Click += BtnEditar_Click;

            btnStatus = CriarBotao(
                "Ativar / Desativar",
                new Point(215, 585),
                new Size(180, 42),
                Color.FromArgb(92, 105, 120));

            btnStatus.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnStatus.Click += BtnStatus_Click;

            btnAtualizar = CriarBotao(
                "Atualizar lista",
                new Point(410, 585),
                new Size(165, 42),
                Color.FromArgb(92, 105, 120));

            btnAtualizar.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            btnAtualizar.Click +=
                (s, e) => CarregarUsuarios();

            lblQuantidade = new Label
            {
                Text = "Usuários: 0",
                Location = new Point(790, 595),
                Size = new Size(265, 25),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Anchor =
                    AnchorStyles.Bottom | AnchorStyles.Right
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
                    "Segoe UI", 10, FontStyle.Bold)
            };

            botao.FlatAppearance.BorderSize = 0;

            return botao;
        }

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
                MessageBox.Show(
                    "Não foi possível carregar os usuários.\n\n" +
                    ex.Message,
                    "Erro de consulta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ConfigurarColunas()
        {
            if (dgvUsuarios.Columns.Contains("IdUsuario"))
            {
                dgvUsuarios.Columns["IdUsuario"]!.HeaderText =
                    "ID";

                dgvUsuarios.Columns["IdUsuario"]!.FillWeight = 35;
            }

            if (dgvUsuarios.Columns.Contains("NomeCompleto"))
            {
                dgvUsuarios.Columns["NomeCompleto"]!.HeaderText =
                    "Nome completo";

                dgvUsuarios.Columns["NomeCompleto"]!.FillWeight = 160;
            }

            if (dgvUsuarios.Columns.Contains("Login"))
            {
                dgvUsuarios.Columns["Login"]!.HeaderText =
                    "Login / Identificador";

                dgvUsuarios.Columns["Login"]!.FillWeight = 105;
            }

            if (dgvUsuarios.Columns.Contains("NomePerfil"))
            {
                dgvUsuarios.Columns["NomePerfil"]!.HeaderText =
                    "Perfil";

                dgvUsuarios.Columns["NomePerfil"]!.FillWeight = 85;
            }

            if (dgvUsuarios.Columns.Contains("IdPerfil"))
            {
                dgvUsuarios.Columns["IdPerfil"]!.Visible = false;
            }

            if (dgvUsuarios.Columns.Contains("Ativo"))
            {
                dgvUsuarios.Columns["Ativo"]!.HeaderText =
                    "Ativo";

                dgvUsuarios.Columns["Ativo"]!.FillWeight = 45;
            }

            if (dgvUsuarios.Columns.Contains("DataCadastro"))
            {
                dgvUsuarios.Columns["DataCadastro"]!.HeaderText =
                    "Data do cadastro";

                dgvUsuarios.Columns["DataCadastro"]!
                    .DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

                dgvUsuarios.Columns["DataCadastro"]!.FillWeight = 110;
            }
        }

        private void FiltrarUsuarios()
        {
            if (_dadosUsuarios == null)
                return;

            string pesquisa = txtPesquisa.Text.Trim();

            DataView visao = _dadosUsuarios.DefaultView;

            if (string.IsNullOrWhiteSpace(pesquisa))
            {
                visao.RowFilter = "";
            }
            else
            {
                string valor = pesquisa
                    .Replace("'", "''")
                    .Replace("[", "[[]")
                    .Replace("%", "[%]")
                    .Replace("*", "[*]");

                visao.RowFilter =
                    $"NomeCompleto LIKE '%{valor}%' OR " +
                    $"Login LIKE '%{valor}%' OR " +
                    $"NomePerfil LIKE '%{valor}%'";
            }

            lblQuantidade.Text =
                $"Usuários: {visao.Count}";
        }

        // Metodo utilizado pelas operacoes de gerenciamento
        private int? ObterUsuarioSelecionado()
        {
            if (dgvUsuarios.CurrentRow == null ||
                dgvUsuarios.CurrentRow.IsNewRow)
            {
                MessageBox.Show(
                    "Selecione um usuário na lista.",
                    "Seleção necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return null;
            }

            object? valor =
                dgvUsuarios.CurrentRow.Cells["IdUsuario"].Value;

            if (valor == null || valor == DBNull.Value)
                return null;

            return Convert.ToInt32(valor);
        }

        // Compatibilidade com FrmGerenciarUsuarios.Redefinicao.cs
        private int? ObterSelecionado()
        {
            return ObterUsuarioSelecionado();
        }

        private void BtnNovo_Click(object? sender, EventArgs e)
        {
            try
            {
                using FrmCadastrarUsuario formulario =
                    new(_usuarioLogado);

                if (formulario.ShowDialog(this) == DialogResult.OK)
                    CarregarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Não foi possível abrir o cadastro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            int? idUsuario = ObterUsuarioSelecionado();

            if (!idUsuario.HasValue)
                return;

            DataGridViewRow linha = dgvUsuarios.CurrentRow!;

            string nomeAtual =
                Convert.ToString(
                    linha.Cells["NomeCompleto"].Value) ?? "";

            string loginAtual =
                Convert.ToString(
                    linha.Cells["Login"].Value) ?? "";

            int perfilAtual = Convert.ToInt32(
                linha.Cells["IdPerfil"].Value);

            using Form janela = new()
            {
                Text = "Editar usuário",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(480, 350),
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblNome = new()
            {
                Text = "Nome completo",
                Location = new Point(25, 25),
                AutoSize = true
            };

            TextBox txtNome = new()
            {
                Text = nomeAtual,
                Location = new Point(25, 50),
                Width = 425,
                MaxLength = 150
            };

            Label lblLogin = new()
            {
                Text = "Login / Identificador",
                Location = new Point(25, 100),
                AutoSize = true
            };

            TextBox txtLogin = new()
            {
                Text = loginAtual,
                Location = new Point(25, 125),
                Width = 425,
                MaxLength = 100,
                ReadOnly = perfilAtual != 1
            };

            Label lblPerfil = new()
            {
                Text = "Perfil de acesso",
                Location = new Point(25, 175),
                AutoSize = true
            };

            ComboBox cmbPerfil = new()
            {
                Location = new Point(25, 200),
                Width = 425,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            cmbPerfil.DisplayMember = "NomePerfil";
            cmbPerfil.ValueMember = "IdPerfil";
            cmbPerfil.DataSource = _repositorio.ListarPerfis();
            cmbPerfil.SelectedValue = perfilAtual;

            Label lblAviso = new()
            {
                Text = "A alteração de perfil está desativada " +
                       "para preservar os vínculos acadêmicos.",
                Location = new Point(25, 240),
                Size = new Size(425, 35),
                ForeColor = Color.DimGray
            };

            Button btnSalvar = new()
            {
                Text = "Salvar alterações",
                Location = new Point(275, 290),
                Size = new Size(175, 38)
            };

            btnSalvar.Click += (s, args) =>
            {
                try
                {
                    _usuarioService.EditarUsuario(
                        _usuarioLogado,
                        idUsuario.Value,
                        txtNome.Text,
                        txtLogin.Text,
                        perfilAtual);

                    MessageBox.Show(
                        "Usuário atualizado com sucesso!",
                        "Edição concluída",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    janela.DialogResult = DialogResult.OK;
                    janela.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Erro na edição",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

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

            if (janela.ShowDialog(this) == DialogResult.OK)
                CarregarUsuarios();
        }

        private void BtnStatus_Click(object? sender, EventArgs e)
        {
            int? idUsuario = ObterUsuarioSelecionado();

            if (!idUsuario.HasValue)
                return;

            DataGridViewRow linha = dgvUsuarios.CurrentRow!;

            string nome =
                Convert.ToString(
                    linha.Cells["NomeCompleto"].Value) ?? "";

            bool ativo = Convert.ToBoolean(
                linha.Cells["Ativo"].Value);

            bool novoStatus = !ativo;

            string acao =
                novoStatus ? "ativar" : "desativar";

            DialogResult resposta = MessageBox.Show(
                $"Deseja realmente {acao} o usuário '{nome}'?",
                "Confirmar alteração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resposta != DialogResult.Yes)
                return;

            try
            {
                _usuarioService.AlterarStatusUsuario(
                    _usuarioLogado,
                    idUsuario.Value,
                    novoStatus);

                MessageBox.Show(
                    "Status atualizado com sucesso!",
                    "Operação concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Erro na alteração de status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
