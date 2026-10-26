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
    // Gerencia o cadastro, a edição e a situação das disciplinas.
    public class FrmGerenciarDisciplinas : Form
    {
        private const int PerfilCoordenador = 2;
        private const string PermissaoGerenciarDisciplinas = "GERENCIAR_DISCIPLINAS";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul = Color.FromArgb(40, 92, 170);
        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);

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
        private bool _atualizandoTabela;

        // Inicializa o formulário com o usuário autenticado.
        public FrmGerenciarDisciplinas(Usuario usuario)
        {
            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            CriarInterface();

            Shown += AoExibirFormulario;
        }

        // Define as propriedades visuais da janela.
        private void ConfigurarFormulario()
        {
            Text = "PIM IV - Gerenciamento de Disciplinas";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1150, 800);
            MinimumSize = new Size(950, 700);

            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Carrega os dados após validar o acesso.
        private void AoExibirFormulario(object? sender, EventArgs e)
        {
            if (!VerificarAcesso())
            {
                Close();
                return;
            }

            LimparFormulario();
            CarregarDisciplinas();
        }

        // Verifica se o coordenador possui autorização.
        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == PerfilCoordenador &&
                    _permissaoService.UsuarioTemPermissao(
                        _usuario,
                        PermissaoGerenciarDisciplinas);

                if (!autorizado)
                {
                    ExibirMensagem(
                        "Somente coordenadores autorizados podem " +
                        "gerenciar disciplinas.",
                        "Acesso negado",
                        MessageBoxIcon.Warning);
                }

                return autorizado;
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao verificar permissão", ex);
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
                RowCount = 5
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

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
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = _corAzul,
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

        // Cria os campos utilizados no cadastro e na edição.
        private void CriarAreaCadastro(TableLayoutPanel principal)
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

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 42));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            painel.Controls.Add(campos);

            campos.Controls.Add(
                CriarRotulo("Código da disciplina"), 0, 0);

            campos.Controls.Add(
                CriarRotulo("Nome da disciplina"), 1, 0);

            campos.Controls.Add(
                CriarRotulo("Carga horária (horas)"), 2, 0);

            txtCodigo = CriarCampoTexto(30);
            txtNome = CriarCampoTexto(150);

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

            CriarBotoesCadastro(campos);
        }

        // Padroniza os campos de texto do cadastro.
        private static TextBox CriarCampoTexto(int tamanhoMaximo)
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                MaxLength = tamanhoMaximo,
                Margin = new Padding(0, 4, 12, 0)
            };
        }

        // Cria os botões de cadastro e alteração.
        private void CriarBotoesCadastro(TableLayoutPanel campos)
        {
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

            btnSalvar.Click += (_, _) => SalvarDisciplina();
            btnNovo.Click += (_, _) => LimparFormulario();
            btnStatus.Click += (_, _) => AlterarStatus();

            botoes.Controls.Add(btnSalvar);
            botoes.Controls.Add(btnNovo);
            botoes.Controls.Add(btnStatus);

            campos.Controls.Add(botoes, 0, 2);
            campos.SetColumnSpan(botoes, 3);
        }

        // Cria os controles de pesquisa e atualização.
        private void CriarAreaPesquisa(TableLayoutPanel principal)
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

        // Configura a tabela de consulta das disciplinas.
        private void CriarTabela(TableLayoutPanel principal)
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
                _corAzul;

            dgvDisciplinas.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvDisciplinas.ColumnHeadersHeight = 42;
            dgvDisciplinas.RowTemplate.Height = 34;

            dgvDisciplinas.SelectionChanged += (_, _) =>
            {
                if (!_atualizandoTabela)
                {
                    PreencherFormularioSelecionado();
                }
            };

            principal.Controls.Add(dgvDisciplinas, 0, 3);
        }

        // Padroniza os rótulos do formulário.
        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 92, 170),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        // Padroniza a aparência dos botões.
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
                BackColor = principal ? _corAzul : Color.White,
                ForeColor = principal ? Color.White : _corAzul
            };

            botao.FlatAppearance.BorderColor = _corAzul;
            botao.FlatAppearance.BorderSize = principal ? 0 : 1;

            return botao;
        }

        // Consulta as disciplinas e atualiza a tabela.
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

                string pesquisa = txtPesquisa.Text.Trim();

                comando.Parameters.Add(
                    "@Pesquisa",
                    SqlDbType.NVarChar,
                    150).Value = pesquisa;

                comando.Parameters.Add(
                    "@Texto",
                    SqlDbType.NVarChar,
                    310).Value = "%" + pesquisa + "%";

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                AtualizarTabela(tabela);

                lblQuantidade.Text =
                    $"Disciplinas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar disciplinas", ex);
            }
        }

        // Atualiza os dados sem disparar eventos de seleção.
        private void AtualizarTabela(DataTable tabela)
        {
            _atualizandoTabela = true;

            try
            {
                dgvDisciplinas.DataSource = tabela;
                ConfigurarColunas();
                dgvDisciplinas.ClearSelection();
            }
            finally
            {
                _atualizandoTabela = false;
            }
        }

        // Define os títulos e as proporções das colunas.
        private void ConfigurarColunas()
        {
            if (dgvDisciplinas.Columns.Contains("IdDisciplina"))
            {
                dgvDisciplinas.Columns["IdDisciplina"]!.Visible = false;
            }

            DefinirColuna("CodigoDisciplina", "Código", 110);
            DefinirColuna("NomeDisciplina", "Nome da disciplina", 240);
            DefinirColuna("CargaHoraria", "Carga horária (h)", 110);
            DefinirColuna("Ativa", "Ativa", 70);
        }

        // Configura uma coluna existente na tabela.
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

        // Preenche os campos com a disciplina selecionada.
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

            int cargaHoraria =
                Convert.ToInt32(
                    linha.Cells["CargaHoraria"].Value);

            nudCargaHoraria.Value = Math.Clamp(
                cargaHoraria,
                (int)nudCargaHoraria.Minimum,
                (int)nudCargaHoraria.Maximum);

            btnSalvar.Text = "Salvar alterações";
            btnStatus.Enabled = true;
        }

        // Restaura o formulário para um novo cadastro.
        private void LimparFormulario()
        {
            _idDisciplinaSelecionada = null;

            txtCodigo.Clear();
            txtNome.Clear();

            nudCargaHoraria.Value = 60;

            btnSalvar.Text = "Salvar disciplina";
            btnStatus.Enabled = false;

            dgvDisciplinas.ClearSelection();

            txtCodigo.Focus();
        }

        // Valida os campos obrigatórios.
        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                ExibirMensagem(
                    "Informe o código da disciplina.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtCodigo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                ExibirMensagem(
                    "Informe o nome da disciplina.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtNome.Focus();
                return false;
            }

            if (nudCargaHoraria.Value <= 0)
            {
                ExibirMensagem(
                    "A carga horária deve ser maior que zero.",
                    "Validação",
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        // Cadastra ou atualiza uma disciplina.
        private void SalvarDisciplina()
        {
            if (!ValidarFormulario() || !VerificarAcesso())
            {
                return;
            }

            string codigo = txtCodigo.Text.Trim();
            string nome = txtNome.Text.Trim();
            int cargaHoraria = (int)nudCargaHoraria.Value;

            bool editando = _idDisciplinaSelecionada.HasValue;

            string mensagem = editando
                ? "Deseja salvar as alterações desta disciplina?"
                : "Deseja cadastrar esta disciplina?";

            if (!ConfirmarOperacao(mensagem))
            {
                return;
            }

            try
            {
                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                // Mantém a gravação e a auditoria na mesma transação.
                using SqlTransaction transacao =
                    conexao.BeginTransaction(
                        IsolationLevel.Serializable);

                try
                {
                    ExigirAutorizacao(conexao, transacao);

                    // Verifica se o código já pertence a outra disciplina.
                    ValidarCodigoDisponivel(
                        conexao,
                        transacao,
                        codigo,
                        _idDisciplinaSelecionada);

                    // Impede disciplinas com nomes duplicados.
                    ValidarNomeDisponivel(
                        conexao,
                        transacao,
                        nome,
                        _idDisciplinaSelecionada);

                    int idRegistrado = GravarDisciplina(
                        conexao,
                        transacao,
                        _idDisciplinaSelecionada,
                        codigo,
                        nome,
                        cargaHoraria);

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        editando
                            ? "ALTERACAO_DISCIPLINA"
                            : "CADASTRO_DISCIPLINA",
                        $"{(editando ? "Alterou" : "Cadastrou")} " +
                        $"a disciplina {codigo} - {nome} " +
                        $"(ID {idRegistrado}, {cargaHoraria} horas).");

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

                ExibirMensagem(
                    editando
                        ? "Disciplina atualizada com sucesso!"
                        : "Disciplina cadastrada com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarDisciplinas();
                LimparFormulario();
            }
            catch (InvalidOperationException ex)
            {
                // Apresenta mensagens amigáveis de validação.
                ExibirMensagem(
                    ex.Message,
                    "Validação",
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao salvar disciplina", ex);
            }
        }

        // Impede o cadastro de códigos duplicados.
        private static void ValidarCodigoDisponivel(
            SqlConnection conexao,
            SqlTransaction transacao,
            string codigo,
            int? idDisciplina)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Disciplinas WITH (UPDLOCK, HOLDLOCK)
                WHERE CodigoDisciplina = @Codigo
                  AND IdDisciplina <> @IdDisciplina";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Codigo",
                SqlDbType.NVarChar,
                30).Value = codigo;

            comando.Parameters.Add(
                "@IdDisciplina",
                SqlDbType.Int).Value = idDisciplina ?? -1;

            int quantidade =
                Convert.ToInt32(comando.ExecuteScalar());

            if (quantidade > 0)
            {
                throw new InvalidOperationException(
                    "Já existe uma disciplina com esse código.");
            }
        }

        // Impede disciplinas com nomes repetidos.
        private static void ValidarNomeDisponivel(
            SqlConnection conexao,
            SqlTransaction transacao,
            string nome,
            int? idDisciplina)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Disciplinas WITH (UPDLOCK, HOLDLOCK)
                WHERE UPPER(LTRIM(RTRIM(NomeDisciplina))) =
                      UPPER(LTRIM(RTRIM(@Nome)))
                  AND IdDisciplina <> @IdDisciplina";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                150).Value = nome;

            comando.Parameters.Add(
                "@IdDisciplina",
                SqlDbType.Int).Value = idDisciplina ?? -1;

            int quantidade =
                Convert.ToInt32(comando.ExecuteScalar());

            if (quantidade > 0)
            {
                throw new InvalidOperationException(
                    "Já existe uma disciplina cadastrada com esse nome.");
            }
        }

        // Insere ou atualiza a disciplina no banco.
        private static int GravarDisciplina(
            SqlConnection conexao,
            SqlTransaction transacao,
            int? idDisciplina,
            string codigo,
            string nome,
            int cargaHoraria)
        {
            if (idDisciplina.HasValue)
            {
                return AtualizarDisciplina(
                    conexao,
                    transacao,
                    idDisciplina.Value,
                    codigo,
                    nome,
                    cargaHoraria);
            }

            return InserirDisciplina(
                conexao,
                transacao,
                codigo,
                nome,
                cargaHoraria);
        }

        // Atualiza os dados de uma disciplina existente.
        private static int AtualizarDisciplina(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idDisciplina,
            string codigo,
            string nome,
            int cargaHoraria)
        {
            const string sql = @"
                UPDATE dbo.Disciplinas
                SET CodigoDisciplina = @Codigo,
                    NomeDisciplina = @Nome,
                    CargaHoraria = @Carga
                WHERE IdDisciplina = @IdDisciplina";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarParametrosDisciplina(
                comando,
                codigo,
                nome,
                cargaHoraria);

            comando.Parameters.Add(
                "@IdDisciplina",
                SqlDbType.Int).Value = idDisciplina;

            if (comando.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException(
                    "Disciplina não encontrada para atualização.");
            }

            return idDisciplina;
        }

        // Insere uma nova disciplina e retorna seu identificador.
        private static int InserirDisciplina(
            SqlConnection conexao,
            SqlTransaction transacao,
            string codigo,
            string nome,
            int cargaHoraria)
        {
            const string sql = @"
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

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarParametrosDisciplina(
                comando,
                codigo,
                nome,
                cargaHoraria);

            object? resultado = comando.ExecuteScalar();

            if (resultado == null || resultado == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Não foi possível cadastrar a disciplina.");
            }

            return Convert.ToInt32(resultado);
        }

        // Centraliza os parâmetros utilizados nas gravações.
        private static void AdicionarParametrosDisciplina(
            SqlCommand comando,
            string codigo,
            string nome,
            int cargaHoraria)
        {
            comando.Parameters.Add(
                "@Codigo",
                SqlDbType.NVarChar,
                30).Value = codigo;

            comando.Parameters.Add(
                "@Nome",
                SqlDbType.NVarChar,
                150).Value = nome;

            comando.Parameters.Add(
                "@Carga",
                SqlDbType.Int).Value = cargaHoraria;
        }

        // Alterna a situação da disciplina selecionada.
        private void AlterarStatus()
        {
            if (!_idDisciplinaSelecionada.HasValue)
            {
                ExibirMensagem(
                    "Selecione uma disciplina na tabela.",
                    "Validação",
                    MessageBoxIcon.Warning);

                return;
            }

            if (!VerificarAcesso())
            {
                return;
            }

            int idDisciplina = _idDisciplinaSelecionada.Value;

            if (!ConfirmarOperacao(
                "Deseja alterar a situação desta disciplina?"))
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
                    ExigirAutorizacao(conexao, transacao);

                    bool novoStatus = AtualizarStatusDisciplina(
                        conexao,
                        transacao,
                        idDisciplina);

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "ALTERACAO_STATUS_DISCIPLINA",
                        $"Disciplina ID {idDisciplina} " +
                        (novoStatus ? "ativada." : "desativada."));

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

                ExibirMensagem(
                    "Situação da disciplina atualizada!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarDisciplinas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao alterar situação", ex);
            }
        }

        // Altera o status sem excluir o registro.
        private static bool AtualizarStatusDisciplina(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idDisciplina)
        {
            const string sql = @"
                UPDATE dbo.Disciplinas
                SET Ativa =
                    CASE WHEN Ativa = 1 THEN 0 ELSE 1 END
                OUTPUT inserted.Ativa
                WHERE IdDisciplina = @IdDisciplina";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdDisciplina",
                SqlDbType.Int).Value = idDisciplina;

            object? resultado = comando.ExecuteScalar();

            if (resultado == null || resultado == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Disciplina não encontrada.");
            }

            return Convert.ToBoolean(resultado);
        }

        // Revalida a autorização no banco antes de modificar dados.
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
                100).Value = PermissaoGerenciarDisciplinas;

            bool autorizado =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!autorizado)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui autorização para " +
                    "gerenciar disciplinas.");
            }
        }

        // Registra a operação na mesma transação dos dados.
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

        // Solicita confirmação antes de executar uma alteração.
        private static bool ConfirmarOperacao(string mensagem)
        {
            return MessageBox.Show(
                mensagem,
                "Confirmar",
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
            string contexto,
            Exception excecao)
        {
            ExibirMensagem(
                contexto + ":\n" + excecao.Message,
                "Erro",
                MessageBoxIcon.Error);
        }
    }
}