using System;

namespace PIM_IV.Painel_ADM.Modelos
{
    /// <summary>
    /// Representa um usuário cadastrado no sistema acadêmico.
    /// </summary>
    public class Usuario
    {
        // Identificador único do usuário.
        public int IdUsuario { get; set; }

        // Nome completo utilizado na identificação do usuário.
        public string NomeCompleto { get; set; } = string.Empty;

        // Identificador utilizado para acessar o sistema.
        public string Login { get; set; } = string.Empty;

        // Mantido temporariamente para compatibilidade com os serviços antigos.
        public string SenhaHash { get; set; } = string.Empty;

        // Identifica o perfil de acesso do usuário.
        public int IdPerfil { get; set; }

        // Indica se o usuário pode acessar o sistema.
        public bool Ativo { get; set; }

        // Indica se a alteração da senha é obrigatória.
        public bool DeveAlterarSenha { get; set; }

        // Registra a data de criação da conta.
        public DateTime DataCadastro { get; set; }
    }
}