
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    public partial class FrmCadastrarUsuario : Form
    {
        private readonly Usuario _administrador;
        private readonly UsuarioService _usuarioService = new();
        private readonly UsuarioRepository _repositorio = new();

        private TextBox txtNome = null!;
        private TextBox txtLogin = null!;
        private TextBox txtSenha = null!;
        private TextBox txtConfirmarSenha = null!;
        private ComboBox cmbPerfil = null!;
        private Button btnSalvar = null!;
        private Label lblLogin = null!;
        private Label lblOrientacao = null!;

        public FrmCadastrarUsuario(Usuario administrador)
        {
            InitializeComponent();

            _administrador = administrador;

            if (_administrador.IdPerfil != 1 ||
                !_administrador.Ativo)
            {
                throw new UnauthorizedAccessException(
                    "Acesso permitido somente ao Administrador.");
            }

            Text = "Cadastrar Usuário";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(540, 590);
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(244, 247, 251);

            CriarLayout();
            CarregarPerfis();
        }

        private void CriarLayout()
        {
            Controls.Clear();

            Label lblTitulo = new()
            {
                Text = "Cadastrar novo usuário",
                Font = new Font(
                    "Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 39, 65),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblNome = new()
            {
                Text = "Nome completo",
                Location = new Point(30, 85),
                AutoSize = true
            };

            txtNome = new TextBox
            {
                Location = new Point(30, 108),
                Width = 470,
                Font = new Font("Segoe UI", 11),
                MaxLength = 150
            };

            Label lblPerfil = new()
            {
                Text = "Perfil de acesso",
                Location = new Point(30, 155),
                AutoSize = true
            };

            cmbPerfil = new ComboBox
            {
                Location = new Point(30, 178),
                Width = 470,
                DropDownStyle =
                    ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11)
            };

            lblLogin = new Label
            {
                Text = "Login / Identificador",
                Location = new Point(30, 225),
                AutoSize = true
            };

            txtLogin = new TextBox
            {
                Location = new Point(30, 248),
                Width = 470,
                Font = new Font("Segoe UI", 11),
                MaxLength = 100
            };

            Label lblSenha = new()
            {
                Text = "Senha inicial",
                Location = new Point(30, 295),
                AutoSize = true
            };

            txtSenha = new TextBox
            {
                Location = new Point(30, 318),
                Width = 470,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                MaxLength = 128
            };

            Label lblConfirmarSenha = new()
            {
                Text = "Confirmar senha",
                Location = new Point(30, 365),
                AutoSize = true
            };

            txtConfirmarSenha = new TextBox
            {
                Location = new Point(30, 388),
                Width = 470,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                MaxLength = 128
            };

            lblOrientacao = new Label
            {
                Text = "A senha deve conter no mínimo 8 caracteres, " +
                       "incluindo maiúscula, minúscula, " +
                       "número e símbolo.",
                Location = new Point(30, 440),
                Size = new Size(470, 55),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9)
            };

            btnSalvar = new Button
            {
                Text = "Cadastrar usuário",
                Location = new Point(300, 515),
                Size = new Size(200, 42),
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnSalvar.FlatAppearance.BorderSize = 0;

            btnSalvar.Click += BtnSalvar_Click;

            cmbPerfil.SelectedIndexChanged +=
                (s, e) => AtualizarCamposPerfil();

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

        private void CarregarPerfis()
        {
            try
            {
                DataTable perfis =
                    _repositorio.ListarPerfis();

                cmbPerfil.DisplayMember = "NomePerfil";
                cmbPerfil.ValueMember = "IdPerfil";
                cmbPerfil.DataSource = perfis;
                cmbPerfil.SelectedIndex = -1;

                AtualizarCamposPerfil();
            }
            catch (Exception ex)
            {
                btnSalvar.Enabled = false;

                MessageBox.Show(
                    "Não foi possível carregar os perfis.\n\n" +
                    ex.Message,
                    "Erro de consulta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AtualizarCamposPerfil()
        {
            if (cmbPerfil.SelectedItem is not DataRowView linha)
            {
                txtLogin.Enabled = false;
                txtLogin.Text = "";
                txtLogin.PlaceholderText = "";
                lblLogin.Text = "Login / Identificador";
                return;
            }

            string perfil =
                linha["NomePerfil"].ToString() ?? "";

            txtLogin.Text = "";

            if (perfil == "Administrador")
            {
                txtLogin.Enabled = true;
                txtLogin.PlaceholderText =
                    "Exemplo: admin.max";

                lblLogin.Text =
                    "Login do Administrador";
            }
            else
            {
                txtLogin.Enabled = false;

                string prefixo = perfil switch
                {
                    "Aluno" => "RA",
                    "Professor" => "RP",
                    "Coordenador" => "RC",
                    _ => ""
                };

                lblLogin.Text =
                    prefixo + " automático";

                txtLogin.PlaceholderText =
                    "Gerado após o cadastro";
            }
        }

        private void BtnSalvar_Click(
            object? sender,
            EventArgs e)
        {
            if (cmbPerfil.SelectedValue is not int idPerfil)
            {
                MessageBox.Show(
                    "Selecione um perfil de acesso.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show(
                    "Informe o nome completo.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (idPerfil == 1 &&
                string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show(
                    "Informe o login do Administrador.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (txtSenha.Text != txtConfirmarSenha.Text)
            {
                MessageBox.Show(
                    "As senhas informadas não coincidem.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                UsuarioService.ValidarSenha(txtSenha.Text);

                btnSalvar.Enabled = false;

                string loginGerado =
                    _usuarioService.CadastrarUsuario(
                        _administrador,
                        txtNome.Text,
                        txtLogin.Text,
                        txtSenha.Text,
                        idPerfil);

                MessageBox.Show(
                    "Usuário cadastrado com sucesso!\n\n" +
                    "Login / Identificador: " + loginGerado +
                    "\n\nA senha foi definida pelo Administrador.",
                    "Cadastro concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Dados inválidos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível cadastrar o usuário.\n\n" +
                    "Detalhes técnicos:\n" +
                    ex.ToString(),
                    "Erro no cadastro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }
    }
}
