
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    public class FrmLogsAtividades : Form
    {
        private readonly Usuario _administrador;

        private readonly Color _azul =
            Color.FromArgb(40, 92, 170);

        private readonly Color _fundo =
            Color.FromArgb(244, 247, 251);

        private TextBox txtPesquisa = null!;
        private DateTimePicker dtpInicio = null!;
        private DateTimePicker dtpFim = null!;
        private DataGridView dgvLogs = null!;
        private Label lblQuantidade = null!;

        public FrmLogsAtividades(Usuario administrador)
        {
            _administrador = administrador;

            Text = "Registros de Atividades - Auditoria";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 720);
            MinimumSize = new Size(850, 550);
            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);

            CriarInterface();

            Shown += (_, _) => CarregarRegistros();
        }

        private void CriarInterface()
        {
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                ColumnCount = 1,
                RowCount = 5
            };

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

            Controls.Add(layout);

            Label titulo = new()
            {
                Text = "Registros de Atividades",
                Font = new Font(
                    "Segoe UI", 20, FontStyle.Bold),
                ForeColor = _azul,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(titulo, 0, 0);

            Label subtitulo = new()
            {
                Text = "Consulte as operações realizadas no sistema.",
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(subtitulo, 0, 1);

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

            dtpInicio = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Value = DateTime.Today.AddDays(-30),
                Margin = new Padding(0, 8, 10, 0)
            };

            dtpFim = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Value = DateTime.Today,
                Margin = new Padding(0, 8, 15, 0)
            };

            Button btnPesquisar = new()
            {
                Text = "Pesquisar",
                Width = 120,
                Height = 38,
                BackColor = _azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnPesquisar.FlatAppearance.BorderSize = 0;
            btnPesquisar.Click +=
                (_, _) => CarregarRegistros();

            Button btnLimpar = new()
            {
                Text = "Limpar filtros",
                Width = 130,
                Height = 38
            };

            btnLimpar.Click += (_, _) =>
            {
                txtPesquisa.Clear();
                dtpInicio.Value = DateTime.Today.AddDays(-30);
                dtpFim.Value = DateTime.Today;
                CarregarRegistros();
            };

            painelFiltros.Controls.Add(txtPesquisa);
            painelFiltros.Controls.Add(
                new Label
                {
                    Text = "De:",
                    AutoSize = true,
                    Margin = new Padding(0, 12, 8, 0)
                });

            painelFiltros.Controls.Add(dtpInicio);
            painelFiltros.Controls.Add(
                new Label
                {
                    Text = "Até:",
                    AutoSize = true,
                    Margin = new Padding(0, 12, 8, 0)
                });

            painelFiltros.Controls.Add(dtpFim);
            painelFiltros.Controls.Add(btnPesquisar);
            painelFiltros.Controls.Add(btnLimpar);

            layout.Controls.Add(painelFiltros, 0, 2);

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

            dgvLogs.ColumnHeadersDefaultCellStyle.BackColor =
                _azul;

            dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            dgvLogs.EnableHeadersVisualStyles = false;
            dgvLogs.ColumnHeadersHeight = 42;
            dgvLogs.RowTemplate.Height = 34;

            layout.Controls.Add(dgvLogs, 0, 3);

            lblQuantidade = new Label
            {
                Text = "Registros encontrados: 0",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(lblQuantidade, 0, 4);
        }

        private void CarregarRegistros()
        {
            if (dtpInicio.Value.Date > dtpFim.Value.Date)
            {
                MessageBox.Show(
                    "A data inicial não pode ser maior que a final.",
                    "Período inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                // A autorização é consultada no banco,
                // não apenas no perfil carregado na memória.
                const string sqlAutorizacao = @"
                    SELECT COUNT(1)
                    FROM dbo.Usuarios u
                    INNER JOIN dbo.Perfis p
                        ON u.IdPerfil = p.IdPerfil
                    WHERE u.IdUsuario = @IdUsuario
                      AND u.Ativo = 1
                      AND p.Ativo = 1
                      AND p.NomePerfil = 'Administrador'";

                const string sqlLogs = @"
                    SELECT
                        l.IdLog,
                        l.DataHora,
                        u.Login,
                        u.NomeCompleto,
                        l.Acao,
                        l.Descricao
                    FROM dbo.LogsAtividades l
                    LEFT JOIN dbo.Usuarios u
                        ON l.IdUsuario = u.IdUsuario
                    WHERE l.DataHora >= @DataInicio
                      AND l.DataHora < @DataFim
                      AND
                      (
                          @Pesquisa = ''
                          OR u.Login LIKE @Texto
                          OR u.NomeCompleto LIKE @Texto
                          OR l.Acao LIKE @Texto
                          OR l.Descricao LIKE @Texto
                      )
                    ORDER BY l.DataHora DESC";

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                using (SqlCommand cmd = new(
                    sqlAutorizacao, conexao))
                {
                    cmd.Parameters.Add(
                        "@IdUsuario", SqlDbType.Int
                    ).Value = _administrador.IdUsuario;

                    if (Convert.ToInt32(
                        cmd.ExecuteScalar()) == 0)
                    {
                        MessageBox.Show(
                            "Somente administradores ativos " +
                            "podem consultar a auditoria.",
                            "Acesso negado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        Close();
                        return;
                    }
                }

                using SqlCommand comando =
                    new(sqlLogs, conexao);

                string pesquisa = txtPesquisa.Text.Trim();

                comando.Parameters.Add(
                    "@DataInicio", SqlDbType.DateTime2
                ).Value = dtpInicio.Value.Date;

                comando.Parameters.Add(
                    "@DataFim", SqlDbType.DateTime2
                ).Value = dtpFim.Value.Date.AddDays(1);

                comando.Parameters.Add(
                    "@Pesquisa", SqlDbType.NVarChar, 200
                ).Value = pesquisa;

                comando.Parameters.Add(
                    "@Texto", SqlDbType.NVarChar, 450
                ).Value = "%" + pesquisa + "%";

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                dgvLogs.DataSource = tabela;

                ConfigurarColunas();

                lblQuantidade.Text =
                    $"Registros encontrados: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao consultar registros:\n" +
                    ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ConfigurarColunas()
        {
            if (dgvLogs.Columns.Contains("IdLog"))
            {
                dgvLogs.Columns["IdLog"]!.HeaderText = "ID";
                dgvLogs.Columns["IdLog"]!.FillWeight = 45;
            }

            if (dgvLogs.Columns.Contains("DataHora"))
            {
                dgvLogs.Columns["DataHora"]!.HeaderText =
                    "Data e hora";

                dgvLogs.Columns["DataHora"]!.DefaultCellStyle
                    .Format = "dd/MM/yyyy HH:mm:ss";

                dgvLogs.Columns["DataHora"]!.FillWeight = 110;
            }

            if (dgvLogs.Columns.Contains("Login"))
            {
                dgvLogs.Columns["Login"]!.HeaderText = "Login";
                dgvLogs.Columns["Login"]!.FillWeight = 75;
            }

            if (dgvLogs.Columns.Contains("NomeCompleto"))
            {
                dgvLogs.Columns["NomeCompleto"]!.HeaderText =
                    "Usuário";

                dgvLogs.Columns["NomeCompleto"]!.FillWeight = 130;
            }

            if (dgvLogs.Columns.Contains("Acao"))
            {
                dgvLogs.Columns["Acao"]!.HeaderText = "Ação";
                dgvLogs.Columns["Acao"]!.FillWeight = 120;
            }

            if (dgvLogs.Columns.Contains("Descricao"))
            {
                dgvLogs.Columns["Descricao"]!.HeaderText =
                    "Descrição";

                dgvLogs.Columns["Descricao"]!.FillWeight = 250;
            }
        }
    }
}
