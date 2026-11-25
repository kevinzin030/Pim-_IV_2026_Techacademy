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
    // Gerencia o cadastro, a edição e a situação das disciplinas.
    public class FrmGerenciarDisciplinas : Form
    {
        private const int PerfilCoordenador = 2;
        private const string PermissaoGerenciarDisciplinas =
            "GERENCIAR_DISCIPLINAS";

        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corAzul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _corFundo =
            Color.FromArgb(244, 247, 251);

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

            Size = new Size(1050, 540);
            MinimumSize = new Size(800, 440);

            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Carrega os dados após validar o acesso.
        private void AoExibirFormulario(
            object? sender,
            EventArgs e)
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
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 150));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

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
        private void CriarAreaCadastro(
            TableLayoutPanel principal)
        {
            Panel painel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
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
        private static TextBox CriarCampoTexto(
            int tamanhoMaximo)
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
        private void CriarBotoesCadastro(
            TableLayoutPanel campos)
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
        private void CriarAreaPesquisa(
            TableLayoutPanel principal)
        {
            FlowLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 0)
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

                BackColor = principal
                    ? _corAzul
                    : Color.White,

                ForeColor = principal
                    ? Color.White
                    : _corAzul
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
                DataTable tabela = ConsultarRegistros();

                AtualizarTabela(tabela);

                lblQuantidade.Text =
                    $"Disciplinas encontradas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar disciplinas",
                    ex);
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
                dgvDisciplinas.Columns["IdDisciplina"]!.Visible =
                    false;
            }

            DefinirColuna(
                "CodigoDisciplina",
                "Código",
                110);

            DefinirColuna(
                "NomeDisciplina",
                "Nome da disciplina",
                240);

            DefinirColuna(
                "CargaHoraria",
                "Carga horária (h)",
                110);

            DefinirColuna(
                "Ativa",
                "Ativa",
                70);
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

        // Cadastra ou atualiza uma disciplina pela API.
        private void SalvarDisciplina()
        {
            if (!ValidarFormulario() || !VerificarAcesso())
            {
                return;
            }

            string codigo = txtCodigo.Text.Trim();
            string nome = txtNome.Text.Trim();

            int cargaHoraria =
                (int)nudCargaHoraria.Value;

            bool editando =
                _idDisciplinaSelecionada.HasValue;

            string mensagem = editando
                ? "Deseja salvar as alterações desta disciplina?"
                : "Deseja cadastrar esta disciplina?";

            if (!ConfirmarOperacao(mensagem))
            {
                return;
            }

            try
            {
                // A API valida, grava e registra a auditoria.
                string rota = editando
                    ? $"academico/disciplinas/{_idDisciplinaSelecionada.Value}"
                    : "academico/disciplinas";

                ExecutarApi(
                    editando
                        ? HttpMethod.Put
                        : HttpMethod.Post,
                    rota,
                    new
                    {
                        codigo,
                        nome,
                        cargaHoraria
                    });

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
                ExibirMensagem(
                    ex.Message,
                    "Validação",
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao salvar disciplina",
                    ex);
            }
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

            int idDisciplina =
                _idDisciplinaSelecionada.Value;

            if (!ConfirmarOperacao(
                "Deseja alterar a situação desta disciplina?"))
            {
                return;
            }

            try
            {
                // Consulta o status atual da disciplina.
                DataTable registros =
                    ConsultarRegistros(false);

                DataRow? registro =
                    registros.Rows.Find(idDisciplina);

                if (registro == null)
                {
                    throw new InvalidOperationException(
                        "Registro não encontrado.");
                }

                bool ativa =
                    !Convert.ToBoolean(registro["Ativa"]);

                // Encaminha a alteração de status para a API.
                ExecutarApi(
                    HttpMethod.Patch,
                    $"academico/disciplinas/{idDisciplina}/status",
                    new
                    {
                        ativa
                    });

                ExibirMensagem(
                    "Situação da disciplina atualizada!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarDisciplinas();
                LimparFormulario();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao alterar situação",
                    ex);
            }
        }

        // Solicita confirmação antes de executar uma alteração.
        private static bool ConfirmarOperacao(
            string mensagem)
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

                // Apresenta a mensagem retornada pelo servidor.
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

        // Converte a resposta JSON para a estrutura da tabela.
        private DataTable ConsultarRegistros(
            bool filtrar = true)
        {
            string pesquisa = filtrar
                ? txtPesquisa.Text.Trim()
                : string.Empty;

            JsonElement resposta = ExecutarApi(
                HttpMethod.Get,
                "academico/disciplinas");

            DataTable tabela = new();

            tabela.Columns.Add(
                "IdDisciplina",
                typeof(int));

            tabela.Columns.Add(
                "CodigoDisciplina",
                typeof(string));

            tabela.Columns.Add(
                "NomeDisciplina",
                typeof(string));

            tabela.Columns.Add(
                "CargaHoraria",
                typeof(int));

            tabela.Columns.Add(
                "Ativa",
                typeof(bool));

            tabela.PrimaryKey = new[]
            {
                tabela.Columns["IdDisciplina"]!
            };

            // Converte os registros retornados pela API.
            foreach (JsonElement registro in
                resposta.GetProperty("registros").EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(
                        "IdDisciplina").GetInt32(),

                    registro.GetProperty(
                        "CodigoDisciplina").GetString(),

                    registro.GetProperty(
                        "NomeDisciplina").GetString(),

                    registro.GetProperty(
                        "CargaHoraria").GetInt32(),

                    registro.GetProperty(
                        "Ativa").GetBoolean());
            }

            // Preserva a pesquisa e a ordenação original.
            DataTable filtrada = tabela.Clone();

            foreach (DataRow linha in
                tabela.Select("", "CodigoDisciplina ASC"))
            {
                string codigo =
                    Convert.ToString(
                        linha["CodigoDisciplina"]) ?? "";

                string nome =
                    Convert.ToString(
                        linha["NomeDisciplina"]) ?? "";

                bool correspondePesquisa =
                    string.IsNullOrEmpty(pesquisa) ||

                    codigo.Contains(
                        pesquisa,
                        StringComparison.OrdinalIgnoreCase) ||

                    nome.Contains(
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