namespace PIM_IV.Painel_ADM
{
    partial class FrmLogin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            lblUsuario = new Label();
            txtUsuario = new TextBox();
            lblSenha = new Label();
            txtSenha = new TextBox();
            btnEntrar = new Button();
            lblInformacao = new Label();
            btnCadastroInicial = new Button();
            SuspendLayout();
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = SystemColors.ActiveCaptionText;
            lblTitulo.Location = new Point(146, 36);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(199, 32);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "TECH ACADEMY";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.Location = new Point(155, 68);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(181, 17);
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Painel Administrativo Desktop";
            // 
            // lblUsuario
            // 
            lblUsuario.AutoSize = true;
            lblUsuario.Location = new Point(90, 140);
            lblUsuario.Name = "lblUsuario";
            lblUsuario.Size = new Size(50, 15);
            lblUsuario.TabIndex = 2;
            lblUsuario.Text = "Usuário ";
            // 
            // txtUsuario
            // 
            txtUsuario.Location = new Point(90, 165);
            txtUsuario.MaxLength = 100;
            txtUsuario.Name = "txtUsuario";
            txtUsuario.Size = new Size(330, 23);
            txtUsuario.TabIndex = 3;
            // 
            // lblSenha
            // 
            lblSenha.AutoSize = true;
            lblSenha.Location = new Point(90, 215);
            lblSenha.Name = "lblSenha";
            lblSenha.Size = new Size(39, 15);
            lblSenha.TabIndex = 4;
            lblSenha.Text = "Senha";
            // 
            // txtSenha
            // 
            txtSenha.Location = new Point(90, 240);
            txtSenha.Name = "txtSenha";
            txtSenha.Size = new Size(330, 23);
            txtSenha.TabIndex = 5;
            txtSenha.UseSystemPasswordChar = true;
            // 
            // btnEntrar
            // 
            btnEntrar.BackColor = SystemColors.MenuHighlight;
            btnEntrar.FlatStyle = FlatStyle.Flat;
            btnEntrar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnEntrar.ForeColor = SystemColors.ButtonHighlight;
            btnEntrar.Location = new Point(90, 300);
            btnEntrar.Name = "btnEntrar";
            btnEntrar.Size = new Size(330, 42);
            btnEntrar.TabIndex = 6;
            btnEntrar.Text = "ENTRAR";
            btnEntrar.UseVisualStyleBackColor = false;
            // 
            // lblInformacao
            // 
            lblInformacao.AutoSize = true;
            lblInformacao.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInformacao.ForeColor = SystemColors.ControlDarkDark;
            lblInformacao.Location = new Point(120, 345);
            lblInformacao.Name = "lblInformacao";
            lblInformacao.Size = new Size(274, 13);
            lblInformacao.TabIndex = 7;
            lblInformacao.Text = "Acesso exclusivo para Administrador e Coordenador";
            // 
            // btnCadastroInicial
            // 
            btnCadastroInicial.Location = new Point(417, 348);
            btnCadastroInicial.Name = "btnCadastroInicial";
            btnCadastroInicial.Size = new Size(75, 23);
            btnCadastroInicial.TabIndex = 8;
            btnCadastroInicial.Text = "Configurar primeiro acesso";
            btnCadastroInicial.UseVisualStyleBackColor = true;
            btnCadastroInicial.Visible = false;
            // 
            // FrmLogin
            // 
            AcceptButton = btnEntrar;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(504, 381);
            Controls.Add(btnCadastroInicial);
            Controls.Add(lblInformacao);
            Controls.Add(btnEntrar);
            Controls.Add(txtSenha);
            Controls.Add(lblSenha);
            Controls.Add(txtUsuario);
            Controls.Add(lblUsuario);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblTitulo);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PIM IV - Login";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private TextBox txtUsuario;
        private Label lblSenha;
        private TextBox txtSenha;
        private Button btnEntrar;
        private Label lblInformacao;
        private Button btnCadastroInicial;
    }
}
