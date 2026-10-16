
using System.Data;
using System.Drawing;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;

namespace PIM_IV.Painel_ADM.Forms
{
    public class FrmAtividades : Form
    {
        private readonly Usuario _administrador;
        private readonly AuditoriaService _auditoria = new();

        private DataGridView dgvAtividades = null!;
        private TextBox txtPesquisar = null!;
        private Label lblQuantidade = null!;

        public FrmAtividades(Usuario administrador)
        {
            _administrador = administrador;

            if (_administrador.IdPerfil != 1 || !_administrador.Ativo)
                throw new UnauthorizedAccessException(
                    "Apenas administradores podem consultar atividades.");

            Text = "PIM IV - Registros de Atividades";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 680);
            MinimumSize = new Size(900, 550);
            BackColor = Color.FromArgb(244, 247, 251);

            CriarLayout();
            CarregarAtividades();
        }

        // Monta a interface de consulta
        private void CriarLayout()
        {
            Label titulo = new()
            {
                Text = "Registros de Atividades",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 39, 65),
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label descricao = new()
            {
                Text = "Consulte as operações administrativas realizadas no sistema.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.DimGray,
                Location = new Point(33, 78),
                AutoSize = true
            };

            txtPesquisar = new TextBox
            {
                Location = new Point(30, 125),
                Width = 430,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Pesquisar por responsável, ação ou descrição..."
            };

            Button btnAtualizar = new()
            {
                Text = "Atualizar registros",
                Location = new Point(475, 123),
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(40, 92, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnAtualizar.FlatAppearance.BorderSize = 0;

            dgvAtividades = new DataGridView
            {
                Location = new Point(30, 185),
                Size = new Size(1020, 365),
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Bottom |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 }
            };

            lblQuantidade = new Label
            {
                Text = "Registros encontrados: 0",
                Location = new Point(30, 565),
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.DimGray
            };

            txtPesquisar.TextChanged += (s, e) =>
                FiltrarAtividades();

            btnAtualizar.Click += (s, e) =>
                CarregarAtividades();

            Controls.AddRange(new Control[]
            {
                titulo,
                descricao,
                txtPesquisar,
                btnAtualizar,
                dgvAtividades,
                lblQuantidade
            });
        }

        // Busca o historico no SQL Server
        private void CarregarAtividades()
        {
            try
            {
                DataTable tabela = _auditoria.ListarTodos();

                dgvAtividades.DataSource = tabela;

                dgvAtividades.Columns["IdLog"]!.HeaderText = "ID";
                dgvAtividades.Columns["Usuario"]!.HeaderText = "Responsável";
                dgvAtividades.Columns["Acao"]!.HeaderText = "Ação";
                dgvAtividades.Columns["Descricao"]!.HeaderText = "Descrição";
                dgvAtividades.Columns["DataHora"]!.HeaderText = "Data e hora";

                dgvAtividades.Columns["IdLog"]!.FillWeight = 35;
                dgvAtividades.Columns["Usuario"]!.FillWeight = 110;
                dgvAtividades.Columns["Acao"]!.FillWeight = 110;
                dgvAtividades.Columns["Descricao"]!.FillWeight = 230;
                dgvAtividades.Columns["DataHora"]!.FillWeight = 110;

                dgvAtividades.Columns["DataHora"]!
                    .DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";

                FiltrarAtividades();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível consultar os registros de atividades.",
                    "Erro ao consultar histórico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Filtra os registros exibidos na tabela
        private void FiltrarAtividades()
        {
            if (dgvAtividades.DataSource is not DataTable tabela)
                return;

            string pesquisa = txtPesquisar.Text.Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            tabela.DefaultView.RowFilter =
                $"Usuario LIKE '%{pesquisa}%' OR " +
                $"Acao LIKE '%{pesquisa}%' OR " +
                $"Descricao LIKE '%{pesquisa}%'";

            lblQuantidade.Text =
                $"Registros encontrados: {tabela.DefaultView.Count}";
        }
    }
}
