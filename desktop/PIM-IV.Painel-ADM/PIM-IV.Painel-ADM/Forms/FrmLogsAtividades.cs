using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    // Permite ao administrador consultar os registros de auditoria.
    public class FrmLogsAtividades : Form
    {
        private readonly Usuario _administrador;
        private readonly AuditoriaService _auditoriaService = new();

        private readonly Color _corAzul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _corFundo =
            Color.FromArgb(244, 247, 251);

        private TextBox txtPesquisa = null!;
        private DateTimePicker dtpInicio = null!;
        private DateTimePicker dtpFim = null!;
        private DataGridView dgvLogs = null!;
        private Label lblQuantidade = null!;

        private Button btnPesquisar = null!;
        private Button btnLimpar = null!;

        // Inicializa o formulário com o administrador autenticado.
        public FrmLogsAtividades(Usuario administrador)
        {
            _administrador = administrador
                ?? throw new ArgumentNullException(nameof(administrador));

            ConfigurarFormulario();
            CriarInterface();
            ConfigurarEventos();

            Shown += (_, _) => CarregarRegistros();
        }

        // Define as propriedades principais da janela.
        private void ConfigurarFormulario()
        {
            Text = "Registros de Atividades - Auditoria";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1100, 720);
            MinimumSize = new Size(850, 550);

            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Organiza os componentes da interface.
        private void CriarInterface()
        {
            TableLayoutPanel layout = CriarLayoutPrincipal();

            Controls.Add(layout);

            CriarCabecalho(layout);
            CriarAreaFiltros(layout);
            CriarAreaTabela(layout);
            CriarRodape(layout);
        }

        // Cria a estrutura principal da interface.
        private static TableLayoutPanel CriarLayoutPrincipal()
        {
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                ColumnCount = 1,
                RowCount = 5
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 45));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));

            return layout;
        }

        // Apresenta o título e a descrição do módulo.
        private void CriarCabecalho(TableLayoutPanel layout)
        {
            Label titulo = new()
            {
                Text = "Registros de Atividades",
                Font = new Font(
                    "Segoe UI",
                    20,
                    FontStyle.Bold),
                ForeColor = _corAzul,
                Dock = DockStyle.Fill
            };

            Label subtitulo = new()
            {
                Text = "Consulte as operações realizadas no sistema.",
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(titulo, 0, 0);
            layout.Controls.Add(subtitulo, 0, 1);
        }

        // Cria os filtros de pesquisa e período.
        private void CriarAreaFiltros(TableLayoutPanel layout)
        {
            FlowLayoutPanel painelFiltros = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            txtPesquisa = new TextBox
            {
                Width = 250,
                PlaceholderText = "Pesquisar usuário ou ação...",
                Margin = new Padding(0, 8, 15, 0)
            };

            dtpInicio = CriarCampoData(
                DateTime.Today.AddDays(-30),
                new Padding(0, 8, 10, 0));

            dtpFim = CriarCampoData(
                DateTime.Today,
                new Padding(0, 8, 15, 0));

            btnPesquisar = CriarBotao(
                "Pesquisar",
                120,
                true);

            btnLimpar = CriarBotao(
                "Limpar filtros",
                130,
                false);

            painelFiltros.Controls.Add(txtPesquisa);
            painelFiltros.Controls.Add(CriarRotuloFiltro("De:"));
            painelFiltros.Controls.Add(dtpInicio);
            painelFiltros.Controls.Add(CriarRotuloFiltro("Até:"));
            painelFiltros.Controls.Add(dtpFim);
            painelFiltros.Controls.Add(btnPesquisar);
            painelFiltros.Controls.Add(btnLimpar);

            layout.Controls.Add(painelFiltros, 0, 2);
        }

        // Padroniza os campos de seleção de datas.
        private static DateTimePicker CriarCampoData(
            DateTime dataInicial,
            Padding margem)
        {
            return new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Value = dataInicial,
                Margin = margem
            };
        }

        // Padroniza os rótulos dos filtros.
        private static Label CriarRotuloFiltro(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Margin = new Padding(0, 12, 8, 0)
            };
        }

        // Cria os botões utilizados na pesquisa.
        private Button CriarBotao(
            string texto,
            int largura,
            bool destacar)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 38,
                Cursor = Cursors.Hand
            };

            if (destacar)
            {
                botao.BackColor = _corAzul;
                botao.ForeColor = Color.White;
                botao.FlatStyle = FlatStyle.Flat;
                botao.FlatAppearance.BorderSize = 0;
            }

            return botao;
        }

        // Cria a tabela de consulta dos registros.
        private void CriarAreaTabela(TableLayoutPanel layout)
        {
            dgvLogs = new DataGridView
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

            ConfigurarEstiloTabela();

            layout.Controls.Add(dgvLogs, 0, 3);
        }

        // Configura a aparência visual da tabela.
        private void ConfigurarEstiloTabela()
        {
            dgvLogs.ColumnHeadersDefaultCellStyle.BackColor =
                _corAzul;

            dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvLogs.EnableHeadersVisualStyles = false;

            dgvLogs.ColumnHeadersHeight = 42;
            dgvLogs.RowTemplate.Height = 34;
        }

        // Exibe a quantidade de registros encontrados.
        private void CriarRodape(TableLayoutPanel layout)
        {
            lblQuantidade = new Label
            {
                Text = "Registros encontrados: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(lblQuantidade, 0, 4);
        }

        // Associa os botões às operações correspondentes.
        private void ConfigurarEventos()
        {
            btnPesquisar.Click += (_, _) =>
                CarregarRegistros();

            btnLimpar.Click += (_, _) =>
                LimparFiltros();
        }

        // Consulta os registros utilizando o serviço de auditoria.
        private void CarregarRegistros()
        {
            try
            {
                DateTime dataInicio = dtpInicio.Value.Date;
                DateTime dataFim = dtpFim.Value.Date;

                string pesquisa = txtPesquisa.Text.Trim();

                DataTable registros = _auditoriaService.Pesquisar(
                    _administrador.IdUsuario,
                    dataInicio,
                    dataFim,
                    pesquisa);

                AtualizarTabela(registros);
            }
            catch (UnauthorizedAccessException)
            {
                ExibirMensagem(
                    "Somente administradores ativos " +
                    "podem consultar a auditoria.",
                    "Acesso negado",
                    MessageBoxIcon.Warning);

                Close();
            }
            catch (ArgumentException ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Filtros inválidos",
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Erro ao consultar registros:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
        }

        // Atualiza a tabela e a quantidade de registros.
        private void AtualizarTabela(DataTable registros)
        {
            dgvLogs.DataSource = registros;

            ConfigurarColunas();

            lblQuantidade.Text =
                $"Registros encontrados: {registros.Rows.Count}";
        }

        // Restaura o período e o texto padrão da pesquisa.
        private void LimparFiltros()
        {
            txtPesquisa.Clear();

            dtpInicio.Value = DateTime.Today.AddDays(-30);
            dtpFim.Value = DateTime.Today;

            CarregarRegistros();
        }

        // Configura os nomes e as larguras das colunas.
        private void ConfigurarColunas()
        {
            ConfigurarColuna(
                "IdLog",
                "ID",
                45);

            ConfigurarColuna(
                "DataHora",
                "Data e hora",
                110,
                "dd/MM/yyyy HH:mm:ss");

            ConfigurarColuna(
                "Login",
                "Login",
                75);

            ConfigurarColuna(
                "NomeCompleto",
                "Usuário",
                130);

            ConfigurarColuna(
                "Acao",
                "Ação",
                120);

            ConfigurarColuna(
                "Descricao",
                "Descrição",
                250);
        }

        // Centraliza a configuração das colunas da tabela.
        private void ConfigurarColuna(
            string nome,
            string titulo,
            float largura,
            string? formato = null)
        {
            if (!dgvLogs.Columns.Contains(nome))
            {
                return;
            }

            DataGridViewColumn coluna =
                dgvLogs.Columns[nome]!;

            coluna.HeaderText = titulo;
            coluna.FillWeight = largura;

            if (!string.IsNullOrWhiteSpace(formato))
            {
                coluna.DefaultCellStyle.Format = formato;
            }
        }

        // Padroniza as mensagens apresentadas ao usuário.
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
    }
}