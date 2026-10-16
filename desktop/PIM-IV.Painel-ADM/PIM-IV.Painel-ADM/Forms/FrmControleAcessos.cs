
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
    public class FrmControleAcessos : Form
    {
        private readonly Usuario _administrador;
        private readonly PermissaoService _permissaoService = new();

        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        private ComboBox cmbPerfil = null!;
        private TextBox txtDescricao = null!;
        private CheckBox chkAtivo = null!;
        private Label lblPlataforma = null!;
        private FlowLayoutPanel painelPermissoes = null!;
        private Button btnSalvar = null!;

        private DataTable _perfis = new();
        private bool _carregando;

        public FrmControleAcessos(Usuario administrador)
        {
            _administrador = administrador;

            Text = "Controle de Acessos";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 760);
            MinimumSize = new Size(750, 650);
            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);

            CriarInterface();

            Shown += (_, _) => CarregarPerfis();
        }

        private void CriarInterface()
        {
            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                ColumnCount = 1,
                RowCount = 9
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));

            Controls.Add(layout);

            Label titulo = new()
            {
                Text = "Gerenciamento de Acessos",
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = _azul,
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
            layout.Controls.Add(painelStatus, 0, 5);

            layout.Controls.Add(
                CriarRotulo("Permissões disponíveis:"), 0, 6);

            painelPermissoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(15),
                BorderStyle = BorderStyle.FixedSingle
            };

            layout.Controls.Add(painelPermissoes, 0, 7);

            FlowLayoutPanel painelBotoes = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            btnSalvar = new Button
            {
                Text = "Salvar alterações",
                Width = 180,
                Height = 42,
                BackColor = _azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.Click += (_, _) => SalvarAlteracoes();

            Button btnAtualizar = new()
            {
                Text = "Atualizar",
                Width = 120,
                Height = 42
            };

            btnAtualizar.Click += (_, _) => CarregarPerfis();

            painelBotoes.Controls.Add(btnSalvar);
            painelBotoes.Controls.Add(btnAtualizar);

            layout.Controls.Add(painelBotoes, 0, 8);
        }

        private Label CriarRotulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 45, 60)
            };
        }

        private static string PlataformaDoPerfil(string nomePerfil)
        {
            return nomePerfil == "Administrador" ||
                   nomePerfil == "Coordenador"
                ? "Desktop"
                : "Web/Mobile";
        }

        private void CarregarPerfis()
        {
            try
            {
                if (!_permissaoService.UsuarioTemPermissao(
                    _administrador, "GERENCIAR_ACESSOS"))
                {
                    MessageBox.Show(
                        "Você não possui permissão para gerenciar acessos.",
                        "Acesso negado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    Close();
                    return;
                }

                const string sql = @"
                    SELECT IdPerfil, NomePerfil, Descricao, Ativo
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

                using SqlCommand comando =
                    new(sql, conexao);

                conexao.Open();

                using SqlDataReader leitor =
                    comando.ExecuteReader();

                DataTable tabela = new();
                tabela.Load(leitor);

                _perfis = tabela;
                _carregando = true;

                cmbPerfil.DataSource = null;
                cmbPerfil.DisplayMember = "NomePerfil";
                cmbPerfil.ValueMember = "IdPerfil";
                cmbPerfil.DataSource = _perfis;

                _carregando = false;
                ExibirPerfilSelecionado();
            }
            catch (Exception ex)
            {
                _carregando = false;

                MessageBox.Show(
                    "Erro ao carregar perfis:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

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

                chkAtivo.Enabled =
                    nomePerfil != "Administrador";

                string plataforma =
                    PlataformaDoPerfil(nomePerfil);

                lblPlataforma.Text =
                    "Plataforma: " + plataforma;

                CarregarPermissoes(idPerfil, nomePerfil);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar permissões:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CarregarPermissoes(
            int idPerfil,
            string nomePerfil)
        {
            painelPermissoes.Controls.Clear();

            DataTable todas =
                _permissaoService.ListarPermissoes();

            DataTable atribuidas =
                _permissaoService.ListarPermissoesDoPerfil(idPerfil);

            HashSet<int> idsAtribuidos = atribuidas
                .AsEnumerable()
                .Select(linha => linha.Field<int>("IdPermissao"))
                .ToHashSet();

            string plataformaPermitida =
                PlataformaDoPerfil(nomePerfil);

            foreach (DataRow linha in todas.Rows)
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

                CheckBox chk = new()
                {
                    Text = descricao,
                    Tag = new PermissaoItem(
                        idPermissao, codigo),
                    Checked = idsAtribuidos.Contains(idPermissao),
                    AutoSize = true,
                    Margin = new Padding(5, 7, 5, 7)
                };

                if (nomePerfil == "Administrador" &&
                    codigo == "GERENCIAR_ACESSOS")
                {
                    chk.Checked = true;
                    chk.Enabled = false;
                }

                painelPermissoes.Controls.Add(chk);
            }
        }

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

            if (string.IsNullOrWhiteSpace(descricao))
            {
                MessageBox.Show(
                    "Informe uma descrição para o perfil.");
                return;
            }

            if (nomePerfil == "Administrador" && !ativo)
            {
                MessageBox.Show(
                    "O perfil Administrador deve permanecer ativo.");
                return;
            }

            List<int> permissoesSelecionadas = new();

            foreach (Control controle in painelPermissoes.Controls)
            {
                if (controle is CheckBox chk &&
                    chk.Checked &&
                    chk.Tag is PermissaoItem item)
                {
                    permissoesSelecionadas.Add(item.IdPermissao);
                }
            }

            if (MessageBox.Show(
                $"Deseja salvar as permissões do perfil {nomePerfil}?",
                "Confirmar alterações",
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
                    conexao.BeginTransaction(
                        IsolationLevel.Serializable);

                try
                {
                    // Confirma que o usuário continua sendo
                    // Administrador e possui autorização.
                    const string sqlAdministrador = @"
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

                    using (SqlCommand cmd = new(
                        sqlAdministrador, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdUsuario", SqlDbType.Int
                        ).Value = _administrador.IdUsuario;

                        if (Convert.ToInt32(
                            cmd.ExecuteScalar()) == 0)
                        {
                            throw new InvalidOperationException(
                                "Administrador sem autorização " +
                                "para alterar permissões.");
                        }
                    }

                    // Bloqueia e confere o perfil selecionado.
                    const string sqlPerfil = @"
                        SELECT NomePerfil
                        FROM dbo.Perfis WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdPerfil = @IdPerfil";

                    string nomeBanco;

                    using (SqlCommand cmd = new(
                        sqlPerfil, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdPerfil", SqlDbType.Int
                        ).Value = idPerfil;

                        nomeBanco =
                            Convert.ToString(
                                cmd.ExecuteScalar()) ?? "";
                    }

                    if (nomeBanco != nomePerfil)
                    {
                        throw new InvalidOperationException(
                            "O perfil selecionado foi alterado. " +
                            "Atualize a tela.");
                    }

                    if (nomePerfil == "Administrador" && !ativo)
                    {
                        throw new InvalidOperationException(
                            "O Administrador não pode ser desativado.");
                    }

                    if (!ativo)
                    {
                        const string sqlUsuarios = @"
                            SELECT COUNT(1)
                            FROM dbo.Usuarios
                            WHERE IdPerfil = @IdPerfil";

                        using SqlCommand cmd = new(
                            sqlUsuarios, conexao, transacao);

                        cmd.Parameters.Add(
                            "@IdPerfil", SqlDbType.Int
                        ).Value = idPerfil;

                        if (Convert.ToInt32(
                            cmd.ExecuteScalar()) > 0)
                        {
                            throw new InvalidOperationException(
                                "Não é possível desativar um perfil " +
                                "que possui usuários vinculados.");
                        }
                    }

                    string plataformaPermitida =
                        PlataformaDoPerfil(nomeBanco);

                    // Valida todas as permissões selecionadas
                    // diretamente no banco, antes de gravar.
                    const string sqlPermissoes = @"
                        SELECT IdPermissao, Codigo, Plataforma
                        FROM dbo.Permissoes WITH (HOLDLOCK)
                        WHERE Ativo = 1";

                    Dictionary<int, (string Codigo, string Plataforma)>
                        permissoesValidas = new();

                    using (SqlCommand cmd = new(
                        sqlPermissoes, conexao, transacao))
                    using (SqlDataReader leitor =
                        cmd.ExecuteReader())
                    {
                        while (leitor.Read())
                        {
                            int id = leitor.GetInt32(0);

                            permissoesValidas[id] = (
                                leitor.GetString(1),
                                leitor.GetString(2)
                            );
                        }
                    }

                    foreach (int id in permissoesSelecionadas)
                    {
                        if (!permissoesValidas.TryGetValue(
                            id, out var permissao))
                        {
                            throw new InvalidOperationException(
                                "Uma das permissões selecionadas " +
                                "não está mais disponível.");
                        }

                        if (!string.Equals(
                            permissao.Plataforma,
                            plataformaPermitida,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "Foi identificada uma permissão " +
                                "incompatível com a plataforma " +
                                "do perfil.");
                        }

                        if (permissao.Codigo == "GERENCIAR_ACESSOS" &&
                            nomePerfil != "Administrador")
                        {
                            throw new InvalidOperationException(
                                "A permissão de gerenciamento de " +
                                "acessos é exclusiva do Administrador.");
                        }
                    }

                    // Proteção da permissão administrativa.
                    if (nomePerfil == "Administrador")
                    {
                        int? idGerenciarAcessos =
                            permissoesValidas
                                .Where(p =>
                                    p.Value.Codigo ==
                                    "GERENCIAR_ACESSOS" &&
                                    string.Equals(
                                        p.Value.Plataforma,
                                        "Desktop",
                                        StringComparison.OrdinalIgnoreCase))
                                .Select(p => (int?)p.Key)
                                .FirstOrDefault();

                        if (idGerenciarAcessos == null)
                        {
                            throw new InvalidOperationException(
                                "A permissão GERENCIAR_ACESSOS " +
                                "não está disponível no banco.");
                        }

                        if (!permissoesSelecionadas.Contains(
                            idGerenciarAcessos.Value))
                        {
                            permissoesSelecionadas.Add(
                                idGerenciarAcessos.Value);
                        }
                    }

                    // Atualiza a descrição e o status.
                    const string sqlAtualizar = @"
                        UPDATE dbo.Perfis
                        SET Descricao = @Descricao,
                            Ativo = @Ativo
                        WHERE IdPerfil = @IdPerfil";

                    using (SqlCommand cmd = new(
                        sqlAtualizar, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@Descricao", SqlDbType.NVarChar, 200
                        ).Value = descricao;

                        cmd.Parameters.Add(
                            "@Ativo", SqlDbType.Bit
                        ).Value = ativo;

                        cmd.Parameters.Add(
                            "@IdPerfil", SqlDbType.Int
                        ).Value = idPerfil;

                        cmd.ExecuteNonQuery();
                    }

                    // Remove apenas permissões da plataforma
                    // correspondente ao perfil.
                    // Permissões de outras plataformas
                    // não são apagadas silenciosamente.
                    const string sqlExcluir = @"
                        DELETE pp
                        FROM dbo.PerfilPermissoes pp
                        INNER JOIN dbo.Permissoes pm
                            ON pp.IdPermissao = pm.IdPermissao
                        WHERE pp.IdPerfil = @IdPerfil
                          AND pm.Plataforma = @Plataforma";

                    using (SqlCommand cmd = new(
                        sqlExcluir, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdPerfil", SqlDbType.Int
                        ).Value = idPerfil;

                        cmd.Parameters.Add(
                            "@Plataforma", SqlDbType.NVarChar, 50
                        ).Value = plataformaPermitida;

                        cmd.ExecuteNonQuery();
                    }

                    // Recria somente os vínculos selecionados.
                    const string sqlInserir = @"
                        INSERT INTO dbo.PerfilPermissoes
                            (IdPerfil, IdPermissao)
                        VALUES
                            (@IdPerfil, @IdPermissao)";

                    foreach (int id in
                        permissoesSelecionadas.Distinct())
                    {
                        using SqlCommand cmd = new(
                            sqlInserir, conexao, transacao);

                        cmd.Parameters.Add(
                            "@IdPerfil", SqlDbType.Int
                        ).Value = idPerfil;

                        cmd.Parameters.Add(
                            "@IdPermissao", SqlDbType.Int
                        ).Value = id;

                        cmd.ExecuteNonQuery();
                    }

                    // Registra a alteração na auditoria.
                    const string sqlLog = @"
                        INSERT INTO dbo.LogsAtividades
                            (IdUsuario, Acao, Descricao)
                        VALUES
                            (@IdUsuario, @Acao, @Descricao)";

                    using (SqlCommand cmd = new(
                        sqlLog, conexao, transacao))
                    {
                        cmd.Parameters.Add(
                            "@IdUsuario", SqlDbType.Int
                        ).Value = _administrador.IdUsuario;

                        cmd.Parameters.Add(
                            "@Acao", SqlDbType.NVarChar, 100
                        ).Value = "ALTERACAO_PERMISSOES";

                        cmd.Parameters.Add(
                            "@Descricao", SqlDbType.NVarChar, 500
                        ).Value =
                            $"Alterou o perfil {nomePerfil} " +
                            $"(ID {idPerfil}). " +
                            $"Plataforma: {plataformaPermitida}. " +
                            $"Permissões selecionadas: " +
                            $"{permissoesSelecionadas.Distinct().Count()}. " +
                            $"Ativo: {ativo}.";

                        cmd.ExecuteNonQuery();
                    }

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }

                MessageBox.Show(
                    "Permissões atualizadas com sucesso!",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                CarregarPerfis();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar alterações:\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private sealed class PermissaoItem
        {
            public int IdPermissao { get; }
            public string Codigo { get; }

            public PermissaoItem(int idPermissao, string codigo)
            {
                IdPermissao = idPermissao;
                Codigo = codigo;
            }
        }
    }
}
