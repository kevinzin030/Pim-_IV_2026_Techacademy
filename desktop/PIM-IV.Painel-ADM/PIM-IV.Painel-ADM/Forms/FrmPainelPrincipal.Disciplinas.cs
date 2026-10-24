using System;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Forms;

namespace PIM_IV.Painel_ADM
{
    // Gerencia a navegação e o acesso aos módulos acadêmicos.
    public partial class FrmPainelPrincipal
    {
        private const int PerfilCoordenadorAcademico = 2;

        private const string PermissaoDisciplinas =
            "GERENCIAR_DISCIPLINAS";

        private const string PermissaoMatriculas =
            "GERENCIAR_MATRICULAS";

        private const string PermissaoProfessores =
            "ALOCAR_PROFESSORES";

        // Direciona o gerenciamento para o módulo unificado.
        private void AbrirGerenciamentoDisciplinas()
        {
            AbrirDisciplinasUnificadas();
        }

        // Direciona a vinculação para o mesmo módulo de disciplinas.
        private void AbrirVinculacaoDisciplinas()
        {
            AbrirDisciplinasUnificadas();
        }

        // Abre o gerenciamento e a vinculação de disciplinas.
        private void AbrirDisciplinasUnificadas()
        {
            AbrirModuloCoordenador(
                "Disciplinas",
                PermissaoDisciplinas,
                "O módulo de disciplinas é exclusivo do Coordenador.",
                "módulo de disciplinas",
                () => new FrmDisciplinas(_usuario));
        }

        // Abre o gerenciamento de matrículas dos alunos.
        private void AbrirGerenciamentoMatriculas()
        {
            AbrirModuloCoordenador(
                "Matrículas",
                PermissaoMatriculas,
                "O gerenciamento de matrículas é exclusivo do Coordenador.",
                "gerenciamento de matrículas",
                () => new FrmGerenciarMatriculas(_usuario));
        }

        // Abre a funcionalidade de alocação de professores.
        private void AbrirAlocacaoProfessores()
        {
            AbrirModuloCoordenador(
                "Professores",
                PermissaoProfessores,
                "A alocação de professores é exclusiva do Coordenador.",
                "alocação de professores",
                () => new FrmAlocarProfessores(_usuario));
        }

        // Centraliza a autorização e a abertura dos módulos acadêmicos.
        private void AbrirModuloCoordenador(
            string titulo,
            string permissao,
            string mensagemAcessoNegado,
            string descricaoModulo,
            Func<Form> criarFormulario)
        {
            if (!ValidarPerfilCoordenador(mensagemAcessoNegado))
            {
                return;
            }

            if (!Autorizar(permissao))
            {
                return;
            }

            try
            {
                Form formulario = criarFormulario();

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = titulo;
            }
            catch (Exception ex)
            {
                ExibirErroModuloAcademico(
                    descricaoModulo,
                    ex);
            }
        }

        // Impede o acesso de perfis diferentes do Coordenador.
        private bool ValidarPerfilCoordenador(
            string mensagemAcessoNegado)
        {
            if (_usuario.IdPerfil == PerfilCoordenadorAcademico)
            {
                return true;
            }

            MessageBox.Show(
                mensagemAcessoNegado,
                "Acesso negado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        // Direciona cada opção para sua funcionalidade correspondente.
        private void AbrirModuloAcademico(
            string nomeModulo,
            string permissao)
        {
            switch (nomeModulo)
            {
                case "Disciplinas":
                case "Vincular Disciplinas":
                    AbrirDisciplinasUnificadas();
                    break;

                case "Matrículas":
                    AbrirGerenciamentoMatriculas();
                    break;

                case "Professores":
                    AbrirAlocacaoProfessores();
                    break;

                case "Calendário":
                    InformarCalendarioIndisponivel();
                    break;

                default:
                    MostrarModulo(nomeModulo, permissao);
                    break;
            }
        }

        // Informa que o calendário não faz mais parte do projeto.
        private static void InformarCalendarioIndisponivel()
        {
            MessageBox.Show(
                "O Calendário Acadêmico foi retirado do projeto.",
                "Módulo indisponível",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Padroniza as mensagens de erro dos módulos acadêmicos.
        private static void ExibirErroModuloAcademico(
            string descricaoModulo,
            Exception erro)
        {
            MessageBox.Show(
                $"Não foi possível abrir o {descricaoModulo}.\n" +
                erro.Message,
                "Erro ao abrir módulo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}