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
    // Gerencia matrículas, transferências e situações acadêmicas.
    public class FrmGerenciarMatriculas : Form
    {
        private const int PerfilCoordenador = 2;

        private const string PermissaoGerenciarMatriculas =
            "GERENCIAR_MATRICULAS";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _corFundo =
            Color.FromArgb(244, 247, 251);

        private ComboBox cmbAluno = null!;
        private ComboBox cmbTurma = null!;
        private ComboBox cmbSituacao = null!;
        private ComboBox cmbFiltroTurma = null!;

        private TextBox txtPesquisa = null!;
        private DataGridView dgvMatriculas = null!;
        private Label lblTotal = null!;

        private Button btnAcoes = null!;
        private Button btnLimpar = null!;
        private Button btnAtualizar = null!;

        private ContextMenuStrip menuAcoes = null!;

        private int _idMatriculaSelecionada;
        private int _idAlunoSelecionado;

        private bool _carregando;

        private readonly System.Windows.Forms.Timer _timerPesquisa = new()
        {
            Interval = 450
        };

        // Inicializa o formulário com o usuário autenticado.
        public FrmGerenciarMatriculas(Usuario usuario)
        {
            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            CriarInterface();
            ConfigurarEventos();
        }

        // Define as propriedades visuais da janela.
        private void ConfigurarFormulario()
        {
            Text = "PIM IV - Gerenciamento de Matrículas";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1200, 800);
            MinimumSize = new Size(850, 600);

            Font = new Font("Segoe UI", 10);
            BackColor = _corFundo;
        }

        // Associa os eventos aos métodos responsáveis.
        private void ConfigurarEventos()
        {
            Shown += AoExibirFormulario;

            btnAcoes.Click += (_, _) => ExibirMenuAcoes();

            btnLimpar.Click += (_, _) => LimparCampos();

            btnAtualizar.Click += (_, _) => AtualizarDados();

            txtPesquisa.TextChanged += (_, _) =>
            {
                if (!_carregando)
                {
                    _timerPesquisa.Stop();
                    _timerPesquisa.Start();
                }
            };

            _timerPesquisa.Tick += (_, _) =>
            {
                _timerPesquisa.Stop();
                CarregarMatriculas();
            };

            cmbFiltroTurma.SelectedIndexChanged += (_, _) =>
            {
                if (!_carregando)
                {
                    _timerPesquisa.Stop();
                    CarregarMatriculas();
                }
            };

            dgvMatriculas.SelectionChanged += (_, _) =>
                SelecionarMatricula();

            FormClosed += (_, _) =>
            {
                _timerPesquisa.Stop();
                _timerPesquisa.Dispose();
                menuAcoes.Dispose();
            };
        }

        // Valida o acesso antes de carregar as informações.
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

        // Verifica o perfil e a permissão do coordenador.
        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == PerfilCoordenador &&
                    _permissaoService.UsuarioTemPermissao(
                        _usuario,
                        PermissaoGerenciarMatriculas);

                if (!autorizado)
                {
                    ExibirMensagem(
                        "Somente coordenadores autorizados podem gerenciar matrículas.",
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

        // Organiza as áreas principais da interface.
        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(20),
                BackColor = _corFundo
            };

            principal.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 115));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Gerenciamento de Matrículas",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI",
                    21,
                    FontStyle.Bold),
                ForeColor = _corAzul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            CriarAreaCadastro(principal);
            CriarAreaPesquisa(principal);
            CriarAreaFiltroTurma(principal);
            CriarAreaTabela(principal);
            CriarAreaBotoes(principal);
        }

        // Cria os campos de aluno, turma e situação.
        private void CriarAreaCadastro(
            TableLayoutPanel principal)
        {
            TableLayoutPanel cadastro = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 40));

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 40));

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 20));

            cadastro.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            cadastro.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            cadastro.Controls.Add(
                CriarRotulo("Aluno / RA"), 0, 0);

            cadastro.Controls.Add(
                CriarRotulo("Turma"), 1, 0);

            cadastro.Controls.Add(
                CriarRotulo("Situação"), 2, 0);

            cmbAluno = CriarCombo();
            cmbTurma = CriarCombo();
            cmbSituacao = CriarCombo();

            cmbSituacao.Items.AddRange(new object[]
            {
                "Ativa",
                "Trancada",
                "Concluida",
                "Cancelada"
            });

            cmbSituacao.SelectedItem = "Ativa";

            cadastro.Controls.Add(cmbAluno, 0, 1);
            cadastro.Controls.Add(cmbTurma, 1, 1);
            cadastro.Controls.Add(cmbSituacao, 2, 1);

            principal.Controls.Add(cadastro, 0, 1);
        }

        // Cria o painel de pesquisa.
        private void CriarAreaPesquisa(
            TableLayoutPanel principal)
        {
            TableLayoutPanel pesquisa =
                CriarPainelHorizontal(100);

            pesquisa.Controls.Add(
                CriarRotulo("Pesquisar:"), 0, 0);

            txtPesquisa = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 150,
                PlaceholderText =
                    "Pesquisar por nome, RA ou turma...",
                Margin = new Padding(5, 5, 0, 0)
            };

            pesquisa.Controls.Add(txtPesquisa, 1, 0);

            principal.Controls.Add(pesquisa, 0, 2);
        }

        // Cria o filtro independente por turma.
        private void CriarAreaFiltroTurma(
            TableLayoutPanel principal)
        {
            TableLayoutPanel filtro =
                CriarPainelHorizontal(155);

            filtro.Controls.Add(
                CriarRotulo("Filtrar por turma:"), 0, 0);

            cmbFiltroTurma = CriarCombo();

            filtro.Controls.Add(cmbFiltroTurma, 1, 0);

            principal.Controls.Add(filtro, 0, 3);
        }

        // Padroniza os painéis horizontais.
        private static TableLayoutPanel CriarPainelHorizontal(
            int larguraRotulo)
        {
            TableLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15, 8, 15, 8),
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

        // Cria a tabela de matrículas.
        private void CriarAreaTabela(
            TableLayoutPanel principal)
        {
            TableLayoutPanel areaTabela = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };

            areaTabela.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            dgvMatriculas = new DataGridView
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
                AutoGenerateColumns = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            ConfigurarEstiloTabela();

            lblTotal = new Label
            {
                Text = "Matrículas encontradas: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            areaTabela.Controls.Add(dgvMatriculas, 0, 0);
            areaTabela.Controls.Add(lblTotal, 0, 1);

            principal.Controls.Add(areaTabela, 0, 4);
        }

        // Configura a aparência da tabela.
        private void ConfigurarEstiloTabela()
        {
            dgvMatriculas.ColumnHeadersHeight = 35;

            dgvMatriculas.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvMatriculas.RowTemplate.Height = 30;

            dgvMatriculas.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            dgvMatriculas.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);
        }

        // Cria os botões de gerenciamento.
        private void CriarAreaBotoes(
            TableLayoutPanel principal)
        {
            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 0),
                Margin = Padding.Empty
            };

            btnAcoes = CriarBotao(
                "Ações da matrícula  ▼", 205);

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

        // Cria o menu de operações acadêmicas.
        private void CriarMenuAcoes()
        {
            menuAcoes = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 10),
                ShowImageMargin = false
            };

            AdicionarAcao(
                "Matricular aluno",
                MatricularAluno);

            menuAcoes.Items.Add(
                new ToolStripSeparator());

            AdicionarAcao(
                "Transferir turma",
                TransferirTurma);

            menuAcoes.Items.Add(
                new ToolStripSeparator());

            AdicionarAcao(
                "Alterar situação",
                AlterarSituacao);
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

        // Exibe o menu de ações.
        private void ExibirMenuAcoes()
        {
            menuAcoes.Show(
                btnAcoes,
                new Point(0, 0),
                ToolStripDropDownDirection.AboveRight);
        }

        // Padroniza os rótulos.
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
                Margin = new Padding(5, 5, 10, 5)
            };
        }

        // Padroniza os botões do formulário.
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

        // Atualiza os campos e a tabela pela API.
        private void AtualizarDados()
        {
            _timerPesquisa.Stop();

            int filtroAnterior = ObterId(cmbFiltroTurma);

            try
            {
                _carregando = true;

                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    "matriculas/opcoes");

                DataTable alunos = ConverterOpcoes(
                    resposta.GetProperty("alunos"),
                    "IdAluno");

                DataTable turmas = ConverterOpcoes(
                    resposta.GetProperty("turmas"),
                    "IdTurma");

                DataTable filtroTurmas = ConverterOpcoes(
                    resposta.GetProperty("filtroTurmas"),
                    "IdTurma");

                ConfigurarFonteCombo(
                    cmbAluno,
                    alunos,
                    "IdAluno");

                ConfigurarFonteCombo(
                    cmbTurma,
                    turmas,
                    "IdTurma");

                DataRow todasTurmas = filtroTurmas.NewRow();

                todasTurmas["IdTurma"] = 0;
                todasTurmas["NomeExibicao"] =
                    "Todas as turmas";

                filtroTurmas.Rows.InsertAt(
                    todasTurmas,
                    0);

                ConfigurarFonteCombo(
                    cmbFiltroTurma,
                    filtroTurmas,
                    "IdTurma");

                cmbFiltroTurma.SelectedValue =
                    filtroAnterior;

                if (cmbFiltroTurma.SelectedIndex < 0)
                {
                    cmbFiltroTurma.SelectedIndex = 0;
                }
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
                _carregando = false;
            }

            CarregarMatriculas();
            LimparCampos();
        }

        // Converte as opções retornadas pela API.
        private static DataTable ConverterOpcoes(
            JsonElement registros,
            string campoIdentificador)
        {
            DataTable tabela = new();

            tabela.Columns.Add(
                campoIdentificador,
                typeof(int));

            tabela.Columns.Add(
                "NomeExibicao",
                typeof(string));

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        campoIdentificador).GetInt32(),

                    registro.GetProperty(
                        "NomeExibicao").GetString()
                            ?? string.Empty);
            }

            return tabela;
        }

        // Configura a fonte de dados dos campos.
        private static void ConfigurarFonteCombo(
            ComboBox combo,
            DataTable tabela,
            string campoIdentificador)
        {
            combo.DisplayMember = "NomeExibicao";
            combo.ValueMember = campoIdentificador;
            combo.DataSource = tabela;
        }

        // Consulta as matrículas conforme os filtros.
        private void CarregarMatriculas()
        {
            if (_carregando)
            {
                return;
            }

            try
            {
                _carregando = true;

                int idTurma = ObterId(cmbFiltroTurma);

                string pesquisa = Uri.EscapeDataString(
                    txtPesquisa.Text.Trim());

                string rota =
                    $"matriculas?idTurma={idTurma}" +
                    $"&pesquisa={pesquisa}";

                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    rota);

                DataTable tabela = ConverterMatriculas(
                    resposta.GetProperty("matriculas"));

                AtualizarTabelaMatriculas(tabela);
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao consultar matrículas",
                    ex);
            }
            finally
            {
                _carregando = false;
            }
        }

        // Converte os registros da API em uma tabela.
        private static DataTable ConverterMatriculas(
            JsonElement registros)
        {
            DataTable tabela = new();

            tabela.Columns.Add(
                "IdMatricula",
                typeof(int));

            tabela.Columns.Add(
                "IdAluno",
                typeof(int));

            tabela.Columns.Add(
                "IdTurma",
                typeof(int));

            tabela.Columns.Add(
                "RA",
                typeof(string));

            tabela.Columns.Add(
                "Aluno",
                typeof(string));

            tabela.Columns.Add(
                "Turma",
                typeof(string));

            tabela.Columns.Add(
                "NomeTurma",
                typeof(string));

            tabela.Columns.Add(
                "Situacao",
                typeof(string));

            tabela.Columns.Add(
                "DataMatricula",
                typeof(DateTime));

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        "IdMatricula").GetInt32(),

                    registro.GetProperty(
                        "IdAluno").GetInt32(),

                    registro.GetProperty(
                        "IdTurma").GetInt32(),

                    registro.GetProperty(
                        "RA").GetString(),

                    registro.GetProperty(
                        "Aluno").GetString(),

                    registro.GetProperty(
                        "Turma").GetString(),

                    registro.GetProperty(
                        "NomeTurma").GetString(),

                    registro.GetProperty(
                        "Situacao").GetString(),

                    registro.GetProperty(
                        "DataMatricula").GetDateTime());
            }

            return tabela;
        }

        // Atualiza os registros exibidos.
        private void AtualizarTabelaMatriculas(
            DataTable tabela)
        {
            dgvMatriculas.DataSource = tabela;

            ConfigurarColunasMatriculas();

            dgvMatriculas.ClearSelection();

            _idMatriculaSelecionada = 0;
            _idAlunoSelecionado = 0;

            lblTotal.Text =
                $"Matrículas encontradas: {tabela.Rows.Count}";
        }

        // Configura os títulos e oculta os identificadores.
        private void ConfigurarColunasMatriculas()
        {
            string[] colunasOcultas =
            {
                "IdMatricula",
                "IdAluno",
                "IdTurma"
            };

            foreach (string coluna in colunasOcultas)
            {
                if (dgvMatriculas.Columns.Contains(coluna))
                {
                    dgvMatriculas.Columns[coluna]!
                        .Visible = false;
                }
            }

            DefinirCabecalho("RA", "RA");
            DefinirCabecalho("Aluno", "Aluno");
            DefinirCabecalho("Turma", "Código da turma");
            DefinirCabecalho("NomeTurma", "Nome da turma");
            DefinirCabecalho("Situacao", "Situação");

            DefinirCabecalho(
                "DataMatricula",
                "Data da matrícula");

            if (dgvMatriculas.Columns.Contains("DataMatricula"))
            {
                dgvMatriculas.Columns["DataMatricula"]!
                    .DefaultCellStyle.Format =
                        "dd/MM/yyyy";
            }
        }

        // Define o título de uma coluna.
        private void DefinirCabecalho(
            string nomeColuna,
            string titulo)
        {
            if (dgvMatriculas.Columns.Contains(nomeColuna))
            {
                dgvMatriculas.Columns[nomeColuna]!
                    .HeaderText = titulo;
            }
        }

        // Preenche os campos com a matrícula selecionada.
        private void SelecionarMatricula()
        {
            if (_carregando)
            {
                return;
            }

            if (dgvMatriculas.SelectedRows.Count == 0)
            {
                _idMatriculaSelecionada = 0;
                _idAlunoSelecionado = 0;
                return;
            }

            try
            {
                DataGridViewRow linha =
                    dgvMatriculas.SelectedRows[0];

                _idMatriculaSelecionada =
                    Convert.ToInt32(
                        linha.Cells["IdMatricula"].Value);

                _idAlunoSelecionado =
                    Convert.ToInt32(
                        linha.Cells["IdAluno"].Value);

                int idTurma =
                    Convert.ToInt32(
                        linha.Cells["IdTurma"].Value);

                cmbAluno.SelectedValue =
                    _idAlunoSelecionado;

                cmbTurma.SelectedValue = idTurma;

                cmbSituacao.SelectedItem =
                    Convert.ToString(
                        linha.Cells["Situacao"].Value)
                    ?? "Ativa";

                // Impede trocar o aluno de uma matrícula existente.
                cmbAluno.Enabled = false;
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao selecionar matrícula",
                    ex);
            }
        }

        // Obtém o identificador selecionado.
        private static int ObterId(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        // Valida a seleção de aluno e turma.
        private static bool ValidarSelecaoMatricula(
            int idAluno,
            int idTurma)
        {
            if (idAluno > 0 && idTurma > 0)
            {
                return true;
            }

            ExibirMensagem(
                "Selecione um aluno e uma turma.",
                "Dados necessários",
                MessageBoxIcon.Information);

            return false;
        }

        // Cria uma matrícula pela API.
        private void MatricularAluno()
        {
            if (_idMatriculaSelecionada > 0)
            {
                ExibirMensagem(
                    "Clique em Novo / Limpar antes de realizar uma matrícula.",
                    "Nova matrícula",
                    MessageBoxIcon.Information);

                return;
            }

            int idAluno = ObterId(cmbAluno);
            int idTurma = ObterId(cmbTurma);

            if (!ValidarSelecaoMatricula(
                idAluno,
                idTurma))
            {
                return;
            }

            if (cmbSituacao.SelectedItem?.ToString() != "Ativa")
            {
                ExibirMensagem(
                    "Uma nova matrícula deve iniciar com a situação Ativa.",
                    "Situação inválida",
                    MessageBoxIcon.Warning);

                return;
            }

            if (!ConfirmarOperacao(
                $"Deseja matricular {cmbAluno.Text} " +
                $"na turma {cmbTurma.Text}?",
                "Confirmar matrícula"))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Post,
                    "matriculas",
                    new
                    {
                        idAluno,
                        idTurma
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Matrícula realizada com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao matricular aluno",
                    ex);
            }
        }

        // Transfere a matrícula utilizando a procedure pela API.
        private void TransferirTurma()
        {
            if (!ValidarMatriculaSelecionada())
            {
                return;
            }

            int idTurma = ObterId(cmbTurma);

            if (idTurma <= 0)
            {
                ExibirMensagem(
                    "Selecione a turma de destino.",
                    "Dados necessários",
                    MessageBoxIcon.Information);

                return;
            }

            DataGridViewRow linha =
                dgvMatriculas.SelectedRows[0];

            int idTurmaAtual = Convert.ToInt32(
                linha.Cells["IdTurma"].Value);

            string situacaoAtual = Convert.ToString(
                linha.Cells["Situacao"].Value)
                ?? string.Empty;

            if (situacaoAtual != "Ativa")
            {
                ExibirMensagem(
                    "Somente matrículas ativas podem ser transferidas.",
                    "Transferência indisponível",
                    MessageBoxIcon.Warning);

                return;
            }

            if (idTurmaAtual == idTurma)
            {
                ExibirMensagem(
                    "Selecione uma turma diferente da atual.",
                    "Transferência",
                    MessageBoxIcon.Information);

                return;
            }

            if (!ConfirmarOperacao(
                $"Deseja transferir o aluno para {cmbTurma.Text}?\n\n" +
                "O histórico da turma anterior será preservado.",
                "Confirmar transferência"))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Put,
                    $"matriculas/{_idMatriculaSelecionada}/transferencia",
                    new
                    {
                        idTurma
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Transferência realizada com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao transferir aluno",
                    ex);
            }
        }

        // Altera a situação acadêmica da matrícula.
        private void AlterarSituacao()
        {
            if (!ValidarMatriculaSelecionada())
            {
                return;
            }

            string situacao =
                cmbSituacao.SelectedItem?.ToString()
                ?? string.Empty;

            if (!SituacaoValida(situacao))
            {
                ExibirMensagem(
                    "Selecione uma situação acadêmica válida.",
                    "Situação inválida",
                    MessageBoxIcon.Warning);

                return;
            }

            string situacaoAtual = Convert.ToString(
                dgvMatriculas.SelectedRows[0]
                    .Cells["Situacao"].Value)
                ?? string.Empty;

            if (situacaoAtual == situacao)
            {
                ExibirMensagem(
                    "A matrícula já possui essa situação.",
                    "Alterar situação",
                    MessageBoxIcon.Information);

                return;
            }

            if (!ConfirmarOperacao(
                $"Deseja alterar a situação de {situacaoAtual} " +
                $"para {situacao}?",
                "Confirmar alteração"))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Patch,
                    $"matriculas/{_idMatriculaSelecionada}/situacao",
                    new
                    {
                        situacao
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Situação atualizada com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao alterar situação",
                    ex);
            }
        }

        // Valida a seleção de uma matrícula existente.
        private bool ValidarMatriculaSelecionada()
        {
            if (_idMatriculaSelecionada > 0 &&
                dgvMatriculas.SelectedRows.Count == 1)
            {
                return true;
            }

            ExibirMensagem(
                "Selecione uma matrícula na tabela.",
                "Seleção necessária",
                MessageBoxIcon.Information);

            return false;
        }

        // Verifica as situações acadêmicas permitidas.
        private static bool SituacaoValida(
            string situacao)
        {
            return situacao == "Ativa" ||
                   situacao == "Trancada" ||
                   situacao == "Concluida" ||
                   situacao == "Cancelada";
        }

        // Atualiza a consulta após uma operação.
        private void AtualizarConsultaAposOperacao()
        {
            CarregarMatriculas();
            LimparCampos();
        }

        // Limpa os campos para uma nova operação.
        private void LimparCampos()
        {
            _idMatriculaSelecionada = 0;
            _idAlunoSelecionado = 0;

            dgvMatriculas.ClearSelection();

            cmbAluno.Enabled = true;
            cmbAluno.SelectedIndex = -1;
            cmbTurma.SelectedIndex = -1;

            cmbSituacao.SelectedItem = "Ativa";
        }

        // Executa requisições autenticadas pela API.
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

                    resultado =
                        documento.RootElement.Clone();
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

        // Recupera a mensagem retornada pela API.
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

        // Solicita confirmação antes de modificar registros.
        private static bool ConfirmarOperacao(
            string mensagem,
            string titulo)
        {
            return MessageBox.Show(
                mensagem,
                titulo,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)
                == DialogResult.Yes;
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