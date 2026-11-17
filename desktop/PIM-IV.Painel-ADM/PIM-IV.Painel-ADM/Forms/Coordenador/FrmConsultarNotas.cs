
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;

namespace PIM_IV.Painel_ADM
{
    public class FrmConsultarNotas : Form
    {
        private readonly ComboBox cmbTurma = new();
        private readonly ComboBox cmbDisciplina = new();
        private readonly ComboBox cmbAluno = new();
        private readonly DataGridView dgvNotas = new();
        private readonly Label lblTotal = new();

        private readonly Color azul = Color.FromArgb(40, 92, 170);

        public FrmConsultarNotas()
        {
            Text = "Consulta de Notas";
            BackColor = Color.FromArgb(244, 247, 251);
            MinimumSize = new Size(700, 450);
            Size = new Size(1050, 650);

            CriarInterface();
            Load += (_, _) => CarregarFiltros();
        }

        private static Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5, 8, 5, 4)
            };
        }

        private static void ConfigurarCombo(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = 190;
            combo.Font = new Font("Segoe UI", 10);
            combo.Margin = new Padding(5);
        }

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
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
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

            var btnConsultar = new Button
            {
                Text = "Consultar",
                Width = 115,
                Height = 35,
                BackColor = azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 4, 5, 5)
            };

            btnConsultar.Click += (_, _) => ConsultarNotas();
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

        private static void PreencherCombo(
            ComboBox combo,
            string sql,
            string campoTexto,
            string campoValor)
        {
            using var conexao = Conexao.CriarConexao();
            using var comando = new SqlCommand(sql, conexao);
            using var adaptador = new SqlDataAdapter(comando);

            var tabela = new DataTable();
            adaptador.Fill(tabela);

            var linhaTodos = tabela.NewRow();
            linhaTodos[campoValor] = 0;
            linhaTodos[campoTexto] = "Todos";
            tabela.Rows.InsertAt(linhaTodos, 0);

            combo.DisplayMember = campoTexto;
            combo.ValueMember = campoValor;
            combo.DataSource = tabela;
            combo.SelectedIndex = 0;
        }

        private void CarregarFiltros()
        {
            try
            {
                PreencherCombo(
                    cmbTurma,
                    @"SELECT IdTurma, NomeTurma
                      FROM dbo.Turmas
                      ORDER BY NomeTurma",
                    "NomeTurma",
                    "IdTurma");

                PreencherCombo(
                    cmbDisciplina,
                    @"SELECT IdDisciplina, NomeDisciplina
                      FROM dbo.Disciplinas
                      ORDER BY NomeDisciplina",
                    "NomeDisciplina",
                    "IdDisciplina");

                PreencherCombo(
                    cmbAluno,
                    @"SELECT a.IdAluno,
                             u.NomeCompleto
                      FROM dbo.Alunos a
                      INNER JOIN dbo.Usuarios u
                          ON u.IdUsuario = a.IdUsuario
                      ORDER BY u.NomeCompleto",
                    "NomeCompleto",
                    "IdAluno");

                ConsultarNotas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar filtros:\n" + ex.Message,
                    "Consulta de Notas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static int ObterId(ComboBox combo)
        {
            if (combo.SelectedValue is int id)
                return id;

            return 0;
        }

        private void ConsultarNotas()
        {
            const string sql = @"
                SELECT
                    u.NomeCompleto AS Aluno,
                    a.MatriculaAluno AS RA,
                    t.NomeTurma AS Turma,
                    d.NomeDisciplina AS Disciplina,
                    av.Titulo AS Avaliacao,
                    n.ValorNota AS Nota,
                    av.ValorMaximo,
                    n.DataLancamento
                FROM dbo.Notas n
                INNER JOIN dbo.Matriculas m
                    ON m.IdMatricula = n.IdMatricula
                INNER JOIN dbo.Alunos a
                    ON a.IdAluno = m.IdAluno
                INNER JOIN dbo.Usuarios u
                    ON u.IdUsuario = a.IdUsuario
                INNER JOIN dbo.Avaliacoes av
                    ON av.IdAvaliacao = n.IdAvaliacao
                INNER JOIN dbo.TurmaDisciplinas td
                    ON td.IdTurmaDisciplina =
                       av.IdTurmaDisciplina
                INNER JOIN dbo.Turmas t
                    ON t.IdTurma = td.IdTurma
                INNER JOIN dbo.Disciplinas d
                    ON d.IdDisciplina = td.IdDisciplina
                WHERE
                    m.IdTurma = td.IdTurma
                    AND (@IdTurma = 0
                         OR t.IdTurma = @IdTurma)
                    AND (@IdDisciplina = 0
                         OR d.IdDisciplina = @IdDisciplina)
                    AND (@IdAluno = 0
                         OR a.IdAluno = @IdAluno)
                ORDER BY
                    t.NomeTurma,
                    u.NomeCompleto,
                    d.NomeDisciplina,
                    av.DataAvaliacao";

            try
            {
                using var conexao = Conexao.CriarConexao();
                using var comando = new SqlCommand(sql, conexao);

                comando.Parameters.Add(
                    "@IdTurma", SqlDbType.Int
                ).Value = ObterId(cmbTurma);

                comando.Parameters.Add(
                    "@IdDisciplina", SqlDbType.Int
                ).Value = ObterId(cmbDisciplina);

                comando.Parameters.Add(
                    "@IdAluno", SqlDbType.Int
                ).Value = ObterId(cmbAluno);

                using var adaptador = new SqlDataAdapter(comando);

                var tabela = new DataTable();
                adaptador.Fill(tabela);

                dgvNotas.DataSource = tabela;

                lblTotal.Text =
                    $"Total de notas encontradas: {tabela.Rows.Count}";

                if (dgvNotas.Columns.Contains("Nota"))
                    dgvNotas.Columns["Nota"]!
                        .DefaultCellStyle.Format = "N2";

                if (dgvNotas.Columns.Contains("ValorMaximo"))
                    dgvNotas.Columns["ValorMaximo"]!
                        .DefaultCellStyle.Format = "N2";

                if (dgvNotas.Columns.Contains("DataLancamento"))
                    dgvNotas.Columns["DataLancamento"]!
                        .DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao consultar notas:\n" + ex.Message,
                    "Consulta de Notas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
