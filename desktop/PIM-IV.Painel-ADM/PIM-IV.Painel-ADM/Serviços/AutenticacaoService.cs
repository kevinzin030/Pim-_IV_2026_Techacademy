using System;
using PIM_IV.Painel_ADM.Modelos;
using PIM_IV.Painel_ADM.Repositorios;
using PIM_IV.Painel_ADM.Seguranca;

namespace PIM_IV.Painel_ADM.Servicos
{
    // Responsável por autenticar usuários no painel desktop.
    public class AutenticacaoService
    {
        private readonly UsuarioRepository _usuarioRepository;

        // Inicializa o serviço com o repositório padrão.
        public AutenticacaoService()
            : this(new UsuarioRepository())
        {
        }

        // Permite fornecer um repositório ao serviço.
        public AutenticacaoService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository
                ?? throw new ArgumentNullException(
                    nameof(usuarioRepository));
        }

        // Valida as credenciais e retorna o usuário autenticado.
        public Usuario? Autenticar(string login, string senha)
        {
            if (!CredenciaisPreenchidas(login, senha))
            {
                return null;
            }

            // Consulta o usuário autorizado a acessar o desktop.
            Usuario? usuario = BuscarUsuario(login);

            if (!UsuarioPodeAutenticar(usuario))
            {
                return null;
            }

            // Compara a senha informada com o hash armazenado.
            if (!SenhaCorreta(senha, usuario!.SenhaHash))
            {
                return null;
            }

            // Impede que o hash permaneça no objeto da sessão.
            RemoverDadosSensiveis(usuario);

            return usuario;
        }

        // Verifica se o login e a senha foram informados.
        private static bool CredenciaisPreenchidas(
            string login,
            string senha)
        {
            return !string.IsNullOrWhiteSpace(login) &&
                   !string.IsNullOrEmpty(senha);
        }

        // Busca o usuário no repositório.
        private Usuario? BuscarUsuario(string login)
        {
            return _usuarioRepository.BuscarPorLogin(login);
        }

        // Bloqueia usuários inexistentes ou inativos.
        private static bool UsuarioPodeAutenticar(Usuario? usuario)
        {
            return usuario != null && usuario.Ativo;
        }

        // Verifica a senha utilizando o mecanismo de segurança.
        private static bool SenhaCorreta(
            string senha,
            string senhaHash)
        {
            return SenhaHelper.VerificarSenha(
                senha,
                senhaHash);
        }

        // Remove informações sensíveis do objeto autenticado.
        private static void RemoverDadosSensiveis(Usuario usuario)
        {
            usuario.SenhaHash = string.Empty;
        }
    }
}