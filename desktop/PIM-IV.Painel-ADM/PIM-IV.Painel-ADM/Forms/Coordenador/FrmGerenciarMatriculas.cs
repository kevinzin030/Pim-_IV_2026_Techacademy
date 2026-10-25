using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia matrículas, transferências e situações acadêmicas.
    public class FrmGerenciarMatriculas : Form
    {
        private const int PerfilCoordenador = 2;
        private const string PermissaoGerenciarMatriculas = "GERENCIAR_MATRICULAS";

        private const string SituacaoAtiva = "Ativa";
        private const string SituacaoTrancada = "Trancada";
        private const string SituacaoConcluida = "Concluida";
        private const string SituacaoCancelada = "Cancelada";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul = Color.FromArgb(40, 92, 170);
        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);

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

            txtPesquisa.TextChanged += (_, _) => CarregarMatriculas();

            cmbFiltroTurma.SelectedIndexChanged += (_, _) =>
            {
                if (!_carregando)
                {
                    CarregarMatriculas();
                }
            };

            dgvMatriculas.SelectionChanged += (_, _) =>
                SelecionarMatricula();
        }

        // Valida o acesso antes de carregar as informações.
        private void AoExibirFormulario(object? sender, EventArgs e)
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
                new RowStyle(SizeType.Absolute, 115));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Gerenciamento de Matrículas",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
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
        private void CriarAreaCadastro(TableLayoutPanel principal)
        {
            TableLayoutPanel cadastro = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8)
            };

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 35));

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 35));

            cadastro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 30));

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
                SituacaoAtiva,
                SituacaoTrancada,
                SituacaoConcluida,
                SituacaoCancelada
            });

            cmbSituacao.SelectedItem = SituacaoAtiva;

            cadastro.Controls.Add(cmbAluno, 0, 1);
            cadastro.Controls.Add(cmbTurma, 1, 1);
            cadastro.Controls.Add(cmbSituacao, 2, 1);

            principal.Controls.Add(cadastro, 0, 1);
        }

        // Cria o campo utilizado para pesquisar matrículas.
        private void CriarAreaPesquisa(TableLayoutPanel principal)
        {
            TableLayoutPanel pesquisa = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15, 12, 15, 10),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            pesquisa.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 90));

            pesquisa.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            pesquisa.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            pesquisa.Controls.Add(
                CriarRotulo("Pesquisar:"), 0, 0);

            txtPesquisa = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText =
                    "Digite o nome, RA ou código da turma...",
                Margin = new Padding(5, 3, 0, 3)
            };

            pesquisa.Controls.Add(txtPesquisa, 1, 0);

            principal.Controls.Add(pesquisa, 0, 2);
        }

        // Cria o filtro de matrículas por turma.
        private void CriarAreaFiltroTurma(TableLayoutPanel principal)
        {
            TableLayoutPanel filtro = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15, 12, 15, 10),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            filtro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 155));

            filtro.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            filtro.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            filtro.Controls.Add(
                CriarRotulo("Filtrar por turma:"), 0, 0);

            cmbFiltroTurma = CriarCombo();

            cmbFiltroTurma.Margin =
                new Padding(5, 3, 0, 3);

            filtro.Controls.Add(cmbFiltroTurma, 1, 0);

            principal.Controls.Add(filtro, 0, 3);
        }

        // Cria a tabela de consulta e o contador de matrículas.
        private void CriarAreaTabela(TableLayoutPanel principal)
        {
            TableLayoutPanel areaTabela = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0)
            };

            areaTabela.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            areaTabela.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 35));

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
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoGenerateColumns = true
            };

            ConfigurarEstiloTabela();

            lblTotal = new Label
            {
                Text = "Matrículas: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            areaTabela.Controls.Add(dgvMatriculas, 0, 0);
            areaTabela.Controls.Add(lblTotal, 0, 1);

            principal.Controls.Add(areaTabela, 0, 4);
        }

        // Padroniza a aparência da tabela.
        private void ConfigurarEstiloTabela()
        {
            dgvMatriculas.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            dgvMatriculas.ColumnHeadersHeight = 35;

            dgvMatriculas.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvMatriculas.RowTemplate.Height = 30;

            dgvMatriculas.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);
        }

        // Cria os botões de operações acadêmicas.
        private void CriarAreaBotoes(TableLayoutPanel principal)
        {
            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 12, 0, 0),
                Margin = new Padding(0)
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

        // Configura as opções do menu de ações.
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

            menuAcoes.Items.Add(new ToolStripSeparator());

            AdicionarAcao(
                "Transferir turma",
                TransferirTurma);

            menuAcoes.Items.Add(new ToolStripSeparator());

            AdicionarAcao(
                "Alterar situação",
                AlterarSituacao);
        }

        // Reutiliza a criação de itens do menu.
        private void AdicionarAcao(
            string texto,
            Action acao)
        {
            ToolStripMenuItem item = new(texto);

            item.Click += (_, _) => acao();

            menuAcoes.Items.Add(item);
        }

        // Exibe o menu de operações acadêmicas.
        private void ExibirMenuAcoes()
        {
            menuAcoes.Show(
                btnAcoes,
                new Point(0, 0),
                ToolStripDropDownDirection.AboveRight);
        }

        // Cria rótulos com a identidade visual do sistema.
        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                ForeColor = _corAzul,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        // Cria campos de seleção padronizados.
        private static ComboBox CriarCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5, 5, 15, 5)
            };
        }

        // Cria botões com efeitos visuais padronizados.
        private Button CriarBotao(string texto, int largura)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 42,
                BackColor = _corAzul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            botao.FlatAppearance.BorderSize = 0;

            botao.MouseEnter += (_, _) =>
                botao.BackColor = Color.FromArgb(30, 75, 145);

            botao.MouseLeave += (_, _) =>
                botao.BackColor = _corAzul;

            return botao;
        }

        // Executa uma consulta e retorna seus resultados.
        private static DataTable ConsultarTabela(
            SqlConnection conexao,
            string sql)
        {
            using SqlCommand comando =
                new(sql, conexao);

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }

        // Atualiza os alunos e as turmas disponíveis.
        private void AtualizarDados()
        {
            int filtroAnterior = ObterId(cmbFiltroTurma);

            try
            {
                _carregando = true;

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                CarregarAlunos(conexao);
                CarregarTurmas(conexao);
                CarregarFiltroTurmas(conexao, filtroAnterior);
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar dados", ex);
                return;
            }
            finally
            {
                _carregando = false;
            }

            CarregarMatriculas();
            LimparCampos();
        }

        // Consulta os alunos ativos para matrícula.
        private void CarregarAlunos(SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    a.IdAluno,
                    a.MatriculaAluno + N' - ' +
                        u.NomeCompleto AS NomeExibicao
                FROM dbo.Alunos a
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                WHERE u.Ativo = 1
                ORDER BY u.NomeCompleto";

            DataTable alunos = ConsultarTabela(conexao, sql);

            ConfigurarFonteCombo(
                cmbAluno,
                alunos,
                "NomeExibicao",
                "IdAluno");
        }

        // Consulta as turmas ativas disponíveis.
        private void CarregarTurmas(SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma AS NomeExibicao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma";

            DataTable turmas = ConsultarTabela(conexao, sql);

            ConfigurarFonteCombo(
                cmbTurma,
                turmas,
                "NomeExibicao",
                "IdTurma");
        }

        // Carrega turmas ativas e inativas para consulta.
        private void CarregarFiltroTurmas(
            SqlConnection conexao,
            int filtroAnterior)
        {
            const string sql = @"
                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma +
                        CASE
                            WHEN Ativa = 0
                            THEN N' (Inativa)'
                            ELSE N''
                        END AS NomeExibicao
                FROM dbo.Turmas
                ORDER BY CodigoTurma";

            DataTable turmas = ConsultarTabela(conexao, sql);

            DataRow todasTurmas = turmas.NewRow();

            todasTurmas["IdTurma"] = 0;
            todasTurmas["NomeExibicao"] = "Todas as turmas";

            turmas.Rows.InsertAt(todasTurmas, 0);

            ConfigurarFonteCombo(
                cmbFiltroTurma,
                turmas,
                "NomeExibicao",
                "IdTurma");

            cmbFiltroTurma.SelectedValue = filtroAnterior;

            if (cmbFiltroTurma.SelectedIndex < 0)
            {
                cmbFiltroTurma.SelectedIndex = 0;
            }
        }

        // Padroniza a vinculação de dados aos ComboBoxes.
        private static void ConfigurarFonteCombo(
            ComboBox combo,
            DataTable dados,
            string campoExibicao,
            string campoValor)
        {
            combo.DisplayMember = campoExibicao;
            combo.ValueMember = campoValor;
            combo.DataSource = dados;
        }

        // Consulta matrículas utilizando pesquisa e filtro por turma.
        private void CarregarMatriculas()
        {
            if (_carregando)
            {
                return;
            }

            try
            {
                _carregando = true;

                const string sql = @"
                    SELECT
                        m.IdMatricula,
                        a.IdAluno,
                        t.IdTurma,
                        a.MatriculaAluno AS RA,
                        u.NomeCompleto AS Aluno,
                        t.CodigoTurma AS Turma,
                        t.NomeTurma AS NomeTurma,
                        m.Situacao,
                        m.DataMatricula
                    FROM dbo.Matriculas m
                    INNER JOIN dbo.Alunos a
                        ON a.IdAluno = m.IdAluno
                    INNER JOIN dbo.Usuarios u
                        ON u.IdUsuario = a.IdUsuario
                    INNER JOIN dbo.Turmas t
                        ON t.IdTurma = m.IdTurma
                    WHERE
                        (@IdTurma = 0 OR t.IdTurma = @IdTurma)
                        AND
                        (
                            @Pesquisa = N''
                            OR u.NomeCompleto LIKE
                                N'%' + @Pesquisa + N'%'
                            OR a.MatriculaAluno LIKE
                                N'%' + @Pesquisa + N'%'
                            OR t.CodigoTurma LIKE
                                N'%' + @Pesquisa + N'%'
                        )
                    ORDER BY u.NomeCompleto";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                using SqlCommand comando =
                    new(sql, conexao);

                comando.Parameters.Add(
                    "@Pesquisa",
                    SqlDbType.NVarChar,
                    150).Value = txtPesquisa.Text.Trim();

                comando.Parameters.Add(
                    "@IdTurma",
                    SqlDbType.Int).Value =
                        ObterId(cmbFiltroTurma);

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

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

        // Atualiza a tabela e limpa a seleção anterior.
        private void AtualizarTabelaMatriculas(DataTable tabela)
        {
            dgvMatriculas.DataSource = tabela;

            ConfigurarColunasMatriculas();

            dgvMatriculas.ClearSelection();

            _idMatriculaSelecionada = 0;
            _idAlunoSelecionado = 0;

            lblTotal.Text =
                $"Matrículas encontradas: {tabela.Rows.Count}";
        }

        // Configura as colunas exibidas na consulta.
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
                    dgvMatriculas.Columns[coluna]!.Visible = false;
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
                    .DefaultCellStyle.Format = "dd/MM/yyyy";
            }
        }

        // Define o título de uma coluna existente.
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
            if (_carregando ||
                dgvMatriculas.SelectedRows.Count == 0)
            {
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

                cmbAluno.SelectedValue = _idAlunoSelecionado;

                cmbTurma.SelectedValue = idTurma;

                cmbSituacao.SelectedItem =
                    Convert.ToString(
                        linha.Cells["Situacao"].Value)
                    ?? SituacaoAtiva;
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao selecionar matrícula",
                    ex);
            }
        }

        // Obtém o identificador selecionado em um ComboBox.
        private static int ObterId(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        // Verifica se aluno e turma foram selecionados.
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

        // Inicia o processo de matrícula do aluno.
        private void MatricularAluno()
        {
            int idAluno = ObterId(cmbAluno);
            int idTurma = ObterId(cmbTurma);

            if (!ValidarSelecaoMatricula(idAluno, idTurma))
            {
                return;
            }

            if (!ConfirmarOperacao(
                "Confirmar a matrícula deste aluno na turma selecionada?",
                "Confirmar matrícula"))
            {
                return;
            }

            try
            {
                bool matriculaCriada = ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        bool criada = InserirMatricula(
                            conexao,
                            transacao,
                            idAluno,
                            idTurma);

                        if (criada)
                        {
                            RegistrarAuditoria(
                                conexao,
                                transacao,
                                "CRIAR_MATRICULA",
                                $"Aluno {idAluno} matriculado " +
                                $"na turma {idTurma}.");
                        }

                        return criada;
                    });

                if (!matriculaCriada)
                {
                    ExibirMensagem(
                        "Este aluno já possui matrícula. " +
                        "Utilize Transferir turma ou Alterar situação.",
                        "Matrícula existente",
                        MessageBoxIcon.Information);

                    return;
                }

                ExibirMensagem(
                    "Matrícula realizada com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao matricular aluno", ex);
            }
        }

        // Impede a criação de matrículas duplicadas.
        private static bool InserirMatricula(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAluno,
            int idTurma)
        {
            const string sql = @"
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Matriculas WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdAluno = @IdAluno
                )
                BEGIN
                    SELECT 0;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.Matriculas
                    (
                        IdAluno,
                        IdTurma,
                        DataMatricula,
                        Situacao
                    )
                    VALUES
                    (
                        @IdAluno,
                        @IdTurma,
                        SYSDATETIME(),
                        N'Ativa'
                    );

                    SELECT 1;
                END";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdAluno",
                SqlDbType.Int).Value = idAluno;

            comando.Parameters.Add(
                "@IdTurma",
                SqlDbType.Int).Value = idTurma;

            return Convert.ToInt32(
                comando.ExecuteScalar()) == 1;
        }

        // Transfere uma matrícula ativa para outra turma.
        private void TransferirTurma()
        {
            if (!ValidarMatriculaSelecionada())
            {
                return;
            }

            int novaTurma = ObterId(cmbTurma);

            if (novaTurma <= 0)
            {
                ExibirMensagem(
                    "Selecione a turma de destino.",
                    "Dados necessários",
                    MessageBoxIcon.Information);

                return;
            }

            if (!ConfirmarOperacao(
                "Deseja transferir o aluno para a turma selecionada?\n" +
                "A matrícula atual será atualizada.",
                "Confirmar transferência"))
            {
                return;
            }

            int idMatricula = _idMatriculaSelecionada;
            int idAluno = _idAlunoSelecionado;

            try
            {
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        AtualizarTurmaMatricula(
                            conexao,
                            transacao,
                            idMatricula,
                            idAluno,
                            novaTurma);

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            "TRANSFERIR_MATRICULA",
                            $"Matrícula {idMatricula} " +
                            $"transferida para turma {novaTurma}.");
                    });

                ExibirMensagem(
                    "Transferência realizada com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao transferir aluno", ex);
            }
        }

        // Atualiza a turma sem excluir a matrícula.
        private static void AtualizarTurmaMatricula(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idMatricula,
            int idAluno,
            int novaTurma)
        {
            const string sql = @"
                UPDATE dbo.Matriculas
                SET IdTurma = @IdTurma
                WHERE IdMatricula = @IdMatricula
                  AND IdAluno = @IdAluno
                  AND IdTurma <> @IdTurma
                  AND Situacao = N'Ativa'";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdTurma",
                SqlDbType.Int).Value = novaTurma;

            comando.Parameters.Add(
                "@IdMatricula",
                SqlDbType.Int).Value = idMatricula;

            comando.Parameters.Add(
                "@IdAluno",
                SqlDbType.Int).Value = idAluno;

            int registrosAlterados = comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "A transferência não foi realizada. " +
                    "Verifique se a matrícula está ativa " +
                    "e se a turma de destino é diferente.");
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
                cmbSituacao.SelectedItem?.ToString() ?? "";

            if (!SituacaoValida(situacao))
            {
                ExibirMensagem(
                    "Selecione uma situação válida.",
                    "Dados inválidos",
                    MessageBoxIcon.Warning);

                return;
            }

            if (!ConfirmarOperacao(
                $"Alterar a situação desta matrícula para '{situacao}'?",
                "Confirmar alteração"))
            {
                return;
            }

            int idMatricula = _idMatriculaSelecionada;
            int idAluno = _idAlunoSelecionado;

            try
            {
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        AtualizarSituacaoMatricula(
                            conexao,
                            transacao,
                            idMatricula,
                            idAluno,
                            situacao);

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            "ALTERAR_SITUACAO_MATRICULA",
                            $"Matrícula {idMatricula} " +
                            $"alterada para {situacao}.");
                    });

                ExibirMensagem(
                    "Situação atualizada com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao alterar situação", ex);
            }
        }

        // Atualiza somente a situação da matrícula.
        private static void AtualizarSituacaoMatricula(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idMatricula,
            int idAluno,
            string situacao)
        {
            const string sql = @"
                UPDATE dbo.Matriculas
                SET Situacao = @Situacao
                WHERE IdMatricula = @IdMatricula
                  AND IdAluno = @IdAluno";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Situacao",
                SqlDbType.NVarChar,
                30).Value = situacao;

            comando.Parameters.Add(
                "@IdMatricula",
                SqlDbType.Int).Value = idMatricula;

            comando.Parameters.Add(
                "@IdAluno",
                SqlDbType.Int).Value = idAluno;

            if (comando.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException(
                    "Matrícula não encontrada.");
            }
        }

        // Executa uma operação dentro de uma transação SQL.
        private T ExecutarTransacao<T>(
            Func<SqlConnection, SqlTransaction, T> operacao)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction();

            try
            {
                ExigirAutorizacao(conexao, transacao);

                T resultado = operacao(conexao, transacao);

                transacao.Commit();

                return resultado;
            }
            catch
            {
                ReverterTransacao(transacao);
                throw;
            }
        }

        // Permite operações que não precisam retornar valores.
        private void ExecutarTransacao(
            Action<SqlConnection, SqlTransaction> operacao)
        {
            ExecutarTransacao(
                (conexao, transacao) =>
                {
                    operacao(conexao, transacao);
                    return true;
                });
        }

        // Atualiza a consulta após uma operação concluída.
        private void AtualizarConsultaAposOperacao()
        {
            CarregarMatriculas();
            LimparCampos();
        }

        // Verifica se existe uma matrícula selecionada.
        private bool ValidarMatriculaSelecionada()
        {
            if (_idMatriculaSelecionada > 0 &&
                _idAlunoSelecionado > 0)
            {
                return true;
            }

            ExibirMensagem(
                "Selecione uma matrícula na tabela.",
                "Seleção necessária",
                MessageBoxIcon.Information);

            return false;
        }

        // Verifica se a situação informada é permitida.
        private static bool SituacaoValida(string situacao)
        {
            return situacao == SituacaoAtiva ||
                   situacao == SituacaoTrancada ||
                   situacao == SituacaoConcluida ||
                   situacao == SituacaoCancelada;
        }

        // Limpa os campos e prepara uma nova matrícula.
        private void LimparCampos()
        {
            _idMatriculaSelecionada = 0;
            _idAlunoSelecionado = 0;

            dgvMatriculas.ClearSelection();

            if (cmbAluno.Items.Count > 0)
            {
                cmbAluno.SelectedIndex = -1;
            }

            if (cmbTurma.Items.Count > 0)
            {
                cmbTurma.SelectedIndex = -1;
            }

            cmbSituacao.SelectedItem = SituacaoAtiva;
        }

        // Revalida a autorização diretamente no banco.
        private void ExigirAutorizacao(
            SqlConnection conexao,
            SqlTransaction transacao)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON p.IdPerfil = u.IdPerfil
                INNER JOIN dbo.PerfilPermissoes pp
                    ON pp.IdPerfil = p.IdPerfil
                INNER JOIN dbo.Permissoes pm
                    ON pm.IdPermissao = pp.IdPermissao
                WHERE u.IdUsuario = @IdUsuario
                  AND u.IdPerfil = @IdPerfil
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND pm.Codigo = @Permissao
                  AND pm.Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = _usuario.IdUsuario;

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = PerfilCoordenador;

            comando.Parameters.Add(
                "@Permissao",
                SqlDbType.NVarChar,
                100).Value = PermissaoGerenciarMatriculas;

            bool autorizado =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!autorizado)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui permissão para alterar matrículas.");
            }
        }

        // Registra a atividade na mesma transação da operação.
        private void RegistrarAuditoria(
            SqlConnection conexao,
            SqlTransaction transacao,
            string acao,
            string descricao)
        {
            const string sql = @"
                INSERT INTO dbo.LogsAtividades
                (
                    IdUsuario,
                    Acao,
                    Descricao
                )
                VALUES
                (
                    @IdUsuario,
                    @Acao,
                    @Descricao
                )";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = _usuario.IdUsuario;

            comando.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                100).Value = acao;

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                500).Value = descricao;

            comando.ExecuteNonQuery();
        }

        // Reverte uma transação que ainda está ativa.
        private static void ReverterTransacao(
            SqlTransaction transacao)
        {
            if (transacao.Connection != null)
            {
                transacao.Rollback();
            }
        }

        // Solicita confirmação antes de executar uma operação.
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