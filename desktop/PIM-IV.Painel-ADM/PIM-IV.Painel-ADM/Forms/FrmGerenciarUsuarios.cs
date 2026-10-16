
using System;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    public partial class FrmGerenciarUsuarios
    {
        private Button? btnRedefinirSenha;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (btnRedefinirSenha != null)
                return;

            btnRedefinirSenha = new Button
            {
                Name = "btnRedefinirSenha",
                Text = "Redefinir senha",
                Location = new Point(590, 585),
                Size = new Size(180, 42),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnRedefinirSenha.FlatAppearance.BorderSize = 0;

            btnRedefinirSenha.Click += (s, args) =>
            {
                AbrirRedefinicaoSenha();
            };

            Controls.Add(btnRedefinirSenha);
            btnRedefinirSenha.BringToFront();
        }

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
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            Label lblUsuario = new Label
            {
                Text = $"Usuário: {nomeUsuario}\nLogin: {loginUsuario}",
                Location = new Point(25, 20),
                Size = new Size(420, 55),
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold)
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
                MaxLength = 128
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
                MaxLength = 128
            };

            Label lblRegras = new Label
            {
                Text = "Mínimo de 8 caracteres, com letra maiúscula, " +
                       "minúscula, número e símbolo.\n" +
                       "O usuário deverá trocar essa senha no próximo acesso.",
                Location = new Point(25, 225),
                Size = new Size(420, 55),
                ForeColor = Color.DimGray
            };

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(195, 295),
                Size = new Size(110, 40),
                DialogResult = DialogResult.Cancel
            };

            Button btnSalvar = new Button
            {
                Text = "Redefinir",
                Location = new Point(320, 295),
                Size = new Size(125, 40),
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnSalvar.FlatAppearance.BorderSize = 0;

            btnSalvar.Click += (s, args) =>
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
                btnCancelar,
                btnSalvar
            });

            janela.AcceptButton = btnSalvar;
            janela.CancelButton = btnCancelar;

            janela.ShowDialog(this);
        }
    }
}
