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
    // Gerencia a vinculação de disciplinas às turmas.
    public class FrmVincularDisciplinas : Form
    {
        private readonly Usuario _usuario;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        private ComboBox cmbTurmas = null!;
        private ComboBox cmbDisciplinas = null!;
        private ListBox lstDisciplinas = null!;
        private DataGridView dgvVinculos = null!;
        private Label lblOrientacao = null!;
        private Label lblQuantidade = null!;

        private Button btnVincular = null!;
        private Button btnDesvincular = null!;
        private Button btnAtualizar = null!;

        private bool _carregando;

        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public FrmVincularDisciplinas(Usuario usuario)
        {
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "PIM IV - Vinculação de Disciplinas às Turmas";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1150, 800);
            MinimumSize = new Size(950, 680);
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

                AtualizarDados();
            };
        }

        // =====================================================
        // PERMISSÕES
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
                        "Somente coordenadores autorizados " +
                        "podem vincular disciplinas.",
                        "Acesso negado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao verificar acesso", ex);
                return false;
            }
        }

        // =====================================================
        // CONSTRUÇÃO DA INTERFACE
        // =====================================================

        private void CriarInterface()
        {
            TableLayoutPanel principal = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = _fundo
            };

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            // Área de seleção ampliada.
            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 125));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

            Controls.Add(principal);

            Label titulo = new()
            {
                Text = "Vinculação de Disciplinas às Turmas",
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI", 19, FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };

            principal.Controls.Add(titulo, 0, 0);

            // =================================================
            // SELEÇÃO DE TURMA E DISCIPLINA
            // =================================================

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

            principal.Controls.Add(selecao, 0, 1);

            selecao.Controls.Add(
                CriarRotulo("Turma de destino"), 0, 0);

            selecao.Controls.Add(
                CriarRotulo("Disciplina para vincular"), 1, 0);

            // Campo ampliado para seleção da turma.
            cmbTurmas = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 13),
                IntegralHeight = false,
                Height = 40,
                Margin = new Padding(0, 8, 12, 0)
            };

            // Campo ampliado para seleção da disciplina.
            cmbDisciplinas = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 13),
                IntegralHeight = false,
                Height = 40,
                Margin = new Padding(12, 8, 0, 0)
            };

            selecao.Controls.Add(cmbTurmas, 0, 1);
            selecao.Controls.Add(cmbDisciplinas, 1, 1);

            cmbTurmas.SelectedIndexChanged += (_, _) =>
            {
                if (!_carregando)
                {
                    CarregarVinculos();
                }
            };

            // =================================================
            // ÁREA CENTRAL
            // =================================================

            TableLayoutPanel conteudo = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 15, 0, 10)
            };

            conteudo.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 42));

            conteudo.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 58));

            principal.Controls.Add(conteudo, 0, 2);

            // =================================================
            // PAINEL ESQUERDO
            // =================================================

            TableLayoutPanel painelEsquerdo = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.White
            };

            painelEsquerdo.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            painelEsquerdo.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            painelEsquerdo.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 65));

            conteudo.Controls.Add(painelEsquerdo, 0, 0);

            painelEsquerdo.Controls.Add(
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

            painelEsquerdo.Controls.Add(
                lstDisciplinas, 0, 1);

            lblOrientacao = new Label
            {
                Text =
                    "Arraste uma disciplina para a tabela à direita " +
                    "ou selecione e clique em Vincular.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            painelEsquerdo.Controls.Add(
                lblOrientacao, 0, 2);

            // =================================================
            // PAINEL DIREITO
            // =================================================

            TableLayoutPanel painelDireito = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.White
            };

            painelDireito.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            painelDireito.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            painelDireito.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));

            conteudo.Controls.Add(painelDireito, 1, 0);

            painelDireito.Controls.Add(
                CriarRotulo(
                    "Disciplinas vinculadas à turma"), 0, 0);

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

            painelDireito.Controls.Add(dgvVinculos, 0, 1);

            lblQuantidade = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Nenhuma disciplina vinculada.",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            painelDireito.Controls.Add(
                lblQuantidade, 0, 2);

            // =================================================
            // BOTÕES
            // =================================================

            FlowLayoutPanel botoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            principal.Controls.Add(botoes, 0, 3);

            btnAtualizar = CriarBotao("Atualizar", 130);
            btnDesvincular = CriarBotao("Desvincular", 155);
            btnVincular = CriarBotao(
                "Vincular disciplina", 190);

            botoes.Controls.Add(btnAtualizar);
            botoes.Controls.Add(btnDesvincular);
            botoes.Controls.Add(btnVincular);

            btnAtualizar.Click += (_, _) =>
                AtualizarDados();

            btnVincular.Click += (_, _) =>
                VincularDisciplina(
                    ObterDisciplinaSelecionada());

            btnDesvincular.Click += (_, _) =>
                DesvincularDisciplina();

            // Eventos de arrastar e soltar.
            lstDisciplinas.MouseDown += IniciarArraste;
            dgvVinculos.DragEnter += AoEntrarArraste;
            dgvVinculos.DragOver += AoMoverArraste;
            dgvVinculos.DragDrop += AoSoltarDisciplina;
        }

        // =====================================================
        // COMPONENTES VISUAIS
        // =====================================================

        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold),
                ForeColor = _azul,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CriarBotao(
            string texto, int largura)
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

        // =====================================================
        // CARREGAMENTO DE DADOS
        // =====================================================

        private void AtualizarDados()
        {
            try
            {
                _carregando = true;

                int turmaAnterior =
                    ObterIdSelecionado(cmbTurmas);

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                const string sqlTurmas = @"
                    SELECT
                        IdTurma,
                        CodigoTurma + ' - ' + NomeTurma
                            AS NomeExibicao
                    FROM dbo.Turmas
                    WHERE Ativa = 1
                    ORDER BY CodigoTurma";

                const string sqlDisciplinas = @"
                    SELECT
                        IdDisciplina,
                        CodigoDisciplina + ' - ' +
                        NomeDisciplina AS NomeExibicao
                    FROM dbo.Disciplinas
                    WHERE Ativa = 1
                    ORDER BY NomeDisciplina";

                DataTable turmas =
                    ConsultarTabela(conexao, sqlTurmas);

                DataTable disciplinas =
                    ConsultarTabela(conexao, sqlDisciplinas);

                cmbTurmas.DataSource = turmas;
                cmbTurmas.DisplayMember = "NomeExibicao";
                cmbTurmas.ValueMember = "IdTurma";

                cmbDisciplinas.DataSource =
                    disciplinas.Copy();

                cmbDisciplinas.DisplayMember =
                    "NomeExibicao";

                cmbDisciplinas.ValueMember =
                    "IdDisciplina";

                lstDisciplinas.DataSource = disciplinas;
                lstDisciplinas.DisplayMember =
                    "NomeExibicao";

                lstDisciplinas.ValueMember =
                    "IdDisciplina";

                // Recupera a turma anteriormente selecionada.
                if (turmaAnterior > 0)
                {
                    foreach (DataRowView item in
                        cmbTurmas.Items)
                    {
                        if (Convert.ToInt32(
                            item["IdTurma"]) == turmaAnterior)
                        {
                            cmbTurmas.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (lstDisciplinas.Items.Count > 0)
                {
                    lstDisciplinas.ClearSelected();
                }
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao carregar dados", ex);
            }
            finally
            {
                _carregando = false;
            }

            CarregarVinculos();
        }

        private DataTable ConsultarTabela(
            SqlConnection conexao,
            string sql)
        {
            using SqlCommand comando =
                new(sql, conexao);

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

            return tabela;
        }

        private int ObterIdSelecionado(
            ComboBox combo)
        {
            if (combo.SelectedValue is int id)
            {
                return id;
            }

            return 0;
        }

        private int ObterDisciplinaSelecionada()
        {
            // Prioriza a disciplina escolhida na lista.
            if (lstDisciplinas.Focused &&
                lstDisciplinas.SelectedItem is
                    DataRowView itemLista)
            {
                return Convert.ToInt32(
                    itemLista["IdDisciplina"]);
            }

            return ObterIdSelecionado(cmbDisciplinas);
        }

        // =====================================================
        // CONSULTA DOS VÍNCULOS
        // =====================================================

        private void CarregarVinculos()
        {
            int idTurma =
                ObterIdSelecionado(cmbTurmas);

            if (idTurma <= 0)
            {
                dgvVinculos.DataSource = null;
                lblQuantidade.Text =
                    "Selecione uma turma.";

                return;
            }

            try
            {
                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                const string sql = @"
                    SELECT
                        td.IdTurmaDisciplina,
                        d.CodigoDisciplina AS Codigo,
                        d.NomeDisciplina AS Disciplina,
                        d.CargaHoraria AS CargaHoraria
                    FROM dbo.TurmaDisciplinas td
                    INNER JOIN dbo.Disciplinas d
                        ON td.IdDisciplina = d.IdDisciplina
                    WHERE td.IdTurma = @IdTurma
                      AND td.Ativa = 1
                    ORDER BY d.NomeDisciplina";

                using SqlCommand comando =
                    new(sql, conexao);

                comando.Parameters.Add(
                    "@IdTurma", SqlDbType.Int
                ).Value = idTurma;

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                dgvVinculos.DataSource = tabela;

                if (dgvVinculos.Columns.Contains(
                    "IdTurmaDisciplina"))
                {
                    dgvVinculos.Columns[
                        "IdTurmaDisciplina"]!.Visible = false;
                }

                dgvVinculos.ClearSelection();

                lblQuantidade.Text =
                    $"Disciplinas vinculadas: {tabela.Rows.Count}";
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao consultar vínculos", ex);
            }
        }

        // =====================================================
        // ARRASTAR E SOLTAR
        // =====================================================

        private void IniciarArraste(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int indice =
                lstDisciplinas.IndexFromPoint(e.Location);

            if (indice < 0 ||
                indice >= lstDisciplinas.Items.Count)
            {
                return;
            }

            if (lstDisciplinas.Items[indice] is
                DataRowView item)
            {
                int idDisciplina =
                    Convert.ToInt32(
                        item["IdDisciplina"]);

                lstDisciplinas.DoDragDrop(
                    idDisciplina,
                    DragDropEffects.Copy);
            }
        }

        private void AoEntrarArraste(
            object? sender,
            DragEventArgs e)
        {
            e.Effect =
                e.Data != null &&
                e.Data.GetDataPresent(typeof(int))
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
        }

        private void AoMoverArraste(
            object? sender,
            DragEventArgs e)
        {
            AoEntrarArraste(sender, e);
        }

        private void AoSoltarDisciplina(
            object? sender,
            DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(int))
                is int idDisciplina)
            {
                VincularDisciplina(idDisciplina);
            }
        }

        // =====================================================
        // VINCULAR DISCIPLINA
        // =====================================================

        private void VincularDisciplina(
            int idDisciplina)
        {
            int idTurma =
                ObterIdSelecionado(cmbTurmas);

            if (idTurma <= 0 ||
                idDisciplina <= 0)
            {
                MessageBox.Show(
                    "Selecione uma turma e uma disciplina.",
                    "Dados necessários",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

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

                    // Verifica se turma e disciplina estão ativas.
                    const string sqlValidar = @"
                        SELECT
                            CASE WHEN EXISTS (
                                SELECT 1
                                FROM dbo.Turmas
                                WHERE IdTurma = @IdTurma
                                  AND Ativa = 1
                            )
                            AND EXISTS (
                                SELECT 1
                                FROM dbo.Disciplinas
                                WHERE IdDisciplina = @IdDisciplina
                                  AND Ativa = 1
                            )
                            THEN 1 ELSE 0 END";

                    using (SqlCommand validar = new(
                        sqlValidar,
                        conexao,
                        transacao))
                    {
                        AdicionarIds(
                            validar,
                            idTurma,
                            idDisciplina);

                        if (Convert.ToInt32(
                            validar.ExecuteScalar()) != 1)
                        {
                            throw new InvalidOperationException(
                                "A turma ou disciplina está " +
                                "inativa ou não existe.");
                        }
                    }

                    // Evita duplicação e reativa vínculo antigo.
                    const string sql = @"
                        IF EXISTS (
                            SELECT 1
                            FROM dbo.TurmaDisciplinas
                                WITH (UPDLOCK, HOLDLOCK)
                            WHERE IdTurma = @IdTurma
                              AND IdDisciplina = @IdDisciplina
                              AND Ativa = 1
                        )
                        BEGIN
                            SELECT 0;
                        END
                        ELSE IF EXISTS (
                            SELECT 1
                            FROM dbo.TurmaDisciplinas
                                WITH (UPDLOCK, HOLDLOCK)
                            WHERE IdTurma = @IdTurma
                              AND IdDisciplina = @IdDisciplina
                        )
                        BEGIN
                            UPDATE dbo.TurmaDisciplinas
                            SET Ativa = 1,
                                DataVinculo = SYSDATETIME()
                            WHERE IdTurma = @IdTurma
                              AND IdDisciplina = @IdDisciplina;

                            SELECT 1;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.TurmaDisciplinas
                                (
                                    IdTurma,
                                    IdDisciplina,
                                    Ativa,
                                    DataVinculo
                                )
                            VALUES
                                (
                                    @IdTurma,
                                    @IdDisciplina,
                                    1,
                                    SYSDATETIME()
                                );

                            SELECT 1;
                        END";

                    int resultado;

                    using (SqlCommand comando = new(
                        sql, conexao, transacao))
                    {
                        AdicionarIds(
                            comando,
                            idTurma,
                            idDisciplina);

                        resultado = Convert.ToInt32(
                            comando.ExecuteScalar());
                    }

                    if (resultado == 0)
                    {
                        transacao.Commit();

                        MessageBox.Show(
                            "Esta disciplina já está " +
                            "vinculada à turma.",
                            "Vínculo existente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return;
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "VINCULAR_DISCIPLINA",
                        $"Disciplina {idDisciplina} " +
                        $"vinculada à turma {idTurma}.");

                    transacao.Commit();

                    MessageBox.Show(
                        "Disciplina vinculada com sucesso!",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                CarregarVinculos();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao vincular disciplina", ex);
            }
        }

        // =====================================================
        // DESVINCULAR DISCIPLINA
        // =====================================================

        private void DesvincularDisciplina()
        {
            if (dgvVinculos.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Selecione uma disciplina " +
                    "vinculada na tabela.",
                    "Seleção necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            DataGridViewRow linha =
                dgvVinculos.SelectedRows[0];

            int idVinculo = Convert.ToInt32(
                linha.Cells[
                    "IdTurmaDisciplina"].Value);

            string disciplina =
                Convert.ToString(
                    linha.Cells["Disciplina"].Value) ?? "";

            if (MessageBox.Show(
                $"Deseja desvincular '{disciplina}' desta turma?",
                "Confirmar desvinculação",
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
                        UPDATE dbo.TurmaDisciplinas
                        SET Ativa = 0
                        WHERE IdTurmaDisciplina = @IdVinculo
                          AND IdTurma = @IdTurma
                          AND Ativa = 1";

                    using SqlCommand comando =
                        new(sql, conexao, transacao);

                    comando.Parameters.Add(
                        "@IdVinculo", SqlDbType.Int
                    ).Value = idVinculo;

                    comando.Parameters.Add(
                        "@IdTurma", SqlDbType.Int
                    ).Value = ObterIdSelecionado(
                        cmbTurmas);

                    int alterados =
                        comando.ExecuteNonQuery();

                    if (alterados == 0)
                    {
                        throw new InvalidOperationException(
                            "O vínculo não está mais ativo.");
                    }

                    RegistrarAuditoria(
                        conexao,
                        transacao,
                        "DESVINCULAR_DISCIPLINA",
                        $"Vínculo acadêmico {idVinculo} " +
                        "desativado.");

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Disciplina desvinculada com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarVinculos();
            }
            catch (Exception ex)
            {
                MostrarErro(
                    "Erro ao desvincular disciplina", ex);
            }
        }

        // =====================================================
        // AUTORIZAÇÃO NO BANCO DE DADOS
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

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario", SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            if (Convert.ToInt32(
                comando.ExecuteScalar()) == 0)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui autorização " +
                    "para esta operação.");
            }
        }

        // =====================================================
        // PARÂMETROS SQL
        // =====================================================

        private void AdicionarIds(
            SqlCommand comando,
            int idTurma,
            int idDisciplina)
        {
            comando.Parameters.Add(
                "@IdTurma", SqlDbType.Int
            ).Value = idTurma;

            comando.Parameters.Add(
                "@IdDisciplina", SqlDbType.Int
            ).Value = idDisciplina;
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

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario", SqlDbType.Int
            ).Value = _usuario.IdUsuario;

            comando.Parameters.Add(
                "@Acao", SqlDbType.NVarChar, 100
            ).Value = acao;

            comando.Parameters.Add(
                "@Descricao", SqlDbType.NVarChar, 500
            ).Value = descricao;

            comando.ExecuteNonQuery();
        }

        // =====================================================
        // TRATAMENTO DE ERROS
        // =====================================================

        private void MostrarErro(
            string titulo,
            Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                titulo,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}