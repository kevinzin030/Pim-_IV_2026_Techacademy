using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    /// <summary>
    /// Formulário de navegação do módulo de disciplinas.
    ///
    /// Organiza as funcionalidades em duas abas:
    /// - Gerenciar disciplinas;
    /// - Vincular disciplinas às turmas.
    ///
    /// As regras de negócio permanecem nos formulários
    /// independentes, evitando duplicação de código.
    /// </summary>
    public class FrmDisciplinas : Form
    {
        // Usuário autenticado, repassado aos formulários internos
        // para que mantenham suas verificações de permissão.
        private readonly Usuario _usuario;

        // Cores utilizadas para diferenciar a aba selecionada.
        private readonly Color _azul = Color.FromArgb(40, 92, 170);
        private readonly Color _azulClaro = Color.FromArgb(225, 233, 244);
        private readonly Color _textoEscuro = Color.FromArgb(24, 39, 65);
        private readonly Color _fundo = Color.FromArgb(244, 247, 251);

        // Controle principal que organiza a navegação por abas.
        private readonly TabControl _abas = new();

        // Cada aba recebe um formulário já existente no projeto.
        private readonly TabPage _abaGerenciar = new();
        private readonly TabPage _abaVincular = new();

        // Referências para impedir a criação repetida das telas.
        private FrmGerenciarDisciplinas? _gerenciamento;
        private FrmVincularDisciplinas? _vinculacao;

        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public FrmDisciplinas(Usuario usuario)
        {
            // Impede abrir o módulo sem um usuário autenticado.
            _usuario = usuario ??
                throw new ArgumentNullException(nameof(usuario));

            Text = "TechAcademy - Disciplinas";
            Size = new Size(1150, 800);
            MinimumSize = new Size(650, 450);
            StartPosition = FormStartPosition.CenterParent;

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = _fundo;
            Font = new Font("Segoe UI", 10);

            ConfigurarAbas();

            // Carrega a primeira tela quando o formulário aparece.
            Shown += (_, _) => CarregarGerenciamento();
        }

        // =====================================================
        // CONFIGURAÇÃO DAS ABAS
        // =====================================================

        private void ConfigurarAbas()
        {
            _abas.Dock = DockStyle.Fill;

            // Desenho personalizado permite destacar a aba ativa.
            _abas.DrawMode = TabDrawMode.OwnerDrawFixed;
            _abas.SizeMode = TabSizeMode.Fixed;
            _abas.ItemSize = new Size(240, 46);
            _abas.Font = new Font(
                "Segoe UI", 12, FontStyle.Bold);

            _abaGerenciar.Text = "Gerenciar disciplinas";
            _abaGerenciar.BackColor = _fundo;

            _abaVincular.Text = "Vincular às turmas";
            _abaVincular.BackColor = _fundo;

            _abas.TabPages.Add(_abaGerenciar);
            _abas.TabPages.Add(_abaVincular);

            _abas.DrawItem += DesenharAba;

            // Cada formulário é carregado somente quando
            // sua respectiva aba é selecionada.
            _abas.SelectedIndexChanged += (_, _) =>
            {
                if (_abas.SelectedTab == _abaGerenciar)
                {
                    CarregarGerenciamento();
                }
                else if (_abas.SelectedTab == _abaVincular)
                {
                    CarregarVinculacao();
                }
            };

            Controls.Add(_abas);
        }

        // =====================================================
        // APARÊNCIA DAS ABAS
        // =====================================================

        private void DesenharAba(
            object? sender,
            DrawItemEventArgs e)
        {
            // Garante que a aba solicitada existe.
            if (e.Index < 0 || e.Index >= _abas.TabPages.Count)
                return;

            bool selecionada = e.Index == _abas.SelectedIndex;

            Rectangle area = _abas.GetTabRect(e.Index);

            // Aba ativa: azul com letras brancas.
            // Aba inativa: azul-claro com letras escuras.
            Color fundo = selecionada ? _azul : _azulClaro;
            Color texto = selecionada
                ? Color.White
                : _textoEscuro;

            using (SolidBrush pincel = new(fundo))
            {
                e.Graphics.FillRectangle(pincel, area);
            }

            Rectangle areaTexto =
                Rectangle.Inflate(area, -10, -4);

            TextRenderer.DrawText(
                e.Graphics,
                _abas.TabPages[e.Index].Text,
                _abas.Font,
                areaTexto,
                texto,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis
            );

            // Linha inferior para identificar a aba ativa.
            if (selecionada)
            {
                using Pen linha = new(_textoEscuro, 3);

                e.Graphics.DrawLine(
                    linha,
                    area.Left + 2,
                    area.Bottom - 2,
                    area.Right - 3,
                    area.Bottom - 2
                );
            }
        }

        // =====================================================
        // GERENCIAMENTO DE DISCIPLINAS
        // =====================================================

        private void CarregarGerenciamento()
        {
            // Reutiliza a instância caso já tenha sido criada.
            if (_gerenciamento != null &&
                !_gerenciamento.IsDisposed)
            {
                return;
            }

            // O formulário original mantém cadastro,
            // pesquisa, edição, SQL Server e auditoria.
            _gerenciamento =
                new FrmGerenciarDisciplinas(_usuario);

            IncorporarFormulario(
                _gerenciamento,
                _abaGerenciar);
        }

        // =====================================================
        // VINCULAÇÃO DE DISCIPLINAS
        // =====================================================

        private void CarregarVinculacao()
        {
            if (_vinculacao != null &&
                !_vinculacao.IsDisposed)
            {
                return;
            }

            // O formulário original mantém vinculação,
            // desvinculação e suas regras de negócio.
            _vinculacao =
                new FrmVincularDisciplinas(_usuario);

            IncorporarFormulario(
                _vinculacao,
                _abaVincular);
        }

        // =====================================================
        // EXIBIÇÃO DE FORMULÁRIOS DENTRO DAS ABAS
        // =====================================================

        private static void IncorporarFormulario(
            Form formulario,
            TabPage aba)
        {
            // Permite incorporar o formulário como um controle.
            formulario.TopLevel = false;

            // Remove a barra de título da janela interna.
            formulario.FormBorderStyle =
                FormBorderStyle.None;

            // Faz o conteúdo preencher o espaço disponível.
            formulario.Dock = DockStyle.Fill;

            aba.Controls.Add(formulario);

            // Exibe o formulário dentro da aba.
            formulario.Show();
            formulario.BringToFront();
        }
    }
}