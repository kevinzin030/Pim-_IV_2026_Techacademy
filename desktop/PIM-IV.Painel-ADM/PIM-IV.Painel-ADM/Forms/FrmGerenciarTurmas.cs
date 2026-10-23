
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    public class FrmGerenciarTurmas : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        private TextBox txtCodigo = null!;
        private TextBox txtNome = null!;
        private TextBox txtPesquisa = null!;

        private ComboBox cmbPeriodo = null!;
        private NumericUpDown nudAno = null!;
        private ComboBox cmbSemestre = null!;

        private DataGridView dgvTurmas = null!;
        private Label lblQuantidade = null!;
        private Label lblModo = null!;

        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnStatus = null!;

        private int? _idTurmaSelecionada;
        private bool _carregando;

        public FrmGerenciarTurmas(Usuario usuario)
        {
            _usuario = usuario ?? throw new ArgumentNullException(
                nameof(usuario));

            Text = "PIM IV - Gerenciamento de Turmas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 780);
            MinimumSize = new Size(950, 680);
            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);

            CriarInterface();

            Shown += (_, _) =>
            {
                if (!VerificarAcesso())
                {
                    Close();
                    return;
                }

                LimparFormulario();
                CarregarTurmas();
            };
        }

        private bool VerificarAcesso()
        {
            try
            {
                if (_usuario.IdPerfil != 2 ||
                    !_permissaoService.UsuarioTemPermissao(
                        _usuario, "GERENCIAR_TURMAS"))
                {
                    MessageBox.Show(
                        "Somente coordenadores autorizados podem " +
                        "gerenciar turmas.",
                        "Acesso negado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao verificar autorização:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 5
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 215));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Gerenciamento de Turmas",
                Font = new Font(
                    "Segoe UI", 21, FontStyle.Bold),
                ForeColor = _azul,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            CriarAreaCadastro(principal);
            CriarAreaPesquisa(principal);
            CriarTabela(principal);

            lblQuantidade = new Label
            {
                Text = "Turmas encontradas: 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray
            };

            principal.Controls.Add(lblQuantidade, 0, 4);
        }

        private void CriarAreaCadastro(TableLayoutPanel principal)
        {
            Panel painel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18)
            };

            principal.Controls.Add(painel, 0, 1);

            TableLayoutPanel campos = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4
            };

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 35));

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 20));

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 20));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 25));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 62));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            painel.Controls.Add(campos);

            campos.Controls.Add(
                CriarRotulo("Código da turma"), 0, 0);

            campos.Controls.Add(
                CriarRotulo("Nome da turma"), 1, 0);

            campos.Controls.Add(
                CriarRotulo("Ano letivo"), 2, 0);

            campos.Controls.Add(
                CriarRotulo("Semestre"), 3, 0);

            txtCodigo = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 30
            };

            txtNome = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 100
            };

            nudAno = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 2000,
                Maximum = 2100,
                Value = DateTime.Today.Year
            };

            cmbSemestre = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbSemestre.Items.AddRange(
                new object[] { "1", "2" });

            cmbSemestre.SelectedIndex = 0;

            campos.Controls.Add(txtCodigo, 0, 1);
            campos.Controls.Add(txtNome, 1, 1);
            campos.Controls.Add(nudAno, 2, 1);
            campos.Controls.Add(cmbSemestre, 3, 1);

            FlowLayoutPanel painelPeriodo = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            Label lblPeriodo = new()
            {
                Text = "Período:",
                AutoSize = true,
                Margin = new Padding(0, 12, 10, 0)
            };

            cmbPeriodo = new ComboBox
            {
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 8, 15, 0)
            };

            cmbPeriodo.Items.AddRange(
                new object[]
                {
                    "Matutino",
                    "Vespertino",
                    "Noturno",
                    "Integral"
                });

            cmbPeriodo.SelectedIndex = 0;

            lblModo = new Label
            {
                Text = "Novo cadastro",
                AutoSize = true,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                ForeColor = _azul,
                Margin = new Padding(12, 12, 0, 0)
            };

            painelPeriodo.Controls.Add(lblPeriodo);
            painelPeriodo.Controls.Add(cmbPeriodo);
            painelPeriodo.Controls.Add(lblModo);

            campos.Controls.Add(painelPeriodo, 0, 2);
            campos.SetColumnSpan(painelPeriodo, 4);

            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            btnSalvar = CriarBotao(
                "Salvar turma", 150, true);

            btnNovo = CriarBotao(
                "Novo / Limpar", 145, false);

            btnStatus = CriarBotao(
                "Ativar / Desativar", 170, false);

            btnSalvar.Click += (_, _) => SalvarTurma();
            btnNovo.Click += (_, _) => LimparFormulario();
            btnStatus.Click += (_, _) => AlterarStatus();

            botoes.Controls.Add(btnSalvar);
            botoes.Controls.Add(btnNovo);
            botoes.Controls.Add(btnStatus);

            campos.Controls.Add(botoes, 0, 3);
            campos.SetColumnSpan(botoes, 4);
        }

        private void CriarAreaPesquisa(TableLayoutPanel principal)
        {
            FlowLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 12, 0, 0)
            };

            txtPesquisa = new TextBox
            {
                Width = 370,
                PlaceholderText =
                    "Pesquisar por código, nome ou período...",
                Margin = new Padding(0, 4, 12, 0)
            };

            txtPesquisa.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CarregarTurmas();
                    e.SuppressKeyPress = true;
                }
            };

            Button btnPesquisar =
                CriarBotao("Pesquisar", 120, true);

            Button btnAtualizar =
                CriarBotao("Atualizar", 120, false);

            btnPesquisar.Click += (_, _) => CarregarTurmas();

            btnAtualizar.Click += (_, _) =>
            {
                txtPesquisa.Clear();
                CarregarTurmas();
            };

            painel.Controls.Add(txtPesquisa);
            painel.Controls.Add(btnPesquisar);
            painel.Controls.Add(btnAtualizar);

            principal.Controls.Add(painel, 0, 2);
        }

        private void CriarTabela(TableLayoutPanel principal)
        {
            dgvTurmas = new DataGridView
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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };

            dgvTurmas.EnableHeadersVisualStyles = false;

            dgvTurmas.ColumnHeadersDefaultCellStyle.BackColor =
                _azul;

            dgvTurmas.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvTurmas.ColumnHeadersHeight = 42;
            dgvTurmas.RowTemplate.Height = 34;

            dgvTurmas.SelectionChanged += (_, _) =>
            {
                if (!_carregando)
                    PreencherFormularioSelecionado();
            };

            principal.Controls.Add(dgvTurmas, 0, 3);
        }

        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 50, 65)
            };
        }

        private Button CriarBotao(
            string texto, int largura, bool principal)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 38,
                Margin = new Padding(5, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = principal ? _azul : Color.White,
                ForeColor = principal ? Color.White : _azul
            };

            botao.FlatAppearance.BorderColor = _azul;
            botao.FlatAppearance.BorderSize = principal ? 0 : 1;

            return botao;
        }

        private void CarregarTurmas()
        {
            try
            {
                if (!VerificarAcesso())
                    return;

                const string sql = @"
                    SELECT
                        IdTurma,
                        CodigoTurma,
                        NomeTurma,
                        Periodo,
                        AnoLetivo,
                        Semestre,
                        Ativa
                    FROM dbo.Turmas
                    WHERE
                        @Pesquisa = ''
                        OR CodigoTurma LIKE @Texto
                        OR NomeTurma LIKE @Texto
                        OR Periodo LIKE @Texto
                    ORDER BY AnoLetivo DESC,
                             Semestre DESC,
                             NomeTurma";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                using SqlCommand comando =
                    new(sql, conexao);

                string pesquisa = txtPesquisa.Text.Trim();

                comando.Parameters.Add(
                    "@Pesquisa", SqlDbType.NVarChar, 100
                ).Value = pesquisa;

                comando.Parameters.Add(
                    "@Texto", SqlDbType.NVarChar, 210
                ).Value = "%" + pesquisa + "%";

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                _carregando = true;
                dgvTurmas.DataSource = tabela;
                ConfigurarColunas();
                dgvTurmas.ClearSelection();
                _carregando = false;

                lblQuantidade.Text =
                    $"Turmas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                _carregando = false;

                MessageBox.Show(
                    "Erro ao carregar turmas:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ConfigurarColunas()
        {
            if (dgvTurmas.Columns.Contains("IdTurma"))
                dgvTurmas.Columns["IdTurma"]!.Visible = false;

            DefinirColuna("CodigoTurma", "Código", 90);
            DefinirColuna("NomeTurma", "Nome da turma", 190);
            DefinirColuna("Periodo", "Período", 100);
            DefinirColuna("AnoLetivo", "Ano letivo", 85);
            DefinirColuna("Semestre", "Semestre", 75);
            DefinirColuna("Ativa", "Ativa", 65);
        }

        private void DefinirColuna(
            string nome, string titulo, float peso)
        {
            if (!dgvTurmas.Columns.Contains(nome))
                return;

            DataGridViewColumn coluna =
                dgvTurmas.Columns[nome]!;

            coluna.HeaderText = titulo;
            coluna.FillWeight = peso;
        }

        private void PreencherFormularioSelecionado()
        {
            if (dgvTurmas.SelectedRows.Count == 0)
                return;

            DataGridViewRow linha =
                dgvTurmas.SelectedRows[0];

            if (linha.Cells["IdTurma"].Value == null)
                return;

            _idTurmaSelecionada = Convert.ToInt32(
                linha.Cells["IdTurma"].Value);

            txtCodigo.Text = Convert.ToString(
                linha.Cells["CodigoTurma"].Value) ?? "";

            txtNome.Text = Convert.ToString(
                linha.Cells["NomeTurma"].Value) ?? "";

            string periodo = Convert.ToString(
                linha.Cells["Periodo"].Value) ?? "";

            if (!cmbPeriodo.Items.Contains(periodo))
                cmbPeriodo.Items.Add(periodo);

            cmbPeriodo.SelectedItem = periodo;

            int ano = Convert.ToInt32(
                linha.Cells["AnoLetivo"].Value);

            nudAno.Value = Math.Clamp(
                ano, (int)nudAno.Minimum, (int)nudAno.Maximum);

            cmbSemestre.SelectedItem = Convert.ToString(
                linha.Cells["Semestre"].Value);

            lblModo.Text =
                $"Editando turma ID {_idTurmaSelecionada}";

            btnSalvar.Text = "Salvar alterações";
            btnStatus.Enabled = true;
        }

        private void LimparFormulario()
        {
            _idTurmaSelecionada = null;

            txtCodigo.Clear();
            txtNome.Clear();

            cmbPeriodo.SelectedIndex = 0;
            cmbSemestre.SelectedIndex = 0;
            nudAno.Value = DateTime.Today.Year;

            lblModo.Text = "Novo cadastro";
            btnSalvar.Text = "Salvar turma";
            btnStatus.Enabled = false;

            dgvTurmas?.ClearSelection();
            txtCodigo.Focus();
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("Informe o código da turma.");
                txtCodigo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Informe o nome da turma.");
                txtNome.Focus();
                return false;
            }

            if (cmbPeriodo.SelectedItem == null ||
                cmbSemestre.SelectedItem == null)
            {
                MessageBox.Show(
                    "Selecione o período e o semestre.");
                return false;
            }

            return true;
        }

        private void SalvarTurma()
        {
            if (!ValidarFormulario())
                return;

            if (!VerificarAcesso())
                return;

            string codigo = txtCodigo.Text.Trim();
            string nome = txtNome.Text.Trim();
            string periodo =
                cmbPeriodo.SelectedItem!.ToString()!;

            int ano = (int)nudAno.Value;
            int semestre = int.Parse(
                cmbSemestre.SelectedItem!.ToString()!);

            bool editando = _idTurmaSelecionada.HasValue;

            string mensagem = editando
                ? "Deseja salvar as alterações desta turma?"
                : "Deseja cadastrar esta turma?";

            if (MessageBox.Show(
                mensagem,
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                using SqlTransaction transacao =
                    conexao.BeginTransaction(
                        IsolationLevel.Serializable);

                try
                {
                    ExigirAutorizacao(
                        conexao, transacao);

                    const string sqlDuplicado = @"
                        SELECT COUNT(1)
                        FROM dbo.Turmas WITH (UPDLOCK, HOLDLOCK)
                        WHERE CodigoTurma = @Codigo
                          AND IdTurma <> @IdTurma";

                    using (SqlCommand cmd = new(
                        sqlDuplicado, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@Codigo", SqlDbType.NVarChar, 30
                        ).Value = codigo;

                        cmd.Parameters.Add(
                            "@IdTurma", SqlDbType.Int
                        ).Value = _idTurmaSelecionada ?? -1;

                        if (Convert.ToInt32(
                            cmd.ExecuteScalar()) > 0)
                        {
                            throw new InvalidOperationException(
                                "Já existe uma turma com esse código.");
                        }
                    }

                    string sql = editando
                        ? @"
                            UPDATE dbo.Turmas
                            SET CodigoTurma = @Codigo,
                                NomeTurma = @Nome,
                                Periodo = @Periodo,
                                AnoLetivo = @Ano,
                                Semestre = @Semestre
                            WHERE IdTurma = @IdTurma"
                        : @"
                            INSERT INTO dbo.Turmas
                                (CodigoTurma, NomeTurma,
                                 Periodo, AnoLetivo,
                                 Semestre, Ativa)
                            VALUES
                                (@Codigo, @Nome,
                                 @Periodo, @Ano,
                                 @Semestre, 1)";

                    int idRegistrado;

                    using (SqlCommand cmd = new(
                        sql, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@Codigo", SqlDbType.NVarChar, 30
                        ).Value = codigo;

                        cmd.Parameters.Add(
                            "@Nome", SqlDbType.NVarChar, 100
                        ).Value = nome;

                        cmd.Parameters.Add(
                            "@Periodo", SqlDbType.NVarChar, 30
                        ).Value = periodo;

                        cmd.Parameters.Add(
                            "@Ano", SqlDbType.Int
                        ).Value = ano;

                        cmd.Parameters.Add(
                            "@Semestre", SqlDbType.Int
                        ).Value = semestre;

                        if (editando)
                        {
                            cmd.Parameters.Add(
                                "@IdTurma", SqlDbType.Int
                            ).Value = _idTurmaSelecionada!.Value;
                        }

                        int afetadas = cmd.ExecuteNonQuery();

                        if (afetadas != 1)
                        {
                            throw new InvalidOperationException(
                                "Não foi possível salvar a turma.");
                        }
                    }

                    if (editando)
                    {
                        idRegistrado = _idTurmaSelecionada!.Value;
                    }
                    else
                    {
                        using SqlCommand cmd = new(
                            "SELECT CONVERT(int, SCOPE_IDENTITY())",
                            conexao, transacao);

                        // A identidade deve ser recuperada
                        // no mesmo comando INSERT. Como
                        // SCOPE_IDENTITY depende do escopo,
                        // buscamos o ID pelo código exclusivo
                        // ainda dentro da transação.
                        cmd.CommandText = @"
                            SELECT IdTurma
                            FROM dbo.Turmas
                            WHERE CodigoTurma = @Codigo";

                        cmd.Parameters.Add(
                            "@Codigo", SqlDbType.NVarChar, 30
                        ).Value = codigo;

                        idRegistrado = Convert.ToInt32(
                            cmd.ExecuteScalar());
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        editando
                            ? "ALTERACAO_TURMA"
                            : "CADASTRO_TURMA",
                        $"{(editando ? "Alterou" : "Cadastrou")} " +
                        $"a turma {codigo} - {nome} " +
                        $"(ID {idRegistrado}).");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    editando
                        ? "Turma atualizada com sucesso!"
                        : "Turma cadastrada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarTurmas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar turma:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AlterarStatus()
        {
            if (!_idTurmaSelecionada.HasValue)
            {
                MessageBox.Show(
                    "Selecione uma turma na tabela.");
                return;
            }

            if (!VerificarAcesso())
                return;

            int idTurma = _idTurmaSelecionada.Value;

            if (MessageBox.Show(
                "Deseja alterar a situação desta turma?",
                "Confirmar alteração",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                using SqlTransaction transacao =
                    conexao.BeginTransaction();

                try
                {
                    ExigirAutorizacao(
                        conexao, transacao);

                    const string sql = @"
                        UPDATE dbo.Turmas
                        SET Ativa =
                            CASE WHEN Ativa = 1 THEN 0 ELSE 1 END
                        OUTPUT inserted.Ativa
                        WHERE IdTurma = @IdTurma";

                    bool novoStatus;

                    using (SqlCommand cmd = new(
                        sql, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdTurma", SqlDbType.Int
                        ).Value = idTurma;

                        object? resultado = cmd.ExecuteScalar();

                        if (resultado == null ||
                            resultado == DBNull.Value)
                        {
                            throw new InvalidOperationException(
                                "Turma não encontrada.");
                        }

                        novoStatus = Convert.ToBoolean(resultado);
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "ALTERACAO_STATUS_TURMA",
                        $"Turma ID {idTurma} " +
                        (novoStatus
                            ? "ativada."
                            : "desativada."));

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Situação da turma atualizada!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarTurmas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao alterar situação:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExigirAutorizacao(
            SqlConnection conexao,
            SqlTransaction transacao)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios u
                INNER JOIN dbo.Perfis p
                    ON u.IdPerfil = p.IdPerfil
                INNER JOIN dbo.PerfilPermissoes pp
                    ON p.IdPerfil = pp.IdPerfil
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE u.IdUsuario = @IdUsuario
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND p.NomePerfil = 'Coordenador'
                  AND pm.Codigo = 'GERENCIAR_TURMAS'
                  AND pm.Ativo = 1
                  AND pm.Plataforma = 'Desktop'";

            using SqlCommand cmd =
                new(sql, conexao, transacao);

            cmd.Parameters.Add(
                "@IdUsuario", SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui autorização para " +
                    "gerenciar turmas.");
            }
        }

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

            using SqlCommand cmd =
                new(sql, conexao, transacao);

            cmd.Parameters.Add(
                "@IdUsuario", SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            cmd.Parameters.Add(
                "@Acao", SqlDbType.NVarChar, 100
            ).Value = acao;

            cmd.Parameters.Add(
                "@Descricao", SqlDbType.NVarChar, 500
            ).Value = descricao;

            cmd.ExecuteNonQuery();
        }
    }
}
