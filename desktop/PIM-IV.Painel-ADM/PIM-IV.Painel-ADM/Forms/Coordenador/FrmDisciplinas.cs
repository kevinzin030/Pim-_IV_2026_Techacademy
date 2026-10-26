using PIM_IV.Painel_ADM.Modelos;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM.Forms
{
    // Organiza a navegação entre gerenciamento e vinculação de disciplinas.
    public class FrmDisciplinas : Form
    {
        private const string TituloFormulario = "TechAcademy - Disciplinas";

        private readonly Usuario _usuario;

        private readonly Color _corAzul = Color.FromArgb(40, 92, 170);
        private readonly Color _corAzulClaro = Color.FromArgb(225, 233, 244);
        private readonly Color _corTexto = Color.FromArgb(24, 39, 65);
        private readonly Color _corFundo = Color.FromArgb(244, 247, 251);

        private readonly TabControl _abas = new();
        private readonly TabPage _abaGerenciar = new();
        private readonly TabPage _abaVincular = new();

        private int _indiceAbaHover = -1;

        private FrmGerenciarDisciplinas? _gerenciamento;
        private FrmVincularDisciplinas? _vinculacao;

        // Inicializa o módulo com o usuário autenticado.
        public FrmDisciplinas(Usuario usuario)
        {
            _usuario = usuario
                ?? throw new ArgumentNullException(nameof(usuario));

            ConfigurarFormulario();
            ConfigurarAbas();
            ConfigurarEventos();
        }

        // Define as propriedades visuais do formulário.
        private void ConfigurarFormulario()
        {
            Text = TituloFormulario;

            Size = new Size(1150, 800);
            MinimumSize = new Size(650, 450);

            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;

            BackColor = _corFundo;
            Font = new Font("Segoe UI", 10);
        }

        // Organiza as abas de gerenciamento e vinculação.
        private void ConfigurarAbas()
        {
            _abas.Dock = DockStyle.Fill;
            _abas.DrawMode = TabDrawMode.OwnerDrawFixed;
            _abas.SizeMode = TabSizeMode.Fixed;
            _abas.ItemSize = new Size(240, 46);

            _abas.Font = new Font(
                "Segoe UI",
                12,
                FontStyle.Bold);

            ConfigurarPagina(
                _abaGerenciar,
                "Gerenciar disciplinas");

            ConfigurarPagina(
                _abaVincular,
                "Vincular às turmas");

            _abas.TabPages.AddRange(new[]
            {
                _abaGerenciar,
                _abaVincular
            });

            Controls.Add(_abas);
        }

        // Padroniza a aparência das páginas.
        private void ConfigurarPagina(
            TabPage pagina,
            string titulo)
        {
            pagina.Text = titulo;
            pagina.BackColor = _corFundo;
        }

        // Associa os eventos aos métodos responsáveis.
        private void ConfigurarEventos()
        {
            _abas.DrawItem += DesenharAba;
            _abas.MouseMove += AoMoverMouseSobreAbas;
            _abas.MouseLeave += AoSairMouseDasAbas;

            _abas.SelectedIndexChanged += AoSelecionarAba;

            Shown += (_, _) => CarregarGerenciamento();
        }

        // Atualiza o destaque quando o mouse passa sobre uma aba.
        private void AoMoverMouseSobreAbas(object? sender, MouseEventArgs e)
        {
            int indice = -1;

            for (int i = 0; i < _abas.TabCount; i++)
            {
                if (_abas.GetTabRect(i).Contains(e.Location))
                {
                    indice = i;
                    break;
                }
            }

            _abas.Cursor = indice >= 0 ? Cursors.Hand : Cursors.Default;

            if (_indiceAbaHover != indice)
            {
                _indiceAbaHover = indice;
                _abas.Invalidate();
            }
        }

        // Remove o destaque ao retirar o mouse das abas.
        private void AoSairMouseDasAbas(object? sender, EventArgs e)
        {
            _indiceAbaHover = -1;
            _abas.Cursor = Cursors.Default;
            _abas.Invalidate();
        }

        // Identifica qual funcionalidade deve ser exibida.
        private void AoSelecionarAba(
            object? sender,
            EventArgs e)
        {
            if (_abas.SelectedTab == _abaGerenciar)
            {
                CarregarGerenciamento();
                return;
            }

            if (_abas.SelectedTab == _abaVincular)
            {
                CarregarVinculacao();
            }
        }

        // Desenha as abas com a identidade visual do sistema.
        private void DesenharAba(
            object? sender,
            DrawItemEventArgs e)
        {
            if (e.Index < 0 ||
                e.Index >= _abas.TabPages.Count)
            {
                return;
            }

            bool selecionada =
                e.Index == _abas.SelectedIndex;

            Rectangle area = _abas.GetTabRect(e.Index);

            Color corFundo = selecionada
                ? _corAzul
                : e.Index == _indiceAbaHover
                    ? Color.FromArgb(178, 205, 238)
                    : _corAzulClaro;

            Color corTexto = selecionada
                ? Color.White
                : _corTexto;

            DesenharFundoAba(
                e.Graphics,
                area,
                corFundo);

            DesenharTextoAba(
                e.Graphics,
                e.Index,
                area,
                corTexto);

            if (selecionada)
            {
                DesenharIndicadorAba(
                    e.Graphics,
                    area);
            }
        }

        // Preenche o fundo da aba.
        private static void DesenharFundoAba(
            Graphics grafico,
            Rectangle area,
            Color cor)
        {
            using SolidBrush pincel = new(cor);

            grafico.FillRectangle(pincel, area);
        }

        // Centraliza o texto dentro da aba.
        private void DesenharTextoAba(
            Graphics grafico,
            int indice,
            Rectangle area,
            Color corTexto)
        {
            Rectangle areaTexto =
                Rectangle.Inflate(area, -10, -4);

            TextRenderer.DrawText(
                grafico,
                _abas.TabPages[indice].Text,
                _abas.Font,
                areaTexto,
                corTexto,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis);
        }

        // Destaca visualmente a aba selecionada.
        private void DesenharIndicadorAba(
            Graphics grafico,
            Rectangle area)
        {
            using Pen linha = new(_corTexto, 3);

            grafico.DrawLine(
                linha,
                area.Left + 2,
                area.Bottom - 2,
                area.Right - 3,
                area.Bottom - 2);
        }

        // Carrega o gerenciamento apenas quando necessário.
        private void CarregarGerenciamento()
        {
            if (FormularioEstaDisponivel(_gerenciamento))
            {
                return;
            }

            _gerenciamento =
                new FrmGerenciarDisciplinas(_usuario);

            IncorporarFormulario(
                _gerenciamento,
                _abaGerenciar);
        }

        // Carrega a vinculação apenas quando necessário.
        private void CarregarVinculacao()
        {
            if (FormularioEstaDisponivel(_vinculacao))
            {
                return;
            }

            _vinculacao =
                new FrmVincularDisciplinas(_usuario);

            IncorporarFormulario(
                _vinculacao,
                _abaVincular);
        }

        // Verifica se o formulário já foi criado e continua ativo.
        private static bool FormularioEstaDisponivel(
            Form? formulario)
        {
            return formulario != null &&
                   !formulario.IsDisposed;
        }

        // Exibe um formulário dentro da aba correspondente.
        private static void IncorporarFormulario(
            Form formulario,
            TabPage aba)
        {
            formulario.TopLevel = false;
            formulario.FormBorderStyle = FormBorderStyle.None;
            formulario.Dock = DockStyle.Fill;

            aba.Controls.Add(formulario);

            formulario.Show();
            formulario.BringToFront();
        }
    }
}