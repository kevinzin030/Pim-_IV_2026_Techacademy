using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia o cadastro, a edição e a situação das turmas acadêmicas.
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

        // Identifica a turma em edição.
        private int? _idTurmaSelecionada;

        // Controla os eventos durante o carregamento.
        private bool _carregando;

        // Inicializa o formulário com o usuário autenticado.
        public FrmGerenciarTurmas(Usuario usuario)
        {
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "PIM IV - Gerenciamento de Turmas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1050, 540);
            MinimumSize = new Size(800, 440);
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

        // Confirma se o coordenador possui a permissão necessária.
        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == 2 &&
                    _permissaoService.UsuarioTemPermissao(
                        _usuario,
                        "GERENCIAR_TURMAS");

                if (!autorizado)
                {
                    MessageBox.Show(
                        "Somente coordenadores autorizados podem " +
                        "gerenciar turmas.",
                        "Acesso negado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return autorizado;
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao verificar autorização",
                    ex);

                return false;
            }
        }

        // Organiza as áreas principais da interface.
        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 52));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 205));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 52));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Gerenciamento de Turmas",
                Font = new Font(
                    "Segoe UI",
                    21,
                    FontStyle.Bold),

                ForeColor = _azul,
                Dock = DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            CriarAreaCadastro(principal);
            CriarAreaPesquisa(principal);
            CriarTabela(principal);

            lblQuantidade = new Label
            {
                Text = "Turmas encontradas: 0",
                Dock = DockStyle.Fill,

                TextAlign =
                    ContentAlignment.MiddleLeft,

                ForeColor = Color.DimGray
            };

            principal.Controls.Add(lblQuantidade, 0, 4);
        }

        // Cria os campos e botões utilizados no cadastro.
        private void CriarAreaCadastro(
            TableLayoutPanel principal)
        {
            Panel painel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
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
                new RowStyle(SizeType.Absolute, 48));

            campos.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            painel.Controls.Add(campos);

            CriarCamposIdentificacao(campos);
            CriarCampoPeriodo(campos);
            CriarBotoesCadastro(campos);
        }

        // Cria os campos de identificação e período letivo.
        private void CriarCamposIdentificacao(
            TableLayoutPanel campos)
        {
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

                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };

            // Disponibiliza os quatro semestres do curso.
            cmbSemestre.Items.AddRange(
                new object[]
                {
        "1",
        "2",
        "3",
        "4"
                });

            cmbSemestre.SelectedIndex = 0;

            campos.Controls.Add(txtCodigo, 0, 1);
            campos.Controls.Add(txtNome, 1, 1);
            campos.Controls.Add(nudAno, 2, 1);
            campos.Controls.Add(cmbSemestre, 3, 1);
        }

        // Cria a seleção do período e o indicador de edição.
        private void CriarCampoPeriodo(
            TableLayoutPanel campos)
        {
            FlowLayoutPanel painelPeriodo = new()
            {
                Dock = DockStyle.Fill,

                FlowDirection =
                    FlowDirection.LeftToRight,

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

                DropDownStyle =
                    ComboBoxStyle.DropDownList,

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
                    "Segoe UI",
                    10,
                    FontStyle.Bold),

                ForeColor = _azul,

                Margin = new Padding(12, 12, 0, 0)
            };

            painelPeriodo.Controls.Add(lblPeriodo);
            painelPeriodo.Controls.Add(cmbPeriodo);
            painelPeriodo.Controls.Add(lblModo);

            campos.Controls.Add(painelPeriodo, 0, 2);
            campos.SetColumnSpan(painelPeriodo, 4);
        }

        // Cria os botões de gerenciamento.
        private void CriarBotoesCadastro(
            TableLayoutPanel campos)
        {
            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,

                FlowDirection =
                    FlowDirection.RightToLeft,

                WrapContents = false
            };

            btnSalvar = CriarBotao(
                "Salvar turma", 150, true);

            btnNovo = CriarBotao(
                "Novo / Limpar", 145, false);

            btnStatus = CriarBotao(
                "Ativar / Desativar", 170, false);

            btnSalvar.Click += (_, _) =>
                SalvarTurma();

            btnNovo.Click += (_, _) =>
                LimparFormulario();

            btnStatus.Click += (_, _) =>
                AlterarStatus();

            botoes.Controls.Add(btnSalvar);
            botoes.Controls.Add(btnNovo);
            botoes.Controls.Add(btnStatus);

            campos.Controls.Add(botoes, 0, 3);
            campos.SetColumnSpan(botoes, 4);
        }

        // Cria a pesquisa por código, nome ou período.
        private void CriarAreaPesquisa(
            TableLayoutPanel principal)
        {
            FlowLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents = false,
                AutoScroll = true,

                Padding = new Padding(0, 5, 0, 0)
            };

            txtPesquisa = new TextBox
            {
                Width = 370,

                PlaceholderText =
                    "Pesquisar por código, nome ou período...",

                Margin = new Padding(0, 4, 12, 0)
            };

            // Permite pesquisar pressionando Enter.
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

            btnPesquisar.Click += (_, _) =>
                CarregarTurmas();

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

        // Cria e configura a tabela de turmas.
        private void CriarTabela(
            TableLayoutPanel principal)
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
                {
                    PreencherFormularioSelecionado();
                }
            };

            principal.Controls.Add(dgvTurmas, 0, 3);
        }

        // Padroniza os rótulos do formulário.
        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,

                Font = new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold),

                ForeColor =
                    Color.FromArgb(40, 50, 65)
            };
        }

        // Padroniza os botões principais e secundários.
        private Button CriarBotao(
            string texto,
            int largura,
            bool principal)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 38,

                Margin = new Padding(5, 0, 0, 0),

                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,

                BackColor = principal
                    ? _azul
                    : Color.White,

                ForeColor = principal
                    ? Color.White
                    : _azul
            };

            botao.FlatAppearance.BorderColor = _azul;

            botao.FlatAppearance.BorderSize =
                principal ? 0 : 1;

            return botao;
        }

        // Consulta as turmas por meio da API.
        private void CarregarTurmas()
        {
            if (!VerificarAcesso())
            {
                return;
            }

            try
            {
                DataTable tabela = ConsultarRegistros();

                // Suspende os eventos durante a atualização.
                _carregando = true;

                try
                {
                    dgvTurmas.DataSource = tabela;

                    ConfigurarColunas();

                    dgvTurmas.ClearSelection();
                }
                finally
                {
                    _carregando = false;
                }

                LimparFormulario();

                lblQuantidade.Text =
                    $"Turmas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar turmas",
                    ex);
            }
        }

        // Configura os títulos e a proporção das colunas.
        private void ConfigurarColunas()
        {
            if (dgvTurmas.Columns.Contains("IdTurma"))
            {
                dgvTurmas.Columns["IdTurma"]!.Visible =
                    false;
            }

            DefinirColuna(
                "CodigoTurma",
                "Código",
                100);

            DefinirColuna(
                "NomeTurma",
                "Nome da turma",
                200);

            DefinirColuna(
                "Periodo",
                "Período",
                110);

            DefinirColuna(
                "AnoLetivo",
                "Ano letivo",
                85);

            DefinirColuna(
                "Semestre",
                "Semestre",
                85);

            DefinirColuna(
                "Ativa",
                "Ativa",
                60);
        }

        // Configura uma coluna existente na tabela.
        private void DefinirColuna(
            string nome,
            string titulo,
            float peso)
        {
            if (!dgvTurmas.Columns.Contains(nome))
            {
                return;
            }

            DataGridViewColumn coluna =
                dgvTurmas.Columns[nome]!;

            coluna.HeaderText = titulo;
            coluna.FillWeight = peso;
        }

        // Preenche os campos com os dados da turma selecionada.
        private void PreencherFormularioSelecionado()
        {
            if (dgvTurmas.SelectedRows.Count == 0)
            {
                return;
            }

            DataGridViewRow linha =
                dgvTurmas.SelectedRows[0];

            if (linha.Cells["IdTurma"].Value == null)
            {
                return;
            }

            _idTurmaSelecionada = Convert.ToInt32(
                linha.Cells["IdTurma"].Value);

            txtCodigo.Text = Convert.ToString(
                linha.Cells["CodigoTurma"].Value) ?? "";

            txtNome.Text = Convert.ToString(
                linha.Cells["NomeTurma"].Value) ?? "";

            string periodo = Convert.ToString(
                linha.Cells["Periodo"].Value) ?? "";

            // Mantém compatibilidade com períodos existentes.
            if (!cmbPeriodo.Items.Contains(periodo))
            {
                cmbPeriodo.Items.Add(periodo);
            }

            cmbPeriodo.SelectedItem = periodo;

            int ano = Convert.ToInt32(
                linha.Cells["AnoLetivo"].Value);

            nudAno.Value = Math.Clamp(
                ano,
                (int)nudAno.Minimum,
                (int)nudAno.Maximum);

            cmbSemestre.SelectedItem = Convert.ToString(
                linha.Cells["Semestre"].Value);

            lblModo.Text =
                $"Editando turma ID {_idTurmaSelecionada}";

            btnSalvar.Text = "Salvar alterações";
            btnStatus.Enabled = true;
        }

        // Restaura os campos para um novo cadastro.
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

            dgvTurmas.ClearSelection();

            txtCodigo.Focus();
        }

        // Valida os campos obrigatórios.
        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show(
                    "Informe o código da turma.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtCodigo.Focus();

                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show(
                    "Informe o nome da turma.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtNome.Focus();

                return false;
            }

            if (cmbPeriodo.SelectedItem == null ||
                cmbSemestre.SelectedItem == null)
            {
                MessageBox.Show(
                    "Selecione o período e o semestre.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        // Cadastra ou atualiza uma turma por meio da API.
        private void SalvarTurma()
        {
            if (!ValidarFormulario() || !VerificarAcesso())
            {
                return;
            }

            string codigo = txtCodigo.Text.Trim();
            string nome = txtNome.Text.Trim();

            string periodo =
                cmbPeriodo.SelectedItem!.ToString()!;

            int ano = (int)nudAno.Value;

            int semestre = int.Parse(
                cmbSemestre.SelectedItem!.ToString()!);

            bool editando =
                _idTurmaSelecionada.HasValue;

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
                // A API realiza a validação e a auditoria.
                string rota = editando
                    ? $"academico/turmas/{_idTurmaSelecionada.Value}"
                    : "academico/turmas";

                ExecutarApi(
                    editando
                        ? HttpMethod.Put
                        : HttpMethod.Post,

                    rota,

                    new
                    {
                        codigo,
                        nome,
                        periodo,
                        anoLetivo = ano,
                        semestre
                    });

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
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao salvar turma",
                    ex);
            }
        }

        // Alterna a situação da turma sem excluir o registro.
        private void AlterarStatus()
        {
            if (!_idTurmaSelecionada.HasValue)
            {
                MessageBox.Show(
                    "Selecione uma turma na tabela.",
                    "Validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!VerificarAcesso())
            {
                return;
            }

            int idTurma =
                _idTurmaSelecionada.Value;

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
                // Consulta o status atual da turma.
                DataTable registros =
                    ConsultarRegistros(false);

                DataRow? registro =
                    registros.Rows.Find(idTurma);

                if (registro == null)
                {
                    throw new InvalidOperationException(
                        "Registro não encontrado.");
                }

                bool ativa =
                    !Convert.ToBoolean(registro["Ativa"]);

                // Solicita a alteração de status pela API.
                ExecutarApi(
                    HttpMethod.Patch,

                    $"academico/turmas/{idTurma}/status",

                    new
                    {
                        ativa
                    });

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
                MostrarErro(
                    "Erro ao alterar situação",
                    ex);
            }
        }

        // Exibe mensagens de erro padronizadas.
        private static void MostrarErro(
            string titulo,
            Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                titulo,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // Executa requisições autenticadas na API.
        private static JsonElement ExecutarApi(
            HttpMethod metodo,
            string rota,
            object? dados = null)
        {
            return Task.Run(async () =>
            {
                using HttpRequestMessage requisicao =
                    new(metodo, rota);

                if (dados != null)
                {
                    requisicao.Content =
                        JsonContent.Create(dados);
                }

                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.SendAsync(
                        requisicao);

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                // Apresenta a mensagem retornada pela API.
                if (!resposta.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        documento.RootElement.TryGetProperty(
                            "mensagem",
                            out JsonElement mensagem)

                            ? mensagem.GetString()

                            : "Não foi possível concluir a operação.");
                }

                return documento.RootElement.Clone();

            }).GetAwaiter().GetResult();
        }

        // Converte os registros JSON para a tabela de turmas.
        private DataTable ConsultarRegistros(
            bool filtrar = true)
        {
            string pesquisa = filtrar
                ? txtPesquisa.Text.Trim()
                : string.Empty;

            JsonElement resposta = ExecutarApi(
                HttpMethod.Get,
                "academico/turmas");

            DataTable tabela = new();

            tabela.Columns.Add(
                "IdTurma",
                typeof(int));

            tabela.Columns.Add(
                "CodigoTurma",
                typeof(string));

            tabela.Columns.Add(
                "NomeTurma",
                typeof(string));

            tabela.Columns.Add(
                "Periodo",
                typeof(string));

            tabela.Columns.Add(
                "AnoLetivo",
                typeof(int));

            tabela.Columns.Add(
                "Semestre",
                typeof(int));

            tabela.Columns.Add(
                "Ativa",
                typeof(bool));

            tabela.PrimaryKey = new[]
            {
                tabela.Columns["IdTurma"]!
            };

            // Preenche a tabela com os registros da API.
            foreach (JsonElement registro in
                resposta.GetProperty("registros").EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        "IdTurma").GetInt32(),

                    registro.GetProperty(
                        "CodigoTurma").GetString(),

                    registro.GetProperty(
                        "NomeTurma").GetString(),

                    registro.GetProperty(
                        "Periodo").GetString(),

                    registro.GetProperty(
                        "AnoLetivo").GetInt32(),

                    registro.GetProperty(
                        "Semestre").GetInt32(),

                    registro.GetProperty(
                        "Ativa").GetBoolean());
            }

            // Aplica a pesquisa e ordena as turmas pelo código.
            DataTable filtrada = tabela.Clone();

            foreach (DataRow linha in
                tabela.Select("", "CodigoTurma ASC"))
            {
                string codigo =
                    Convert.ToString(
                        linha["CodigoTurma"]) ?? "";

                string nome =
                    Convert.ToString(
                        linha["NomeTurma"]) ?? "";

                string periodo =
                    Convert.ToString(
                        linha["Periodo"]) ?? "";

                bool correspondePesquisa =
                    string.IsNullOrEmpty(pesquisa) ||

                    codigo.Contains(
                        pesquisa,
                        StringComparison.OrdinalIgnoreCase) ||

                    nome.Contains(
                        pesquisa,
                        StringComparison.OrdinalIgnoreCase) ||

                    periodo.Contains(
                        pesquisa,
                        StringComparison.OrdinalIgnoreCase);

                if (correspondePesquisa)
                {
                    filtrada.ImportRow(linha);
                }
            }

            return filtrada;
        }
    }
}