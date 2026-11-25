/**
 * Integração das disciplinas, aulas e questionários
 * do aluno com a API REST do TechAcademy.
 *
 * Os registros acadêmicos são consultados diretamente
 * no banco de dados Azure SQL por meio da API.
 */
(() => {
    "use strict";

    const CONFIGURACAO_API = {
        enderecoBase: "http://localhost:3000/api",
        chaveToken: "techacademy_token",
        endpointDisciplinas: "/alunos/me/disciplinas",
        endpointQuestionarios: "/alunos/me/quizzes",
        endpointAulas: "/alunos/me/aulas"
    };

    /**
     * Protege os dados apresentados nos templates HTML.
     *
     * @param {*} valor Conteúdo original.
     * @returns {string} Conteúdo protegido.
     */
    function escaparHtml(valor) {
        const elementoTemporario =
            document.createElement("div");

        elementoTemporario.textContent =
            String(valor ?? "");

        return elementoTemporario.innerHTML
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    }

    /**
     * Recupera o token JWT da sessão autenticada.
     *
     * @returns {string} Token de autenticação.
     */
    function obterTokenAutenticacao() {
        const token = sessionStorage.getItem(
            CONFIGURACAO_API.chaveToken
        );

        if (!token) {
            throw new Error(
                "Sessão não encontrada. Realize o login novamente."
            );
        }

        return token;
    }

    /**
     * Realiza uma consulta autenticada à API REST.
     *
     * @param {string} endpoint Rota da API.
     * @returns {Promise<Object>} Dados retornados.
     */
    async function consultarApi(endpoint) {
        const token = obterTokenAutenticacao();

        const enderecoRequisicao =
            CONFIGURACAO_API.enderecoBase + endpoint;

        const resposta = await fetch(
            enderecoRequisicao,
            {
                method: "GET",
                cache: "no-store",

                headers: {
                    Authorization: `Bearer ${token}`,
                    Accept: "application/json"
                }
            }
        );

        const dados = await resposta.json();

        if (!resposta.ok) {
            throw new Error(
                dados.mensagem ||
                "Não foi possível consultar os dados acadêmicos."
            );
        }

        return dados;
    }

    /**
     * Consulta as disciplinas vinculadas ao aluno.
     *
     * @returns {Promise<Array>} Lista de disciplinas.
     */
    async function consultarDisciplinasAluno() {
        const dados = await consultarApi(
            CONFIGURACAO_API.endpointDisciplinas
        );

        if (!Array.isArray(dados.disciplinas)) {
            throw new Error(
                "A API retornou uma lista de disciplinas inválida."
            );
        }

        return dados.disciplinas;
    }

    /**
     * Consulta os questionários disponíveis ao aluno.
     *
     * @returns {Promise<Array>} Lista de questionários.
     */
    async function consultarQuestionariosAluno() {
        const dados = await consultarApi(
            CONFIGURACAO_API.endpointQuestionarios
        );

        if (!Array.isArray(dados.quizzes)) {
            throw new Error(
                "A API retornou uma lista de questionários inválida."
            );
        }

        return dados.quizzes;
    }

    /**
     * Consulta as aulas vinculadas ao aluno.
     *
     * @returns {Promise<Array>} Lista de aulas.
     */
    async function consultarAulasAluno() {
        const dados = await consultarApi(
            CONFIGURACAO_API.endpointAulas
        );

        if (!Array.isArray(dados.aulas)) {
            throw new Error(
                "A API retornou uma lista de aulas inválida."
            );
        }

        return dados.aulas;
    }

    /**
     * Verifica se um registro pertence à disciplina
     * e à turma correspondente.
     *
     * @param {Object} registro Registro acadêmico.
     * @param {Object} disciplina Disciplina vinculada.
     * @returns {boolean} Resultado da comparação.
     */
    function pertenceDisciplina(registro, disciplina) {
        return (
            Number(registro.idDisciplina) ===
            Number(disciplina.idDisciplina) &&

            Number(registro.idTurma) ===
            Number(disciplina.idTurma)
        );
    }

    /**
     * Adapta um questionário retornado pela API
     * ao formato utilizado pelo painel.
     *
     * @param {Object} questionario Questionário da API.
     * @returns {Object} Questionário adaptado.
     */
    function converterQuestionario(questionario) {
        return {
            id: escaparHtml(questionario.idQuiz),

            titulo: escaparHtml(questionario.titulo),

            aulaId: questionario.idAula == null
                ? "geral"
                : String(questionario.idAula),

            xp: Number(questionario.xpMaximo ?? 0),

            respondido: Boolean(questionario.respondido),

            pontuacao: Number(questionario.xpGanho ?? 0),

            acertos: Number(questionario.acertos ?? 0),

            totalPerguntas: questionario.totalPerguntas,

            dataHora: questionario.dataCriacao
        };
    }

    /**
     * Filtra os questionários da disciplina e turma.
     *
     * @param {Object} disciplina Disciplina da API.
     * @param {Array} questionarios Questionários disponíveis.
     * @returns {Array} Questionários vinculados.
     */
    function filtrarQuestionarios(
        disciplina,
        questionarios
    ) {
        return questionarios
            .filter(questionario =>
                pertenceDisciplina(
                    questionario,
                    disciplina
                )
            )
            .map(converterQuestionario);
    }

    /**
     * Adapta uma aula retornada pela API
     * ao formato utilizado pelo painel existente.
     *
     * Os arquivos PDF são consultados posteriormente,
     * evitando carregar arquivos desnecessários.
     *
     * @param {Object} aula Aula retornada pela API.
     * @returns {Object} Aula adaptada.
     */
    function converterAula(aula) {
        return {
            id: String(aula.idAula),

            idTurmaDisciplina:
                Number(aula.idTurmaDisciplina),

            titulo: escaparHtml(
                aula.conteudo || "Aula sem conteúdo informado"
            ),

            data: aula.dataAula,

            realizada: Boolean(aula.realizada),

            totalMateriais: Number(
                aula.totalMateriais ?? 0
            )
        };
    }

    /**
     * Filtra as aulas vinculadas à disciplina e turma.
     *
     * Organiza as aulas em ordem cronológica.
     *
     * @param {Object} disciplina Disciplina da API.
     * @param {Array} aulas Aulas disponíveis.
     * @returns {Array} Aulas vinculadas.
     */
    function filtrarAulas(disciplina, aulas) {
        return aulas
            .filter(aula =>
                pertenceDisciplina(
                    aula,
                    disciplina
                )
            )
            .sort(
                (primeira, segunda) =>
                    new Date(primeira.dataAula) -
                    new Date(segunda.dataAula)
            )
            .map(converterAula);
    }

    /**
     * Adapta uma disciplina ao formato utilizado
     * pelo painel acadêmico existente.
     *
     * @param {Object} disciplina Disciplina da API.
     * @param {Array} questionarios Questionários disponíveis.
     * @param {Array} aulas Aulas disponíveis.
     * @returns {Object} Disciplina adaptada.
     */
    function converterDisciplinaParaPainel(
        disciplina,
        questionarios,
        aulas
    ) {
        return {
            id: String(disciplina.idDisciplina),

            nome: escaparHtml(
                disciplina.nomeDisciplina
            ),

            codigo: escaparHtml(
                disciplina.codigoDisciplina
            ),

            cargaHoraria: disciplina.cargaHoraria,

            idTurma: disciplina.idTurma,

            nomeTurma: escaparHtml(
                disciplina.nomeTurma
            ),

            aulas: filtrarAulas(
                disciplina,
                aulas
            ),

            questionarios: filtrarQuestionarios(
                disciplina,
                questionarios
            ),

            notas: {
                p1: null,
                p2: null,
                trabalho: null,
                quizzes: null
            }
        };
    }

    /**
     * Atualiza os cartões das disciplinas utilizando
     * o mecanismo de navegação existente.
     */
    function atualizarPainelDisciplinas() {
        const menuDisciplinas =
            document.getElementById(
                "nav-disciplinas"
            );

        if (menuDisciplinas) {
            menuDisciplinas.click();
        }
    }

    /**
     * Apresenta uma mensagem de erro no painel.
     *
     * @param {string} mensagem Descrição do erro.
     */
    function exibirErroConsulta(mensagem) {
        const areaConteudo =
            document.getElementById(
                "main-content-area"
            );

        if (areaConteudo) {
            areaConteudo.textContent = mensagem;
        }
    }

    /**
     * Carrega disciplinas, aulas e questionários reais.
     *
     * As consultas são executadas simultaneamente
     * para reduzir o tempo de carregamento.
     *
     * @returns {Promise<void>}
     */
    async function carregarDisciplinasReais() {
        estadoAluno.disciplinas = [];

        try {
            const [
                disciplinas,
                questionarios,
                aulas
            ] = await Promise.all([
                consultarDisciplinasAluno(),
                consultarQuestionariosAluno(),
                consultarAulasAluno()
            ]);

            // Associa os registros acadêmicos reais.
            estadoAluno.disciplinas = disciplinas.map(
                disciplina =>
                    converterDisciplinaParaPainel(
                        disciplina,
                        questionarios,
                        aulas
                    )
            );

            atualizarPainelDisciplinas();

            console.info(
                "TechAcademy: disciplinas carregadas.",
                disciplinas.length
            );

            console.info(
                "TechAcademy: questionários carregados.",
                questionarios.length
            );

            console.info(
                "TechAcademy: aulas carregadas.",
                aulas.length
            );

            // Exibe os vínculos para conferência.
            console.table(
                estadoAluno.disciplinas.map(
                    disciplina => ({
                        id: disciplina.id,
                        turma: disciplina.idTurma,
                        nome: disciplina.nome,
                        aulas: disciplina.aulas.length,
                        quizzes: disciplina.questionarios.length
                    })
                )
            );

        } catch (erro) {
            console.error(
                "Erro ao carregar os dados acadêmicos:",
                erro
            );

            exibirErroConsulta(erro.message);
        }
    }

    /**
     * Impede que registros locais sobrescrevam
     * os dados retornados pela API.
     */
    function desativarCarregamentoLocal() {
        carregarDadosCompartilhados = function () {
            // Os registros acadêmicos são consultados
            // diretamente pela API REST.
        };
    }

    /**
     * Inicializa a integração acadêmica do aluno.
     */
    function inicializarIntegracaoDisciplinas() {
        desativarCarregamentoLocal();

        carregarDisciplinasReais();
    }

    // Aguarda o carregamento do documento.
    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            inicializarIntegracaoDisciplinas
        );

    } else {
        inicializarIntegracaoDisciplinas();
    }
})();