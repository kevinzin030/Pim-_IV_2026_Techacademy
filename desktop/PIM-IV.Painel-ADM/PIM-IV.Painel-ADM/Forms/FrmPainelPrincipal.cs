
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

            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = _usuario.IdPerfil == 2
                ? "TechAcademy - Painel do Coordenador"
                : "TechAcademy - Painel Administrativo";

            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = _fundo;

            CriarLayout();

            FormClosing += FrmPainelPrincipal_FormClosing;
        }

        // =====================================================
        // PERMISSÕES E SEGURANÇA
        // =====================================================

        private bool TemPermissao(string codigo)
        {
            try
            {
                return _permissaoService.UsuarioTemPermissao(
                    _usuario, codigo);
            }
            catch (Exception)
            {
                return false;
            }
        }

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

        // =====================================================
        // ENCERRAMENTO
        // =====================================================

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

        // =====================================================
        // LAYOUT PRINCIPAL
        // =====================================================

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

            lblTituloPagina = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 23, FontStyle.Bold),
                ForeColor = _azulEscuro
            };

            Controls.Add(pnlConteudo);
            Controls.Add(pnlCabecalho);
            Controls.Add(pnlMenu);

            CriarMenu();
            CriarCabecalho(pnlCabecalho);
            MostrarInicio();
        }

        private void AtualizarPainel()
        {
            pnlMenu.Controls.Clear();
            CriarMenu();
            MostrarInicio();
        }

        // =====================================================
        // MENU LATERAL
        // =====================================================

        private void CriarMenu()
        {
            pnlMenu.Controls.Clear();

            Panel rodape = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(33, 51, 80)
            };

            Button btnSair = new Button
            {
                Text = "↪  Sair",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = rodape.BackColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(22, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btnSair.FlatAppearance.BorderSize = 0;
            btnSair.Click += (_, _) => Close();

            rodape.Controls.Add(btnSair);

            Panel areaRolavel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = _azulEscuro
            };

            pnlMenu.Controls.Add(areaRolavel);
            pnlMenu.Controls.Add(rodape);

            Label lblSistema = new Label
            {
                Text = "TechAcademy",
                ForeColor = Color.White,
                Font = new Font(
                    "Segoe UI", 17, FontStyle.Bold),
                Location = new Point(20, 25),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = _usuario.IdPerfil == 2
                    ? "PAINEL DO COORDENADOR"
                    : "PAINEL ADMINISTRATIVO",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                Location = new Point(22, 65),
                AutoSize = true
            };

            areaRolavel.Controls.Add(lblSistema);
            areaRolavel.Controls.Add(lblSubtitulo);

            int posicaoY = 130;

            AdicionarBotaoMenu(
                areaRolavel,
                "⌂  Início",
                posicaoY,
                MostrarInicio);

            posicaoY += 55;

            if (TemPermissao("GERENCIAR_USUARIOS"))
            {
                AdicionarBotaoMenu(
                    areaRolavel,
                    "♙  Usuários",
                    posicaoY,
                    AbrirGerenciamentoUsuarios);

                posicaoY += 55;
            }

            if (TemPermissao("GERENCIAR_ACESSOS"))
            {
                AdicionarBotaoMenu(
                    areaRolavel,
                    "⚿  Acessos",
                    posicaoY,
                    AbrirControleAcessos);

                posicaoY += 55;
            }

            if (TemPermissao("CONSULTAR_AUDITORIA"))
            {
                AdicionarBotaoMenu(
                    areaRolavel,
                    "◷  Atividades",
                    posicaoY,
                    AbrirAtividades);

                posicaoY += 55;
            }

            if (_usuario.IdPerfil == 2 &&
                TemPermissao("GERENCIAR_TURMAS"))
            {
                AdicionarBotaoMenu(
                    areaRolavel,
                    "›  Turmas",
                    posicaoY,
                    AbrirGerenciamentoTurmas);

                posicaoY += 55;
            }

            // Um único acesso ao gerenciamento e à
            // vinculação de disciplinas.
            if (_usuario.IdPerfil == 2)
            {
                AdicionarModuloMenu(
                    areaRolavel,
                    "Disciplinas",
                    "GERENCIAR_DISCIPLINAS",
                    ref posicaoY);
            }

            AdicionarModuloMenu(
                areaRolavel,
                "Matrículas",
                "GERENCIAR_MATRICULAS",
                ref posicaoY);

            AdicionarModuloMenu(
                areaRolavel,
                "Professores",
                "ALOCAR_PROFESSORES",
                ref posicaoY);
        }

        private void AdicionarModuloMenu(
            Panel areaMenu,
            string nomeModulo,
            string permissao,
            ref int posicaoY)
        {
            if (!TemPermissao(permissao))
                return;

            AdicionarBotaoMenu(
                areaMenu,
                "›  " + nomeModulo,
                posicaoY,
                () => AbrirModuloAcademico(
                    nomeModulo, permissao));

            posicaoY += 55;
        }

        private void AdicionarBotaoMenu(
            Panel areaMenu,
            string texto,
            int posicaoY,
            Action acao)
        {
            Button botao = new Button
            {
                Text = texto,
                Location = new Point(10, posicaoY),
                Size = new Size(225, 45),
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = _azulEscuro,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            botao.FlatAppearance.BorderSize = 0;

            botao.MouseEnter += (_, _) =>
                botao.BackColor = _azul;

            botao.MouseLeave += (_, _) =>
                botao.BackColor = _azulEscuro;

            botao.Click += (_, _) => acao();

            areaMenu.Controls.Add(botao);
        }

        // =====================================================
        // CABEÇALHO
        // =====================================================

        private void CriarCabecalho(Panel cabecalho)
        {
            Label lblSistema = new Label
            {
                Text = "TechAcademy | Plataforma Acadêmica",
                Font = new Font(
                    "Segoe UI", 15, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(30, 0, 0, 0),
                AutoEllipsis = true
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
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };

            cabecalho.Controls.Add(lblSistema);
            cabecalho.Controls.Add(lblUsuario);
        }

        // =====================================================
        // PÁGINA INICIAL
        // =====================================================

        private void MostrarInicio()
        {
            pnlConteudo.Controls.Clear();

            lblTituloPagina.Text = "Início";

            TableLayoutPanel estrutura = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(30, 25, 30, 25),
                BackColor = _fundo
            };

            estrutura.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            estrutura.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 120));

            estrutura.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            Panel apresentacao = new Panel
            {
                Dock = DockStyle.Fill
            };

            Label lblBoasVindas = new Label
            {
                Text = $"Bem-vindo(a), {_usuario.NomeCompleto}!",
                Font = new Font(
                    "Segoe UI", 23, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Dock = DockStyle.Top,
                Height = 65,
                AutoEllipsis = true
            };

            Label lblDescricao = new Label
            {
                Text = "Selecione uma funcionalidade para começar.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Top,
                Height = 30
            };

            apresentacao.Controls.Add(lblDescricao);
            apresentacao.Controls.Add(lblBoasVindas);

            FlowLayoutPanel cartoes = new()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = Padding.Empty
            };

            estrutura.Controls.Add(apresentacao, 0, 0);
            estrutura.Controls.Add(cartoes, 0, 1);

            pnlConteudo.Controls.Add(estrutura);

            int indice = 0;

            AdicionarCartaoPermitido(
                cartoes,
                "Usuários",
                "Gerenciamento de contas",
                "GERENCIAR_USUARIOS",
                AbrirGerenciamentoUsuarios,
                ref indice);

            AdicionarCartaoPermitido(
                cartoes,
                "Acessos",
                "Perfis e permissões",
                "GERENCIAR_ACESSOS",
                AbrirControleAcessos,
                ref indice);

            AdicionarCartaoPermitido(
                cartoes,
                "Atividades",
                "Registros do sistema",
                "CONSULTAR_AUDITORIA",
                AbrirAtividades,
                ref indice);

            if (_usuario.IdPerfil == 2)
            {
                AdicionarCartaoPermitido(
                    cartoes,
                    "Turmas",
                    "Cadastro e gerenciamento de turmas",
                    "GERENCIAR_TURMAS",
                    AbrirGerenciamentoTurmas,
                    ref indice);

                // Cartão unificado: cadastro e vinculação.
                AdicionarCartaoModulo(
                    cartoes,
                    "Disciplinas",
                    "Gerenciamento e vinculação às turmas",
                    "GERENCIAR_DISCIPLINAS",
                    ref indice);
            }

            AdicionarCartaoModulo(
                cartoes,
                "Matrículas",
                "Alunos e transferências",
                "GERENCIAR_MATRICULAS",
                ref indice);

            AdicionarCartaoModulo(
                cartoes,
                "Professores",
                "Vínculos acadêmicos",
                "ALOCAR_PROFESSORES",
                ref indice);
        }

        private void AdicionarCartaoModulo(
            FlowLayoutPanel areaCartoes,
            string titulo,
            string descricao,
            string permissao,
            ref int indice)
        {
            AdicionarCartaoPermitido(
                areaCartoes,
                titulo,
                descricao,
                permissao,
                () => AbrirModuloAcademico(
                    titulo, permissao),
                ref indice);
        }

        private void AdicionarCartaoPermitido(
            FlowLayoutPanel areaCartoes,
            string titulo,
            string descricao,
            string permissao,
            Action acao,
            ref int indice)
        {
            if (!TemPermissao(permissao))
                return;

            CriarCartao(
                areaCartoes,
                titulo,
                descricao,
                acao);

            indice++;
        }

        private void CriarCartao(
            FlowLayoutPanel areaCartoes,
            string titulo,
            string descricao,
            Action acao)
        {
            Panel cartao = new Panel
            {
                Size = new Size(265, 165),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 5, 20, 20)
            };

            Label lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font(
                    "Segoe UI", 16, FontStyle.Bold),
                ForeColor = _azulEscuro,
                Location = new Point(20, 30),
                Size = new Size(225, 40),
                AutoEllipsis = true
            };

            Label lblDescricao = new Label
            {
                Text = descricao,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(22, 75),
                Size = new Size(220, 65),
                AutoEllipsis = true
            };

            cartao.Controls.Add(lblTitulo);
            cartao.Controls.Add(lblDescricao);

            cartao.Click += (_, _) => acao();
            lblTitulo.Click += (_, _) => acao();
            lblDescricao.Click += (_, _) => acao();

            areaCartoes.Controls.Add(cartao);
        }

        // =====================================================
        // GERENCIAMENTO DE USUÁRIOS
        // =====================================================

        private void AbrirGerenciamentoUsuarios()
        {
            if (!Autorizar("GERENCIAR_USUARIOS"))
                return;

            try
            {
                FrmGerenciarUsuarios formulario =
                    new FrmGerenciarUsuarios(_usuario);

                ExibirFormularioCentral(formulario);
                lblTituloPagina.Text = "Usuários";
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

        // =====================================================
        // CONTROLE DE ACESSOS
        // =====================================================

        private void AbrirControleAcessos()
        {
            if (!Autorizar("GERENCIAR_ACESSOS"))
                return;

            try
            {
                FrmControleAcessos formulario =
                    new FrmControleAcessos(_usuario);

                ExibirFormularioCentral(formulario);
                lblTituloPagina.Text = "Acessos";
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

        // =====================================================
        // REGISTROS DE ATIVIDADES
        // =====================================================

        private void AbrirAtividades()
        {
            if (!Autorizar("CONSULTAR_AUDITORIA"))
                return;

            try
            {
                FrmLogsAtividades formulario =
                    new FrmLogsAtividades(_usuario);

                ExibirFormularioCentral(formulario);
                lblTituloPagina.Text = "Atividades";
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

        // =====================================================
        // GERENCIAMENTO DE TURMAS
        // =====================================================

        private void AbrirGerenciamentoTurmas()
        {
            if (_usuario.IdPerfil != 2)
            {
                MessageBox.Show(
                    "O gerenciamento de turmas é exclusivo do Coordenador.",
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Autorizar("GERENCIAR_TURMAS"))
                return;

            try
            {
                FrmGerenciarTurmas formulario =
                    new FrmGerenciarTurmas(_usuario);

                ExibirFormularioCentral(formulario);
                lblTituloPagina.Text = "Turmas";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir o gerenciamento de turmas.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // MÓDULOS SEM INTERFACE PRÓPRIA
        // =====================================================

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
                Font = new Font(
                    "Segoe UI", 23, FontStyle.Bold),
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
