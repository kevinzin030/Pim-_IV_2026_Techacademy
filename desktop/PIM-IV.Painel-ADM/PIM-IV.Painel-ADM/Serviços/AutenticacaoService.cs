
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    public class AutenticacaoService
    {
        private readonly UsuarioRepository _usuarioRepository;

        public AutenticacaoService()
        {
            _usuarioRepository = new UsuarioRepository();
        }

        // Valida o login e a senha do usuario
        public Usuario? Autenticar(string login, string senha)
        {
            // Verifica se os campos foram preenchidos
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrEmpty(senha))
            {
                return null;
            }

            // Busca um usuario com perfil autorizado
            Usuario? usuario = _usuarioRepository.BuscarPorLogin(login);

            // Bloqueia usuarios inexistentes ou inativos
            if (usuario == null || !usuario.Ativo)
            {
                return null;
            }

            // Verifica a senha armazenada como hash
            if (!SenhaHelper.VerificarSenha(senha, usuario.SenhaHash))
            {
                return null;
            }

            // Remove o hash do objeto retornado
            usuario.SenhaHash = string.Empty;

            return usuario;
        }
    }
}
