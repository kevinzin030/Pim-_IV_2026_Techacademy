
using System;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM
{
    public partial class FrmPainelPrincipal
    {
        private Form? _formularioCentral;

        // Exibe um formulário dentro da área principal.
        private void ExibirFormularioCentral(Form formulario)
        {
            if (_formularioCentral != null)
            {
                _formularioCentral.Close();
                _formularioCentral.Dispose();
                _formularioCentral = null;
            }

            pnlConteudo.Controls.Clear();

            formulario.TopLevel = false;
            formulario.FormBorderStyle = FormBorderStyle.None;
            formulario.Dock = DockStyle.Fill;

            pnlConteudo.Controls.Add(formulario);

            _formularioCentral = formulario;

            formulario.Show();
            formulario.BringToFront();
        }
    }
}
