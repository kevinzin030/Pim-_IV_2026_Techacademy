using System;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    // Complementa o formulário principal com a redefinição de senhas.
    public partial class FrmGerenciarUsuarios
    {
        private Button? btnRedefinirSenha;

        private static readonly Color CorPrincipal =
            Color.FromArgb(40, 92, 170);

        // Adiciona o botão de redefinição quando o formulário é exibido.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (btnRedefinirSenha != null)
            {
                return;
            }

            AdicionarBotaoRedefinicao();
        }

        // Cria o botão responsável por iniciar a redefinição.
        private void AdicionarBotaoRedefinicao()
        {
            btnRedefinirSenha = new Button
            {
                Name = "btnRedefinirSenha",
                Text = "Redefinir senha",
                Size = new Size(180, 42),
                BackColor = CorPrincipal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            btnRedefinirSenha.FlatAppearance.BorderSize = 0;

            btnRedefinirSenha.Click += (_, _) =>
                AbrirRedefinicaoSenha();

            Button? botaoAtualizar =
                LocalizarBotaoAtualizar(this);

            if (botaoAtualizar?.Parent != null)
            {
                PosicionarBotaoJuntoAoAtualizar(botaoAtualizar);
            }
            else
            {
                CriarPainelAlternativoRedefinicao();
            }
        }

        // Posiciona o botão ao lado da opção Atualizar lista.
        private void PosicionarBotaoJuntoAoAtualizar(
            Button botaoAtualizar)
        {
            Control container = botaoAtualizar.Parent!;

            container.Controls.Add(btnRedefinirSenha!);

            btnRedefinirSenha!.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left;

            AtualizarPosicaoBotaoRedefinicao(botaoAtualizar);

            container.Resize += (_, _) =>
                AtualizarPosicaoBotaoRedefinicao(botaoAtualizar);
        }

        // Reposiciona o botão quando o tamanho da janela muda.
        private void AtualizarPosicaoBotaoRedefinicao(
            Button botaoReferencia)
        {
            if (btnRedefinirSenha == null ||
                btnRedefinirSenha.IsDisposed ||
                botaoReferencia.IsDisposed)
            {
                return;
            }

            Control? container = botaoReferencia.Parent;

            if (container == null)
            {
                return;
            }

            const int espacamento = 14;

            btnRedefinirSenha.Height =
                botaoReferencia.Height;

            btnRedefinirSenha.Location = new Point(
                botaoReferencia.Right + espacamento,
                botaoReferencia.Top);

            if (container is ScrollableControl painel)
            {
                painel.AutoScroll = true;
            }
        }

        // Cria uma área alternativa caso o botão Atualizar não exista.
        private void CriarPainelAlternativoRedefinicao()
        {
            FlowLayoutPanel painelAcoes = new()
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

            btnRedefinirSenha!.Margin = new Padding(4);

            painelAcoes.Controls.Add(btnRedefinirSenha);

            Controls.Add(painelAcoes);

            painelAcoes.BringToFront();
        }

        // Procura recursivamente o botão Atualizar lista.
        private static Button? LocalizarBotaoAtualizar(
            Control container)
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

                Button? encontrado =
                    LocalizarBotaoAtualizar(controle);

                if (encontrado != null)
                {
                    return encontrado;
                }
            }

            return null;
        }

        // Verifica a seleção e o status antes de redefinir a senha.
        private void AbrirRedefinicaoSenha()
        {
            int? idUsuario = ObterSelecionado();

            if (!idUsuario.HasValue)
            {
                return;
            }

            DataGridViewRow? linhaSelecionada =
                dgvUsuarios.CurrentRow;

            if (linhaSelecionada == null)
            {
                return;
            }

            string nomeUsuario = Convert.ToString(
                linhaSelecionada.Cells["NomeCompleto"].Value) ?? "";

            string loginUsuario = Convert.ToString(
                linhaSelecionada.Cells["Login"].Value) ?? "";

            bool usuarioAtivo = Convert.ToBoolean(
                linhaSelecionada.Cells["Ativo"].Value);

            if (!usuarioAtivo)
            {
                ExibirMensagemRedefinicao(
                    "Não é possível redefinir a senha de uma conta desativada.",
                    "Usuário inativo",
                    MessageBoxIcon.Warning);

                return;
            }

            ExibirFormularioRedefinicao(
                idUsuario.Value,
                nomeUsuario,
                loginUsuario);
        }

        // Monta a janela para definir uma nova senha provisória.
        private void ExibirFormularioRedefinicao(
            int idUsuario,
            string nomeUsuario,
            string loginUsuario)
        {
            using Form janela = new()
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

            Label lblUsuario = new()
            {
                Text =
                    $"Usuário: {nomeUsuario}\nLogin: {loginUsuario}",

                Location = new Point(25, 20),
                Size = new Size(420, 55),

                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),

                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right
            };

            Label lblSenha = CriarRotuloRedefinicao(
                "Senha provisória",
                85);

            TextBox txtSenha = CriarCampoSenha(110);

            Label lblConfirmar = CriarRotuloRedefinicao(
                "Confirmar senha provisória",
                155);

            TextBox txtConfirmar = CriarCampoSenha(180);

            Label lblRegras = new()
            {
                Text =
                    "Mínimo de 8 caracteres, com letra maiúscula, " +
                    "minúscula, número e símbolo.\n" +
                    "O usuário deverá trocar essa senha no próximo acesso.",

                Location = new Point(25, 225),
                Size = new Size(420, 55),
                ForeColor = Color.DimGray,

                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right
            };

            Button btnCancelar = new()
            {
                Text = "Cancelar",
                Size = new Size(110, 40),
                DialogResult = DialogResult.Cancel
            };

            Button btnSalvar = new()
            {
                Text = "Redefinir",
                Size = new Size(125, 40),
                BackColor = CorPrincipal,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnSalvar.FlatAppearance.BorderSize = 0;

            FlowLayoutPanel painelBotoes = new()
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

            // Encaminha os dados digitados para validação.
            btnSalvar.Click += (_, _) =>
                ProcessarRedefinicaoSenha(
                    janela,
                    btnSalvar,
                    txtSenha,
                    txtConfirmar,
                    idUsuario,
                    nomeUsuario,
                    loginUsuario);

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

        // Padroniza os rótulos utilizados na janela.
        private static Label CriarRotuloRedefinicao(
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

        // Cria campos que ocultam os caracteres da senha.
        private static TextBox CriarCampoSenha(int posicaoY)
        {
            return new TextBox
            {
                Location = new Point(25, posicaoY),
                Width = 420,
                UseSystemPasswordChar = true,
                MaxLength = 128,

                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right
            };
        }

        // Valida os campos e solicita confirmação da operação.
        private void ProcessarRedefinicaoSenha(
            Form janela,
            Button btnSalvar,
            TextBox txtSenha,
            TextBox txtConfirmar,
            int idUsuario,
            string nomeUsuario,
            string loginUsuario)
        {
            if (string.IsNullOrWhiteSpace(txtSenha.Text))
            {
                ExibirMensagemRedefinicao(
                    "Informe a senha provisória.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtSenha.Focus();
                return;
            }

            if (txtSenha.Text != txtConfirmar.Text)
            {
                ExibirMensagemRedefinicao(
                    "As senhas não coincidem.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtConfirmar.Focus();
                return;
            }

            try
            {
                // Reutiliza a regra de complexidade da camada de serviços.
                UsuarioService.ValidarSenha(txtSenha.Text);

                DialogResult confirmacao = MessageBox.Show(
                    $"Deseja redefinir a senha de '{nomeUsuario}'?\n\n" +
                    "A senha atual deixará de funcionar e o usuário " +
                    "deverá cadastrar uma nova senha no próximo login.",
                    "Confirmar redefinição",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmacao != DialogResult.Yes)
                {
                    return;
                }

                ExecutarRedefinicaoSenha(
                    janela,
                    btnSalvar,
                    idUsuario,
                    nomeUsuario,
                    loginUsuario,
                    txtSenha.Text);
            }
            catch (Exception ex)
            {
                ExibirMensagemRedefinicao(
                    ex.Message,
                    "Não foi possível redefinir a senha",
                    MessageBoxIcon.Warning);
            }
        }

        // Executa a operação pela camada de serviços.
        private void ExecutarRedefinicaoSenha(
            Form janela,
            Button btnSalvar,
            int idUsuario,
            string nomeUsuario,
            string loginUsuario,
            string senhaProvisoria)
        {
            try
            {
                btnSalvar.Enabled = false;

                RedefinicaoSenhaService servico = new();

                servico.RedefinirSenha(
                    _usuarioLogado,
                    idUsuario,
                    senhaProvisoria);

                ExibirMensagemRedefinicao(
                    "Senha provisória definida com sucesso!\n\n" +
                    $"Usuário: {nomeUsuario}\n" +
                    $"Login: {loginUsuario}\n\n" +
                    "No próximo acesso, o sistema exigirá " +
                    "a criação de uma nova senha.\n\n" +
                    "Informe a senha provisória ao usuário " +
                    "por um meio seguro.",
                    "Redefinição concluída",
                    MessageBoxIcon.Information);

                janela.DialogResult = DialogResult.OK;
                janela.Close();
            }
            finally
            {
                // Libera o botão mesmo quando ocorre uma exceção.
                btnSalvar.Enabled = true;
            }
        }

        // Centraliza as mensagens da funcionalidade de redefinição.
        private static void ExibirMensagemRedefinicao(
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