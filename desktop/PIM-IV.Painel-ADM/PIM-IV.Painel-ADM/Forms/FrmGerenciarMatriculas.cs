
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
    public class FrmGerenciarMatriculas : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissoes = new();

        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        private ComboBox cmbAluno = null!;
        private ComboBox cmbTurma = null!;
        private ComboBox cmbSituacao = null!;
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

        public FrmGerenciarMatriculas(Usuario usuario)
        {
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "PIM IV - Gerenciamento de Matrículas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1200, 800);
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
                        _usuario, "GERENCIAR_MATRICULAS");

                if (!autorizado)
                {
                    MessageBox.Show(
                        "Somente coordenadores autorizados podem gerenciar matrículas.",
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
                new RowStyle(SizeType.Absolute, 115));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            Controls.Add(principal);

            var titulo = new Label
            {
                Text = "Gerenciamento de Matrículas",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            // =================================================
            // CAMPOS DE MATRÍCULA
            // =================================================

            var cadastro = new TableLayoutPanel
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

            principal.Controls.Add(cadastro, 0, 1);

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

            // =================================================
            // PESQUISA
            // =================================================

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

            // =================================================
            // TABELA
            // =================================================

            var areaTabela = new TableLayoutPanel
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

            dgvMatriculas.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);

            dgvMatriculas.ColumnHeadersHeight = 35;
            dgvMatriculas.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvMatriculas.RowTemplate.Height = 30;

            dgvMatriculas.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(245, 248, 252);

            lblTotal = new Label
            {
                Text = "Matrículas: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            areaTabela.Controls.Add(dgvMatriculas, 0, 0);
            areaTabela.Controls.Add(lblTotal, 0, 1);

            principal.Controls.Add(areaTabela, 0, 3);

            // =================================================
            // BARRA SIMPLIFICADA DE BOTÕES
            // =================================================

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
                "Ações da matrícula  ▼", 205);

            btnAtualizar = CriarBotao(
                "Atualizar", 125);

            btnLimpar = CriarBotao(
                "Novo / Limpar", 145);

            botoes.Controls.Add(btnAcoes);
            botoes.Controls.Add(btnAtualizar);
            botoes.Controls.Add(btnLimpar);

            principal.Controls.Add(botoes, 0, 4);

            // =================================================
            // MENU SUSPENSO DE AÇÕES
            // =================================================

            menuAcoes = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 10),
                ShowImageMargin = false
            };

            var itemMatricular = new ToolStripMenuItem(
                "Matricular aluno");

            var itemTransferir = new ToolStripMenuItem(
                "Transferir turma");

            var itemSituacao = new ToolStripMenuItem(
                "Alterar situação");

            itemMatricular.Click += (_, _) =>
                MatricularAluno();

            itemTransferir.Click += (_, _) =>
                TransferirTurma();

            itemSituacao.Click += (_, _) =>
                AlterarSituacao();

            menuAcoes.Items.Add(itemMatricular);
            menuAcoes.Items.Add(new ToolStripSeparator());
            menuAcoes.Items.Add(itemTransferir);
            menuAcoes.Items.Add(new ToolStripSeparator());
            menuAcoes.Items.Add(itemSituacao);

            btnAcoes.Click += (_, _) =>
            {
                // O menu aparece acima do botão quando
                // não há espaço suficiente abaixo.
                menuAcoes.Show(
                    btnAcoes,
                    new Point(0, 0),
                    ToolStripDropDownDirection.AboveRight);
            };

            btnLimpar.Click += (_, _) =>
                LimparCampos();

            btnAtualizar.Click += (_, _) =>
                AtualizarDados();

            txtPesquisa.TextChanged += (_, _) =>
                CarregarMatriculas();

            dgvMatriculas.SelectionChanged += (_, _) =>
                SelecionarMatricula();
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
                Height = 42,
                BackColor = _azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            botao.FlatAppearance.BorderSize = 0;

            botao.MouseEnter += (_, _) =>
                botao.BackColor = Color.FromArgb(30, 75, 145);

            botao.MouseLeave += (_, _) =>
                botao.BackColor = _azul;

            return botao;
        }

        // =====================================================
        // ATUALIZAÇÃO DOS DADOS
        // =====================================================

        private void AtualizarDados()
        {
            try
            {
                _carregando = true;

                using var conexao = Conexao.CriarConexao();
                conexao.Open();

                const string sqlAlunos = @"
                    SELECT
                        a.IdAluno,
                        a.MatriculaAluno + ' - ' +
                            u.NomeCompleto AS NomeExibicao
                    FROM dbo.Alunos a
                    INNER JOIN dbo.Usuarios u
                        ON u.IdUsuario = a.IdUsuario
                    WHERE u.Ativo = 1
                    ORDER BY u.NomeCompleto";

                const string sqlTurmas = @"
                    SELECT
                        IdTurma,
                        CodigoTurma + ' - ' +
                            NomeTurma AS NomeExibicao
                    FROM dbo.Turmas
                    WHERE Ativa = 1
                    ORDER BY CodigoTurma";

                cmbAluno.DataSource =
                    ConsultarTabela(conexao, sqlAlunos);

                cmbAluno.DisplayMember = "NomeExibicao";
                cmbAluno.ValueMember = "IdAluno";

                cmbTurma.DataSource =
                    ConsultarTabela(conexao, sqlTurmas);

                cmbTurma.DisplayMember = "NomeExibicao";
                cmbTurma.ValueMember = "IdTurma";
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar dados", ex);
            }
            finally
            {
                _carregando = false;
            }

            CarregarMatriculas();
            LimparCampos();
        }

        private DataTable ConsultarTabela(
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

        // =====================================================
        // CONSULTA E PESQUISA
        // =====================================================

        private void CarregarMatriculas()
        {
            if (_carregando)
                return;

            try
            {
                _carregando = true;

                using var conexao = Conexao.CriarConexao();
                conexao.Open();

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
                        @Pesquisa = N''
                        OR u.NomeCompleto LIKE
                            N'%' + @Pesquisa + N'%'
                        OR a.MatriculaAluno LIKE
                            N'%' + @Pesquisa + N'%'
                        OR t.CodigoTurma LIKE
                            N'%' + @Pesquisa + N'%'
                    ORDER BY u.NomeCompleto";

                using var comando =
                    new SqlCommand(sql, conexao);

                comando.Parameters.Add(
                    "@Pesquisa",
                    SqlDbType.NVarChar,
                    150).Value = txtPesquisa.Text.Trim();

                using var leitor = comando.ExecuteReader();

                var tabela = new DataTable();
                tabela.Load(leitor);

                dgvMatriculas.DataSource = tabela;

                foreach (string coluna in new[]
                {
                    "IdMatricula",
                    "IdAluno",
                    "IdTurma"
                })
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
                    "DataMatricula", "Data da matrícula");

                if (dgvMatriculas.Columns.Contains(
                    "DataMatricula"))
                {
                    dgvMatriculas.Columns["DataMatricula"]!
                        .DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                dgvMatriculas.ClearSelection();

                _idMatriculaSelecionada = 0;
                _idAlunoSelecionado = 0;

                lblTotal.Text =
                    $"Matrículas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao consultar matrículas", ex);
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
            if (dgvMatriculas.Columns.Contains(coluna))
            {
                dgvMatriculas.Columns[coluna]!
                    .HeaderText = titulo;
            }
        }

        // =====================================================
        // SELEÇÃO DA MATRÍCULA
        // =====================================================

        private void SelecionarMatricula()
        {
            if (_carregando ||
                dgvMatriculas.SelectedRows.Count == 0)
            {
                return;
            }

            try
            {
                var linha = dgvMatriculas.SelectedRows[0];

                _idMatriculaSelecionada =
                    Convert.ToInt32(
                        linha.Cells["IdMatricula"].Value);

                _idAlunoSelecionado =
                    Convert.ToInt32(
                        linha.Cells["IdAluno"].Value);

                int idTurma = Convert.ToInt32(
                    linha.Cells["IdTurma"].Value);

                cmbAluno.SelectedValue =
                    _idAlunoSelecionado;

                cmbTurma.SelectedValue = idTurma;

                cmbSituacao.SelectedItem =
                    Convert.ToString(
                        linha.Cells["Situacao"].Value)
                    ?? "Ativa";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao selecionar matrícula", ex);
            }
        }

        private int ObterId(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        // =====================================================
        // MATRICULAR ALUNO
        // =====================================================

        private void MatricularAluno()
        {
            int idAluno = ObterId(cmbAluno);
            int idTurma = ObterId(cmbTurma);

            if (idAluno <= 0 || idTurma <= 0)
            {
                MessageBox.Show(
                    "Selecione um aluno e uma turma.",
                    "Dados necessários",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (MessageBox.Show(
                "Confirmar a matrícula deste aluno na turma selecionada?",
                "Confirmar matrícula",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using var conexao = Conexao.CriarConexao();
                conexao.Open();

                using var transacao =
                    conexao.BeginTransaction();

                try
                {
                    ExigirAutorizacao(conexao, transacao);

                    const string sql = @"
                        IF EXISTS (
                            SELECT 1
                            FROM dbo.Matriculas
                                WITH (UPDLOCK, HOLDLOCK)
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

                    using var comando = new SqlCommand(
                        sql, conexao, transacao);

                    comando.Parameters.Add(
                        "@IdAluno", SqlDbType.Int)
                        .Value = idAluno;

                    comando.Parameters.Add(
                        "@IdTurma", SqlDbType.Int)
                        .Value = idTurma;

                    int resultado = Convert.ToInt32(
                        comando.ExecuteScalar());

                    if (resultado == 0)
                    {
                        transacao.Commit();

                        MessageBox.Show(
                            "Este aluno já possui matrícula. " +
                            "Utilize Transferir turma ou Alterar situação.",
                            "Matrícula existente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return;
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "CRIAR_MATRICULA",
                        $"Aluno {idAluno} matriculado " +
                        $"na turma {idTurma}.");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Matrícula realizada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarMatriculas();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao matricular aluno", ex);
            }
        }

        // =====================================================
        // TRANSFERIR TURMA
        // =====================================================

        private void TransferirTurma()
        {
            if (_idMatriculaSelecionada <= 0)
            {
                MessageBox.Show(
                    "Selecione uma matrícula na tabela.",
                    "Seleção necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            int novaTurma = ObterId(cmbTurma);

            if (novaTurma <= 0)
            {
                MessageBox.Show(
                    "Selecione a turma de destino.",
                    "Dados necessários",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (MessageBox.Show(
                "Deseja transferir o aluno para a turma selecionada?\n" +
                "A matrícula atual será atualizada.",
                "Confirmar transferência",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using var conexao = Conexao.CriarConexao();
                conexao.Open();

                using var transacao =
                    conexao.BeginTransaction();

                try
                {
                    ExigirAutorizacao(conexao, transacao);

                    const string sql = @"
                        UPDATE dbo.Matriculas
                        SET IdTurma = @IdTurma
                        WHERE IdMatricula = @IdMatricula
                          AND IdAluno = @IdAluno
                          AND IdTurma <> @IdTurma
                          AND Situacao = N'Ativa'";

                    using var comando = new SqlCommand(
                        sql, conexao, transacao);

                    comando.Parameters.Add(
                        "@IdTurma", SqlDbType.Int)
                        .Value = novaTurma;

                    comando.Parameters.Add(
                        "@IdMatricula", SqlDbType.Int)
                        .Value = _idMatriculaSelecionada;

                    comando.Parameters.Add(
                        "@IdAluno", SqlDbType.Int)
                        .Value = _idAlunoSelecionado;

                    int alterados =
                        comando.ExecuteNonQuery();

                    if (alterados == 0)
                    {
                        throw new InvalidOperationException(
                            "A transferência não foi realizada. " +
                            "Verifique se a matrícula está ativa " +
                            "e se a turma de destino é diferente.");
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "TRANSFERIR_MATRICULA",
                        $"Matrícula {_idMatriculaSelecionada} " +
                        $"transferida para turma {novaTurma}.");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Transferência realizada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarMatriculas();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao transferir aluno", ex);
            }
        }

        // =====================================================
        // ALTERAR SITUAÇÃO
        // =====================================================

        private void AlterarSituacao()
        {
            if (_idMatriculaSelecionada <= 0)
            {
                MessageBox.Show(
                    "Selecione uma matrícula na tabela.",
                    "Seleção necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            string situacao =
                cmbSituacao.SelectedItem?.ToString() ?? "";

            if (situacao != "Ativa" &&
                situacao != "Trancada" &&
                situacao != "Concluida" &&
                situacao != "Cancelada")
            {
                MessageBox.Show(
                    "Selecione uma situação válida.",
                    "Dados inválidos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (MessageBox.Show(
                $"Alterar a situação desta matrícula para '{situacao}'?",
                "Confirmar alteração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using var conexao = Conexao.CriarConexao();
                conexao.Open();

                using var transacao =
                    conexao.BeginTransaction();

                try
                {
                    ExigirAutorizacao(conexao, transacao);

                    const string sql = @"
                        UPDATE dbo.Matriculas
                        SET Situacao = @Situacao
                        WHERE IdMatricula = @IdMatricula
                          AND IdAluno = @IdAluno";

                    using var comando = new SqlCommand(
                        sql, conexao, transacao);

                    comando.Parameters.Add(
                        "@Situacao",
                        SqlDbType.NVarChar,
                        30).Value = situacao;

                    comando.Parameters.Add(
                        "@IdMatricula", SqlDbType.Int)
                        .Value = _idMatriculaSelecionada;

                    comando.Parameters.Add(
                        "@IdAluno", SqlDbType.Int)
                        .Value = _idAlunoSelecionado;

                    if (comando.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException(
                            "Matrícula não encontrada.");
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "ALTERAR_SITUACAO_MATRICULA",
                        $"Matrícula {_idMatriculaSelecionada} " +
                        $"alterada para {situacao}.");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Situação atualizada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarMatriculas();
                LimparCampos();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao alterar situação", ex);
            }
        }

        // =====================================================
        // LIMPAR FORMULÁRIO
        // =====================================================

        private void LimparCampos()
        {
            _idMatriculaSelecionada = 0;
            _idAlunoSelecionado = 0;

            dgvMatriculas.ClearSelection();

            if (cmbAluno.Items.Count > 0)
                cmbAluno.SelectedIndex = -1;

            if (cmbTurma.Items.Count > 0)
                cmbTurma.SelectedIndex = -1;

            cmbSituacao.SelectedItem = "Ativa";
        }

        // =====================================================
        // AUTORIZAÇÃO NO BANCO DE DADOS
        // =====================================================

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
                  AND u.IdPerfil = 2
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND pm.Codigo = N'GERENCIAR_MATRICULAS'
                  AND pm.Ativo = 1";

            using var comando = new SqlCommand(
                sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario", SqlDbType.Int)
                .Value = _usuario.IdUsuario;

            if (Convert.ToInt32(
                comando.ExecuteScalar()) == 0)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui permissão para alterar matrículas.");
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
                    (IdUsuario, Acao, Descricao)
                VALUES
                    (@IdUsuario, @Acao, @Descricao)";

            using var comando = new SqlCommand(
                sql, conexao, transacao);

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
        // TRATAMENTO DE ERROS
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
