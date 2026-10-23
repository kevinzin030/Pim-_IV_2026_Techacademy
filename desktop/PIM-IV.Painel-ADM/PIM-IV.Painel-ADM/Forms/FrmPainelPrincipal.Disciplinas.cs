
using System;
using System.Windows.Forms;
using PIM_IV.Painel_ADM.Forms;

namespace PIM_IV.Painel_ADM
{
    public partial class FrmPainelPrincipal
    {
        // =====================================================
        // DISCIPLINAS - MÓDULO UNIFICADO
        // =====================================================

        private void AbrirGerenciamentoDisciplinas()
        {
            AbrirDisciplinasUnificadas();
        }

        private void AbrirVinculacaoDisciplinas()
        {
            AbrirDisciplinasUnificadas();
        }

        private void AbrirDisciplinasUnificadas()
        {
            if (_usuario.IdPerfil != 2)
            {
                MessageBox.Show(
                    "O módulo de disciplinas é exclusivo do Coordenador.",
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Autorizar("GERENCIAR_DISCIPLINAS"))
                return;

            try
            {
                FrmDisciplinas formulario =
                    new FrmDisciplinas(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Disciplinas";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir o módulo de disciplinas.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // GERENCIAMENTO DE MATRÍCULAS
        // =====================================================

        private void AbrirGerenciamentoMatriculas()
        {
            if (_usuario.IdPerfil != 2)
            {
                MessageBox.Show(
                    "O gerenciamento de matrículas é exclusivo do Coordenador.",
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Autorizar("GERENCIAR_MATRICULAS"))
                return;

            try
            {
                FrmGerenciarMatriculas formulario =
                    new FrmGerenciarMatriculas(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Matrículas";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir o gerenciamento de matrículas.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // ALOCAÇÃO DE PROFESSORES
        // =====================================================

        private void AbrirAlocacaoProfessores()
        {
            if (_usuario.IdPerfil != 2)
            {
                MessageBox.Show(
                    "A alocação de professores é exclusiva do Coordenador.",
                    "Acesso negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!Autorizar("ALOCAR_PROFESSORES"))
                return;

            try
            {
                FrmAlocarProfessores formulario =
                    new FrmAlocarProfessores(_usuario);

                ExibirFormularioCentral(formulario);

                lblTituloPagina.Text = "Professores";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir a alocação de professores.\n" +
                    ex.Message,
                    "Erro ao abrir módulo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =====================================================
        // DIRECIONAMENTO DOS MÓDULOS ACADÊMICOS
        // =====================================================

        private void AbrirModuloAcademico(
            string nomeModulo,
            string permissao)
        {
            switch (nomeModulo)
            {
                case "Disciplinas":
                case "Vincular Disciplinas":
                    AbrirDisciplinasUnificadas();
                    return;

                case "Matrículas":
                    AbrirGerenciamentoMatriculas();
                    return;

                case "Professores":
                    AbrirAlocacaoProfessores();
                    return;

                case "Calendário":
                    MessageBox.Show(
                        "O Calendário Acadêmico foi retirado do projeto.",
                        "Módulo indisponível",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;

                default:
                    MostrarModulo(nomeModulo, permissao);
                    return;
            }
        }
    }
}
