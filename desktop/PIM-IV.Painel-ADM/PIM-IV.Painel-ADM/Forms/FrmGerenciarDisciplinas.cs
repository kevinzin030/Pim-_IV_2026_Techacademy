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
    public class FrmGerenciarDisciplinas : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        private TextBox txtCodigo = null!;
        private TextBox txtNome = null!;
        private TextBox txtPesquisa = null!;
        private NumericUpDown nudCargaHoraria = null!;

        private DataGridView dgvDisciplinas = null!;
        private Label lblQuantidade = null!;

        private Button btnSalvar = null!;
        private Button btnNovo = null!;
        private Button btnStatus = null!;

        private int? _idDisciplinaSelecionada;
        private bool _carregando;

        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public FrmGerenciarDisciplinas(Usuario usuario)
        {
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "PIM IV - Gerenciamento de Disciplinas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1150, 800);
            MinimumSize = new Size(950, 700);
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
                CarregarDisciplinas();
            };
        }

        // =====================================================
        // VERIFICAÇÃO DE PERMISSÕES
        // =====================================================

        private bool VerificarAcesso()
        {
            try
            {
                if (_usuario.IdPerfil != 2 ||
                    !_permissaoService.UsuarioTemPermissao(
                        _usuario, "GERENCIAR_DISCIPLINAS"))
                {
                    MessageBox.Show(
                        "Somente coordenadores autorizados podem " +
                        "gerenciar disciplinas.",
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
                    "Erro ao verificar permissão:\n" + ex.Message,
                    "Erro de autorização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        // =====================================================
        // INTERFACE PRINCIPAL
        // =====================================================

        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 5
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            // Espaço suficiente para campos e botões.
            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 170));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 60));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Gerenciamento de Disciplinas",
                Font = new Font(
                    "Segoe UI", 19, FontStyle.Bold),
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
                Text = "Disciplinas encontradas: 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray
            };

            principal.Controls.Add(lblQuantidade, 0, 4);
        }

        // =====================================================
        // ÁREA DE CADASTRO
        // =====================================================

        private void CriarAreaCadastro(
            TableLayoutPanel principal)
        {
            Panel painel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            principal.Controls.Add(painel, 0, 1);

            TableLayoutPanel campos = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3
            };

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            campos.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));

            // Rótulos.
            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28));

            // Campos com altura proporcional.
            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            // Linha exclusiva para os botões.
            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            painel.Controls.Add(campos);

            campos.Controls.Add(
                CriarRotulo("Código da disciplina"), 0, 0);

            campos.Controls.Add(
                CriarRotulo("Nome da disciplina"), 1, 0);

            campos.Controls.Add(
                CriarRotulo("Carga horária (horas)"), 2, 0);

            txtCodigo = new TextBox
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                MaxLength = 30,
                Margin = new Padding(0, 4, 12, 0)
            };

            txtNome = new TextBox
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                MaxLength = 150,
                Margin = new Padding(0, 4, 12, 0)
            };

            nudCargaHoraria = new NumericUpDown
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                Minimum = 1,
                Maximum = 10000,
                Value = 60,
                ThousandsSeparator = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            campos.Controls.Add(txtCodigo, 0, 1);
            campos.Controls.Add(txtNome, 1, 1);
            campos.Controls.Add(nudCargaHoraria, 2, 1);

            // =================================================
            // BOTÕES DE CADASTRO
            // =================================================

            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 5, 0, 0),
                Margin = Padding.Empty
            };

            btnSalvar = CriarBotao(
                "Salvar disciplina", 165, true);

            btnNovo = CriarBotao(
                "Novo / Limpar", 145, false);

            btnStatus = CriarBotao(
                "Ativar / Desativar", 170, false);

            btnSalvar.Click += (_, _) =>
                SalvarDisciplina();

            btnNovo.Click += (_, _) =>
                LimparFormulario();

            btnStatus.Click += (_, _) =>
                AlterarStatus();

            botoes.Controls.Add(btnSalvar);
            botoes.Controls.Add(btnNovo);
            botoes.Controls.Add(btnStatus);

            campos.Controls.Add(botoes, 0, 2);
            campos.SetColumnSpan(botoes, 3);
        }

        // =====================================================
        // ÁREA DE PESQUISA
        // =====================================================

        private void CriarAreaPesquisa(
            TableLayoutPanel principal)
        {
            FlowLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            txtPesquisa = new TextBox
            {
                Width = 380,
                AutoSize = false,
                Height = 36,
                Font = new Font("Segoe UI", 11),
                PlaceholderText =
                    "Pesquisar por código ou nome da disciplina...",
                Margin = new Padding(0, 2, 12, 0)
            };

            txtPesquisa.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CarregarDisciplinas();
                    e.SuppressKeyPress = true;
                }
            };

            Button btnPesquisar =
                CriarBotao("Pesquisar", 120, true);

            Button btnAtualizar =
                CriarBotao("Atualizar", 120, false);

            btnPesquisar.Click += (_, _) =>
                CarregarDisciplinas();

            btnAtualizar.Click += (_, _) =>
            {
                txtPesquisa.Clear();
                CarregarDisciplinas();
                LimparFormulario();
            };

            painel.Controls.Add(txtPesquisa);
            painel.Controls.Add(btnPesquisar);
            painel.Controls.Add(btnAtualizar);

            principal.Controls.Add(painel, 0, 2);
        }

        // =====================================================
        // TABELA DE DISCIPLINAS
        // =====================================================

        private void CriarTabela(
            TableLayoutPanel principal)
        {
            dgvDisciplinas = new DataGridView
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

            dgvDisciplinas.EnableHeadersVisualStyles = false;

            dgvDisciplinas.ColumnHeadersDefaultCellStyle.BackColor =
                _azul;

            dgvDisciplinas.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvDisciplinas.ColumnHeadersHeight = 42;
            dgvDisciplinas.RowTemplate.Height = 34;

            dgvDisciplinas.SelectionChanged += (_, _) =>
            {
                if (!_carregando)
                {
                    PreencherFormularioSelecionado();
                }
            };

            principal.Controls.Add(dgvDisciplinas, 0, 3);
        }

        // =====================================================
        // COMPONENTES VISUAIS
        // =====================================================

        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 92, 170),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CriarBotao(
            string texto,
            int largura,
            bool principal)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 36,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(5, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = principal ? _azul : Color.White,
                ForeColor = principal ? Color.White : _azul
            };

            botao.FlatAppearance.BorderColor = _azul;
            botao.FlatAppearance.BorderSize =
                principal ? 0 : 1;

            return botao;
        }

        // =====================================================
        // CARREGAMENTO DE DISCIPLINAS
        // =====================================================

        private void CarregarDisciplinas()
        {
            try
            {
                const string sql = @"
                    SELECT
                        IdDisciplina,
                        CodigoDisciplina,
                        NomeDisciplina,
                        CargaHoraria,
                        Ativa
                    FROM dbo.Disciplinas
                    WHERE @Pesquisa = ''
                       OR CodigoDisciplina LIKE @Texto
                       OR NomeDisciplina LIKE @Texto
                    ORDER BY NomeDisciplina";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                using SqlCommand comando =
                    new(sql, conexao);

                string pesquisa =
                    txtPesquisa.Text.Trim();

                comando.Parameters.Add(
                    "@Pesquisa",
                    SqlDbType.NVarChar,
                    150
                ).Value = pesquisa;

                comando.Parameters.Add(
                    "@Texto",
                    SqlDbType.NVarChar,
                    310
                ).Value = "%" + pesquisa + "%";

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                _carregando = true;

                dgvDisciplinas.DataSource = tabela;

                ConfigurarColunas();

                dgvDisciplinas.ClearSelection();

                _carregando = false;

                lblQuantidade.Text =
                    $"Disciplinas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                _carregando = false;

                MessageBox.Show(
                    "Erro ao carregar disciplinas:\n" +
                    ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // CONFIGURAÇÃO DAS COLUNAS
        // =====================================================

        private void ConfigurarColunas()
        {
            if (dgvDisciplinas.Columns.Contains(
                "IdDisciplina"))
            {
                dgvDisciplinas.Columns[
                    "IdDisciplina"]!.Visible = false;
            }

            DefinirColuna(
                "CodigoDisciplina", "Código", 110);

            DefinirColuna(
                "NomeDisciplina", "Nome da disciplina", 240);

            DefinirColuna(
                "CargaHoraria", "Carga horária (h)", 110);

            DefinirColuna(
                "Ativa", "Ativa", 70);
        }

        private void DefinirColuna(
            string nome,
            string titulo,
            float peso)
        {
            if (!dgvDisciplinas.Columns.Contains(nome))
            {
                return;
            }

            DataGridViewColumn coluna =
                dgvDisciplinas.Columns[nome]!;

            coluna.HeaderText = titulo;
            coluna.FillWeight = peso;
        }

        // =====================================================
        // SELEÇÃO DE DISCIPLINA
        // =====================================================

        private void PreencherFormularioSelecionado()
        {
            if (dgvDisciplinas.SelectedRows.Count == 0)
            {
                return;
            }

            DataGridViewRow linha =
                dgvDisciplinas.SelectedRows[0];

            if (linha.Cells["IdDisciplina"].Value == null)
            {
                return;
            }

            _idDisciplinaSelecionada =
                Convert.ToInt32(
                    linha.Cells["IdDisciplina"].Value);

            txtCodigo.Text =
                Convert.ToString(
                    linha.Cells["CodigoDisciplina"].Value) ?? "";

            txtNome.Text =
                Convert.ToString(
                    linha.Cells["NomeDisciplina"].Value) ?? "";

            int carga =
                Convert.ToInt32(
                    linha.Cells["CargaHoraria"].Value);

            nudCargaHoraria.Value = Math.Clamp(
                carga,
                (int)nudCargaHoraria.Minimum,
                (int)nudCargaHoraria.Maximum);

            btnSalvar.Text = "Salvar alterações";
            btnStatus.Enabled = true;
        }

        // =====================================================
        // LIMPAR FORMULÁRIO
        // =====================================================

        private void LimparFormulario()
        {
            _idDisciplinaSelecionada = null;

            txtCodigo.Clear();
            txtNome.Clear();

            nudCargaHoraria.Value = 60;

            btnSalvar.Text = "Salvar disciplina";
            btnStatus.Enabled = false;

            dgvDisciplinas?.ClearSelection();

            txtCodigo.Focus();
        }

        // =====================================================
        // VALIDAÇÃO
        // =====================================================

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show(
                    "Informe o código da disciplina.");

                txtCodigo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show(
                    "Informe o nome da disciplina.");

                txtNome.Focus();
                return false;
            }

            if (nudCargaHoraria.Value <= 0)
            {
                MessageBox.Show(
                    "A carga horária deve ser maior que zero.");

                return false;
            }

            return true;
        }

        // =====================================================
        // CADASTRO E EDIÇÃO DE DISCIPLINAS
        // =====================================================

        private void SalvarDisciplina()
        {
            if (!ValidarFormulario())
            {
                return;
            }

            if (!VerificarAcesso())
            {
                return;
            }

            string codigo = txtCodigo.Text.Trim();
            string nome = txtNome.Text.Trim();
            int carga = (int)nudCargaHoraria.Value;

            bool editando =
                _idDisciplinaSelecionada.HasValue;

            string mensagem = editando
                ? "Deseja salvar as alterações desta disciplina?"
                : "Deseja cadastrar esta disciplina?";

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

                    // Evita códigos duplicados.
                    const string sqlDuplicado = @"
                        SELECT COUNT(1)
                        FROM dbo.Disciplinas
                             WITH (UPDLOCK, HOLDLOCK)
                        WHERE CodigoDisciplina = @Codigo
                          AND IdDisciplina <> @IdDisciplina";

                    using (SqlCommand cmd = new(
                        sqlDuplicado, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@Codigo",
                            SqlDbType.NVarChar,
                            30
                        ).Value = codigo;

                        cmd.Parameters.Add(
                            "@IdDisciplina",
                            SqlDbType.Int
                        ).Value =
                            _idDisciplinaSelecionada ?? -1;

                        if (Convert.ToInt32(
                            cmd.ExecuteScalar()) > 0)
                        {
                            throw new InvalidOperationException(
                                "Já existe uma disciplina " +
                                "com esse código.");
                        }
                    }

                    int idRegistrado;

                    if (editando)
                    {
                        const string sqlAtualizar = @"
                            UPDATE dbo.Disciplinas
                            SET CodigoDisciplina = @Codigo,
                                NomeDisciplina = @Nome,
                                CargaHoraria = @Carga
                            WHERE IdDisciplina = @IdDisciplina";

                        using SqlCommand cmd = new(
                            sqlAtualizar,
                            conexao,
                            transacao);

                        AdicionarParametrosDisciplina(
                            cmd, codigo, nome, carga);

                        cmd.Parameters.Add(
                            "@IdDisciplina",
                            SqlDbType.Int
                        ).Value =
                            _idDisciplinaSelecionada!.Value;

                        if (cmd.ExecuteNonQuery() != 1)
                        {
                            throw new InvalidOperationException(
                                "Disciplina não encontrada " +
                                "para atualização.");
                        }

                        idRegistrado =
                            _idDisciplinaSelecionada.Value;
                    }
                    else
                    {
                        const string sqlInserir = @"
                            INSERT INTO dbo.Disciplinas
                                (
                                    CodigoDisciplina,
                                    NomeDisciplina,
                                    CargaHoraria,
                                    Ativa
                                )
                            OUTPUT inserted.IdDisciplina
                            VALUES
                                (
                                    @Codigo,
                                    @Nome,
                                    @Carga,
                                    1
                                )";

                        using SqlCommand cmd = new(
                            sqlInserir,
                            conexao,
                            transacao);

                        AdicionarParametrosDisciplina(
                            cmd, codigo, nome, carga);

                        idRegistrado = Convert.ToInt32(
                            cmd.ExecuteScalar());
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        editando
                            ? "ALTERACAO_DISCIPLINA"
                            : "CADASTRO_DISCIPLINA",
                        $"{(editando ? "Alterou" : "Cadastrou")} " +
                        $"a disciplina {codigo} - {nome} " +
                        $"(ID {idRegistrado}, {carga} horas).");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    editando
                        ? "Disciplina atualizada com sucesso!"
                        : "Disciplina cadastrada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarDisciplinas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar disciplina:\n" +
                    ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // PARÂMETROS SQL
        // =====================================================

        private static void AdicionarParametrosDisciplina(
            SqlCommand comando,
            string codigo,
            string nome,
            int carga)
        {
            comando.Parameters.Add(
                "@Codigo",
                SqlDbType.NVarChar,
                30
            ).Value = codigo;

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                150
            ).Value = nome;

            comando.Parameters.Add(
                "@Carga",
                SqlDbType.Int
            ).Value = carga;
        }

        // =====================================================
        // ATIVAR / DESATIVAR DISCIPLINA
        // =====================================================

        private void AlterarStatus()
        {
            if (!_idDisciplinaSelecionada.HasValue)
            {
                MessageBox.Show(
                    "Selecione uma disciplina na tabela.");

                return;
            }

            if (!VerificarAcesso())
            {
                return;
            }

            int idDisciplina =
                _idDisciplinaSelecionada.Value;

            if (MessageBox.Show(
                "Deseja alterar a situação desta disciplina?",
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
                        UPDATE dbo.Disciplinas
                        SET Ativa =
                            CASE
                                WHEN Ativa = 1 THEN 0
                                ELSE 1
                            END
                        OUTPUT inserted.Ativa
                        WHERE IdDisciplina = @IdDisciplina";

                    bool novoStatus;

                    using (SqlCommand cmd = new(
                        sql, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdDisciplina",
                            SqlDbType.Int
                        ).Value = idDisciplina;

                        object? resultado =
                            cmd.ExecuteScalar();

                        if (resultado == null ||
                            resultado == DBNull.Value)
                        {
                            throw new InvalidOperationException(
                                "Disciplina não encontrada.");
                        }

                        novoStatus =
                            Convert.ToBoolean(resultado);
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "ALTERACAO_STATUS_DISCIPLINA",
                        $"Disciplina ID {idDisciplina} " +
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
                    "Situação da disciplina atualizada!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarDisciplinas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao alterar situação:\n" +
                    ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // AUTORIZAÇÃO NO SQL SERVER
        // =====================================================

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
                  AND u.IdPerfil = 2
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND pm.Codigo = 'GERENCIAR_DISCIPLINAS'
                  AND pm.Ativo = 1";

            using SqlCommand cmd =
                new(sql, conexao, transacao);

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            if (Convert.ToInt32(
                cmd.ExecuteScalar()) == 0)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui autorização para " +
                    "gerenciar disciplinas.");
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
                    )";

            using SqlCommand cmd =
                new(sql, conexao, transacao);

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            cmd.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                100
            ).Value = acao;

            cmd.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                500
            ).Value = descricao;

            cmd.ExecuteNonQuery();
        }
    }
}