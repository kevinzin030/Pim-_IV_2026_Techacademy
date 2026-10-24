using System;
using System.Windows.Forms;

namespace PIM_IV.Painel_ADM
{
    // Define o ponto de entrada da aplicação.
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Inicializa as configurações do Windows Forms.
            ApplicationConfiguration.Initialize();

            // Inicia a aplicação pela tela de login.
            IniciarAplicacao();
        }

        // Centraliza a inicialização da interface principal.
        private static void IniciarAplicacao()
        {
            Application.Run(new FrmLogin());
        }
    }
}