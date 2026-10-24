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
    // Gerencia os vínculos entre turmas e disciplinas.
    public class FrmVincularDisciplinas : Form
    {
        private const int PerfilCoordenador = 2;
        private const string PermissaoDisciplinas = "GERENCIAR_DISCIPLINAS";

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

        // Define as propriedades principais da janela.
        private void ConfigurarFormulario()
        {
            Text = "PIM IV - Vinculação de Disciplinas às Turmas";
            StartPosition = FormStartPosition.CenterParent;

            Size = new Size(1150, 800);
            MinimumSize = new Size(950, 680);

            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);
        }

        // Valida o acesso antes de carregar os dados.
        private void AoExibirFormulario(object? sender, EventArgs e)
        {
            if (!VerificarAcesso())
            {
                Close();
                return;
            }

            AtualizarDados();
        }

        // Verifica se o coordenador possui a permissão necessária.
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
                Padding = new Padding(20),
                BackColor = _fundo
            };

            principal.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 75));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 125));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            principal.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));

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
                Padding = new Padding(0, 15, 0, 10)
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
                Padding = new Padding(15),
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
                Padding = new Padding(0, 10, 0, 0)
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

        // Atualiza as turmas e disciplinas disponíveis.
        private void AtualizarDados()
        {
            int turmaAnterior = ObterIdSelecionado(cmbTurmas);
            bool dadosCarregados = false;

            try
            {
                _atualizandoDados = true;

                using SqlConnection conexao =
                    Conexao.CriarConexao();

                conexao.Open();

                DataTable turmas = ConsultarTurmas(conexao);
                DataTable disciplinas = ConsultarDisciplinas(conexao);

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

        // Consulta as turmas ativas.
        private static DataTable ConsultarTurmas(
            SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    IdTurma,
                    CodigoTurma + ' - ' + NomeTurma
                        AS NomeExibicao
                FROM dbo.Turmas
                WHERE Ativa = 1
                ORDER BY CodigoTurma";

            return ConsultarTabela(conexao, sql);
        }

        // Consulta as disciplinas ativas.
        private static DataTable ConsultarDisciplinas(
            SqlConnection conexao)
        {
            const string sql = @"
                SELECT
                    IdDisciplina,
                    CodigoDisciplina + ' - ' +
                        NomeDisciplina AS NomeExibicao
                FROM dbo.Disciplinas
                WHERE Ativa = 1
                ORDER BY NomeDisciplina";

            return ConsultarTabela(conexao, sql);
        }

        // Executa uma consulta SQL e retorna uma tabela.
        private static DataTable ConsultarTabela(
            SqlConnection conexao,
            string sql)
        {
            using SqlCommand comando = new(sql, conexao);

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

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

        // Obtém a disciplina escolhida na lista ou no ComboBox.
        private int ObterDisciplinaSelecionada()
        {
            if (lstDisciplinas.Focused &&
                lstDisciplinas.SelectedItem is DataRowView disciplina)
            {
                return Convert.ToInt32(
                    disciplina["IdDisciplina"]);
            }

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
                MostrarErro("Erro ao consultar vínculos", ex);
            }
        }

        // Consulta as disciplinas vinculadas à turma.
        private static DataTable ConsultarVinculos(int idTurma)
        {
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

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando =
                new(sql, conexao);

            comando.Parameters.Add(
                "@IdTurma",
                SqlDbType.Int).Value = idTurma;

            conexao.Open();

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();
            tabela.Load(leitor);

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

        // Vincula uma disciplina à turma selecionada.
        private void VincularDisciplina(int idDisciplina)
        {
            int idTurma = ObterIdSelecionado(cmbTurmas);

            if (!ValidarSelecao(idTurma, idDisciplina))
            {
                return;
            }

            try
            {
                int resultado = ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        ValidarTurmaEDisciplina(
                            conexao,
                            transacao,
                            idTurma,
                            idDisciplina);

                        int resultadoVinculo = ExecutarVinculacao(
                            conexao,
                            transacao,
                            idTurma,
                            idDisciplina);

                        if (resultadoVinculo == 1)
                        {
                            RegistrarAuditoria(
                                conexao,
                                transacao,
                                "VINCULAR_DISCIPLINA",
                                $"Disciplina {idDisciplina} " +
                                $"vinculada à turma {idTurma}.");
                        }

                        return resultadoVinculo;
                    });

                if (resultado == 0)
                {
                    ExibirMensagem(
                        "Esta disciplina já está vinculada à turma.",
                        "Vínculo existente",
                        MessageBoxIcon.Information);

                    return;
                }

                ExibirMensagem(
                    "Disciplina vinculada com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarVinculos();
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

        // Confirma se a turma e a disciplina estão ativas.
        private static void ValidarTurmaEDisciplina(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idTurma,
            int idDisciplina)
        {
            const string sql = @"
                SELECT
                    CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.Turmas
                        WHERE IdTurma = @IdTurma
                          AND Ativa = 1
                    )
                    AND EXISTS
                    (
                        SELECT 1
                        FROM dbo.Disciplinas
                        WHERE IdDisciplina = @IdDisciplina
                          AND Ativa = 1
                    )
                    THEN 1 ELSE 0 END";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarIds(
                comando,
                idTurma,
                idDisciplina);

            bool dadosValidos =
                Convert.ToInt32(comando.ExecuteScalar()) == 1;

            if (!dadosValidos)
            {
                throw new InvalidOperationException(
                    "A turma ou disciplina está inativa ou não existe.");
            }
        }

        // Cria, reativa ou identifica um vínculo existente.
        private static int ExecutarVinculacao(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idTurma,
            int idDisciplina)
        {
            const string sql = @"
                IF EXISTS
                (
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
                ELSE IF EXISTS
                (
                    SELECT 1
                    FROM dbo.TurmaDisciplinas
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdTurma = @IdTurma
                      AND IdDisciplina = @IdDisciplina
                )
                BEGIN
                    UPDATE dbo.TurmaDisciplinas
                    SET
                        Ativa = 1,
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

            using SqlCommand comando =
                new(sql, conexao, transacao);

            AdicionarIds(
                comando,
                idTurma,
                idDisciplina);

            return Convert.ToInt32(comando.ExecuteScalar());
        }

        // Desativa o vínculo selecionado sem excluir seu histórico.
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
                ExecutarTransacao(
                    (conexao, transacao) =>
                    {
                        DesativarVinculo(
                            conexao,
                            transacao,
                            idVinculo,
                            idTurma);

                        RegistrarAuditoria(
                            conexao,
                            transacao,
                            "DESVINCULAR_DISCIPLINA",
                            $"Vínculo acadêmico {idVinculo} desativado.");

                        return true;
                    });

                ExibirMensagem(
                    "Disciplina desvinculada com sucesso!",
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

        // Desativa um vínculo existente no banco de dados.
        private static void DesativarVinculo(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idVinculo,
            int idTurma)
        {
            const string sql = @"
                UPDATE dbo.TurmaDisciplinas
                SET Ativa = 0
                WHERE IdTurmaDisciplina = @IdVinculo
                  AND IdTurma = @IdTurma
                  AND Ativa = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdVinculo",
                SqlDbType.Int).Value = idVinculo;

            comando.Parameters.Add(
                "@IdTurma",
                SqlDbType.Int).Value = idTurma;

            int registrosAlterados =
                comando.ExecuteNonQuery();

            if (registrosAlterados != 1)
            {
                throw new InvalidOperationException(
                    "O vínculo não está mais ativo.");
            }
        }

        // Executa a operação e a auditoria em uma transação.
        private T ExecutarTransacao<T>(
            Func<SqlConnection, SqlTransaction, T> operacao)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                ExigirAutorizacao(conexao, transacao);

                T resultado = operacao(conexao, transacao);

                transacao.Commit();

                return resultado;
            }
            catch
            {
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Revalida a autorização diretamente no banco.
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
                  AND pm.Codigo = @CodigoPermissao
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
                "@CodigoPermissao",
                SqlDbType.VarChar,
                100).Value = PermissaoDisciplinas;

            bool autorizado =
                Convert.ToInt32(comando.ExecuteScalar()) > 0;

            if (!autorizado)
            {
                throw new UnauthorizedAccessException(
                    "Você não possui autorização para esta operação.");
            }
        }

        // Adiciona os identificadores utilizados nas consultas SQL.
        private static void AdicionarIds(
            SqlCommand comando,
            int idTurma,
            int idDisciplina)
        {
            comando.Parameters.Add(
                "@IdTurma",
                SqlDbType.Int).Value = idTurma;

            comando.Parameters.Add(
                "@IdDisciplina",
                SqlDbType.Int).Value = idDisciplina;
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