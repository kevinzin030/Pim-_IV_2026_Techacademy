using System;
using System.Data;
using PIM_IV.Painel_ADM.Repositorios;

namespace PIM_IV.Painel_ADM.Servicos
{
    // Aplica as regras de negócio relacionadas à auditoria.
    public class AuditoriaService
    {
        private const int TamanhoMaximoAcao = 100;
        private const int TamanhoMaximoDescricao = 500;
        private const int TamanhoMaximoPesquisa = 200;

        private readonly AuditoriaRepository _repositorio;

        // Inicializa o serviço com o repositório padrão.
        public AuditoriaService()
            : this(new AuditoriaRepository())
        {
        }

        // Permite fornecer um repositório ao serviço.
        public AuditoriaService(AuditoriaRepository repositorio)
        {
            _repositorio = repositorio
                ?? throw new ArgumentNullException(nameof(repositorio));
        }

        // Registra uma atividade após validar os dados.
        public void Registrar(
            int idUsuario,
            string acao,
            string descricao)
        {
            ValidarIdentificadorUsuario(idUsuario);
            ValidarDadosAuditoria(acao, descricao);

            _repositorio.Registrar(
                idUsuario,
                acao,
                descricao);
        }

        // Preserva a consulta utilizada pelos módulos existentes.
        public DataTable ListarTodos()
        {
            return _repositorio.ListarTodos();
        }

        // Confirma a autorização do administrador no banco.
        public bool AdministradorEstaAutorizado(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return false;
            }

            return _repositorio.AdministradorEstaAutorizado(
                idUsuario);
        }

        // Pesquisa os registros após validar os filtros e o acesso.
        public DataTable Pesquisar(
            int idAdministrador,
            DateTime dataInicio,
            DateTime dataFim,
            string pesquisa)
        {
            ValidarAcessoAdministrador(idAdministrador);
            ValidarPeriodo(dataInicio, dataFim);

            string textoPesquisa = PrepararPesquisa(pesquisa);

            return _repositorio.Pesquisar(
                dataInicio,
                dataFim,
                textoPesquisa);
        }

        // Verifica se o identificador do usuário é válido.
        private static void ValidarIdentificadorUsuario(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "O usuário responsável pela ação é inválido.",
                    nameof(idUsuario));
            }
        }

        // Valida os dados obrigatórios do registro de auditoria.
        private static void ValidarDadosAuditoria(
            string acao,
            string descricao)
        {
            if (string.IsNullOrWhiteSpace(acao) ||
                string.IsNullOrWhiteSpace(descricao))
            {
                throw new ArgumentException(
                    "A ação e a descrição são obrigatórias.");
            }

            if (acao.Length > TamanhoMaximoAcao ||
                descricao.Length > TamanhoMaximoDescricao)
            {
                throw new ArgumentException(
                    "Os dados da auditoria excedem " +
                    "o tamanho permitido.");
            }
        }

        // Confirma se o usuário possui autorização administrativa.
        private void ValidarAcessoAdministrador(int idAdministrador)
        {
            if (!AdministradorEstaAutorizado(idAdministrador))
            {
                throw new UnauthorizedAccessException(
                    "Somente administradores ativos " +
                    "podem consultar a auditoria.");
            }
        }

        // Verifica se o intervalo de datas é válido.
        private static void ValidarPeriodo(
            DateTime dataInicio,
            DateTime dataFim)
        {
            if (dataInicio.Date > dataFim.Date)
            {
                throw new ArgumentException(
                    "A data inicial não pode ser maior que a final.");
            }

            // A consulta utiliza o dia seguinte como limite exclusivo.
            if (dataFim.Date == DateTime.MaxValue.Date)
            {
                throw new ArgumentException(
                    "A data final informada é inválida.");
            }
        }

        // Normaliza e valida o texto utilizado na pesquisa.
        private static string PrepararPesquisa(string pesquisa)
        {
            string texto = (pesquisa ?? string.Empty).Trim();

            if (texto.Length > TamanhoMaximoPesquisa)
            {
                throw new ArgumentException(
                    "A pesquisa deve conter no máximo " +
                    "200 caracteres.");
            }

            return texto;
        }
    }
}