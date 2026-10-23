
using System;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    public partial class FrmGerenciarUsuarios
    {
        private Button? btnRedefinirSenha;

        // Adiciona a ação de redefinição após a criação da interface.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (btnRedefinirSenha != null)
                return;

            btnRedefinirSenha = new Button
            {
                Name = "btnRedefinirSenha",
                Text = "Redefinir senha",
                Size = new Size(180, 42),
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            btnRedefinirSenha.FlatAppearance.BorderSize = 0;

            btnRedefinirSenha.Click += (sender, args) =>
            {
                AbrirRedefinicaoSenha();
            };

            // Procura o botão existente, inclusive em painéis internos.
            Button? botaoAtualizar = LocalizarBotaoAtualizar(this);

            if (botaoAtualizar != null &&
                botaoAtualizar.Parent != null)
            {
                Control container = botaoAtualizar.Parent;

                container.Controls.Add(btnRedefinirSenha);

                // Utiliza o mesmo sistema de posicionamento dos botões.
                btnRedefinirSenha.Anchor =
                    AnchorStyles.Bottom | AnchorStyles.Left;

                PosicionarBotaoRedefinirSenha(botaoAtualizar);

                container.Resize += (sender, args) =>
                {
                    PosicionarBotaoRedefinirSenha(botaoAtualizar);
                };
            }
            else
            {
                // Mantém o botão acessível caso a referência não exista.
                FlowLayoutPanel painelAcoes = new FlowLayoutPanel
                {
                    Name = "pnlRedefinicaoSenha",
                    Dock = DockStyle.Bottom,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    Padding = new Padding(12, 8, 12, 8),
                    BackColor = Color.Transparent
                };

                btnRedefinirSenha.Margin =
                    new Padding(4);

                painelAcoes.Controls.Add(btnRedefinirSenha);
                Controls.Add(painelAcoes);
                painelAcoes.BringToFront();
            }
        }

        // Localiza recursivamente o botão de atualização.
        private Button? LocalizarBotaoAtualizar(Control container)
        {
            foreach (Control controle in container.Controls)
            {
                if (controle is Button botao &&
                    botao.Text.Contains(
                        "Atualizar lista",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return botao;
                }

                Button? resultado =
                    LocalizarBotaoAtualizar(controle);

                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        // Alinha a redefinição de senha à área de ações.
        private void PosicionarBotaoRedefinirSenha(
            Button botaoReferencia)
        {
            if (btnRedefinirSenha == null ||
                botaoReferencia.IsDisposed)
            {
                return;
            }

            Control? container = botaoReferencia.Parent;

            if (container == null)
                return;

            const int espacamento = 14;

            int posicaoX =
                botaoReferencia.Right + espacamento;

            int posicaoY = botaoReferencia.Top;

            btnRedefinirSenha.Height =
                botaoReferencia.Height;

            btnRedefinirSenha.Location =
                new Point(posicaoX, posicaoY);

            // Caso não haja espaço horizontal, permite rolagem.
            if (container is ScrollableControl painel)
            {
                painel.AutoScroll = true;
            }
        }

        // Permite redefinir a senha de um usuário ativo.
        private void AbrirRedefinicaoSenha()
        {
            int? idUsuario = ObterSelecionado();

            if (!idUsuario.HasValue)
                return;

            DataGridViewRow linha = dgvUsuarios.CurrentRow!;

            string nomeUsuario = Convert.ToString(
                linha.Cells["NomeCompleto"].Value) ?? "";

            string loginUsuario = Convert.ToString(
                linha.Cells["Login"].Value) ?? "";

            bool ativo = Convert.ToBoolean(
                linha.Cells["Ativo"].Value);

            if (!ativo)
            {
                MessageBox.Show(
                    "Não é possível redefinir a senha de uma conta desativada.",
                    "Usuário inativo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using Form janela = new Form
            {
                Text = "Redefinir senha provisória",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ClientSize = new Size(470, 355),
                MinimumSize = new Size(440, 380),
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10),
                AutoScaleMode = AutoScaleMode.Dpi
            };

            Label lblUsuario = new Label
            {
                Text = $"Usuário: {nomeUsuario}\nLogin: {loginUsuario}",
                Location = new Point(25, 20),
                Size = new Size(420, 55),
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Left |
                         AnchorStyles.Right
            };

            Label lblSenha = new Label
            {
                Text = "Senha provisória",
                Location = new Point(25, 85),
                AutoSize = true
            };

            TextBox txtSenha = new TextBox
            {
                Location = new Point(25, 110),
                Width = 420,
                UseSystemPasswordChar = true,
                MaxLength = 128,
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Left |
                         AnchorStyles.Right
            };

            Label lblConfirmar = new Label
            {
                Text = "Confirmar senha provisória",
                Location = new Point(25, 155),
                AutoSize = true
            };

            TextBox txtConfirmar = new TextBox
            {
                Location = new Point(25, 180),
                Width = 420,
                UseSystemPasswordChar = true,
                MaxLength = 128,
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Left |
                         AnchorStyles.Right
            };

            Label lblRegras = new Label
            {
                Text = "Mínimo de 8 caracteres, com letra maiúscula, " +
                       "minúscula, número e símbolo.\n" +
                       "O usuário deverá trocar essa senha no próximo acesso.",
                Location = new Point(25, 225),
                Size = new Size(420, 55),
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Left |
                         AnchorStyles.Right
            };

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(110, 40),
                DialogResult = DialogResult.Cancel
            };

            Button btnSalvar = new Button
            {
                Text = "Redefinir",
                Size = new Size(125, 40),
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnSalvar.FlatAppearance.BorderSize = 0;

            // Organiza os botões do diálogo sem sobreposição.
            FlowLayoutPanel painelBotoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(15, 8, 15, 8),
                WrapContents = false,
                BackColor = Color.White
            };

            painelBotoes.Controls.Add(btnSalvar);
            painelBotoes.Controls.Add(btnCancelar);

            btnSalvar.Click += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtSenha.Text))
                {
                    MessageBox.Show(
                        "Informe a senha provisória.",
                        "Validação",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (txtSenha.Text != txtConfirmar.Text)
                {
                    MessageBox.Show(
                        "As senhas não coincidem.",
                        "Validação",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    UsuarioService.ValidarSenha(txtSenha.Text);

                    DialogResult confirmacao = MessageBox.Show(
                        $"Deseja redefinir a senha de '{nomeUsuario}'?\n\n" +
                        "A senha atual deixará de funcionar e o usuário " +
                        "deverá cadastrar uma nova senha no próximo login.",
                        "Confirmar redefinição",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmacao != DialogResult.Yes)
                        return;

                    RedefinicaoSenhaService servico = new();

                    servico.RedefinirSenha(
                        _usuarioLogado,
                        idUsuario.Value,
                        txtSenha.Text);

                    MessageBox.Show(
                        "Senha provisória definida com sucesso!\n\n" +
                        $"Usuário: {nomeUsuario}\n" +
                        $"Login: {loginUsuario}\n\n" +
                        "No próximo acesso, o sistema exigirá " +
                        "a criação de uma nova senha.\n\n" +
                        "Informe a senha provisória ao usuário " +
                        "por um meio seguro.",
                        "Redefinição concluída",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    janela.DialogResult = DialogResult.OK;
                    janela.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Não foi possível redefinir a senha",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            janela.Controls.AddRange(new Control[]
            {
                lblUsuario,
                lblSenha,
                txtSenha,
                lblConfirmar,
                txtConfirmar,
                lblRegras,
                painelBotoes
            });

            janela.AcceptButton = btnSalvar;
            janela.CancelButton = btnCancelar;

            janela.ShowDialog(this);
        }
    }
}
