using System;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM
{
    // Gerencia a navegação entre os formulários do painel principal.
    public partial class FrmPainelPrincipal
    {
        private Form? _formularioCentral;

        // Exibe um formulário dentro da área de conteúdo do painel.
        private void ExibirFormularioCentral(Form formulario)
        {
            ArgumentNullException.ThrowIfNull(formulario);

            FecharFormularioCentral();

            // Remove os controles da página anterior.
            pnlConteudo.Controls.Clear();

            ConfigurarFormularioCentral(formulario);

            pnlConteudo.Controls.Add(formulario);

            _formularioCentral = formulario;

            formulario.Show();
            formulario.BringToFront();
        }

        // Configura o formulário para funcionar dentro do painel.
        private static void ConfigurarFormularioCentral(Form formulario)
        {
            formulario.TopLevel = false;
            formulario.FormBorderStyle = FormBorderStyle.None;
            formulario.Dock = DockStyle.Fill;
        }

        // Encerra o formulário anterior e libera seus recursos.
        private void FecharFormularioCentral()
        {
            if (_formularioCentral == null)
            {
                return;
            }

            _formularioCentral.Close();
            _formularioCentral.Dispose();

            _formularioCentral = null;
        }
    }
}