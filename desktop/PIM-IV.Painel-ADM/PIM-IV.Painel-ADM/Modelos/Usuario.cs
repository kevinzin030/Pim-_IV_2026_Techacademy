using System;

namespace PIM_IV.Painel_ADM.Modelos
{
    // Representa um usuário cadastrado no sistema acadêmico.
    public class Usuario
    {
        // Identificador único do usuário.
        public int IdUsuario { get; set; }

        // Nome completo utilizado na identificação do usuário.
        public string NomeCompleto { get; set; } = string.Empty;

        // Identificador utilizado para acessar o sistema.
        public string Login { get; set; } = string.Empty;

        // Armazena o hash da senha, nunca a senha original.
        public string SenhaHash { get; set; } = string.Empty;

        // Identifica o perfil de acesso do usuário.
        public int IdPerfil { get; set; }

        // Indica se o usuário pode acessar o sistema.
        public bool Ativo { get; set; }

        // Registra a data de criação da conta.
        public DateTime DataCadastro { get; set; }
    }
}