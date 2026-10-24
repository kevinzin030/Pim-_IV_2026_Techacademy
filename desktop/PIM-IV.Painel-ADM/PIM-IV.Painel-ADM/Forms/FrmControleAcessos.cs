using Microsoft.Data.SqlClient;
using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    // Gerencia os perfis de acesso e suas permissões no sistema.
    public class FrmControleAcessos : Form
    {
        private const int PerfilAdministrador = 1;

        private const string NomeAdministrador = "Administrador";
        private const string PermissaoGerenciarAcessos = "GERENCIAR_ACESSOS";
        private const string PlataformaDesktop = "Desktop";
        private const string PlataformaWebMobile = "Web/Mobile";

        private readonly Usuario _administrador;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _corPrincipal = Color.FromArgb(40, 92, 170);
        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);
        private readonly Color _corTexto = Color.FromArgb(35, 45, 60);

        private ComboBox cmbPerfil = null!;
        private TextBox txtDescricao = null!;
        private CheckBox chkAtivo = null!;
        private Label lblPlataforma = null!;
        private FlowLayoutPanel painelPermissoes = null!;
        private Button btnSalvar = null!;

        private DataTable _perfis = new();

        private bool _carregando;

        // Inicializa o formulário com o administrador autenticado.
        public FrmControleAcessos(Usuario administrador)
        {
            _administrador = administrador
                ?? throw new ArgumentNullException(nameof(administrador));

            ConfigurarFormulario();
            CriarInterface();

            Shown += (_, _) => CarregarPerfis();
        }

        // Define a aparência inicial do formulário.
        private void ConfigurarFormulario()
        {
            Text = "Controle de Acessos";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 760);
            MinimumSize = new Size(750, 650);
            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Organiza os controles visuais do gerenciamento de acessos.
        private void CriarInterface()
        {
            TableLayoutPanel layout = CriarLayoutPrincipal();

            Controls.Add(layout);

            Label titulo = new()
            {
                Text = "Gerenciamento de Acessos",
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = _corPrincipal,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(titulo, 0, 0);
            layout.Controls.Add(CriarRotulo("Selecione o perfil:"), 0, 1);

            cmbPerfil = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cmbPerfil.SelectedIndexChanged +=
                (_, _) => ExibirPerfilSelecionado();

            layout.Controls.Add(cmbPerfil, 0, 2);

            layout.Controls.Add(
                CriarRotulo("Descrição do perfil:"), 0, 3);

            txtDescricao = new TextBox
            {
                Dock = DockStyle.Fill,
                MaxLength = 200
            };

            layout.Controls.Add(txtDescricao, 0, 4);

            layout.Controls.Add(CriarPainelStatus(), 0, 5);

            layout.Controls.Add(
                CriarRotulo("Permissões disponíveis:"), 0, 6);

            painelPermissoes = CriarPainelPermissoes();

            layout.Controls.Add(painelPermissoes, 0, 7);

            layout.Controls.Add(CriarPainelBotoes(), 0, 8);
        }

        // Cria a estrutura principal da interface.
        private static TableLayoutPanel CriarLayoutPrincipal()
        {
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                ColumnCount = 1,
                RowCount = 9
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));

            return layout;
        }

        // Cria os controles de status e plataforma do perfil.
        private FlowLayoutPanel CriarPainelStatus()
        {
            FlowLayoutPanel painelStatus = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };

            chkAtivo = new CheckBox
            {
                Text = "Perfil ativo",
                AutoSize = true,
                Margin = new Padding(0, 10, 30, 0)
            };

            lblPlataforma = new Label
            {
                AutoSize = true,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 10, 0, 0)
            };

            painelStatus.Controls.Add(chkAtivo);
            painelStatus.Controls.Add(lblPlataforma);

            return painelStatus;
        }

        // Cria a área rolável das permissões.
        private static FlowLayoutPanel CriarPainelPermissoes()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(15),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        // Cria os botões de salvar e atualizar.
        private FlowLayoutPanel CriarPainelBotoes()
        {
            FlowLayoutPanel painelBotoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            btnSalvar = CriarBotao(
                "Salvar alterações",
                180,
                _corPrincipal,
                Color.White);

            Button btnAtualizar = CriarBotao(
                "Atualizar",
                120,
                SystemColors.Control,
                Color.Black);

            btnSalvar.Click += (_, _) => SalvarAlteracoes();
            btnAtualizar.Click += (_, _) => CarregarPerfis();

            painelBotoes.Controls.Add(btnSalvar);
            painelBotoes.Controls.Add(btnAtualizar);

            return painelBotoes;
        }

        // Cria rótulos com aparência padronizada.
        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _corTexto
            };
        }

        // Cria botões reutilizáveis para as ações do formulário.
        private static Button CriarBotao(
            string texto,
            int largura,
            Color corFundo,
            Color corTexto)
        {
            Button botao = new()
            {
                Text = texto,
                Width = largura,
                Height = 42,
                BackColor = corFundo,
                ForeColor = corTexto,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            botao.FlatAppearance.BorderSize = 0;

            return botao;
        }

        // Identifica a plataforma correspondente ao perfil.
        private static string PlataformaDoPerfil(string nomePerfil)
        {
            return nomePerfil == NomeAdministrador ||
                   nomePerfil == "Coordenador"
                ? PlataformaDesktop
                : PlataformaWebMobile;
        }

        // Verifica se o administrador possui autorização para gerenciar acessos.
        private bool AdministradorPossuiPermissao()
        {
            return _administrador.IdPerfil == PerfilAdministrador &&
                   _administrador.Ativo &&
                   _permissaoService.UsuarioTemPermissao(
                       _administrador,
                       PermissaoGerenciarAcessos);
        }

        // Consulta os perfis cadastrados no banco de dados.
        private static DataTable ConsultarPerfis()
        {
            const string sql = @"
                SELECT
                    IdPerfil,
                    NomePerfil,
                    Descricao,
                    Ativo
                FROM dbo.Perfis
                WHERE NomePerfil IN
                (
                    'Administrador',
                    'Coordenador',
                    'Professor',
                    'Aluno'
                )
                ORDER BY IdPerfil";

            using SqlConnection conexao =
                Conexao.CriarConexao();

            using SqlCommand comando = new(sql, conexao);

            conexao.Open();

            using SqlDataReader leitor =
                comando.ExecuteReader();

            DataTable tabela = new();

            tabela.Load(leitor);

            return tabela;
        }

        // Atualiza a lista de perfis disponíveis.
        private void CarregarPerfis()
        {
            try
            {
                if (!AdministradorPossuiPermissao())
                {
                    ExibirMensagem(
                        "Você não possui permissão para gerenciar acessos.",
                        "Acesso negado",
                        MessageBoxIcon.Warning);

                    Close();
                    return;
                }

                DataTable tabela = ConsultarPerfis();

                AtualizarListaPerfis(tabela);

                ExibirPerfilSelecionado();
            }
            catch (Exception ex)
            {
                _carregando = false;

                ExibirMensagem(
                    "Erro ao carregar perfis:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
        }

        // Atualiza o ComboBox sem executar eventos intermediários.
        private void AtualizarListaPerfis(DataTable tabela)
        {
            _carregando = true;

            try
            {
                _perfis = tabela;

                cmbPerfil.DataSource = null;
                cmbPerfil.DisplayMember = "NomePerfil";
                cmbPerfil.ValueMember = "IdPerfil";
                cmbPerfil.DataSource = _perfis;
            }
            finally
            {
                _carregando = false;
            }
        }

        // Exibe os dados e as permissões do perfil selecionado.
        private void ExibirPerfilSelecionado()
        {
            if (_carregando ||
                cmbPerfil.SelectedItem is not DataRowView linha)
            {
                return;
            }

            try
            {
                int idPerfil =
                    Convert.ToInt32(linha["IdPerfil"]);

                string nomePerfil =
                    Convert.ToString(linha["NomePerfil"]) ?? "";

                txtDescricao.Text =
                    Convert.ToString(linha["Descricao"]) ?? "";

                chkAtivo.Checked =
                    Convert.ToBoolean(linha["Ativo"]);

                // O Administrador não pode ser desativado.
                chkAtivo.Enabled =
                    nomePerfil != NomeAdministrador;

                lblPlataforma.Text =
                    "Plataforma: " + PlataformaDoPerfil(nomePerfil);

                CarregarPermissoes(idPerfil, nomePerfil);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Erro ao carregar permissões:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
        }

        // Consulta e apresenta as permissões do perfil selecionado.
        private void CarregarPermissoes(
            int idPerfil,
            string nomePerfil)
        {
            painelPermissoes.Controls.Clear();

            DataTable todasPermissoes =
                _permissaoService.ListarPermissoes();

            DataTable permissoesAtribuidas =
                _permissaoService.ListarPermissoesDoPerfil(idPerfil);

            HashSet<int> idsAtribuidos = permissoesAtribuidas
                .AsEnumerable()
                .Select(linha => linha.Field<int>("IdPermissao"))
                .ToHashSet();

            string plataformaPermitida =
                PlataformaDoPerfil(nomePerfil);

            foreach (DataRow linha in todasPermissoes.Rows)
            {
                int idPermissao =
                    Convert.ToInt32(linha["IdPermissao"]);

                string codigo =
                    Convert.ToString(linha["Codigo"]) ?? "";

                string descricao =
                    Convert.ToString(linha["Descricao"]) ?? "";

                string plataforma =
                    Convert.ToString(linha["Plataforma"]) ?? "";

                if (!string.Equals(
                    plataforma,
                    plataformaPermitida,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CheckBox chkPermissao = CriarCheckBoxPermissao(
                    idPermissao,
                    codigo,
                    descricao,
                    idsAtribuidos.Contains(idPermissao),
                    nomePerfil);

                painelPermissoes.Controls.Add(chkPermissao);
            }
        }

        // Cria o CheckBox de cada permissão.
        private static CheckBox CriarCheckBoxPermissao(
            int idPermissao,
            string codigo,
            string descricao,
            bool atribuida,
            string nomePerfil)
        {
            CheckBox chkPermissao = new()
            {
                Text = descricao,
                Tag = new PermissaoItem(idPermissao, codigo),
                Checked = atribuida,
                AutoSize = true,
                Margin = new Padding(5, 7, 5, 7)
            };

            // O Administrador deve manter a permissão de gerenciar acessos.
            if (nomePerfil == NomeAdministrador &&
                codigo == PermissaoGerenciarAcessos)
            {
                chkPermissao.Checked = true;
                chkPermissao.Enabled = false;
            }

            return chkPermissao;
        }

        // Reúne os identificadores das permissões selecionadas.
        private List<int> ObterPermissoesSelecionadas()
        {
            List<int> permissoesSelecionadas = new();

            foreach (Control controle in painelPermissoes.Controls)
            {
                if (controle is CheckBox chkPermissao &&
                    chkPermissao.Checked &&
                    chkPermissao.Tag is PermissaoItem item)
                {
                    permissoesSelecionadas.Add(item.IdPermissao);
                }
            }

            return permissoesSelecionadas;
        }

        // Valida os dados antes de iniciar a gravação.
        private bool ValidarDadosPerfil(
            string nomePerfil,
            string descricao,
            bool ativo)
        {
            if (string.IsNullOrWhiteSpace(descricao))
            {
                ExibirMensagem(
                    "Informe uma descrição para o perfil.",
                    "Validação",
                    MessageBoxIcon.Warning);

                txtDescricao.Focus();

                return false;
            }

            if (nomePerfil == NomeAdministrador && !ativo)
            {
                ExibirMensagem(
                    "O perfil Administrador deve permanecer ativo.",
                    "Validação",
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        // Confirma e executa a atualização do perfil selecionado.
        private void SalvarAlteracoes()
        {
            if (cmbPerfil.SelectedItem is not DataRowView linha)
            {
                return;
            }

            int idPerfil =
                Convert.ToInt32(linha["IdPerfil"]);

            string nomePerfil =
                Convert.ToString(linha["NomePerfil"]) ?? "";

            string descricao = txtDescricao.Text.Trim();
            bool ativo = chkAtivo.Checked;

            if (!ValidarDadosPerfil(nomePerfil, descricao, ativo))
            {
                return;
            }

            List<int> permissoesSelecionadas =
                ObterPermissoesSelecionadas();

            DialogResult confirmacao = MessageBox.Show(
                $"Deseja salvar as permissões do perfil {nomePerfil}?",
                "Confirmar alterações",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes)
            {
                return;
            }

            try
            {
                btnSalvar.Enabled = false;

                ExecutarAtualizacaoPerfil(
                    idPerfil,
                    nomePerfil,
                    descricao,
                    ativo,
                    permissoesSelecionadas);

                ExibirMensagem(
                    "Permissões atualizadas com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);

                CarregarPerfis();
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Erro ao salvar alterações:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSalvar.Enabled = true;
            }
        }

        // Executa todas as alterações dentro de uma transação SQL.
        private void ExecutarAtualizacaoPerfil(
            int idPerfil,
            string nomePerfil,
            string descricao,
            bool ativo,
            List<int> permissoesSelecionadas)
        {
            using SqlConnection conexao =
                Conexao.CriarConexao();

            conexao.Open();

            using SqlTransaction transacao =
                conexao.BeginTransaction(
                    IsolationLevel.Serializable);

            try
            {
                ValidarAdministradorNoBanco(
                    conexao,
                    transacao);

                string nomePerfilBanco =
                    ValidarPerfilNoBanco(
                        conexao,
                        transacao,
                        idPerfil,
                        nomePerfil,
                        ativo);

                string plataformaPermitida =
                    PlataformaDoPerfil(nomePerfilBanco);

                Dictionary<int, (string Codigo, string Plataforma)>
                    permissoesValidas =
                        ConsultarPermissoesValidas(
                            conexao,
                            transacao);

                ValidarPermissoesSelecionadas(
                    permissoesSelecionadas,
                    permissoesValidas,
                    nomePerfilBanco,
                    plataformaPermitida);

                GarantirPermissaoAdministrativa(
                    permissoesSelecionadas,
                    permissoesValidas,
                    nomePerfilBanco);

                AtualizarPerfilNoBanco(
                    conexao,
                    transacao,
                    idPerfil,
                    descricao,
                    ativo);

                AtualizarPermissoesDoPerfil(
                    conexao,
                    transacao,
                    idPerfil,
                    plataformaPermitida,
                    permissoesSelecionadas);

                RegistrarAuditoria(
                    conexao,
                    transacao,
                    idPerfil,
                    nomePerfilBanco,
                    plataformaPermitida,
                    permissoesSelecionadas,
                    ativo);

                transacao.Commit();
            }
            catch
            {
                // Desfaz as alterações caso alguma operação falhe.
                if (transacao.Connection != null)
                {
                    transacao.Rollback();
                }

                throw;
            }
        }

        // Confirma a autorização do administrador diretamente no banco.
        private void ValidarAdministradorNoBanco(
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
                  AND u.Ativo = 1
                  AND p.Ativo = 1
                  AND p.NomePerfil = 'Administrador'
                  AND pm.Codigo = 'GERENCIAR_ACESSOS'
                  AND pm.Ativo = 1";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value = _administrador.IdUsuario;

            if (Convert.ToInt32(comando.ExecuteScalar()) == 0)
            {
                throw new InvalidOperationException(
                    "Administrador sem autorização para alterar permissões.");
            }
        }

        // Bloqueia o perfil e verifica se ele ainda pode ser alterado.
        private static string ValidarPerfilNoBanco(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil,
            string nomePerfil,
            bool ativo)
        {
            const string sqlPerfil = @"
                SELECT NomePerfil
                FROM dbo.Perfis WITH (UPDLOCK, HOLDLOCK)
                WHERE IdPerfil = @IdPerfil";

            string nomePerfilBanco;

            using (SqlCommand comando = new(
                sqlPerfil,
                conexao,
                transacao))
            {
                comando.Parameters.Add(
                    "@IdPerfil",
                    SqlDbType.Int).Value = idPerfil;

                nomePerfilBanco =
                    Convert.ToString(comando.ExecuteScalar()) ?? "";
            }

            if (nomePerfilBanco != nomePerfil)
            {
                throw new InvalidOperationException(
                    "O perfil selecionado foi alterado. Atualize a tela.");
            }

            if (nomePerfilBanco == NomeAdministrador && !ativo)
            {
                throw new InvalidOperationException(
                    "O Administrador não pode ser desativado.");
            }

            if (!ativo)
            {
                ValidarPerfilSemUsuarios(
                    conexao,
                    transacao,
                    idPerfil);
            }

            return nomePerfilBanco;
        }

        // Impede a desativação de perfis que possuem usuários vinculados.
        private static void ValidarPerfilSemUsuarios(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.Usuarios
                WHERE IdPerfil = @IdPerfil";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = idPerfil;

            if (Convert.ToInt32(comando.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException(
                    "Não é possível desativar um perfil que possui usuários vinculados.");
            }
        }

        // Consulta as permissões ativas para validar os vínculos.
        private static Dictionary<int, (string Codigo, string Plataforma)>
            ConsultarPermissoesValidas(
                SqlConnection conexao,
                SqlTransaction transacao)
        {
            const string sql = @"
                SELECT IdPermissao, Codigo, Plataforma
                FROM dbo.Permissoes WITH (HOLDLOCK)
                WHERE Ativo = 1";

            Dictionary<int, (string Codigo, string Plataforma)>
                permissoesValidas = new();

            using SqlCommand comando =
                new(sql, conexao, transacao);

            using SqlDataReader leitor =
                comando.ExecuteReader();

            while (leitor.Read())
            {
                int idPermissao = leitor.GetInt32(0);

                permissoesValidas[idPermissao] = (
                    leitor.GetString(1),
                    leitor.GetString(2)
                );
            }

            return permissoesValidas;
        }

        // Impede a atribuição de permissões inválidas ou incompatíveis.
        private static void ValidarPermissoesSelecionadas(
            List<int> permissoesSelecionadas,
            Dictionary<int, (string Codigo, string Plataforma)> permissoesValidas,
            string nomePerfil,
            string plataformaPermitida)
        {
            foreach (int idPermissao in permissoesSelecionadas)
            {
                if (!permissoesValidas.TryGetValue(
                    idPermissao,
                    out var permissao))
                {
                    throw new InvalidOperationException(
                        "Uma das permissões selecionadas não está mais disponível.");
                }

                if (!string.Equals(
                    permissao.Plataforma,
                    plataformaPermitida,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Foi identificada uma permissão incompatível " +
                        "com a plataforma do perfil.");
                }

                if (permissao.Codigo == PermissaoGerenciarAcessos &&
                    nomePerfil != NomeAdministrador)
                {
                    throw new InvalidOperationException(
                        "A permissão de gerenciamento de acessos " +
                        "é exclusiva do Administrador.");
                }
            }
        }

        // Garante que o Administrador mantenha a permissão de gerenciar acessos.
        private static void GarantirPermissaoAdministrativa(
            List<int> permissoesSelecionadas,
            Dictionary<int, (string Codigo, string Plataforma)> permissoesValidas,
            string nomePerfil)
        {
            if (nomePerfil != NomeAdministrador)
            {
                return;
            }

            int? idGerenciarAcessos = permissoesValidas
                .Where(permissao =>
                    permissao.Value.Codigo == PermissaoGerenciarAcessos &&
                    string.Equals(
                        permissao.Value.Plataforma,
                        PlataformaDesktop,
                        StringComparison.OrdinalIgnoreCase))
                .Select(permissao => (int?)permissao.Key)
                .FirstOrDefault();

            if (idGerenciarAcessos == null)
            {
                throw new InvalidOperationException(
                    "A permissão GERENCIAR_ACESSOS não está disponível no banco.");
            }

            if (!permissoesSelecionadas.Contains(idGerenciarAcessos.Value))
            {
                permissoesSelecionadas.Add(idGerenciarAcessos.Value);
            }
        }

        // Atualiza a descrição e a situação do perfil.
        private static void AtualizarPerfilNoBanco(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil,
            string descricao,
            bool ativo)
        {
            const string sql = @"
                UPDATE dbo.Perfis
                SET
                    Descricao = @Descricao,
                    Ativo = @Ativo
                WHERE IdPerfil = @IdPerfil";

            using SqlCommand comando =
                new(sql, conexao, transacao);

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                200).Value = descricao;

            comando.Parameters.Add(
                "@Ativo",
                SqlDbType.Bit).Value = ativo;

            comando.Parameters.Add(
                "@IdPerfil",
                SqlDbType.Int).Value = idPerfil;

            comando.ExecuteNonQuery();
        }

        // Substitui apenas as permissões da plataforma correspondente.
        private static void AtualizarPermissoesDoPerfil(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil,
            string plataformaPermitida,
            List<int> permissoesSelecionadas)
        {
            const string sqlExcluir = @"
                DELETE pp
                FROM dbo.PerfilPermissoes pp
                INNER JOIN dbo.Permissoes pm
                    ON pp.IdPermissao = pm.IdPermissao
                WHERE pp.IdPerfil = @IdPerfil
                  AND pm.Plataforma = @Plataforma";

            using (SqlCommand comando = new(
                sqlExcluir,
                conexao,
                transacao))
            {
                comando.Parameters.Add(
                    "@IdPerfil",
                    SqlDbType.Int).Value = idPerfil;

                comando.Parameters.Add(
                    "@Plataforma",
                    SqlDbType.NVarChar,
                    50).Value = plataformaPermitida;

                comando.ExecuteNonQuery();
            }

            const string sqlInserir = @"
                INSERT INTO dbo.PerfilPermissoes
                (
                    IdPerfil,
                    IdPermissao
                )
                VALUES
                (
                    @IdPerfil,
                    @IdPermissao
                )";

            foreach (int idPermissao in permissoesSelecionadas.Distinct())
            {
                using SqlCommand comando =
                    new(sqlInserir, conexao, transacao);

                comando.Parameters.Add(
                    "@IdPerfil",
                    SqlDbType.Int).Value = idPerfil;

                comando.Parameters.Add(
                    "@IdPermissao",
                    SqlDbType.Int).Value = idPermissao;

                comando.ExecuteNonQuery();
            }
        }

        // Registra as alterações de acesso para fins de auditoria.
        private void RegistrarAuditoria(
            SqlConnection conexao,
            SqlTransaction transacao,
            int idPerfil,
            string nomePerfil,
            string plataformaPermitida,
            List<int> permissoesSelecionadas,
            bool ativo)
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
                SqlDbType.Int).Value = _administrador.IdUsuario;

            comando.Parameters.Add(
                "@Acao",
                SqlDbType.NVarChar,
                100).Value = "ALTERACAO_PERMISSOES";

            comando.Parameters.Add(
                "@Descricao",
                SqlDbType.NVarChar,
                500).Value =
                    $"Alterou o perfil {nomePerfil} (ID {idPerfil}). " +
                    $"Plataforma: {plataformaPermitida}. " +
                    $"Permissões selecionadas: " +
                    $"{permissoesSelecionadas.Distinct().Count()}. " +
                    $"Ativo: {ativo}.";

            comando.ExecuteNonQuery();
        }

        // Exibe mensagens padronizadas ao usuário.
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

        // Representa uma permissão associada ao CheckBox.
        private sealed class PermissaoItem
        {
            public int IdPermissao { get; }
            public string Codigo { get; }

            public PermissaoItem(
                int idPermissao,
                string codigo)
            {
                IdPermissao = idPermissao;
                Codigo = codigo;
            }
        }
    }
}