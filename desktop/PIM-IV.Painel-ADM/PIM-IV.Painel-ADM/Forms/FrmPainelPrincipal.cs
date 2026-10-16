
using System;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM
{
    public partial class FrmPainelPrincipal : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azulEscuro =
            Color.FromArgb(24, 39, 65);

        private readonly Color _azul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _fundo =
            Color.FromArgb(244, 247, 251);

        private Panel pnlMenu = null!;
        private Panel pnlConteudo = null!;
        private Label lblTituloPagina = null!;

        public FrmPainelPrincipal(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario;

            Text = "PIM IV - Painel Administrativo";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 650);
            BackColor = _fundo;

            CriarLayout();

            FormClosing += FrmPainelPrincipal_FormClosing;
        }

        // Consulta a permissao atual no banco de dados.
        private bool TemPermissao(string codigo)
        {
            try
            {
                return _permissaoService.UsuarioTemPermissao(
                    _usuario, codigo);
            }
            catch (Exception)
            {
                // Em caso de erro, o acesso nao e liberado.
                return false;
            }
        }

        // Confirma a autorizacao antes de abrir o modulo.
        private bool Autorizar(string codigo)
        {
            try
            {
                if (_permissaoService.UsuarioTemPermissao(
                    _usuario, codigo))
                {
                    return true;
                }

                MessageBox.Show(
                    "Você não possui permissão para acessar este módulo.",
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                AtualizarPainel();
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível verificar sua permissão:\n" +
                    ex.Message,
                    "Erro de autorização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void FrmPainelPrincipal_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;

            DialogResult resposta = MessageBox.Show(
                "Tem certeza de que deseja sair do sistema?",
                "Confirmar saída",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (resposta != DialogResult.Yes)
                e.Cancel = true;
        }

        // Monta o painel principal.
        private void CriarLayout()
        {
            Controls.Clear();

            pnlMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 245,
                BackColor = _azulEscuro
            };

            Panel pnlCabecalho = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.White
            };

            pnlConteudo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _fundo,
                AutoScroll = true
            };

            Controls.Add(pnlConteudo);
            Controls.Add(pnlCabecalho);
            Controls.Add(pnlMenu);

            CriarMenu();
            CriarCabecalho(pnlCabecalho);
            MostrarInicio();
        }

        // Atualiza menus e cartoes apos alterar permissoes.
        private void AtualizarPainel()
        {
            pnlMenu.Controls.Clear();
            CriarMenu();
            MostrarInicio();
        }

        private void CriarMenu()
        {
            Label lblSistema = new Label
            {
                Text = "PIM IV",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(25, 25),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "PAINEL ADMINISTRATIVO",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                Location = new Point(27, 65),
                AutoSize = true
            };

            pnlMenu.Controls.Add(lblSistema);
            pnlMenu.Controls.Add(lblSubtitulo);

            int posicaoY = 130;

            AdicionarBotaoMenu(
                "⌂  Início",
                posicaoY,
                MostrarInicio);

            posicaoY += 55;

            if (TemPermissao("GERENCIAR_USUARIOS"))
            {
                AdicionarBotaoMenu(
                    "♙  Usuários",
                    posicaoY,
                    AbrirGerenciamentoUsuarios);

                posicaoY += 55;
            }

            if (TemPermissao("GERENCIAR_ACESSOS"))
            {
                AdicionarBotaoMenu(
                    "⚿  Acessos",
                    posicaoY,
                    AbrirControleAcessos);

                posicaoY += 55;
            }

            if (TemPermissao("CONSULTAR_AUDITORIA"))
            {
                AdicionarBotaoMenu(
                    "◷  Atividades",
                    posicaoY,
                    AbrirAtividades);

                posicaoY += 55;
            }

            AdicionarModuloMenu(
                "Turmas",
                "GERENCIAR_TURMAS",
                ref posicaoY);

            AdicionarModuloMenu(
                "Disciplinas",
                "GERENCIAR_DISCIPLINAS",
                ref posicaoY);

            AdicionarModuloMenu(
                "Matrículas",
                "GERENCIAR_MATRICULAS",
                ref posicaoY);

            AdicionarModuloMenu(
                "Professores",
                "ALOCAR_PROFESSORES",
                ref posicaoY);

            AdicionarModuloMenu(
                "Calendário",
                "GERENCIAR_CALENDARIO",
                ref posicaoY);

            Button btnSair = new Button
            {
                Text = "↪  Sair",
                Dock = DockStyle.Bottom,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 51, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(22, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btnSair.FlatAppearance.BorderSize = 0;
            btnSair.Click += (sender, e) => Close();

            pnlMenu.Controls.Add(btnSair);
            btnSair.BringToFront();
        }

        private void AdicionarModuloMenu(
            string nomeModulo,
            string permissao,
            ref int posicaoY)
        {
            if (!TemPermissao(permissao))
                return;

            AdicionarBotaoMenu(
                "›  " + nomeModulo,
                posicaoY,
                () => MostrarModulo(nomeModulo, permissao));

            posicaoY += 55;
        }

        private void AdicionarBotaoMenu(
            string texto,
            int posicaoY,
            Action acao)
        {
            Button botao = new Button
            {
                Text = texto,
                Location = new Point(10, posicaoY),
                Size = new Size(225, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = _azulEscuro,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            botao.FlatAppearance.BorderSize = 0;

            botao.MouseEnter += (sender, e) =>
                botao.BackColor = _azul;

            botao.MouseLeave += (sender, e) =>
                botao.BackColor = _azulEscuro;

            botao.Click += (sender, e) => acao();

            pnlMenu.Controls.Add(botao);
        }

        private void CriarCabecalho(Panel cabecalho)
        {
            Label lblSistema = new Label
            {
                Text = "Plataforma Acadêmica",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Location = new Point(30, 22),
                AutoSize = true
            };

            string perfil = _usuario.IdPerfil switch
            {
                1 => "Administrador",
                2 => "Coordenador",
                3 => "Professor",
                4 => "Aluno",
                _ => "Usuário"
            };

            Label lblUsuario = new Label
            {
                Text = $"{_usuario.NomeCompleto} | {perfil}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Right,
                Width = 320,
                TextAlign = ContentAlignment.MiddleCenter
            };

            cabecalho.Controls.Add(lblSistema);
            cabecalho.Controls.Add(lblUsuario);
        }

        private void MostrarInicio()
        {
            pnlConteudo.Controls.Clear();

            Label lblBoasVindas = new Label
            {
                Text = $"Bem-vindo(a), {_usuario.NomeCompleto}!",
                Font = new Font("Segoe UI", 23, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Location = new Point(35, 35),
                AutoSize = true
            };

            Label lblDescricao = new Label
            {
                Text = "Selecione uma funcionalidade para começar.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray,
                Location = new Point(38, 90),
                AutoSize = true
            };

            pnlConteudo.Controls.Add(lblBoasVindas);
            pnlConteudo.Controls.Add(lblDescricao);

            int indice = 0;

            AdicionarCartaoPermitido(
                "Usuários",
                "Gerenciamento de contas",
                "GERENCIAR_USUARIOS",
                AbrirGerenciamentoUsuarios,
                ref indice);

            AdicionarCartaoPermitido(
                "Acessos",
                "Perfis e permissões",
                "GERENCIAR_ACESSOS",
                AbrirControleAcessos,
                ref indice);

            AdicionarCartaoPermitido(
                "Atividades",
                "Registros do sistema",
                "CONSULTAR_AUDITORIA",
                AbrirAtividades,
                ref indice);

            AdicionarCartaoModulo(
                "Turmas",
                "Organização acadêmica",
                "GERENCIAR_TURMAS",
                ref indice);

            AdicionarCartaoModulo(
                "Disciplinas",
                "Gerenciamento de disciplinas",
                "GERENCIAR_DISCIPLINAS",
                ref indice);

            AdicionarCartaoModulo(
                "Matrículas",
                "Alunos e transferências",
                "GERENCIAR_MATRICULAS",
                ref indice);

            AdicionarCartaoModulo(
                "Professores",
                "Vínculos acadêmicos",
                "ALOCAR_PROFESSORES",
                ref indice);

            AdicionarCartaoModulo(
                "Calendário",
                "Cronograma acadêmico",
                "GERENCIAR_CALENDARIO",
                ref indice);
        }

        private void AdicionarCartaoModulo(
            string titulo,
            string descricao,
            string permissao,
            ref int indice)
        {
            AdicionarCartaoPermitido(
                titulo,
                descricao,
                permissao,
                () => MostrarModulo(titulo, permissao),
                ref indice);
        }

        private void AdicionarCartaoPermitido(
            string titulo,
            string descricao,
            string permissao,
            Action acao,
            ref int indice)
        {
            if (!TemPermissao(permissao))
                return;

            int coluna = indice % 2;
            int linha = indice / 2;

            int x = 38 + coluna * 290;
            int y = 155 + linha * 195;

            CriarCartao(
                titulo,
                descricao,
                x,
                y,
                acao);

            indice++;
        }

        private void CriarCartao(
            string titulo,
            string descricao,
            int x,
            int y,
            Action acao)
        {
            Panel cartao = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(265, 165),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };

            Label lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Location = new Point(20, 30),
                AutoSize = true
            };

            Label lblDescricao = new Label
            {
                Text = descricao,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(22, 75),
                AutoSize = true
            };

            cartao.Controls.Add(lblTitulo);
            cartao.Controls.Add(lblDescricao);

            cartao.Click += (sender, e) => acao();
            lblTitulo.Click += (sender, e) => acao();
            lblDescricao.Click += (sender, e) => acao();

            pnlConteudo.Controls.Add(cartao);
        }

        private void AbrirGerenciamentoUsuarios()
        {
            if (!Autorizar("GERENCIAR_USUARIOS"))
                return;

            try
            {
                using FrmGerenciarUsuarios formulario =
                    new FrmGerenciarUsuarios(_usuario);

                formulario.ShowDialog(this);
                AtualizarPainel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir o gerenciamento de usuários.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AbrirControleAcessos()
        {
            if (!Autorizar("GERENCIAR_ACESSOS"))
                return;

            try
            {
                using FrmControleAcessos formulario =
                    new FrmControleAcessos(_usuario);

                formulario.ShowDialog(this);

                // Recarrega as permissoes apos fechar a tela.
                AtualizarPainel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir o controle de acessos.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AbrirAtividades()
        {
            if (!Autorizar("CONSULTAR_AUDITORIA"))
                return;

            try
            {
                // Abre o novo formulario de auditoria.
                using FrmLogsAtividades formulario =
                    new FrmLogsAtividades(_usuario);

                formulario.ShowDialog(this);
                AtualizarPainel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir os registros de atividades.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Modulos academicos ainda em desenvolvimento.
        private void MostrarModulo(
            string nomeModulo,
            string permissao)
        {
            if (!Autorizar(permissao))
                return;

            pnlConteudo.Controls.Clear();

            lblTituloPagina = new Label
            {
                Text = nomeModulo,
                Font = new Font("Segoe UI", 23, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Location = new Point(35, 35),
                AutoSize = true
            };

            Label lblDescricao = new Label
            {
                Text = "Área de gerenciamento do módulo selecionado.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray,
                Location = new Point(38, 90),
                AutoSize = true
            };

            pnlConteudo.Controls.Add(lblTituloPagina);
            pnlConteudo.Controls.Add(lblDescricao);
        }
    }
}
