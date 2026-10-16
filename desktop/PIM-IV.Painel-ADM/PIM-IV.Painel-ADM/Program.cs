
namespace PIM_IV.Painel_ADM
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Inicializa as configuracoes do Windows Forms
            ApplicationConfiguration.Initialize();

            // Abre a tela de login
            Application.Run(new FrmLogin());
        }
    }
}
