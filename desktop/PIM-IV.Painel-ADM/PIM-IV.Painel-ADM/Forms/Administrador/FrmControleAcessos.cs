using PIM_IV.Painel_ADM.Data;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Servicos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    /// <summary>
    /// Gerencia os perfis e suas permissões através da API REST.
    /// </summary>
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
        private Button btnAtualizar = null!;

        private DataTable _perfis = new();

        private bool _carregando;
        private bool _processando;

        // Configura a interpretação das respostas JSON da API.
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Inicializa o formulário com o administrador autenticado.
        public FrmControleAcessos(Usuario administrador)
        {
            _administrador = administrador
                ?? throw new ArgumentNullException(nameof(administrador));

            ConfigurarFormulario();
            CriarInterface();

            Shown += async (_, _) => await CarregarPerfisAsync();
        }

        // Define a aparência original do formulário.
        private void ConfigurarFormulario()
        {
            Text = "Controle de Acessos";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 760);
            MinimumSize = new Size(750, 650);
            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Organiza os componentes visuais do gerenciamento de acessos.
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
                async (_, _) => await ExibirPerfilSelecionadoAsync();

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

            btnSalvar.Enabled = false;
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

            btnAtualizar = CriarBotao(
                "Atualizar",
                120,
                SystemColors.Control,
                Color.Black);

            btnSalvar.Click +=
                async (_, _) => await SalvarAlteracoesAsync();

            btnAtualizar.Click +=
                async (_, _) => await CarregarPerfisAsync();

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

        // Cria botões reutilizáveis.
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

        // Controla a disponibilidade da interface durante requisições.
        private void DefinirProcessamento(
            bool processando,
            bool permitirSalvar = false)
        {
            _processando = processando;

            UseWaitCursor = processando;

            cmbPerfil.Enabled = !processando;
            txtDescricao.Enabled = !processando;
            painelPermissoes.Enabled = !processando;
            btnAtualizar.Enabled = !processando;

            btnSalvar.Enabled =
                !processando && permitirSalvar;

            string nomePerfil =
                cmbPerfil.SelectedItem is DataRowView linha
                    ? Convert.ToString(linha["NomePerfil"]) ?? ""
                    : "";

            chkAtivo.Enabled =
                !processando &&
                nomePerfil != NomeAdministrador;
        }

        // Consulta os perfis cadastrados através da API REST.
        private static async Task<DataTable> ConsultarPerfisAsync()
        {
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.GetAsync("perfis");

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ObterMensagemErro(conteudo));
            }

            RespostaPerfis? resultado =
                JsonSerializer.Deserialize<RespostaPerfis>(
                    conteudo,
                    OpcoesJson);

            if (resultado?.Perfis == null)
            {
                throw new InvalidOperationException(
                    "A API retornou uma lista de perfis inválida.");
            }

            // Preserva a estrutura utilizada pelo ComboBox original.
            DataTable tabela = new();

            tabela.Columns.Add("IdPerfil", typeof(int));
            tabela.Columns.Add("NomePerfil", typeof(string));
            tabela.Columns.Add("Descricao", typeof(string));
            tabela.Columns.Add("Ativo", typeof(bool));

            foreach (DadosPerfil perfil in resultado.Perfis)
            {
                tabela.Rows.Add(
                    perfil.IdPerfil,
                    perfil.NomePerfil,
                    perfil.Descricao,
                    perfil.Ativo);
            }

            return tabela;
        }

        // Atualiza a lista de perfis sem bloquear a interface.
        private async Task CarregarPerfisAsync()
        {
            if (_processando)
            {
                return;
            }

            bool carregamentoConcluido = false;

            try
            {
                DefinirProcessamento(true);

                bool autorizado = await Task.Run(
                    AdministradorPossuiPermissao);

                if (!autorizado)
                {
                    ExibirMensagem(
                        "Você não possui permissão para gerenciar acessos.",
                        "Acesso negado",
                        MessageBoxIcon.Warning);

                    Close();
                    return;
                }

                DataTable tabela = await ConsultarPerfisAsync();

                if (IsDisposed || Disposing)
                {
                    return;
                }

                AtualizarListaPerfis(tabela);

                carregamentoConcluido = true;
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Erro ao carregar perfis:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    DefinirProcessamento(false);
                }
            }

            if (carregamentoConcluido &&
                !IsDisposed &&
                !Disposing)
            {
                await ExibirPerfilSelecionadoAsync();
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
        private async Task ExibirPerfilSelecionadoAsync()
        {
            if (_carregando ||
                _processando ||
                cmbPerfil.SelectedItem is not DataRowView linha)
            {
                return;
            }

            bool permissoesCarregadas = false;

            try
            {
                DefinirProcessamento(true);

                int idPerfil =
                    Convert.ToInt32(linha["IdPerfil"]);

                string nomePerfil =
                    Convert.ToString(linha["NomePerfil"]) ?? "";

                txtDescricao.Text =
                    Convert.ToString(linha["Descricao"]) ?? "";

                chkAtivo.Checked =
                    Convert.ToBoolean(linha["Ativo"]);

                lblPlataforma.Text =
                    "Plataforma: " + PlataformaDoPerfil(nomePerfil);

                await CarregarPermissoesAsync(
                    idPerfil,
                    nomePerfil);

                permissoesCarregadas = true;
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    "Erro ao carregar permissões:\n" + ex.Message,
                    "Erro",
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    DefinirProcessamento(
                        false,
                        permissoesCarregadas);
                }
            }
        }

        // Consulta e apresenta as permissões do perfil selecionado.
        private async Task CarregarPermissoesAsync(
            int idPerfil,
            string nomePerfil)
        {
            // Remove e libera os componentes da seleção anterior.
            while (painelPermissoes.Controls.Count > 0)
            {
                Control controle =
                    painelPermissoes.Controls[0];

                painelPermissoes.Controls.Remove(controle);

                controle.Dispose();
            }

            // Executa as consultas fora da thread da interface.
            Task<DataTable> consultaTodas = Task.Run(
                () => _permissaoService.ListarPermissoes());

            Task<DataTable> consultaAtribuidas = Task.Run(
                () => _permissaoService.ListarPermissoesDoPerfil(
                    idPerfil));

            await Task.WhenAll(
                consultaTodas,
                consultaAtribuidas);

            if (IsDisposed || Disposing)
            {
                return;
            }

            DataTable todasPermissoes = await consultaTodas;

            DataTable permissoesAtribuidas =
                await consultaAtribuidas;

            HashSet<int> idsAtribuidos = permissoesAtribuidas
                .AsEnumerable()
                .Select(linha => linha.Field<int>("IdPermissao"))
                .ToHashSet();

            string plataformaPermitida =
                PlataformaDoPerfil(nomePerfil);

            // Apresenta somente permissões da plataforma correspondente.
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

        // Cria o CheckBox correspondente a cada permissão.
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

            // Garante que o administrador mantenha o gerenciamento de acessos.
            if (codigo == PermissaoGerenciarAcessos)
            {
                chkPermissao.Checked =
                    nomePerfil == NomeAdministrador;

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
                    permissoesSelecionadas.Add(
                        item.IdPermissao);
                }
            }

            return permissoesSelecionadas
                .Distinct()
                .ToList();
        }

        // Valida os dados antes de solicitar a atualização.
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

            if (descricao.Length > 200)
            {
                ExibirMensagem(
                    "A descrição deve possuir até 200 caracteres.",
                    "Validação",
                    MessageBoxIcon.Warning);

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

        // Confirma e solicita a atualização do perfil selecionado.
        private async Task SalvarAlteracoesAsync()
        {
            if (_processando ||
                !btnSalvar.Enabled ||
                cmbPerfil.SelectedItem is not DataRowView linha)
            {
                return;
            }

            int idPerfil =
                Convert.ToInt32(linha["IdPerfil"]);

            string nomePerfil =
                Convert.ToString(linha["NomePerfil"]) ?? "";

            string descricao = txtDescricao.Text.Trim();

            bool ativo = chkAtivo.Checked;

            if (!ValidarDadosPerfil(
                nomePerfil,
                descricao,
                ativo))
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

            bool atualizacaoConcluida = false;

            try
            {
                DefinirProcessamento(true);

                await ExecutarAtualizacaoPerfilAsync(
                    idPerfil,
                    descricao,
                    ativo,
                    permissoesSelecionadas);

                atualizacaoConcluida = true;

                ExibirMensagem(
                    "Permissões atualizadas com sucesso!",
                    "Sucesso",
                    MessageBoxIcon.Information);
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
                if (!IsDisposed && !Disposing)
                {
                    DefinirProcessamento(
                        false,
                        !atualizacaoConcluida);
                }
            }

            if (atualizacaoConcluida &&
                !IsDisposed &&
                !Disposing)
            {
                await CarregarPerfisAsync();
            }
        }

        /// <summary>
        /// Envia os dados do perfil para a API.
        /// A transação, autorização e auditoria são executadas no servidor.
        /// </summary>
        private static async Task ExecutarAtualizacaoPerfilAsync(
            int idPerfil,
            string descricao,
            bool ativo,
            List<int> permissoesSelecionadas)
        {
            // Prepara os dados esperados pelo PerfilController.
            var dadosAtualizacao = new
            {
                descricao,
                ativo,
                permissoes = permissoesSelecionadas
            };

            // Envia a atualização utilizando a sessão JWT.
            using HttpResponseMessage resposta =
                await ApiClient.Cliente.PutAsJsonAsync(
                    $"perfis/{idPerfil}",
                    dadosAtualizacao);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            // Apresenta as validações retornadas pelo servidor.
            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ObterMensagemErro(conteudo));
            }
        }

        // Extrai a mensagem de erro retornada pela API.
        private static string ObterMensagemErro(string conteudo)
        {
            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(conteudo);

                if (documento.RootElement.TryGetProperty(
                    "mensagem",
                    out JsonElement mensagem))
                {
                    return mensagem.GetString()
                        ?? "Não foi possível concluir a operação.";
                }
            }
            catch (JsonException)
            {
                // Utiliza a mensagem padrão para respostas inválidas.
            }

            return "Não foi possível concluir a operação.";
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

        // Representa a resposta de listagem de perfis da API.
        private sealed class RespostaPerfis
        {
            public DadosPerfil[]? Perfis { get; set; }
        }

        // Representa os dados cadastrais de um perfil.
        private sealed class DadosPerfil
        {
            public int IdPerfil { get; set; }

            public string NomePerfil { get; set; } = "";

            public string Descricao { get; set; } = "";

            public bool Ativo { get; set; }
        }
    }
}