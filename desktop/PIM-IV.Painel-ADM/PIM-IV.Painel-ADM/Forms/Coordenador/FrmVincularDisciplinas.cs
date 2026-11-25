using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;
using System;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia os vínculos entre turmas e disciplinas pela API.
    public class FrmVincularDisciplinas : Form
    {
        private const int PerfilCoordenador = 2;

        private const string PermissaoDisciplinas =
            "GERENCIAR_DISCIPLINAS";

        private readonly Usuario _usuario;

        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _fundo =
            Color.FromArgb(244, 247, 251);

        private ComboBox cmbTurmas = null!;
        private ComboBox cmbDisciplinas = null!;
        private ListBox lstDisciplinas = null!;
        private DataGridView dgvVinculos = null!;

        private Label lblOrientacao = null!;
        private Label lblQuantidade = null!;

        private Button btnVincular = null!;
        private Button btnDesvincular = null!;
        private Button btnAtualizar = null!;

        private bool _atualizandoDados;

        // Inicializa o formulário com o usuário autenticado.
        public FrmVincularDisciplinas(Usuario usuario)
        {
            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            CriarInterface();
            ConfigurarEventos();

            Shown += AoExibirFormulario;
        }

        // Define as dimensões e a identidade visual.
        private void ConfigurarFormulario()
        {
            Text = "PIM IV - Vinculação de Disciplinas às Turmas";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1050, 540);
            MinimumSize = new Size(800, 440);

            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);
        }

        // Valida o acesso antes de carregar os dados.
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

        // Verifica a permissão do coordenador autenticado.
        private bool VerificarAcesso()
        {
            try
            {
                bool autorizado =
                    _usuario.IdPerfil == PerfilCoordenador &&
                    _permissaoService.UsuarioTemPermissao(
                        _usuario,
                        PermissaoDisciplinas);

                if (!autorizado)
                {
                    ExibirMensagem(
                        "Somente coordenadores autorizados " +
                        "podem vincular disciplinas.",
                        "Acesso negado",
                        MessageBoxIcon.Warning);
                }

                return autorizado;
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao verificar acesso", ex);
                return false;
            }
        }

        // Organiza as áreas principais da interface.
        private void CriarInterface()
        {
            TableLayoutPanel principal = CriarLayoutPrincipal();

            Controls.Add(principal);

            CriarCabecalho(principal);
            CriarAreaSelecao(principal);
            CriarAreaConteudo(principal);
            CriarAreaBotoes(principal);
        }

        // Cria a estrutura principal do formulário.
        private TableLayoutPanel CriarLayoutPrincipal()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12),
                BackColor = _fundo
            };

            principal.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 90));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 60));

            return principal;
        }

        // Apresenta o título do módulo.
        private void CriarCabecalho(TableLayoutPanel principal)
        {
            Label titulo = new()
            {
                Text = "Vinculação de Disciplinas às Turmas",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI",
                    19,
                    FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);
        }

        // Cria os campos de seleção de turma e disciplina.
        private void CriarAreaSelecao(TableLayoutPanel principal)
        {
            TableLayoutPanel selecao = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(15),
                BackColor = Color.White
            };

            selecao.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            selecao.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 50));

            selecao.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30));

            selecao.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            selecao.Controls.Add(
                CriarRotulo("Turma de destino"), 0, 0);

            selecao.Controls.Add(
                CriarRotulo("Disciplina para vincular"), 1, 0);

            cmbTurmas = CriarCombo(
                new Padding(0, 8, 12, 0));

            cmbDisciplinas = CriarCombo(
                new Padding(12, 8, 0, 0));

            selecao.Controls.Add(cmbTurmas, 0, 1);
            selecao.Controls.Add(cmbDisciplinas, 1, 1);

            principal.Controls.Add(selecao, 0, 1);
        }

        // Padroniza os campos de seleção.
        private static ComboBox CriarCombo(Padding margem)
        {
            return new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 13),
                IntegralHeight = false,
                Height = 40,
                Margin = margem
            };
        }

        // Divide o conteúdo entre disciplinas disponíveis e vinculadas.
        private void CriarAreaConteudo(TableLayoutPanel principal)
        {
            TableLayoutPanel conteudo = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 5)
            };

            conteudo.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 42));

            conteudo.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 58));

            conteudo.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            CriarPainelDisciplinasDisponiveis(conteudo);
            CriarPainelDisciplinasVinculadas(conteudo);

            principal.Controls.Add(conteudo, 0, 2);
        }

        // Cria o painel das disciplinas disponíveis.
        private void CriarPainelDisciplinasDisponiveis(
            TableLayoutPanel conteudo)
        {
            TableLayoutPanel painel = CriarPainelConteudo(
                new Padding(0, 0, 8, 0),
                65);

            painel.Controls.Add(
                CriarRotulo("Disciplinas disponíveis"), 0, 0);

            lstDisciplinas = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = "NomeExibicao",
                ValueMember = "IdDisciplina",
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            painel.Controls.Add(lstDisciplinas, 0, 1);

            lblOrientacao = new Label
            {
                Text =
                    "Arraste uma disciplina para a tabela à direita " +
                    "ou selecione e clique em Vincular.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            painel.Controls.Add(lblOrientacao, 0, 2);

            conteudo.Controls.Add(painel, 0, 0);
        }

        // Cria o painel das disciplinas vinculadas.
        private void CriarPainelDisciplinasVinculadas(
            TableLayoutPanel conteudo)
        {
            TableLayoutPanel painel = CriarPainelConteudo(
                new Padding(8, 0, 0, 0),
                40);

            painel.Controls.Add(
                CriarRotulo("Disciplinas vinculadas à turma"),
                0,
                0);

            dgvVinculos = new DataGridView
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
                AllowDrop = true
            };

            painel.Controls.Add(dgvVinculos, 0, 1);

            lblQuantidade = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Nenhuma disciplina vinculada.",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            painel.Controls.Add(lblQuantidade, 0, 2);

            conteudo.Controls.Add(painel, 1, 0);
        }

        // Padroniza os painéis de disciplinas.
        private static TableLayoutPanel CriarPainelConteudo(
            Padding margem,
            int alturaRodape)
        {
            TableLayoutPanel painel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                Margin = margem,
                BackColor = Color.White
            };

            painel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            painel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            painel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            painel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, alturaRodape));

            return painel;
        }

        // Cria os botões responsáveis pelas operações.
        private void CriarAreaBotoes(TableLayoutPanel principal)
        {
            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 0)
            };

            btnAtualizar = CriarBotao("Atualizar", 130);
            btnDesvincular = CriarBotao("Desvincular", 155);
            btnVincular = CriarBotao("Vincular disciplina", 190);

            botoes.Controls.Add(btnAtualizar);
            botoes.Controls.Add(btnDesvincular);
            botoes.Controls.Add(btnVincular);

            principal.Controls.Add(botoes, 0, 3);
        }

        // Padroniza os rótulos utilizados no formulário.
        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        // Padroniza os botões utilizados no formulário.
        private Button CriarBotao(string texto, int largura)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 40,
                BackColor = _azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            botao.FlatAppearance.BorderSize = 0;

            return botao;
        }

        // Associa os controles aos métodos responsáveis.
        private void ConfigurarEventos()
        {
            cmbTurmas.SelectedIndexChanged += (_, _) =>
            {
                if (!_atualizandoDados)
                {
                    CarregarVinculos();
                }
            };

            // Sincroniza a disciplina da lista com o ComboBox.
            lstDisciplinas.SelectedIndexChanged += (_, _) =>
            {
                if (!_atualizandoDados &&
                    lstDisciplinas.SelectedValue is int idDisciplina)
                {
                    cmbDisciplinas.SelectedValue = idDisciplina;
                }
            };

            btnAtualizar.Click += (_, _) =>
                AtualizarDados();

            btnVincular.Click += (_, _) =>
                VincularDisciplina(
                    ObterDisciplinaSelecionada());

            btnDesvincular.Click += (_, _) =>
                DesvincularDisciplina();

            lstDisciplinas.MouseDown += IniciarArraste;

            dgvVinculos.DragEnter += AoEntrarArraste;
            dgvVinculos.DragOver += AoMoverArraste;
            dgvVinculos.DragDrop += AoSoltarDisciplina;
        }

        // Atualiza as turmas e disciplinas disponíveis pela API.
        private void AtualizarDados()
        {
            int turmaAnterior = ObterIdSelecionado(cmbTurmas);

            bool dadosCarregados = false;

            try
            {
                _atualizandoDados = true;

                JsonElement resposta = ExecutarApi(
                    HttpMethod.Get,
                    "vinculos/opcoes");

                DataTable turmas = ConverterOpcoes(
                    resposta.GetProperty("turmas"),
                    "IdTurma");

                DataTable disciplinas = ConverterOpcoes(
                    resposta.GetProperty("disciplinas"),
                    "IdDisciplina");

                PreencherControles(
                    turmas,
                    disciplinas,
                    turmaAnterior);

                dadosCarregados = true;
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao carregar dados", ex);
            }
            finally
            {
                _atualizandoDados = false;
            }

            if (dadosCarregados)
            {
                CarregarVinculos();
            }
        }

        // Converte as opções retornadas pela API em uma tabela.
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

            foreach (JsonElement registro in registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(campoIdentificador).GetInt32(),
                    registro.GetProperty("NomeExibicao").GetString()
                        ?? string.Empty);
            }

            return tabela;
        }

        // Preenche os controles com os dados consultados.
        private void PreencherControles(
            DataTable turmas,
            DataTable disciplinas,
            int turmaAnterior)
        {
            ConfigurarCombo(
                cmbTurmas,
                turmas,
                "NomeExibicao",
                "IdTurma");

            ConfigurarCombo(
                cmbDisciplinas,
                disciplinas.Copy(),
                "NomeExibicao",
                "IdDisciplina");

            lstDisciplinas.DisplayMember = "NomeExibicao";
            lstDisciplinas.ValueMember = "IdDisciplina";
            lstDisciplinas.DataSource = disciplinas;

            RestaurarTurmaSelecionada(turmaAnterior);

            lstDisciplinas.ClearSelected();
        }

        // Associa uma tabela aos campos de seleção.
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

        // Mantém a turma selecionada após a atualização.
        private void RestaurarTurmaSelecionada(int turmaAnterior)
        {
            if (turmaAnterior <= 0)
            {
                return;
            }

            cmbTurmas.SelectedValue = turmaAnterior;

            if (cmbTurmas.SelectedIndex < 0 &&
                cmbTurmas.Items.Count > 0)
            {
                cmbTurmas.SelectedIndex = 0;
            }
        }

        // Obtém o identificador selecionado no ComboBox.
        private static int ObterIdSelecionado(ComboBox combo)
        {
            return combo.SelectedValue is int id
                ? id
                : 0;
        }

        // Obtém a disciplina selecionada.
        private int ObterDisciplinaSelecionada()
        {
            return ObterIdSelecionado(cmbDisciplinas);
        }

        // Consulta os vínculos ativos da turma selecionada.
        private void CarregarVinculos()
        {
            int idTurma = ObterIdSelecionado(cmbTurmas);

            if (idTurma <= 0)
            {
                LimparTabelaVinculos();
                return;
            }

            try
            {
                DataTable vinculos = ConsultarVinculos(idTurma);

                AtualizarTabelaVinculos(vinculos);
            }
            catch (Exception ex)
            {
                LimparTabelaVinculos();

                MostrarErro("Erro ao consultar vínculos", ex);
            }
        }

        // Consulta as disciplinas vinculadas por meio da API.
        private static DataTable ConsultarVinculos(int idTurma)
        {
            JsonElement resposta = ExecutarApi(
                HttpMethod.Get,
                $"vinculos/turmas/{idTurma}");

            DataTable tabela = new();

            tabela.Columns.Add(
                "IdTurmaDisciplina",
                typeof(int));

            tabela.Columns.Add(
                "Codigo",
                typeof(string));

            tabela.Columns.Add(
                "Disciplina",
                typeof(string));

            tabela.Columns.Add(
                "CargaHoraria",
                typeof(int));

            foreach (
                JsonElement registro in
                resposta.GetProperty("vinculos").EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty("IdTurmaDisciplina").GetInt32(),

                    registro.GetProperty("Codigo").GetString()
                        ?? string.Empty,

                    registro.GetProperty("Disciplina").GetString()
                        ?? string.Empty,

                    registro.GetProperty("CargaHoraria").GetInt32());
            }

            return tabela;
        }

        // Atualiza a tabela e a quantidade de vínculos.
        private void AtualizarTabelaVinculos(DataTable vinculos)
        {
            dgvVinculos.DataSource = vinculos;

            if (dgvVinculos.Columns.Contains("IdTurmaDisciplina"))
            {
                dgvVinculos.Columns["IdTurmaDisciplina"]!
                    .Visible = false;
            }

            dgvVinculos.ClearSelection();

            lblQuantidade.Text =
                $"Disciplinas vinculadas: {vinculos.Rows.Count}";
        }

        // Limpa a tabela quando não há turma selecionada.
        private void LimparTabelaVinculos()
        {
            dgvVinculos.DataSource = null;

            lblQuantidade.Text = "Selecione uma turma.";
        }

        // Inicia o arraste de uma disciplina da lista.
        private void IniciarArraste(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int indice = lstDisciplinas.IndexFromPoint(e.Location);

            if (indice < 0 ||
                indice >= lstDisciplinas.Items.Count)
            {
                return;
            }

            if (lstDisciplinas.Items[indice] is DataRowView disciplina)
            {
                int idDisciplina = Convert.ToInt32(
                    disciplina["IdDisciplina"]);

                lstDisciplinas.SelectedIndex = indice;

                lstDisciplinas.DoDragDrop(
                    idDisciplina,
                    DragDropEffects.Copy);
            }
        }

        // Verifica se o objeto arrastado é uma disciplina válida.
        private void AoEntrarArraste(
            object? sender,
            DragEventArgs e)
        {
            bool disciplinaValida =
                e.Data != null &&
                e.Data.GetDataPresent(typeof(int));

            e.Effect = disciplinaValida
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        // Mantém o indicador visual durante o arraste.
        private void AoMoverArraste(
            object? sender,
            DragEventArgs e)
        {
            AoEntrarArraste(sender, e);
        }

        // Vincula a disciplina solta sobre a tabela.
        private void AoSoltarDisciplina(
            object? sender,
            DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(int)) is int idDisciplina)
            {
                VincularDisciplina(idDisciplina);
            }
        }

        // Vincula uma disciplina à turma selecionada pela API.
        private void VincularDisciplina(int idDisciplina)
        {
            int idTurma = ObterIdSelecionado(cmbTurmas);

            if (!ValidarSelecao(idTurma, idDisciplina))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Post,
                    $"vinculos/turmas/{idTurma}",
                    new
                    {
                        idDisciplina
                    });

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Disciplina vinculada com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarVinculos();
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ExibirMensagem(
                    ex.Message,
                    "Vínculo existente",
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao vincular disciplina", ex);
            }
        }

        // Verifica se uma turma e uma disciplina foram selecionadas.
        private static bool ValidarSelecao(
            int idTurma,
            int idDisciplina)
        {
            if (idTurma > 0 && idDisciplina > 0)
            {
                return true;
            }

            ExibirMensagem(
                "Selecione uma turma e uma disciplina.",
                "Dados necessários",
                MessageBoxIcon.Information);

            return false;
        }

        // Desativa o vínculo sem excluir seu histórico.
        private void DesvincularDisciplina()
        {
            if (!TentarObterVinculoSelecionado(
                out int idVinculo,
                out string nomeDisciplina))
            {
                return;
            }

            int idTurma = ObterIdSelecionado(cmbTurmas);

            if (idTurma <= 0)
            {
                ExibirMensagem(
                    "Selecione uma turma válida.",
                    "Seleção necessária",
                    MessageBoxIcon.Information);

                return;
            }

            if (!ConfirmarDesvinculacao(nomeDisciplina))
            {
                return;
            }

            try
            {
                JsonElement resposta = ExecutarApi(
                    HttpMethod.Delete,
                    $"vinculos/turmas/{idTurma}/{idVinculo}");

                ExibirMensagem(
                    ObterMensagem(
                        resposta,
                        "Disciplina desvinculada com sucesso!"),
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarVinculos();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao desvincular disciplina", ex);
            }
        }

        // Obtém os dados do vínculo selecionado na tabela.
        private bool TentarObterVinculoSelecionado(
            out int idVinculo,
            out string nomeDisciplina)
        {
            idVinculo = 0;
            nomeDisciplina = string.Empty;

            if (dgvVinculos.SelectedRows.Count == 0)
            {
                ExibirMensagem(
                    "Selecione uma disciplina vinculada na tabela.",
                    "Seleção necessária",
                    MessageBoxIcon.Information);

                return false;
            }

            DataGridViewRow linha =
                dgvVinculos.SelectedRows[0];

            idVinculo = Convert.ToInt32(
                linha.Cells["IdTurmaDisciplina"].Value);

            nomeDisciplina = Convert.ToString(
                linha.Cells["Disciplina"].Value) ?? string.Empty;

            return true;
        }

        // Solicita confirmação antes de desvincular.
        private static bool ConfirmarDesvinculacao(
            string nomeDisciplina)
        {
            DialogResult resposta = MessageBox.Show(
                $"Deseja desvincular '{nomeDisciplina}' desta turma?",
                "Confirmar desvinculação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return resposta == DialogResult.Yes;
        }

        // Executa requisições HTTP utilizando a sessão autenticada.
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
                    requisicao.Content = JsonContent.Create(corpo);
                }

                using HttpResponseMessage resposta =
                    await ApiClient.Cliente.SendAsync(requisicao);

                string conteudo =
                    await resposta.Content.ReadAsStringAsync();

                JsonElement resultado;

                try
                {
                    using JsonDocument documento =
                        JsonDocument.Parse(conteudo);

                    resultado = documento.RootElement.Clone();
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

        // Obtém a mensagem retornada pela API.
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
                return mensagem.GetString() ?? mensagemPadrao;
            }

            return mensagemPadrao;
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

        // Centraliza a apresentação de erros.
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