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
    // Gerencia a alocação e a substituição de professores nas disciplinas.
    public class FrmAlocarProfessores : Form
    {
        private const int PerfilCoordenador = 2;
        private const string PermissaoAlocarProfessores = "ALOCAR_PROFESSORES";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul = Color.FromArgb(40, 92, 170);
        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);

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
        private void AoExibirFormulario(object? sender, EventArgs e)
        {
            if (!VerificarAcesso())
            {
                Close();
                return;
            }

            AtualizarDados();
        }

        // Verifica se o usuário possui autorização para o módulo.
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

        // Impede alterações realizadas por usuários não autorizados.
        private void ExigirAutorizacao()
        {
            bool autorizado =
                _usuario.IdPerfil == PerfilCoordenador &&
                _permissaoService.UsuarioTemPermissao(
                    _usuario,
                    PermissaoAlocarProfessores);

            if (!autorizado)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui permissão para alterar alocações.");
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
                Font = new Font("Segoe UI", 21, FontStyle.Bold),
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
        private void CriarAreaSelecao(TableLayoutPanel principal)
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
                    new ColumnStyle(SizeType.Percent, 33.333f));
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
        private void CriarAreaPesquisa(TableLayoutPanel principal)
        {
            TableLayoutPanel pesquisa = CriarPainelHorizontal(
                95,
                new Padding(15, 12, 15, 10));

            pesquisa.Controls.Add(
                CriarRotulo("Pesquisar:"), 0, 0);

            txtPesquisa = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText =
                    "Pesquisar por turma, disciplina, professor ou RP...",
                Margin = new Padding(5, 3, 0, 3)
            };

            pesquisa.Controls.Add(txtPesquisa, 1, 0);
            principal.Controls.Add(pesquisa, 0, 2);
        }

        // Cria o filtro independente de turmas.
        private void CriarAreaFiltroTurma(TableLayoutPanel principal)
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

        // Cria a tabela e os indicadores de seleção.
        private void CriarAreaTabela(TableLayoutPanel principal)
        {
            TableLayoutPanel areaTabela = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15),
                BackColor = Color.White,
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
        private void CriarAreaAcoes(TableLayoutPanel principal)
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

        // Evita repetir a criação de itens do menu.
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
                    CarregarAlocacoes();
                }
            };

            txtPesquisa.TextChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    CarregarAlocacoes();
                }
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
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
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
        private Button CriarBotao(string texto, int largura)
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
                botao.BackColor = Color.FromArgb(30, 75, 145);

            botao.MouseLeave += (_, _) =>
                botao.BackColor = _corAzul;

            return botao;
        }

        // Executa consultas que não precisam de parâmetros.
        private static DataTable ConsultarTabela(
            SqlConnection conexao,
            string sql)
        {
            using SqlCommand comando = new(sql, conexao);

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
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

        // Atualiza as turmas, os professores e o filtro.
        private void AtualizarDados()
        {
            int filtroAnterior = ObterId(cmbFiltroTurma);

            try
            {
                _atualizandoDados = true;

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                CarregarTurmas(conexao);
                CarregarProfessores(conexao);
                CarregarFiltroTurmas(conexao, filtroAnterior);
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar dados", ex);
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

        // Consulta as turmas ativas.
        private void CarregarTurmas(SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    IdTurma,
                    CodigoTurma + N' - ' +
                        NomeTurma AS Descricao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma";

            DataTable turmas = ConsultarTabela(conexao, sql);

            ConfigurarCombo(
                cmbTurma,
                turmas,
                "Descricao",
                "IdTurma");

            cmbTurma.SelectedIndex =
                cmbTurma.Items.Count > 0 ? 0 : -1;
        }

        // Consulta os professores com contas ativas.
        private void CarregarProfessores(SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    p.IdProfessor,
                    p.RegistroProfessor + N' - ' +
                        u.NomeCompleto AS Descricao
                FROM dbo.Professores p
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = p.IdUsuario
                WHERE u.Ativo = 1
                ORDER BY u.NomeCompleto";

            DataTable professores =
                ConsultarTabela(conexao, sql);

            ConfigurarCombo(
                cmbProfessor,
                professores,
                "Descricao",
                "IdProfessor");

            cmbProfessor.SelectedIndex = -1;
        }

        // Consulta todas as turmas para o filtro.
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
                        END AS Descricao
                FROM dbo.Turmas
                ORDER BY CodigoTurma";

            DataTable turmas = ConsultarTabela(conexao, sql);

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
        private void CarregarDisciplinas()
        {
            int idTurma = ObterId(cmbTurma);

            try
            {
                _atualizandoDados = true;

                const string sql = @"
                    SELECT
                        td.IdTurmaDisciplina,
                        d.CodigoDisciplina + N' - ' +
                            d.NomeDisciplina AS Descricao
                    FROM dbo.TurmaDisciplinas td
                    INNER JOIN dbo.Disciplinas d
                        ON d.IdDisciplina = td.IdDisciplina
                    WHERE td.IdTurma = @IdTurma
                      AND td.Ativa = 1
                      AND d.Ativa = 1
                    ORDER BY d.NomeDisciplina";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                using SqlCommand comando =
                    new(sql, conexao);

                comando.Parameters.Add(
                    "@IdTurma",
                    SqlDbType.Int).Value = idTurma;

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable disciplinas = new();
                disciplinas.Load(leitor);

                ConfigurarCombo(
                    cmbDisciplina,
                    disciplinas,
                    "Descricao",
                    "IdTurmaDisciplina");
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

            AtualizarResponsavel();
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
                const string sql = @"
                    SELECT
                        p.RegistroProfessor + N' - ' +
                            u.NomeCompleto
                    FROM dbo.AlocacoesProfessores ap
                    INNER JOIN dbo.Professores p
                        ON p.IdProfessor = ap.IdProfessor
                    INNER JOIN dbo.Usuarios u
                        ON u.IdUsuario = p.IdUsuario
                    WHERE ap.IdTurmaDisciplina = @IdVinculo";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                using SqlCommand comando =
                    new(sql, conexao);

                comando.Parameters.Add(
                    "@IdVinculo",
                    SqlDbType.Int).Value = idVinculo;

                conexao.Open();

                string? responsavel =
                    comando.ExecuteScalar()?.ToString();

                lblResponsavel.Text =
                    string.IsNullOrWhiteSpace(responsavel)
                        ? "Esta disciplina ainda não possui professor responsável."
                        : "Professor responsável: " + responsavel;
            }
            catch (Exception ex)
            {
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

                const string sql = @"
                    SELECT
                        ap.IdAlocacao,
                        ap.IdProfessor,
                        ap.IdTurmaDisciplina,
                        td.IdTurma,
                        t.CodigoTurma AS Turma,
                        d.NomeDisciplina AS Disciplina,
                        p.RegistroProfessor AS RP,
                        u.NomeCompleto AS Professor,
                        ap.DataAlocacao
                    FROM dbo.AlocacoesProfessores ap
                    INNER JOIN dbo.TurmaDisciplinas td
                        ON td.IdTurmaDisciplina =
                            ap.IdTurmaDisciplina
                    INNER JOIN dbo.Turmas t
                        ON t.IdTurma = td.IdTurma
                    INNER JOIN dbo.Disciplinas d
                        ON d.IdDisciplina = td.IdDisciplina
                    INNER JOIN dbo.Professores p
                        ON p.IdProfessor = ap.IdProfessor
                    INNER JOIN dbo.Usuarios u
                        ON u.IdUsuario = p.IdUsuario
                    WHERE
                        (@IdTurma = 0 OR td.IdTurma = @IdTurma)
                        AND
                        (
                            @Pesquisa = N''
                            OR t.CodigoTurma LIKE
                                N'%' + @Pesquisa + N'%'
                            OR d.NomeDisciplina LIKE
                                N'%' + @Pesquisa + N'%'
                            OR u.NomeCompleto LIKE
                                N'%' + @Pesquisa + N'%'
                            OR p.RegistroProfessor LIKE
                                N'%' + @Pesquisa + N'%'
                        )
                    ORDER BY
                        t.CodigoTurma,
                        d.NomeDisciplina";

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

                DataTable alocacoes = new();
                alocacoes.Load(leitor);

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
                    .DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
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

                // Seleciona a turma antes de carregar suas disciplinas.
                _atualizandoDados = true;

                try
                {
                    cmbTurma.SelectedValue = idTurma;
                }
                finally
                {
                    _atualizandoDados = false;
                }

                CarregarDisciplinas();

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

            if (!ValidarSelecao(idVinculo, idProfessor))
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
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        ValidarDados(
                            conexao,
                            transacao,
                            idVinculo,
                            idProfessor);

                        InserirAlocacao(
                            conexao,
                            transacao,
                            idVinculo,
                            idProfessor);

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            "ALOCAR_PROFESSOR",
                            $"Professor {idProfessor} alocado " +
                            $"ao vínculo {idVinculo}.");
                    });

                ExibirMensagem(
                    "Professor alocado com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                AtualizarConsultaAposOperacao();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao alocar professor", ex);
            }
        }

        // Impede alocações duplicadas para a mesma disciplina.
        private static void InserirAlocacao(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idVinculo,
            int idProfessor)
        {
            const string sql = @"
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.AlocacoesProfessores
                         WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdTurmaDisciplina = @IdVinculo
                )
                BEGIN
                    THROW 51001,
                        'Esta disciplina já possui professor responsável. Utilize Substituir professor.',
                        1;
                END;

                INSERT INTO dbo.AlocacoesProfessores
                (
                    IdProfessor,
                    IdTurmaDisciplina,
                    DataAlocacao
                )
                VALUES
                (
                    @IdProfessor,
                    @IdVinculo,
                    SYSDATETIME()
                );";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarParametrosAlocacao(
                comando,
                idVinculo,
                idProfessor);

            comando.ExecuteNonQuery();
        }

        // Substitui o professor e preserva o histórico anterior.
        private void SubstituirProfessor()
        {
            int idVinculo = ObterId(cmbDisciplina);
            int idProfessorNovo = ObterId(cmbProfessor);

            if (!ValidarSelecao(idVinculo, idProfessorNovo))
            {
                return;
            }

            if (!ConfirmarOperacao(
                "Substituir o professor responsável por esta disciplina?",
                "Confirmar substituição"))
            {
                return;
            }

            try
            {
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        ValidarDados(
                            conexao,
                            transacao,
                            idVinculo,
                            idProfessorNovo);

                        AlocacaoAnterior anterior =
                            ConsultarAlocacaoAnterior(
                                conexao,
                                transacao,
                                idVinculo);

                        if (anterior.IdProfessor == idProfessorNovo)
                        {
                            throw new InvalidOperationException(
                                "O professor selecionado já é o responsável " +
                                "por esta disciplina.");
                        }

                        RegistrarHistoricoSubstituicao(
                            conexao,
                            transacao,
                            anterior,
                            idVinculo);

                        AtualizarProfessorResponsavel(
                            conexao,
                            transacao,
                            anterior.IdAlocacao,
                            idVinculo,
                            idProfessorNovo);

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            "SUBSTITUIR_PROFESSOR",
                            $"Vínculo {idVinculo}: professor anterior " +
                            $"{anterior.IdProfessor}, novo professor " +
                            $"{idProfessorNovo}. " +
                            $"Alocação {anterior.IdAlocacao}.");
                    });

                ExibirMensagem(
                    "Professor substituído com sucesso!",
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

        // Recupera a alocação atual com bloqueio para atualização.
        private static AlocacaoAnterior ConsultarAlocacaoAnterior(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idVinculo)
        {
            const string sql = @"
                SELECT
                    IdAlocacao,
                    IdProfessor,
                    DataAlocacao
                FROM dbo.AlocacoesProfessores
                     WITH (UPDLOCK, HOLDLOCK)
                WHERE IdTurmaDisciplina = @IdVinculo";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdVinculo",
                SqlDbType.Int).Value = idVinculo;

            using SqlDataReader leitor =
                comando.ExecuteReader();

            if (!leitor.Read())
            {
                throw new InvalidOperationException(
                    "Esta disciplina ainda não possui professor responsável. " +
                    "Utilize Alocar professor.");
            }

            return new AlocacaoAnterior(
                leitor.GetInt32(0),
                leitor.GetInt32(1),
                leitor.GetDateTime(2));
        }

        // Preserva os dados do professor antes da substituição.
        private static void RegistrarHistoricoSubstituicao(
            SqlConnection conexao,
            SqlTransaction transacao,
            AlocacaoAnterior anterior,
            int idVinculo)
        {
            const string sql = @"
                INSERT INTO dbo.HistoricoAlocacoesProfessores
                (
                    IdAlocacaoOriginal,
                    IdProfessor,
                    IdTurmaDisciplina,
                    DataAlocacao,
                    EstavaAtiva
                )
                VALUES
                (
                    @IdAlocacao,
                    @IdProfessorAnterior,
                    @IdVinculo,
                    @DataAnterior,
                    1
                );";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdAlocacao",
                SqlDbType.Int).Value = anterior.IdAlocacao;

            comando.Parameters.Add(
                "@IdProfessorAnterior",
                SqlDbType.Int).Value = anterior.IdProfessor;

            comando.Parameters.Add(
                "@IdVinculo",
                SqlDbType.Int).Value = idVinculo;

            comando.Parameters.Add(
                "@DataAnterior",
                SqlDbType.DateTime2).Value =
                    anterior.DataAlocacao;

            comando.ExecuteNonQuery();
        }

        // Atualiza o professor responsável sem recriar a alocação.
        private static void AtualizarProfessorResponsavel(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idAlocacao,
            int idVinculo,
            int idProfessorNovo)
        {
            const string sql = @"
                UPDATE dbo.AlocacoesProfessores
                SET
                    IdProfessor = @IdProfessorNovo,
                    DataAlocacao = SYSDATETIME()
                WHERE IdAlocacao = @IdAlocacao
                  AND IdTurmaDisciplina = @IdVinculo";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdProfessorNovo",
                SqlDbType.Int).Value = idProfessorNovo;

            comando.Parameters.Add(
                "@IdAlocacao",
                SqlDbType.Int).Value = idAlocacao;

            comando.Parameters.Add(
                "@IdVinculo",
                SqlDbType.Int).Value = idVinculo;

            if (comando.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException(
                    "Não foi possível atualizar a alocação.");
            }
        }

        // Valida a disponibilidade da turma, disciplina e professor.
        private static void ValidarDados(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idVinculo,
            int idProfessor)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.TurmaDisciplinas td
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas d
                    ON d.IdDisciplina = td.IdDisciplina
                INNER JOIN dbo.Professores p
                    ON p.IdProfessor = @IdProfessor
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = p.IdUsuario
                WHERE td.IdTurmaDisciplina = @IdVinculo
                  AND td.Ativa = 1
                  AND t.Ativa = 1
                  AND d.Ativa = 1
                  AND u.Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarParametrosAlocacao(
                comando,
                idVinculo,
                idProfessor);

            bool dadosValidos =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!dadosValidos)
            {
                throw new InvalidOperationException(
                    "O professor ou a turma/disciplina não está " +
                    "disponível para alocação.");
            }
        }

        // Centraliza os parâmetros utilizados nas alocações.
        private static void AdicionarParametrosAlocacao(
            SqlCommand comando,
            int idVinculo,
            int idProfessor)
        {
            comando.Parameters.Add(
                "@IdVinculo",
                SqlDbType.Int).Value = idVinculo;

            comando.Parameters.Add(
                "@IdProfessor",
                SqlDbType.Int).Value = idProfessor;
        }

        // Executa alterações e auditoria em uma única transação.
        private void ExecutarTransacao(
            Action<SqlConnection, SqlTransaction> operacao)
        {
            ExigirAutorizacao();

            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(
                    IsolationLevel.Serializable);

            try
            {
                operacao(conexao, transacao);

                transacao.Commit();
            }
            catch
            {
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Atualiza a consulta após uma operação concluída.
        private void AtualizarConsultaAposOperacao()
        {
            AtualizarResponsavel();
            CarregarAlocacoes();
        }

        // Registra a atividade na mesma transação da alteração.
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

        // Representa os dados necessários para preservar uma alocação.
        private sealed class AlocacaoAnterior
        {
            public int IdAlocacao { get; }
            public int IdProfessor { get; }
            public DateTime DataAlocacao { get; }

            public AlocacaoAnterior(
                int idAlocacao,
                int idProfessor,
                DateTime dataAlocacao)
            {
                IdAlocacao = idAlocacao;
                IdProfessor = idProfessor;
                DataAlocacao = dataAlocacao;
            }
        }
    }
}