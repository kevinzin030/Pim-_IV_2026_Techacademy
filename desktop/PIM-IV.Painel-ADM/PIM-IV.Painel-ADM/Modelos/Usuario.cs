
namespace PIM_IV.Painel_ADM.Modelos
{
    public class Usuario
    {
        // Identificador unico do usuario
        public int IdUsuario { get; set; }

        // Nome completo do usuario
        public string NomeCompleto { get; set; } = string.Empty;

        // Login utilizado para acessar o sistema
        public string Login { get; set; } = string.Empty;

        // Armazena o hash da senha
        public string SenhaHash { get; set; } = string.Empty;

        // Identifica o perfil do usuario
        public int IdPerfil { get; set; }

        // Indica se a conta esta ativa
        public bool Ativo { get; set; }

        // Data em que o usuario foi cadastrado
        public DateTime DataCadastro { get; set; }
    }
}
