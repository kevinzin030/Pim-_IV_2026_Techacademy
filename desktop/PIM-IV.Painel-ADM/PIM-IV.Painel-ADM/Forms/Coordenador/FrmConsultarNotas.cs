using System;
using System.Data;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Data;

namespace PIM_IV.Painel_ADM
{
    // Permite ao coordenador consultar as notas acadêmicas.
    public class FrmConsultarNotas : Form
    {
        private readonly ComboBox cmbTurma = new();
        private readonly ComboBox cmbDisciplina = new();
        private readonly ComboBox cmbAluno = new();
        private readonly DataGridView dgvNotas = new();
        private readonly Label lblTotal = new();
        private readonly Button btnConsultar = new();

        private readonly Color azul =
            Color.FromArgb(40, 92, 170);

        private bool _carregando;

        // Inicializa o formulário de consulta.
        public FrmConsultarNotas()
        {
            Text = "Consulta de Notas";
            BackColor = Color.FromArgb(244, 247, 251);
            MinimumSize = new Size(700, 450);
            Size = new Size(1050, 650);

            CriarInterface();

            Load += async (_, _) =>
                await CarregarFiltrosAsync();
        }

        // Padroniza os rótulos dos filtros.
        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),
                Margin = new Padding(5, 8, 5, 4)
            };
        }

        // Configura os campos de seleção.
        private static void ConfigurarCombo(ComboBox combo)
        {
            combo.DropDownStyle =
                ComboBoxStyle.DropDownList;

            combo.Width = 190;
            combo.Font = new Font("Segoe UI", 10);
            combo.Margin = new Padding(5);
        }

        // Cria a interface original do formulário.
        private void CriarInterface()
        {
            var principal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 3
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 115));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            Controls.Add(principal);

            var titulo = new Label
            {
                Text = "Consulta de Notas dos Alunos",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI",
                    17,
                    FontStyle.Bold),
                ForeColor = azul
            };

            principal.Controls.Add(titulo, 0, 0);

            var filtros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true
            };

            ConfigurarCombo(cmbTurma);
            ConfigurarCombo(cmbDisciplina);
            ConfigurarCombo(cmbAluno);

            filtros.Controls.Add(CriarRotulo("Turma:"));
            filtros.Controls.Add(cmbTurma);

            filtros.Controls.Add(CriarRotulo("Disciplina:"));
            filtros.Controls.Add(cmbDisciplina);

            filtros.Controls.Add(CriarRotulo("Aluno:"));
            filtros.Controls.Add(cmbAluno);

            btnConsultar.Text = "Consultar";
            btnConsultar.Width = 115;
            btnConsultar.Height = 35;
            btnConsultar.BackColor = azul;
            btnConsultar.ForeColor = Color.White;
            btnConsultar.FlatStyle = FlatStyle.Flat;
            btnConsultar.Margin = new Padding(10, 4, 5, 5);

            btnConsultar.Click += async (_, _) =>
                await ConsultarNotasAsync();

            filtros.Controls.Add(btnConsultar);

            principal.Controls.Add(filtros, 0, 1);

            var areaResultados = new Panel
            {
                Dock = DockStyle.Fill
            };

            lblTotal.Text = "Nenhuma consulta realizada.";
            lblTotal.Dock = DockStyle.Top;
            lblTotal.Height = 32;
            lblTotal.Font = new Font("Segoe UI", 10);

            dgvNotas.Dock = DockStyle.Fill;
            dgvNotas.ReadOnly = true;
            dgvNotas.AllowUserToAddRows = false;
            dgvNotas.AllowUserToDeleteRows = false;
            dgvNotas.AllowUserToResizeRows = false;
            dgvNotas.MultiSelect = false;

            dgvNotas.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvNotas.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            dgvNotas.BackgroundColor = Color.White;
            dgvNotas.RowHeadersVisible = false;

            areaResultados.Controls.Add(dgvNotas);
            areaResultados.Controls.Add(lblTotal);

            principal.Controls.Add(areaResultados, 0, 2);
        }

        // Controla a interface durante as requisições.
        private void DefinirCarregamento(bool carregando)
        {
            _carregando = carregando;

            btnConsultar.Enabled = !carregando;
            cmbTurma.Enabled = !carregando;
            cmbDisciplina.Enabled = !carregando;
            cmbAluno.Enabled = !carregando;

            UseWaitCursor = carregando;
        }

        // Preenche os filtros com os dados recebidos da API.
        private static void PreencherCombo(
            ComboBox combo,
            JsonElement registros,
            string campoTexto,
            string campoValor)
        {
            var tabela = new DataTable();

            tabela.Columns.Add(
                campoValor,
                typeof(int));

            tabela.Columns.Add(
                campoTexto,
                typeof(string));

            tabela.Rows.Add(0, "Todos");

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty(campoValor)
                        .GetInt32(),

                    registro.GetProperty(campoTexto)
                        .GetString() ?? string.Empty);
            }

            combo.DisplayMember = campoTexto;
            combo.ValueMember = campoValor;
            combo.DataSource = tabela;
            combo.SelectedIndex = 0;
        }

        // Carrega os filtros acadêmicos pela API.
        private async Task CarregarFiltrosAsync()
        {
            if (_carregando)
                return;

            try
            {
                DefinirCarregamento(true);

                lblTotal.Text =
                    "Carregando filtros acadêmicos...";

                JsonElement resposta =
                    await ExecutarConsultaApiAsync(
                        "coordenadores/notas/opcoes");

                if (IsDisposed || Disposing)
                    return;

                PreencherCombo(
                    cmbTurma,
                    resposta.GetProperty("turmas"),
                    "NomeTurma",
                    "IdTurma");

                PreencherCombo(
                    cmbDisciplina,
                    resposta.GetProperty("disciplinas"),
                    "NomeDisciplina",
                    "IdDisciplina");

                PreencherCombo(
                    cmbAluno,
                    resposta.GetProperty("alunos"),
                    "NomeCompleto",
                    "IdAluno");
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    lblTotal.Text =
                        "Não foi possível carregar os filtros.";

                    ExibirErro(
                        "Erro ao carregar filtros",
                        ex);
                }

                return;
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                    DefinirCarregamento(false);
            }

            if (!IsDisposed && !Disposing)
                await ConsultarNotasAsync();
        }

        // Obtém o identificador do filtro selecionado.
        private static int ObterId(ComboBox combo)
        {
            if (combo.SelectedValue is int id)
                return id;

            return 0;
        }

        // Consulta as notas utilizando os filtros selecionados.
        private async Task ConsultarNotasAsync()
        {
            if (_carregando)
                return;

            try
            {
                DefinirCarregamento(true);

                lblTotal.Text = "Consultando notas...";

                int idTurma = ObterId(cmbTurma);

                int idDisciplina =
                    ObterId(cmbDisciplina);

                int idAluno = ObterId(cmbAluno);

                string rota =
                    $"coordenadores/notas?idTurma={idTurma}" +
                    $"&idDisciplina={idDisciplina}" +
                    $"&idAluno={idAluno}";

                JsonElement resposta =
                    await ExecutarConsultaApiAsync(rota);

                if (IsDisposed || Disposing)
                    return;

                DataTable tabela = ConverterNotas(
                    resposta.GetProperty("notas"));

                dgvNotas.DataSource = tabela;

                lblTotal.Text =
                    $"Total de notas encontradas: {tabela.Rows.Count}";

                ConfigurarColunas();
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    dgvNotas.DataSource = null;

                    lblTotal.Text =
                        "Não foi possível consultar as notas.";

                    ExibirErro(
                        "Erro ao consultar notas",
                        ex);
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                    DefinirCarregamento(false);
            }
        }

        // Converte os registros JSON em uma tabela tipada.
        private static DataTable ConverterNotas(
            JsonElement registros)
        {
            var tabela = new DataTable();

            tabela.Columns.Add(
                "Aluno",
                typeof(string));

            tabela.Columns.Add(
                "RA",
                typeof(string));

            tabela.Columns.Add(
                "Turma",
                typeof(string));

            tabela.Columns.Add(
                "Disciplina",
                typeof(string));

            tabela.Columns.Add(
                "Avaliacao",
                typeof(string));

            tabela.Columns.Add(
                "Nota",
                typeof(decimal));

            tabela.Columns.Add(
                "ValorMaximo",
                typeof(decimal));

            tabela.Columns.Add(
                "DataLancamento",
                typeof(DateTime));

            foreach (
                JsonElement registro in
                registros.EnumerateArray())
            {
                tabela.Rows.Add(
                    registro.GetProperty("Aluno")
                        .GetString(),

                    registro.GetProperty("RA")
                        .GetString(),

                    registro.GetProperty("Turma")
                        .GetString(),

                    registro.GetProperty("Disciplina")
                        .GetString(),

                    registro.GetProperty("Avaliacao")
                        .GetString(),

                    ObterDecimal(registro, "Nota"),

                    ObterDecimal(registro, "ValorMaximo"),

                    ObterData(registro, "DataLancamento"));
            }

            return tabela;
        }

        // Converte valores numéricos, respeitando campos nulos.
        private static object ObterDecimal(
            JsonElement registro,
            string propriedade)
        {
            JsonElement valor =
                registro.GetProperty(propriedade);

            if (valor.ValueKind == JsonValueKind.Null)
                return DBNull.Value;

            return valor.GetDecimal();
        }

        // Converte datas retornadas pela API.
        private static object ObterData(
            JsonElement registro,
            string propriedade)
        {
            JsonElement valor =
                registro.GetProperty(propriedade);

            if (valor.ValueKind == JsonValueKind.Null)
                return DBNull.Value;

            return valor.GetDateTime();
        }

        // Configura a apresentação das colunas.
        private void ConfigurarColunas()
        {
            if (dgvNotas.Columns.Contains("Avaliacao"))
            {
                dgvNotas.Columns["Avaliacao"]!
                    .HeaderText = "Avaliação";
            }

            if (dgvNotas.Columns.Contains("Nota"))
            {
                dgvNotas.Columns["Nota"]!
                    .DefaultCellStyle.Format = "N2";
            }

            if (dgvNotas.Columns.Contains("ValorMaximo"))
            {
                dgvNotas.Columns["ValorMaximo"]!
                    .HeaderText = "Valor máximo";

                dgvNotas.Columns["ValorMaximo"]!
                    .DefaultCellStyle.Format = "N2";
            }

            if (dgvNotas.Columns.Contains("DataLancamento"))
            {
                dgvNotas.Columns["DataLancamento"]!
                    .HeaderText = "Data de lançamento";

                dgvNotas.Columns["DataLancamento"]!
                    .DefaultCellStyle.Format = "dd/MM/yyyy";
            }
        }

        // Executa consultas autenticadas sem bloquear a interface.
        private static async Task<JsonElement>
            ExecutarConsultaApiAsync(string rota)
        {
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.GetAsync(rota);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            JsonElement resultado;

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                resultado =
                    documento.RootElement.Clone();
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
                string mensagem =
                    "Não foi possível realizar a consulta.";

                if (
                    resultado.ValueKind == JsonValueKind.Object &&
                    resultado.TryGetProperty(
                        "mensagem",
                        out JsonElement mensagemApi) &&
                    mensagemApi.ValueKind == JsonValueKind.String
                )
                {
                    mensagem =
                        mensagemApi.GetString() ?? mensagem;
                }

                throw new HttpRequestException(
                    mensagem,
                    null,
                    resposta.StatusCode);
            }

            return resultado;
        }

        // Exibe os erros de consulta.
        private void ExibirErro(
            string mensagem,
            Exception ex)
        {
            MessageBox.Show(
                this,
                $"{mensagem}:\n{ex.Message}",
                "Consulta de Notas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}