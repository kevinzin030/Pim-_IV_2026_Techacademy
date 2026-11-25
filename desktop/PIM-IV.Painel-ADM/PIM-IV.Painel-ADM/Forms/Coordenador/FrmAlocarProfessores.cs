using System;
using System.Data;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia a alocação e a substituição de professores pela API.
    public class FrmAlocarProfessores : Form
    {
        private const int PerfilCoordenador = 2;

        private const string PermissaoAlocarProfessores =
            "ALOCAR_PROFESSORES";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _corFundo =
            Color.FromArgb(244, 247, 251);

        private ComboBox cmbTurma = null!;
        private ComboBox cmbDisciplina = null!;
        private ComboBox cmbProfessor = null!;
        private ComboBox cmbFiltroTurma = null!;

        private TextBox txtPesquisa = null!;
        private DataGridView dgvAlocacoes = null!;

        private Label lblResponsavel = null!;
        private Label lblTotal = null!;
        private Label lblSelecao = null!;

        private Button btnAcoes = null!;
        private Button btnLimpar = null!;
        private Button btnAtualizar = null!;

        private ContextMenuStrip menuAcoes = null!;

        private readonly System.Windows.Forms.Timer _timerPesquisa = new()
        {
            Interval = 450
        };

        private bool _atualizandoDados;
        private int _idAlocacaoSelecionada;

        // Inicializa o formulário com o usuário autenticado.
        public FrmAlocarProfessores(Usuario usuario)
        {
            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            CriarInterface();
            ConfigurarEventos();

            Shown += AoExibirFormulario;

            FormClosed += (_, _) =>
            {
                _timerPesquisa.Stop();
                _timerPesquisa.Dispose();
                menuAcoes.Dispose();
            };
        }

        // Define as propriedades visuais da janela.
        private void ConfigurarFormulario()
        {
            Text = "PIM IV - Alocação de Professores";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1250, 820);
            MinimumSize = new Size(850, 600);

            Font = new Font("Segoe UI", 10);
            BackColor = _corFundo;
        }

        // Valida o acesso antes de consultar os dados.
        private void AoExibirFormulario(
            object? sender,
            EventArgs e)
        {
            if (!VerificarAcesso())
            {
                Close();
                return;
            }

            AtualizarDados();
        }

        // Verifica a autorização do coordenador.
        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == PerfilCoordenador &&
                    _permissaoService.UsuarioTemPermissao(
                        _usuario,
                        PermissaoAlocarProfessores);

                if (!autorizado)
                {
                    ExibirMensagem(
                        "Você não possui permissão para alocar professores.",
                        "Acesso negado",
                        MessageBoxIcon.Warning);
                }

                return autorizado;
            }
            catch (Exception ex)
            {
                MostrarErro("Erro de autorização", ex);
                return false;
            }
        }

        // Organiza os componentes principais da interface.
        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 6,
                BackColor = _corFundo
            };

            principal.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 145));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 80));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Alocação de Professores",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI",
                    21,
                    FontStyle.Bold),
                ForeColor = _corAzul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            CriarAreaSelecao(principal);
            CriarAreaPesquisa(principal);
            CriarAreaFiltroTurma(principal);
            CriarAreaTabela(principal);
            CriarAreaAcoes(principal);
        }

        // Cria os campos utilizados para selecionar uma alocação.
        private void CriarAreaSelecao(
            TableLayoutPanel principal)
        {
            TableLayoutPanel cadastro = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            for (int indice = 0; indice < 3; indice++)
            {
                cadastro.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Percent,
                        33.333f));
            }

            cadastro.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            cadastro.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            cadastro.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            cadastro.Controls.Add(
                CriarRotulo("Turma"), 0, 0);

            cadastro.Controls.Add(
                CriarRotulo("Disciplina"), 1, 0);

            cadastro.Controls.Add(
                CriarRotulo("Professor / RP"), 2, 0);

            cmbTurma = CriarCombo();
            cmbDisciplina = CriarCombo();
            cmbProfessor = CriarCombo();

            cadastro.Controls.Add(cmbTurma, 0, 1);
            cadastro.Controls.Add(cmbDisciplina, 1, 1);
            cadastro.Controls.Add(cmbProfessor, 2, 1);

            lblResponsavel = new Label
            {
                Text = "Selecione uma turma e uma disciplina.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            cadastro.Controls.Add(lblResponsavel, 0, 2);
            cadastro.SetColumnSpan(lblResponsavel, 3);

            principal.Controls.Add(cadastro, 0, 1);
        }

        // Cria o campo de pesquisa das alocações.
        private void CriarAreaPesquisa(
            TableLayoutPanel principal)
        {
            TableLayoutPanel pesquisa = CriarPainelHorizontal(
                95,
                new Padding(15, 12, 15, 10));

            pesquisa.Controls.Add(
                CriarRotulo("Pesquisar:"), 0, 0);

            txtPesquisa = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 150,
                PlaceholderText =
                    "Pesquisar por turma, disciplina, professor ou RP...",
                Margin = new Padding(5, 3, 0, 3)
            };

            pesquisa.Controls.Add(txtPesquisa, 1, 0);

            principal.Controls.Add(pesquisa, 0, 2);
        }

        // Cria o filtro independente de turmas.
        private void CriarAreaFiltroTurma(
            TableLayoutPanel principal)
        {
            TableLayoutPanel filtro = CriarPainelHorizontal(
                155,
                new Padding(15, 12, 15, 10));

            filtro.Controls.Add(
                CriarRotulo("Filtrar por turma:"), 0, 0);

            cmbFiltroTurma = CriarCombo();

            cmbFiltroTurma.Margin =
                new Padding(5, 3, 0, 3);

            filtro.Controls.Add(cmbFiltroTurma, 1, 0);

            principal.Controls.Add(filtro, 0, 3);
        }

        // Padroniza os painéis de pesquisa e filtro.
        private static TableLayoutPanel CriarPainelHorizontal(
            int larguraRotulo,
            Padding espacamento)
        {
            TableLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = espacamento,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            painel.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    larguraRotulo));

            painel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            painel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            return painel;
        }

        // Cria a tabela de alocações e seus indicadores.
        private void CriarAreaTabela(
            TableLayoutPanel principal)
        {
            TableLayoutPanel areaTabela = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };

            areaTabela.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            dgvAlocacoes = new DataGridView
            {
                Dock = DockStyle.Fill,
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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoGenerateColumns = true
            };

            ConfigurarEstiloTabela();

            lblSelecao = new Label
            {
                Text = "Nenhuma alocação selecionada.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            lblTotal = new Label
            {
                Text = "Alocações: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            areaTabela.Controls.Add(dgvAlocacoes, 0, 0);
            areaTabela.Controls.Add(lblSelecao, 0, 1);
            areaTabela.Controls.Add(lblTotal, 0, 2);

            principal.Controls.Add(areaTabela, 0, 4);
        }

        // Configura a aparência da tabela.
        private void ConfigurarEstiloTabela()
        {
            dgvAlocacoes.ColumnHeadersHeight = 35;

            dgvAlocacoes.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvAlocacoes.RowTemplate.Height = 30;

            dgvAlocacoes.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            dgvAlocacoes.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);
        }

        // Cria os botões de navegação e operações.
        private void CriarAreaAcoes(
            TableLayoutPanel principal)
        {
            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 12, 0, 0),
                Margin = Padding.Empty
            };

            btnAcoes = CriarBotao(
                "Ações da alocação  ▼", 205);

            btnAtualizar = CriarBotao(
                "Atualizar", 125);

            btnLimpar = CriarBotao(
                "Novo / Limpar", 145);

            botoes.Controls.Add(btnAcoes);
            botoes.Controls.Add(btnAtualizar);
            botoes.Controls.Add(btnLimpar);

            principal.Controls.Add(botoes, 0, 5);

            CriarMenuAcoes();
        }

        // Cria o menu de alocação e substituição.
        private void CriarMenuAcoes()
        {
            menuAcoes = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 10),
                ShowImageMargin = false
            };

            AdicionarAcao(
                "Alocar professor",
                AlocarProfessor);

            AdicionarAcao(
                "Substituir professor",
                SubstituirProfessor);
        }

        // Adiciona uma operação ao menu.
        private void AdicionarAcao(
            string texto,
            Action acao)
        {
            ToolStripMenuItem item = new(texto);

            item.Click += (_, _) => acao();

            menuAcoes.Items.Add(item);
        }

        // Exibe as operações disponíveis.
        private void ExibirMenuAcoes()
        {
            menuAcoes.Show(
                btnAcoes,
                new Point(0, 0),
                ToolStripDropDownDirection.AboveRight);
        }

        // Associa os eventos aos métodos responsáveis.
        private void ConfigurarEventos()
        {
            cmbTurma.SelectedIndexChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    CarregarDisciplinas();
                }
            };

            cmbDisciplina.SelectedIndexChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    AtualizarResponsavel();
                }
            };

            cmbFiltroTurma.SelectedIndexChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    _timerPesquisa.Stop();
                    CarregarAlocacoes();
                }
            };

            // Aguarda o término da digitação antes de consultar a API.
            txtPesquisa.TextChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    _timerPesquisa.Stop();
                    _timerPesquisa.Start();
                }
            };

            _timerPesquisa.Tick += (_, _) =>
            {
                _timerPesquisa.Stop();
                CarregarAlocacoes();
            };

            dgvAlocacoes.SelectionChanged += (_, _) =>
                SelecionarAlocacao();

            btnAcoes.Click += (_, _) =>
                ExibirMenuAcoes();

            btnLimpar.Click += (_, _) =>
                LimparSelecao();

            btnAtualizar.Click += (_, _) =>
                AtualizarDados();
        }

        // Cria rótulos com a identidade visual do sistema.
        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                ForeColor = _corAzul,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        // Padroniza os campos de seleção.
        private static ComboBox CriarCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5, 5, 15, 5)
            };
        }

        // Cria botões com a aparência do sistema.
        private Button CriarBotao(
            string texto,
            int largura)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 43,
                BackColor = _corAzul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            botao.FlatAppearance.BorderSize = 0;

            botao.MouseEnter += (_, _) =>
                botao.BackColor =
                    Color.FromArgb(30, 75, 145);

            botao.MouseLeave += (_, _) =>
                botao.BackColor = _corAzul;

            return botao;
        }

        // Associa uma tabela a um campo de seleção.
        private static void ConfigurarCombo(
            ComboBox combo,
            DataTable dados,
            string campoExibicao,
            string campoValor)
        {
            combo.DisplayMember = campoExibicao;
            combo.ValueMember = campoValor;
            combo.DataSource = dados;
        }

        // Converte as opções recebidas da API em uma tabela.
        private static DataTable ConverterOpcoes(
            JsonElement registros,
            string campoIdentificador)
        {
            DataTable tabela = new();

            tabela.Columns.Add(
                campoIdentificador,
                typeof(int));

            tabela.Columns.Add(
                "Descricao",
                typeof(string));

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        campoIdentificador).GetInt32(),

                    registro.GetProperty(
                        "Descricao").GetString()
                            ?? string.Empty);
            }

            return tabela;
        }

        // Atualiza as turmas, professores e filtros pela API.
        private void AtualizarDados()
        {
            _timerPesquisa.Stop();

            int filtroAnterior = ObterId(cmbFiltroTurma);

            try
            {
                _atualizandoDados = true;

                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    "alocacoes/opcoes");

                DataTable turmas = ConverterOpcoes(
                    resposta.GetProperty("turmas"),
                    "IdTurma");

                DataTable professores = ConverterOpcoes(
                    resposta.GetProperty("professores"),
                    "IdProfessor");

                DataTable filtroTurmas = ConverterOpcoes(
                    resposta.GetProperty("filtroTurmas"),
                    "IdTurma");

                ConfigurarCombo(
                    cmbTurma,
                    turmas,
                    "Descricao",
                    "IdTurma");

                ConfigurarCombo(
                    cmbProfessor,
                    professores,
                    "Descricao",
                    "IdProfessor");

                cmbTurma.SelectedIndex =
                    cmbTurma.Items.Count > 0 ? 0 : -1;

                cmbProfessor.SelectedIndex = -1;

                ConfigurarFiltroTurmas(
                    filtroTurmas,
                    filtroAnterior);
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar dados",
                    ex);

                return;
            }
            finally
            {
                _atualizandoDados = false;
            }

            CarregarDisciplinas();
            CarregarAlocacoes();
            LimparSelecao();
        }

        // Configura o filtro com a opção de todas as turmas.
        private void ConfigurarFiltroTurmas(
            DataTable turmas,
            int filtroAnterior)
        {
            DataRow todasTurmas = turmas.NewRow();

            todasTurmas["IdTurma"] = 0;
            todasTurmas["Descricao"] = "Todas as turmas";

            turmas.Rows.InsertAt(todasTurmas, 0);

            ConfigurarCombo(
                cmbFiltroTurma,
                turmas,
                "Descricao",
                "IdTurma");

            cmbFiltroTurma.SelectedValue = filtroAnterior;

            if (cmbFiltroTurma.SelectedIndex < 0)
            {
                cmbFiltroTurma.SelectedIndex = 0;
            }
        }

        // Consulta as disciplinas vinculadas à turma selecionada.
        private void CarregarDisciplinas(
            bool consultarResponsavel = true)
        {
            int idTurma = ObterId(cmbTurma);

            try
            {
                _atualizandoDados = true;

                // Limpa os dados anteriores para evitar seleções inválidas.
                cmbDisciplina.DataSource = null;

                if (idTurma > 0)
                {
                    JsonElement resposta = ExecutarApi(
                        HttpMethod.Get,
                        $"alocacoes/turmas/{idTurma}/disciplinas");

                    DataTable disciplinas = ConverterOpcoes(
                        resposta.GetProperty("disciplinas"),
                        "IdTurmaDisciplina");

                    ConfigurarCombo(
                        cmbDisciplina,
                        disciplinas,
                        "Descricao",
                        "IdTurmaDisciplina");
                }
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar disciplinas",
                    ex);
            }
            finally
            {
                _atualizandoDados = false;
            }

            if (consultarResponsavel)
            {
                AtualizarResponsavel();
            }
        }

        // Obtém o identificador selecionado.
        private static int ObterId(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        // Consulta o professor responsável pela disciplina.
        private void AtualizarResponsavel()
        {
            int idVinculo = ObterId(cmbDisciplina);

            lblResponsavel.Text =
                "Nenhum professor responsável selecionado.";

            if (idVinculo <= 0)
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    $"alocacoes/vinculos/{idVinculo}/responsavel");

                string? responsavel =
                    resposta.GetProperty("responsavel").GetString();

                lblResponsavel.Text =
                    string.IsNullOrWhiteSpace(responsavel)
                        ? "Esta disciplina ainda não possui professor responsável."
                        : "Professor responsável: " + responsavel;
            }
            catch (Exception ex)
            {
                lblResponsavel.Text =
                    "Não foi possível consultar o responsável.";

                MostrarErro(
                    "Erro ao consultar responsável",
                    ex);
            }
        }

        // Consulta as alocações conforme os filtros informados.
        private void CarregarAlocacoes()
        {
            if (_atualizandoDados)
            {
                return;
            }

            try
            {
                _atualizandoDados = true;

                int idTurma = ObterId(cmbFiltroTurma);

                string pesquisa = Uri.EscapeDataString(
                    txtPesquisa.Text.Trim());

                string rota =
                    $"alocacoes?idTurma={idTurma}" +
                    $"&pesquisa={pesquisa}";

                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    rota);

                DataTable alocacoes = ConverterAlocacoes(
                    resposta.GetProperty("alocacoes"));

                AtualizarTabela(alocacoes);
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao consultar alocações",
                    ex);
            }
            finally
            {
                _atualizandoDados = false;
            }
        }

        // Converte os registros retornados pela API.
        private static DataTable ConverterAlocacoes(
            JsonElement registros)
        {
            DataTable tabela = new();

            tabela.Columns.Add(
                "IdAlocacao",
                typeof(int));

            tabela.Columns.Add(
                "IdProfessor",
                typeof(int));

            tabela.Columns.Add(
                "IdTurmaDisciplina",
                typeof(int));

            tabela.Columns.Add(
                "IdTurma",
                typeof(int));

            tabela.Columns.Add(
                "Turma",
                typeof(string));

            tabela.Columns.Add(
                "Disciplina",
                typeof(string));

            tabela.Columns.Add(
                "RP",
                typeof(string));

            tabela.Columns.Add(
                "Professor",
                typeof(string));

            tabela.Columns.Add(
                "DataAlocacao",
                typeof(DateTime));

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        "IdAlocacao").GetInt32(),

                    registro.GetProperty(
                        "IdProfessor").GetInt32(),

                    registro.GetProperty(
                        "IdTurmaDisciplina").GetInt32(),

                    registro.GetProperty(
                        "IdTurma").GetInt32(),

                    registro.GetProperty(
                        "Turma").GetString(),

                    registro.GetProperty(
                        "Disciplina").GetString(),

                    registro.GetProperty(
                        "RP").GetString(),

                    registro.GetProperty(
                        "Professor").GetString(),

                    registro.GetProperty(
                        "DataAlocacao").GetDateTime());
            }

            return tabela;
        }

        // Atualiza a tabela e seus indicadores.
        private void AtualizarTabela(DataTable alocacoes)
        {
            dgvAlocacoes.DataSource = alocacoes;

            ConfigurarColunasAlocacoes();

            dgvAlocacoes.ClearSelection();

            _idAlocacaoSelecionada = 0;

            lblSelecao.Text =
                "Nenhuma alocação selecionada.";

            lblTotal.Text =
                $"Alocações atuais: {alocacoes.Rows.Count}";
        }

        // Oculta os identificadores e configura os títulos.
        private void ConfigurarColunasAlocacoes()
        {
            string[] colunasOcultas =
            {
                "IdAlocacao",
                "IdProfessor",
                "IdTurmaDisciplina",
                "IdTurma"
            };

            foreach (string nome in colunasOcultas)
            {
                if (dgvAlocacoes.Columns.Contains(nome))
                {
                    dgvAlocacoes.Columns[nome]!.Visible = false;
                }
            }

            DefinirCabecalho("Turma", "Turma");
            DefinirCabecalho("Disciplina", "Disciplina");
            DefinirCabecalho("RP", "RP");
            DefinirCabecalho("Professor", "Professor");

            DefinirCabecalho(
                "DataAlocacao",
                "Data da alocação");

            if (dgvAlocacoes.Columns.Contains("DataAlocacao"))
            {
                dgvAlocacoes.Columns["DataAlocacao"]!
                    .DefaultCellStyle.Format =
                        "dd/MM/yyyy HH:mm";
            }
        }

        // Define o título de uma coluna existente.
        private void DefinirCabecalho(
            string nomeColuna,
            string titulo)
        {
            if (dgvAlocacoes.Columns.Contains(nomeColuna))
            {
                dgvAlocacoes.Columns[nomeColuna]!
                    .HeaderText = titulo;
            }
        }

        // Preenche os campos com a alocação selecionada.
        private void SelecionarAlocacao()
        {
            if (_atualizandoDados ||
                dgvAlocacoes.SelectedRows.Count == 0)
            {
                return;
            }

            try
            {
                DataGridViewRow linha =
                    dgvAlocacoes.SelectedRows[0];

                int idAlocacao = Convert.ToInt32(
                    linha.Cells["IdAlocacao"].Value);

                int idTurma = Convert.ToInt32(
                    linha.Cells["IdTurma"].Value);

                int idVinculo = Convert.ToInt32(
                    linha.Cells["IdTurmaDisciplina"].Value);

                int idProfessor = Convert.ToInt32(
                    linha.Cells["IdProfessor"].Value);

                // Seleciona a turma antes de consultar as disciplinas.
                _atualizandoDados = true;

                try
                {
                    cmbTurma.SelectedValue = idTurma;
                }
                finally
                {
                    _atualizandoDados = false;
                }

                // Evita consultar o responsável antes da seleção correta.
                CarregarDisciplinas(false);

                _atualizandoDados = true;

                try
                {
                    cmbDisciplina.SelectedValue = idVinculo;
                    cmbProfessor.SelectedValue = idProfessor;
                }
                finally
                {
                    _atualizandoDados = false;
                }

                _idAlocacaoSelecionada = idAlocacao;

                lblSelecao.Text =
                    $"Selecionada: {linha.Cells["Professor"].Value} " +
                    $"| {linha.Cells["Disciplina"].Value}";

                AtualizarResponsavel();
            }
            catch (Exception ex)
            {
                _atualizandoDados = false;

                MostrarErro(
                    "Erro ao selecionar alocação",
                    ex);
            }
        }

        // Limpa a seleção sem alterar os registros.
        private void LimparSelecao()
        {
            _idAlocacaoSelecionada = 0;

            dgvAlocacoes.ClearSelection();

            if (cmbProfessor.Items.Count > 0)
            {
                cmbProfessor.SelectedIndex = -1;
            }

            lblSelecao.Text =
                "Nenhuma alocação selecionada.";

            AtualizarResponsavel();
        }

        // Verifica se os campos necessários foram selecionados.
        private static bool ValidarSelecao(
            int idVinculo,
            int idProfessor)
        {
            if (idVinculo > 0 && idProfessor > 0)
            {
                return true;
            }

            ExibirMensagem(
                "Selecione uma turma, uma disciplina e um professor.",
                "Dados necessários",
                MessageBoxIcon.Information);

            return false;
        }

        // Aloca um professor em uma disciplina sem responsável.
        private void AlocarProfessor()
        {
            int idVinculo = ObterId(cmbDisciplina);
            int idProfessor = ObterId(cmbProfessor);

            if (!ValidarSelecao(
                idVinculo,
                idProfessor))
            {
                return;
            }

            if (!ConfirmarOperacao(
                "Confirmar a alocação deste professor?",
                "Confirmar alocação"))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Post,
                    "alocacoes",
                    new
                    {
                        idVinculo,
                        idProfessor
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Professor alocado com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao alocar professor",
                    ex);
            }
        }

        // Substitui o professor responsável pela disciplina.
        private void SubstituirProfessor()
        {
            int idVinculo = ObterId(cmbDisciplina);
            int idProfessor = ObterId(cmbProfessor);

            if (!ValidarSelecao(
                idVinculo,
                idProfessor))
            {
                return;
            }

            if (!ConfirmarOperacao(
                "Deseja substituir o professor responsável?\n\n" +
                "O vínculo anterior será preservado no histórico.",
                "Confirmar substituição"))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Put,
                    $"alocacoes/vinculos/{idVinculo}",
                    new
                    {
                        idProfessor
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Professor substituído com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao substituir professor",
                    ex);
            }
        }

        // Atualiza os dados após uma operação concluída.
        private void AtualizarConsultaAposOperacao()
        {
            AtualizarResponsavel();
            CarregarAlocacoes();
        }

        // Executa requisições utilizando o token da sessão.
        private static JsonElement ExecutarApi(
            HttpMethod metodo,
            string rota,
            object? corpo = null)
        {
            return Task.Run(async () =>
            {
                using HttpRequestMessage requisicao =
                    new(metodo, rota);

                if (corpo != null)
                {
                    requisicao.Content =
                        JsonContent.Create(corpo);
                }

                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.SendAsync(
                        requisicao);

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                JsonElement resultado;

                try
                {
                    using JsonDocument documento =
                        JsonDocument.Parse(conteudo);

                    resultado = documento.RootElement.Clone();
                }
                catch (JsonException)
                {
                    throw new HttpRequestException(
                        "A API retornou uma resposta inválida.",
                        null,
                        resposta.StatusCode);
                }

                if (!resposta.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        ObterMensagem(
                            resultado,
                            "Não foi possível concluir a operação."),
                        null,
                        resposta.StatusCode);
                }

                return resultado;

            }).GetAwaiter().GetResult();
        }

        // Obtém a mensagem retornada pela API.
        private static string ObterMensagem(
            JsonElement resposta,
            string mensagemPadrao)
        {
            if (resposta.ValueKind == JsonValueKind.Object &&
                resposta.TryGetProperty(
                    "mensagem",
                    out JsonElement mensagem) &&
                mensagem.ValueKind == JsonValueKind.String)
            {
                return mensagem.GetString()
                    ?? mensagemPadrao;
            }

            return mensagemPadrao;
        }

        // Solicita confirmação antes de modificar uma alocação.
        private static bool ConfirmarOperacao(
            string mensagem,
            string titulo)
        {
            return MessageBox.Show(
                mensagem,
                titulo,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        // Exibe mensagens padronizadas.
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

        // Centraliza a apresentação dos erros.
        private static void MostrarErro(
            string titulo,
            Exception ex)
        {
            ExibirMensagem(
                ex.Message,
                titulo,
                MessageBoxIcon.Error);
        }
    }
}