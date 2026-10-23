
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
    public class FrmAlocarProfessores : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissoes = new();

        private readonly Color _azul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _fundo =
            Color.FromArgb(244, 247, 251);

        private ComboBox cmbTurma = null!;
        private ComboBox cmbDisciplina = null!;
        private ComboBox cmbProfessor = null!;
        private TextBox txtPesquisa = null!;
        private DataGridView dgvAlocacoes = null!;
        private Label lblResponsavel = null!;
        private Label lblTotal = null!;
        private Label lblSelecao = null!;

        private Button btnAcoes = null!;
        private Button btnLimpar = null!;
        private Button btnAtualizar = null!;
        private ContextMenuStrip menuAcoes = null!;

        private bool _carregando;
        private int _idAlocacaoSelecionada;

        public FrmAlocarProfessores(Usuario usuario)
        {
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "PIM IV - Alocação de Professores";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1250, 820);
            MinimumSize = new Size(850, 600);
            Font = new Font("Segoe UI", 10);
            BackColor = _fundo;

            CriarInterface();

            Shown += (_, _) =>
            {
                if (!VerificarAcesso())
                {
                    Close();
                    return;
                }

                AtualizarDados();
            };
        }

        // =====================================================
        // SEGURANÇA
        // =====================================================

        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == 2 &&
                    _permissoes.UsuarioTemPermissao(
                        _usuario, "ALOCAR_PROFESSORES");

                if (!autorizado)
                {
                    MessageBox.Show(
                        "Você não possui permissão para alocar professores.",
                        "Acesso negado",
                        MessageBoxButtons.OK,
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

        private void ExigirAutorizacao()
        {
            if (_usuario.IdPerfil != 2 ||
                !_permissoes.UsuarioTemPermissao(
                    _usuario, "ALOCAR_PROFESSORES"))
            {
                throw new UnauthorizedAccessException(
                    "Você não possui permissão para alterar alocações.");
            }
        }

        // =====================================================
        // INTERFACE
        // =====================================================

        private void CriarInterface()
        {
            var principal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 5,
                BackColor = _fundo
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
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 80));

            Controls.Add(principal);

            var titulo = new Label
            {
                Text = "Alocação de Professores",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI", 21, FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            // CAMPOS

            var cadastro = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            for (int i = 0; i < 3; i++)
            {
                cadastro.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Percent, 33.333f));
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

            // PESQUISA

            var pesquisa = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15, 12, 15, 10),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };

            pesquisa.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 95));

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
                    "Pesquisar por turma, disciplina, professor ou RP...",
                Margin = new Padding(5, 3, 0, 3)
            };

            pesquisa.Controls.Add(txtPesquisa, 1, 0);
            principal.Controls.Add(pesquisa, 0, 2);

            // TABELA

            var areaTabela = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15),
                BackColor = Color.White,
                Margin = new Padding(0)
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
                MultiSelect = false,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoGenerateColumns = true,
                AllowUserToResizeRows = false
            };

            dgvAlocacoes.ColumnHeadersHeight = 35;

            dgvAlocacoes.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvAlocacoes.RowTemplate.Height = 30;

            dgvAlocacoes.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            dgvAlocacoes.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);

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

            principal.Controls.Add(areaTabela, 0, 3);

            // BOTÕES ORIGINAIS

            var botoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 12, 0, 0),
                Margin = new Padding(0)
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

            principal.Controls.Add(botoes, 0, 4);

            // MENU DE AÇÕES

            menuAcoes = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 10),
                ShowImageMargin = false
            };

            var itemAlocar =
                new ToolStripMenuItem("Alocar professor");

            var itemSubstituir =
                new ToolStripMenuItem("Substituir professor");

            itemAlocar.Click += (_, _) =>
                AlocarProfessor();

            itemSubstituir.Click += (_, _) =>
                SubstituirProfessor();

            menuAcoes.Items.Add(itemAlocar);
            menuAcoes.Items.Add(itemSubstituir);

            btnAcoes.Click += (_, _) =>
            {
                menuAcoes.Show(
                    btnAcoes,
                    new Point(0, 0),
                    ToolStripDropDownDirection.AboveRight);
            };

            btnLimpar.Click += (_, _) =>
                LimparSelecao();

            btnAtualizar.Click += (_, _) =>
                AtualizarDados();

            // EVENTOS

            cmbTurma.SelectedIndexChanged += (_, _) =>
            {
                if (!_carregando)
                    CarregarDisciplinas();
            };

            cmbDisciplina.SelectedIndexChanged += (_, _) =>
            {
                if (!_carregando)
                    AtualizarResponsavel();
            };

            txtPesquisa.TextChanged += (_, _) =>
                CarregarAlocacoes();

            dgvAlocacoes.SelectionChanged += (_, _) =>
                SelecionarAlocacao();
        }

        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                ForeColor = _azul,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private ComboBox CriarCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(5, 5, 15, 5)
            };
        }

        private Button CriarBotao(
            string texto,
            int largura)
        {
            var botao = new Button
            {
                Text = texto,
                Width = largura,
                Height = 43,
                BackColor = _azul,
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
                botao.BackColor = _azul;

            return botao;
        }

        // =====================================================
        // CONSULTAS
        // =====================================================

        private DataTable Consultar(
            SqlConnection conexao,
            string sql)
        {
            using var comando =
                new SqlCommand(sql, conexao);

            using var leitor =
                comando.ExecuteReader();

            var tabela = new DataTable();
            tabela.Load(leitor);

            return tabela;
        }

        private void AtualizarDados()
        {
            try
            {
                _carregando = true;

                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                const string sqlTurmas = @"
                    SELECT
                        IdTurma,
                        CodigoTurma + N' - ' +
                            NomeTurma AS Descricao
                    FROM dbo.Turmas
                    WHERE Ativa = 1
                    ORDER BY CodigoTurma";

                const string sqlProfessores = @"
                    SELECT
                        p.IdProfessor,
                        p.RegistroProfessor + N' - ' +
                            u.NomeCompleto AS Descricao
                    FROM dbo.Professores p
                    INNER JOIN dbo.Usuarios u
                        ON u.IdUsuario = p.IdUsuario
                    WHERE u.Ativo = 1
                    ORDER BY u.NomeCompleto";

                cmbTurma.DataSource =
                    Consultar(conexao, sqlTurmas);

                cmbTurma.DisplayMember = "Descricao";
                cmbTurma.ValueMember = "IdTurma";

                cmbProfessor.DataSource =
                    Consultar(conexao, sqlProfessores);

                cmbProfessor.DisplayMember = "Descricao";
                cmbProfessor.ValueMember = "IdProfessor";
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar dados", ex);
            }
            finally
            {
                _carregando = false;
            }

            if (cmbTurma.Items.Count > 0)
                cmbTurma.SelectedIndex = 0;

            if (cmbProfessor.Items.Count > 0)
                cmbProfessor.SelectedIndex = -1;

            CarregarDisciplinas();
            CarregarAlocacoes();
            LimparSelecao();
        }

        private void CarregarDisciplinas()
        {
            int idTurma = ObterId(cmbTurma);

            try
            {
                _carregando = true;

                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

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

                using var comando =
                    new SqlCommand(sql, conexao);

                comando.Parameters.Add(
                    "@IdTurma", SqlDbType.Int)
                    .Value = idTurma;

                using var leitor =
                    comando.ExecuteReader();

                var tabela = new DataTable();
                tabela.Load(leitor);

                cmbDisciplina.DataSource = tabela;
                cmbDisciplina.DisplayMember = "Descricao";
                cmbDisciplina.ValueMember =
                    "IdTurmaDisciplina";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar disciplinas", ex);
            }
            finally
            {
                _carregando = false;
            }

            AtualizarResponsavel();
        }

        private int ObterId(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        private void AtualizarResponsavel()
        {
            int idVinculo = ObterId(cmbDisciplina);

            lblResponsavel.Text =
                "Nenhum professor responsável selecionado.";

            if (idVinculo <= 0)
                return;

            try
            {
                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

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

                using var comando =
                    new SqlCommand(sql, conexao);

                comando.Parameters.Add(
                    "@IdVinculo", SqlDbType.Int)
                    .Value = idVinculo;

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
                    "Erro ao consultar responsável", ex);
            }
        }

        // =====================================================
        // LISTAGEM SEM COLUNA ATIVA
        // =====================================================

        private void CarregarAlocacoes()
        {
            if (_carregando)
                return;

            try
            {
                _carregando = true;

                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

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
                        @Pesquisa = N''
                        OR t.CodigoTurma LIKE
                            N'%' + @Pesquisa + N'%'
                        OR d.NomeDisciplina LIKE
                            N'%' + @Pesquisa + N'%'
                        OR u.NomeCompleto LIKE
                            N'%' + @Pesquisa + N'%'
                        OR p.RegistroProfessor LIKE
                            N'%' + @Pesquisa + N'%'
                    ORDER BY
                        t.CodigoTurma,
                        d.NomeDisciplina";

                using var comando =
                    new SqlCommand(sql, conexao);

                comando.Parameters.Add(
                    "@Pesquisa", SqlDbType.NVarChar, 150)
                    .Value = txtPesquisa.Text.Trim();

                using var leitor =
                    comando.ExecuteReader();

                var tabela = new DataTable();
                tabela.Load(leitor);

                dgvAlocacoes.DataSource = tabela;

                foreach (string coluna in new[]
                {
                    "IdAlocacao",
                    "IdProfessor",
                    "IdTurmaDisciplina",
                    "IdTurma"
                })
                {
                    if (dgvAlocacoes.Columns.Contains(coluna))
                    {
                        dgvAlocacoes.Columns[coluna]!
                            .Visible = false;
                    }
                }

                DefinirCabecalho("Turma", "Turma");
                DefinirCabecalho("Disciplina", "Disciplina");
                DefinirCabecalho("RP", "RP");
                DefinirCabecalho("Professor", "Professor");
                DefinirCabecalho(
                    "DataAlocacao", "Data da alocação");

                if (dgvAlocacoes.Columns.Contains(
                    "DataAlocacao"))
                {
                    dgvAlocacoes.Columns["DataAlocacao"]!
                        .DefaultCellStyle.Format =
                            "dd/MM/yyyy HH:mm";
                }

                dgvAlocacoes.ClearSelection();
                _idAlocacaoSelecionada = 0;

                lblSelecao.Text =
                    "Nenhuma alocação selecionada.";

                lblTotal.Text =
                    $"Alocações atuais: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao consultar alocações", ex);
            }
            finally
            {
                _carregando = false;
            }
        }

        private void DefinirCabecalho(
            string coluna,
            string titulo)
        {
            if (dgvAlocacoes.Columns.Contains(coluna))
            {
                dgvAlocacoes.Columns[coluna]!
                    .HeaderText = titulo;
            }
        }

        // =====================================================
        // SELEÇÃO E LIMPEZA
        // =====================================================

        private void SelecionarAlocacao()
        {
            if (_carregando ||
                dgvAlocacoes.SelectedRows.Count == 0)
            {
                return;
            }

            try
            {
                var linha =
                    dgvAlocacoes.SelectedRows[0];

                int idAlocacao = Convert.ToInt32(
                    linha.Cells["IdAlocacao"].Value);

                int idTurma = Convert.ToInt32(
                    linha.Cells["IdTurma"].Value);

                int idVinculo = Convert.ToInt32(
                    linha.Cells["IdTurmaDisciplina"].Value);

                int idProfessor = Convert.ToInt32(
                    linha.Cells["IdProfessor"].Value);

                _carregando = true;

                try
                {
                    cmbTurma.SelectedValue = idTurma;

                    _carregando = false;
                    CarregarDisciplinas();
                    _carregando = true;

                    cmbDisciplina.SelectedValue = idVinculo;
                    cmbProfessor.SelectedValue = idProfessor;
                }
                finally
                {
                    _carregando = false;
                }

                _idAlocacaoSelecionada = idAlocacao;

                lblSelecao.Text =
                    $"Selecionada: {linha.Cells["Professor"].Value} " +
                    $"| {linha.Cells["Disciplina"].Value}";

                AtualizarResponsavel();
            }
            catch (Exception ex)
            {
                _carregando = false;
                MostrarErro(
                    "Erro ao selecionar alocação", ex);
            }
        }

        private void LimparSelecao()
        {
            _idAlocacaoSelecionada = 0;

            dgvAlocacoes.ClearSelection();

            if (cmbProfessor.Items.Count > 0)
                cmbProfessor.SelectedIndex = -1;

            lblSelecao.Text =
                "Nenhuma alocação selecionada.";

            AtualizarResponsavel();
        }

        // =====================================================
        // ALOCAR PROFESSOR
        // =====================================================

        private void AlocarProfessor()
        {
            int idVinculo = ObterId(cmbDisciplina);
            int idProfessor = ObterId(cmbProfessor);

            if (idVinculo <= 0 || idProfessor <= 0)
            {
                MessageBox.Show(
                    "Selecione uma turma, uma disciplina e um professor.",
                    "Dados necessários",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Confirmar a alocação deste professor?",
                "Confirmar alocação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                ExigirAutorizacao();

                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                using var transacao =
                    conexao.BeginTransaction(
                        IsolationLevel.Serializable);

                try
                {
                    ValidarDados(
                        conexao, transacao,
                        idVinculo, idProfessor);

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

                    using var comando =
                        new SqlCommand(
                            sql, conexao, transacao);

                    comando.Parameters.Add(
                        "@IdVinculo", SqlDbType.Int)
                        .Value = idVinculo;

                    comando.Parameters.Add(
                        "@IdProfessor", SqlDbType.Int)
                        .Value = idProfessor;

                    comando.ExecuteNonQuery();

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "ALOCAR_PROFESSOR",
                        $"Professor {idProfessor} alocado ao vínculo {idVinculo}.");

                    transacao.Commit();
                }
                catch
                {
                    if (transacao.Connection != null)
                        transacao.Rollback();

                    throw;
                }

                MessageBox.Show(
                    "Professor alocado com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                AtualizarResponsavel();
                CarregarAlocacoes();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao alocar professor", ex);
            }
        }

        // =====================================================
        // SUBSTITUIR PROFESSOR SEM ATIVA / INATIVA
        // =====================================================

        private void SubstituirProfessor()
        {
            int idVinculo = ObterId(cmbDisciplina);
            int idProfessorNovo = ObterId(cmbProfessor);

            if (idVinculo <= 0 || idProfessorNovo <= 0)
            {
                MessageBox.Show(
                    "Selecione a disciplina e o novo professor.",
                    "Dados necessários",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Substituir o professor responsável por esta disciplina?",
                "Confirmar substituição",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                ExigirAutorizacao();

                using var conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                using var transacao =
                    conexao.BeginTransaction(
                        IsolationLevel.Serializable);

                try
                {
                    ValidarDados(
                        conexao, transacao,
                        idVinculo, idProfessorNovo);

                    int idAlocacao;
                    int idProfessorAnterior;
                    DateTime dataAnterior;

                    // Bloqueia o vínculo durante a substituição.

                    const string sqlConsultar = @"
                        SELECT
                            IdAlocacao,
                            IdProfessor,
                            DataAlocacao
                        FROM dbo.AlocacoesProfessores
                             WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdTurmaDisciplina = @IdVinculo;";

                    using (var comando = new SqlCommand(
                        sqlConsultar, conexao, transacao))
                    {
                        comando.Parameters.Add(
                            "@IdVinculo", SqlDbType.Int)
                            .Value = idVinculo;

                        using var leitor =
                            comando.ExecuteReader();

                        if (!leitor.Read())
                        {
                            throw new InvalidOperationException(
                                "Esta disciplina ainda não possui professor responsável. Utilize Alocar professor.");
                        }

                        idAlocacao = leitor.GetInt32(0);
                        idProfessorAnterior = leitor.GetInt32(1);
                        dataAnterior = leitor.GetDateTime(2);
                    }

                    if (idProfessorAnterior == idProfessorNovo)
                    {
                        throw new InvalidOperationException(
                            "O professor selecionado já é o responsável por esta disciplina.");
                    }

                    // Preserva o responsável anterior no histórico.
                    // EstavaAtiva = 1 representa a condição
                    // do registro antes da substituição.

                    const string sqlHistorico = @"
                        INSERT INTO
                            dbo.HistoricoAlocacoesProfessores
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

                    using (var comando = new SqlCommand(
                        sqlHistorico, conexao, transacao))
                    {
                        comando.Parameters.Add(
                            "@IdAlocacao", SqlDbType.Int)
                            .Value = idAlocacao;

                        comando.Parameters.Add(
                            "@IdProfessorAnterior", SqlDbType.Int)
                            .Value = idProfessorAnterior;

                        comando.Parameters.Add(
                            "@IdVinculo", SqlDbType.Int)
                            .Value = idVinculo;

                        comando.Parameters.Add(
                            "@DataAnterior", SqlDbType.DateTime2)
                            .Value = dataAnterior;

                        comando.ExecuteNonQuery();
                    }

                    // Atualiza o mesmo registro.
                    // Não cria nova alocação nem altera status.

                    const string sqlAtualizar = @"
                        UPDATE dbo.AlocacoesProfessores
                        SET
                            IdProfessor = @IdProfessorNovo,
                            DataAlocacao = SYSDATETIME()
                        WHERE IdAlocacao = @IdAlocacao
                          AND IdTurmaDisciplina = @IdVinculo;";

                    using (var comando = new SqlCommand(
                        sqlAtualizar, conexao, transacao))
                    {
                        comando.Parameters.Add(
                            "@IdProfessorNovo", SqlDbType.Int)
                            .Value = idProfessorNovo;

                        comando.Parameters.Add(
                            "@IdAlocacao", SqlDbType.Int)
                            .Value = idAlocacao;

                        comando.Parameters.Add(
                            "@IdVinculo", SqlDbType.Int)
                            .Value = idVinculo;

                        if (comando.ExecuteNonQuery() != 1)
                        {
                            throw new InvalidOperationException(
                                "Não foi possível atualizar a alocação.");
                        }
                    }

                    // Auditoria identifica o responsável
                    // anterior, o novo e o usuário da ação.

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "SUBSTITUIR_PROFESSOR",
                        $"Vínculo {idVinculo}: professor anterior " +
                        $"{idProfessorAnterior}, novo professor " +
                        $"{idProfessorNovo}. Alocação {idAlocacao}.");

                    transacao.Commit();
                }
                catch
                {
                    if (transacao.Connection != null)
                        transacao.Rollback();

                    throw;
                }

                MessageBox.Show(
                    "Professor substituído com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                AtualizarResponsavel();
                CarregarAlocacoes();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao substituir professor", ex);
            }
        }

        // =====================================================
        // VALIDAÇÃO
        // =====================================================

        private void ValidarDados(
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

            using var comando =
                new SqlCommand(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdVinculo", SqlDbType.Int)
                .Value = idVinculo;

            comando.Parameters.Add(
                "@IdProfessor", SqlDbType.Int)
                .Value = idProfessor;

            if (Convert.ToInt32(
                comando.ExecuteScalar()) == 0)
            {
                throw new InvalidOperationException(
                    "O professor ou a turma/disciplina não está disponível para alocação.");
            }
        }

        // =====================================================
        // AUDITORIA
        // =====================================================

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
                );";

            using var comando =
                new SqlCommand(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario", SqlDbType.Int)
                .Value = _usuario.IdUsuario;

            comando.Parameters.Add(
                "@Acao", SqlDbType.NVarChar, 100)
                .Value = acao;

            comando.Parameters.Add(
                "@Descricao", SqlDbType.NVarChar, 500)
                .Value = descricao;

            comando.ExecuteNonQuery();
        }

        // =====================================================
        // ERROS
        // =====================================================

        private void MostrarErro(
            string titulo,
            Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                titulo,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
