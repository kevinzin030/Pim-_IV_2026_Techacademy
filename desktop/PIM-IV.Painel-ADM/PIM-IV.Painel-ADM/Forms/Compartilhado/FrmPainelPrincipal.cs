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
        private const int PerfilAdministrador = 1;
        private const int PerfilCoordenador = 2;

        private const int LarguraMenu = 245;
        private const int AlturaCabecalho = 65;
        private const int EspacamentoMenu = 50;

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

        // Recebe o usuário autenticado e prepara o painel.
        public FrmPainelPrincipal(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            CriarLayout();

            FormClosing += FrmPainelPrincipal_FormClosing;
        }

        // Define a aparência e o título conforme o perfil.
        private void ConfigurarFormulario()
        {
            Text = _usuario.IdPerfil == PerfilCoordenador
                ? "TechAcademy - Painel do Coordenador"
                : "TechAcademy - Painel Administrativo";

            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(800, 540);

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = _fundo;
        }

        // Consulta as permissões do usuário sem liberar acesso em caso de erro.
        private bool TemPermissao(string codigo)
        {
            try
            {
                return _permissaoService.UsuarioTemPermissao(
                    _usuario,
                    codigo);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Verifica a autorização antes de abrir um módulo.
        private bool Autorizar(string codigo)
        {
            try
            {
                if (_permissaoService.UsuarioTemPermissao(
                    _usuario,
                    codigo))
                {
                    return true;
                }

                ExibirMensagemPainel(
                    "Você não possui permissão para acessar este módulo.",
                    "Acesso negado",
                    MessageBoxIcon.Warning);

                AtualizarPainel();

                return false;
            }
            catch (Exception ex)
            {
                ExibirMensagemPainel(
                    "Não foi possível verificar sua permissão:\n" +
                    ex.Message,
                    "Erro de autorização",
                    MessageBoxIcon.Error);

                return false;
            }
        }

        // Solicita confirmação antes de encerrar o painel.
        private void FrmPainelPrincipal_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
            {
                return;
            }

            DialogResult resposta = MessageBox.Show(
                "Tem certeza de que deseja sair do sistema?",
                "Confirmar saída",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (resposta != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        // Cria as três áreas principais da interface.
        private void CriarLayout()
        {
            Controls.Clear();

            pnlMenu = CriarPainelMenu();
            pnlConteudo = CriarPainelConteudo();

            Panel pnlCabecalho = CriarPainelCabecalho();

            lblTituloPagina = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                Font = new Font(
                    "Segoe UI",
                    23,
                    FontStyle.Bold),
                ForeColor = _azulEscuro
            };

            Controls.Add(pnlConteudo);
            Controls.Add(pnlCabecalho);
            Controls.Add(pnlMenu);

            CriarMenu();
            CriarCabecalho(pnlCabecalho);
            MostrarInicio();
        }

        // Cria a área lateral de navegação.
        private Panel CriarPainelMenu()
        {
            return new Panel
            {
                Dock = DockStyle.Left,
                Width = LarguraMenu,
                BackColor = _azulEscuro
            };
        }

        // Cria a área central onde os módulos serão apresentados.
        private Panel CriarPainelConteudo()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _fundo,
                AutoScroll = true
            };
        }

        // Cria o cabeçalho superior do sistema.
        private static Panel CriarPainelCabecalho()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = AlturaCabecalho,
                BackColor = Color.White
            };
        }

        // Reconstrói o menu e retorna à página inicial.
        private void AtualizarPainel()
        {
            pnlMenu.Controls.Clear();

            CriarMenu();
            MostrarInicio();
        }

        // Monta o menu lateral de acordo com as permissões.
        private void CriarMenu()
        {
            pnlMenu.Controls.Clear();

            Panel rodape = CriarRodapeMenu();

            Panel areaRolavel = new()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = _azulEscuro
            };

            pnlMenu.Controls.Add(areaRolavel);
            pnlMenu.Controls.Add(rodape);

            AdicionarIdentificacaoMenu(areaRolavel);

            int posicaoY = 130;

            AdicionarBotaoMenu(
                areaRolavel,
                "⌂  Início",
                posicaoY,
                MostrarInicio);

            posicaoY += EspacamentoMenu;

            AdicionarOpcaoMenuPermitida(
                areaRolavel,
                "♙  Usuários",
                "GERENCIAR_USUARIOS",
                AbrirGerenciamentoUsuarios,
                ref posicaoY);

            AdicionarOpcaoMenuPermitida(
                areaRolavel,
                "⚿  Acessos",
                "GERENCIAR_ACESSOS",
                AbrirControleAcessos,
                ref posicaoY);

            AdicionarOpcaoMenuPermitida(
                areaRolavel,
                "◷  Atividades",
                "CONSULTAR_AUDITORIA",
                AbrirAtividades,
                ref posicaoY);

            if (_usuario.IdPerfil == PerfilCoordenador)
            {
                AdicionarOpcaoMenuPermitida(
                    areaRolavel,
                    "›  Turmas",
                    "GERENCIAR_TURMAS",
                    AbrirGerenciamentoTurmas,
                    ref posicaoY);

                // Mantém o acesso unificado às disciplinas.
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

        // Cria o rodapé com a opção de sair do sistema.
        private Panel CriarRodapeMenu()
        {
            Panel rodape = new()
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(33, 51, 80)
            };

            Button btnSair = new()
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

            return rodape;
        }

        // Exibe o nome do sistema e o tipo de painel.
        private void AdicionarIdentificacaoMenu(Panel areaMenu)
        {
            Label lblSistema = new()
            {
                Text = "TechAcademy",
                ForeColor = Color.White,
                Font = new Font(
                    "Segoe UI",
                    17,
                    FontStyle.Bold),
                Location = new Point(20, 25),
                AutoSize = true
            };

            Label lblSubtitulo = new()
            {
                Text = _usuario.IdPerfil == PerfilCoordenador
                    ? "PAINEL DO COORDENADOR"
                    : "PAINEL ADMINISTRATIVO",

                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                Location = new Point(22, 65),
                AutoSize = true
            };

            areaMenu.Controls.Add(lblSistema);
            areaMenu.Controls.Add(lblSubtitulo);
        }

        // Adiciona uma opção somente quando o usuário possui permissão.
        private void AdicionarOpcaoMenuPermitida(
            Panel areaMenu,
            string texto,
            string permissao,
            Action acao,
            ref int posicaoY)
        {
            if (!TemPermissao(permissao))
            {
                return;
            }

            AdicionarBotaoMenu(
                areaMenu,
                texto,
                posicaoY,
                acao);

            posicaoY += EspacamentoMenu;
        }

        // Adiciona um módulo acadêmico ao menu.
        private void AdicionarModuloMenu(
            Panel areaMenu,
            string nomeModulo,
            string permissao,
            ref int posicaoY)
        {
            if (!TemPermissao(permissao))
            {
                return;
            }

            AdicionarBotaoMenu(
                areaMenu,
                "›  " + nomeModulo,
                posicaoY,
                () => AbrirModuloAcademico(
                    nomeModulo,
                    permissao));

            posicaoY += EspacamentoMenu;
        }

        // Cria botões de navegação com aparência padronizada.
        private void AdicionarBotaoMenu(
            Panel areaMenu,
            string texto,
            int posicaoY,
            Action acao)
        {
            Button botao = new()
            {
                Text = texto,
                Location = new Point(10, posicaoY),
                Size = new Size(225, 45),

                Anchor =
                    AnchorStyles.Top |
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

        // Exibe o nome e o perfil do usuário autenticado.
        private void CriarCabecalho(Panel cabecalho)
        {
            Label lblSistema = new()
            {
                Text = "TechAcademy | Plataforma Acadêmica",

                Font = new Font(
                    "Segoe UI",
                    15,
                    FontStyle.Bold),

                ForeColor = _azulEscuro,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(30, 0, 0, 0),
                AutoEllipsis = true
            };

            Label lblUsuario = new()
            {
                Text =
                    $"{_usuario.NomeCompleto} | {ObterNomePerfil()}",

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

        // Converte o identificador do perfil em um nome legível.
        private string ObterNomePerfil()
        {
            return _usuario.IdPerfil switch
            {
                1 => "Administrador",
                2 => "Coordenador",
                3 => "Professor",
                4 => "Aluno",
                _ => "Usuário"
            };
        }

        // Apresenta a página inicial do painel.
        private void MostrarInicio()
        {
            pnlConteudo.Controls.Clear();

            lblTituloPagina.Text = "Início";

            TableLayoutPanel estrutura = CriarEstruturaInicio();

            Panel apresentacao = CriarApresentacaoInicio();

            FlowLayoutPanel cartoes = CriarAreaCartoes();

            estrutura.Controls.Add(apresentacao, 0, 0);
            estrutura.Controls.Add(cartoes, 0, 1);

            pnlConteudo.Controls.Add(estrutura);

            AdicionarCartoesInicio(cartoes);
        }

        // Organiza a apresentação e os cartões em duas linhas.
        private TableLayoutPanel CriarEstruturaInicio()
        {
            TableLayoutPanel estrutura = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20, 18, 20, 18),
                BackColor = _fundo
            };

            estrutura.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            estrutura.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 105));

            estrutura.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            return estrutura;
        }

        // Exibe a saudação e a orientação inicial.
        private Panel CriarApresentacaoInicio()
        {
            Panel apresentacao = new()
            {
                Dock = DockStyle.Fill
            };

            Label lblBoasVindas = new()
            {
                Text = $"Bem-vindo(a), {_usuario.NomeCompleto}!",

                Font = new Font(
                    "Segoe UI",
                    23,
                    FontStyle.Bold),

                ForeColor = _azulEscuro,
                Dock = DockStyle.Top,
                Height = 65,
                AutoEllipsis = true
            };

            Label lblDescricao = new()
            {
                Text = "Selecione uma funcionalidade para começar.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Top,
                Height = 30
            };

            apresentacao.Controls.Add(lblDescricao);
            apresentacao.Controls.Add(lblBoasVindas);

            return apresentacao;
        }

        // Cria a área rolável que receberá os cartões.
        private static FlowLayoutPanel CriarAreaCartoes()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = Padding.Empty
            };
        }

        // Adiciona os cartões conforme o perfil e as permissões.
        private void AdicionarCartoesInicio(
            FlowLayoutPanel cartoes)
        {
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

            if (_usuario.IdPerfil == PerfilCoordenador)
            {
                AdicionarCartaoPermitido(
                    cartoes,
                    "Turmas",
                    "Cadastro e gerenciamento de turmas",
                    "GERENCIAR_TURMAS",
                    AbrirGerenciamentoTurmas,
                    ref indice);

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

        // Adiciona um cartão de módulo acadêmico.
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
                    titulo,
                    permissao),
                ref indice);
        }

        // Exibe o cartão somente quando a permissão está disponível.
        private void AdicionarCartaoPermitido(
            FlowLayoutPanel areaCartoes,
            string titulo,
            string descricao,
            string permissao,
            Action acao,
            ref int indice)
        {
            if (!TemPermissao(permissao))
            {
                return;
            }

            CriarCartao(
                areaCartoes,
                titulo,
                descricao,
                acao);

            indice++;
        }

        // Cria um cartão clicável para acessar um módulo.
        private void CriarCartao(
            FlowLayoutPanel areaCartoes,
            string titulo,
            string descricao,
            Action acao)
        {
            Panel cartao = new()
            {
                Size = new Size(250, 150),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 5, 15, 15)
            };

            Label lblTitulo = new()
            {
                Text = titulo,

                Font = new Font(
                    "Segoe UI",
                    16,
                    FontStyle.Bold),

                ForeColor = _azulEscuro,
                Location = new Point(20, 30),
                Size = new Size(225, 40),
                AutoEllipsis = true
            };

            Label lblDescricao = new()
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

        // Abre o gerenciamento de usuários após verificar a permissão.
        private void AbrirGerenciamentoUsuarios()
        {
            if (!Autorizar("GERENCIAR_USUARIOS"))
            {
                return;
            }

            try
            {
                FrmGerenciarUsuarios formulario =
                    new(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Usuários";
            }
            catch (Exception ex)
            {
                ExibirErroAberturaModulo(
                    "gerenciamento de usuários",
                    ex);
            }
        }

        // Abre o gerenciamento de perfis e permissões.
        private void AbrirControleAcessos()
        {
            if (!Autorizar("GERENCIAR_ACESSOS"))
            {
                return;
            }

            try
            {
                FrmControleAcessos formulario =
                    new(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Acessos";
            }
            catch (Exception ex)
            {
                ExibirErroAberturaModulo(
                    "controle de acessos",
                    ex);
            }
        }

        // Abre a consulta dos registros de atividades.
        private void AbrirAtividades()
        {
            if (!Autorizar("CONSULTAR_AUDITORIA"))
            {
                return;
            }

            try
            {
                FrmLogsAtividades formulario =
                    new(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Atividades";
            }
            catch (Exception ex)
            {
                ExibirErroAberturaModulo(
                    "registros de atividades",
                    ex);
            }
        }

        // Abre o gerenciamento de turmas exclusivamente ao Coordenador.
        private void AbrirGerenciamentoTurmas()
        {
            if (_usuario.IdPerfil != PerfilCoordenador)
            {
                ExibirMensagemPainel(
                    "O gerenciamento de turmas é exclusivo do Coordenador.",
                    "Acesso negado",
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Autorizar("GERENCIAR_TURMAS"))
            {
                return;
            }

            try
            {
                FrmGerenciarTurmas formulario =
                    new(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Turmas";
            }
            catch (Exception ex)
            {
                ExibirErroAberturaModulo(
                    "gerenciamento de turmas",
                    ex);
            }
        }

        // Exibe uma área informativa para módulos sem interface própria.
        private void MostrarModulo(
            string nomeModulo,
            string permissao)
        {
            if (!Autorizar(permissao))
            {
                return;
            }

            pnlConteudo.Controls.Clear();

            lblTituloPagina = new Label
            {
                Text = nomeModulo,

                Font = new Font(
                    "Segoe UI",
                    23,
                    FontStyle.Bold),

                ForeColor = _azulEscuro,
                Location = new Point(35, 35),
                AutoSize = true
            };

            Label lblDescricao = new()
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

        // Padroniza os erros apresentados ao abrir módulos.
        private static void ExibirErroAberturaModulo(
            string nomeModulo,
            Exception erro)
        {
            ExibirMensagemPainel(
                $"Não foi possível abrir o {nomeModulo}.\n" +
                erro.Message,
                "Erro ao abrir módulo",
                MessageBoxIcon.Error);
        }

        // Centraliza as mensagens utilizadas pelo painel.
        private static void ExibirMensagemPainel(
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