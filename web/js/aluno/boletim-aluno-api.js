/**
 * TechAcademy Web
 * Integração do boletim acadêmico do aluno.
 *
 * Responsável pela consulta de disciplinas,
 * avaliações, questionários e frequências.
 *
 * Os registros acadêmicos são obtidos exclusivamente
 * pela API REST, sem utilização do localStorage.
 */
(() => {
    "use strict";

    const CONFIGURACAO = {
        enderecoApi: "http://localhost:3000/api",
        chaveToken: "techacademy_token",
        limiteFaltas: 15,
        alertaFaltas: 10,

        pesos: {
            p1: 0.30,
            p2: 0.30,
            trabalho: 0.25,
            quizzes: 0.15
        }
    };

    const estado = {
        requisicao: 0
    };

    /**
     * Protege os dados apresentados na interface.
     *
     * @param {*} valor Conteúdo original.
     * @returns {string} Conteúdo protegido.
     */
    function escaparHTML(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    }

    /**
     * Consulta um endpoint autenticado da API.
     *
     * @param {string} endpoint Caminho da consulta.
     * @returns {Promise<Object>} Dados retornados.
     */
    async function consultarApi(endpoint) {
        const token = sessionStorage.getItem(
            CONFIGURACAO.chaveToken
        );

        if (!token) {
            window.location.replace("../../index.html");

            throw new Error(
                "Sessão não encontrada."
            );
        }

        const resposta = await fetch(
            `${CONFIGURACAO.enderecoApi}${endpoint}`,
            {
                method: "GET",
                cache: "no-store",

                headers: {
                    Authorization: `Bearer ${token}`
                }
            }
        );

        const resultado = await resposta.json()
            .catch(() => ({}));

        // Encerra a sessão quando o token é inválido.
        if (resposta.status === 401) {
            sessionStorage.removeItem(
                CONFIGURACAO.chaveToken
            );

            sessionStorage.removeItem(
                "usuario_logado"
            );

            window.location.replace("../../index.html");
        }

        if (!resposta.ok) {
            throw new Error(
                resultado.mensagem ||
                "Não foi possível consultar os dados acadêmicos."
            );
        }

        return resultado;
    }

    /**
     * Consulta os dados necessários para o boletim.
     *
     * As requisições são executadas paralelamente
     * para reduzir o tempo de carregamento.
     *
     * @returns {Promise<Object>} Dados acadêmicos.
     */
    async function consultarDadosAcademicos() {
        const [
            resultadoDisciplinas,
            resultadoNotas,
            resultadoFrequencias,
            resultadoQuizzes
        ] = await Promise.all([
            consultarApi("/alunos/me/disciplinas"),
            consultarApi("/alunos/me/notas"),
            consultarApi("/alunos/me/frequencias"),
            consultarApi("/alunos/me/quizzes")
        ]);

        const dados = {
            disciplinas: resultadoDisciplinas.disciplinas,
            notas: resultadoNotas.notas,
            frequencias: resultadoFrequencias.frequencias,
            quizzes: resultadoQuizzes.quizzes
        };

        // Valida a estrutura recebida do servidor.
        for (const registros of Object.values(dados)) {
            if (!Array.isArray(registros)) {
                throw new Error(
                    "A API retornou dados acadêmicos inválidos."
                );
            }
        }

        return dados;
    }

    /**
     * Verifica se um registro pertence à disciplina
     * e à turma consultadas.
     *
     * @param {Object} registro Registro acadêmico.
     * @param {Object} disciplina Disciplina vinculada.
     * @returns {boolean} Resultado da comparação.
     */
    function pertenceDisciplina(
        registro,
        disciplina
    ) {
        return (
            Number(registro.idDisciplina) ===
            Number(disciplina.idDisciplina) &&

            Number(registro.idTurma) ===
            Number(disciplina.idTurma)
        );
    }

    /**
     * Normaliza o título de uma avaliação.
     *
     * @param {string} titulo Título original.
     * @returns {string} Título normalizado.
     */
    function normalizarTitulo(titulo) {
        return String(titulo ?? "")
            .trim()
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "");
    }

    /**
     * Converte a nota para uma escala de zero a dez.
     *
     * Notas não lançadas permanecem nulas.
     *
     * @param {Object} avaliacao Avaliação registrada.
     * @returns {number|null} Nota normalizada.
     */
    function normalizarNota(avaliacao) {
        if (
            !avaliacao ||
            avaliacao.valorNota == null ||
            avaliacao.valorMaximo == null
        ) {
            return null;
        }

        const valorNota = Number(
            avaliacao.valorNota
        );

        const valorMaximo = Number(
            avaliacao.valorMaximo
        );

        if (
            !Number.isFinite(valorNota) ||
            !Number.isFinite(valorMaximo) ||
            valorMaximo <= 0 ||
            valorNota < 0 ||
            valorNota > valorMaximo
        ) {
            return null;
        }

        return (
            valorNota / valorMaximo
        ) * 10;
    }

    /**
     * Localiza a avaliação correspondente ao título.
     *
     * Caso existam vários registros com o mesmo
     * título, utiliza a avaliação mais recente.
     *
     * @param {Array} notas Avaliações da disciplina.
     * @param {string} titulo Título procurado.
     * @returns {number|null} Nota da avaliação.
     */
    function obterNotaAvaliacao(
        notas,
        titulo
    ) {
        const tituloNormalizado =
            normalizarTitulo(titulo);

        const avaliacoes = notas.filter(
            nota =>
                normalizarTitulo(nota.avaliacao) ===
                tituloNormalizado
        );

        avaliacoes.sort(
            (primeira, segunda) =>
                new Date(segunda.dataAvaliacao) -
                new Date(primeira.dataAvaliacao)
        );

        return normalizarNota(
            avaliacoes[0]
        );
    }

    /**
     * Calcula a nota dos questionários da disciplina.
     *
     * A nota é calculada pelo total de acertos
     * dividido pelo total de perguntas respondidas.
     *
     * O XP não interfere na nota acadêmica.
     *
     * @param {Array} quizzes Questionários vinculados.
     * @returns {number|null} Nota dos questionários.
     */
    function calcularNotaQuizzes(quizzes) {
        const concluidos = quizzes.filter(
            quiz => quiz.respondido
        );

        if (concluidos.length === 0) {
            return null;
        }

        let totalAcertos = 0;
        let totalPerguntas = 0;

        for (const quiz of concluidos) {
            const acertos = Number(
                quiz.acertos ?? 0
            );

            const perguntas = Number(
                quiz.totalPerguntas ?? 0
            );

            if (
                !Number.isInteger(acertos) ||
                !Number.isInteger(perguntas) ||
                perguntas <= 0 ||
                acertos < 0 ||
                acertos > perguntas
            ) {
                continue;
            }

            totalAcertos += acertos;
            totalPerguntas += perguntas;
        }

        if (totalPerguntas === 0) {
            return null;
        }

        return (
            totalAcertos / totalPerguntas
        ) * 10;
    }

    /**
     * Calcula a média ponderada da disciplina.
     *
     * P1: 30%
     * P2: 30%
     * Trabalho: 25%
     * Questionários: 15%
     *
     * A média somente é apresentada quando todas
     * as categorias possuem notas registradas.
     *
     * @param {Object} notas Notas acadêmicas.
     * @returns {number|null} Média ponderada.
     */
    function calcularMedia(notas) {
        const categorias = [
            notas.p1,
            notas.p2,
            notas.trabalho,
            notas.quizzes
        ];

        const possuiNotaPendente = categorias.some(
            nota =>
                nota == null ||
                !Number.isFinite(nota)
        );

        if (possuiNotaPendente) {
            return null;
        }

        const pesos = CONFIGURACAO.pesos;

        const media =
            notas.p1 * pesos.p1 +
            notas.p2 * pesos.p2 +
            notas.trabalho * pesos.trabalho +
            notas.quizzes * pesos.quizzes;

        return Number(
            media.toFixed(2)
        );
    }

    /**
     * Consolida os registros de frequência.
     *
     * A porcentagem considera somente as aulas
     * que possuem frequência registrada.
     *
     * @param {Array} frequencias Registros de frequência.
     * @returns {Object} Resumo de frequência.
     */
    function calcularFrequencia(frequencias) {
        const presencas = frequencias.filter(
            registro => registro.presente === true
        ).length;

        const faltas = frequencias.filter(
            registro => registro.presente === false
        ).length;

        const totalAulas =
            presencas + faltas;

        const percentual = totalAulas > 0
            ? (presencas / totalAulas) * 100
            : null;

        return {
            totalAulas,
            presencas,
            faltas,
            percentual
        };
    }

    /**
     * Organiza os registros acadêmicos por disciplina.
     *
     * Utiliza os identificadores oficiais da API
     * para evitar associações incorretas.
     *
     * @param {Object} dados Dados acadêmicos.
     * @returns {Array} Boletim consolidado.
     */
    function consolidarBoletim(dados) {
        return dados.disciplinas.map(
            disciplina => {
                const notasDisciplina =
                    dados.notas.filter(
                        nota =>
                            pertenceDisciplina(
                                nota,
                                disciplina
                            )
                    );

                const quizzesDisciplina =
                    dados.quizzes.filter(
                        quiz =>
                            pertenceDisciplina(
                                quiz,
                                disciplina
                            )
                    );

                const frequenciasDisciplina =
                    dados.frequencias.filter(
                        frequencia =>
                            pertenceDisciplina(
                                frequencia,
                                disciplina
                            )
                    );

                const notas = {
                    p1: obterNotaAvaliacao(
                        notasDisciplina,
                        "P1"
                    ),

                    p2: obterNotaAvaliacao(
                        notasDisciplina,
                        "P2"
                    ),

                    trabalho: obterNotaAvaliacao(
                        notasDisciplina,
                        "Trabalho"
                    ),

                    quizzes: calcularNotaQuizzes(
                        quizzesDisciplina
                    )
                };

                return {
                    idDisciplina:
                        disciplina.idDisciplina,

                    idTurma:
                        disciplina.idTurma,

                    nome:
                        disciplina.nomeDisciplina,

                    turma:
                        disciplina.nomeTurma,

                    notas,

                    media: calcularMedia(notas),

                    frequencia: calcularFrequencia(
                        frequenciasDisciplina
                    )
                };
            }
        );
    }

    /**
     * Determina a situação acadêmica pela média.
     *
     * Preserva os critérios do painel original.
     *
     * @param {number|null} media Média acadêmica.
     * @returns {Object} Situação e estilo visual.
     */
    function obterSituacaoAcademica(media) {
        if (media == null) {
            return {
                texto: "Pendente",

                classe:
                    "text-slate-600 bg-slate-100 " +
                    "border-slate-200"
            };
        }

        if (media < 5) {
            return {
                texto: "Reprovado",

                classe:
                    "text-red-700 bg-red-50 " +
                    "border-red-200"
            };
        }

        if (media < 7) {
            return {
                texto: "Exame",

                classe:
                    "text-amber-700 bg-amber-50 " +
                    "border-amber-200"
            };
        }

        return {
            texto: "Aprovado",

            classe:
                "text-emerald-700 bg-emerald-50 " +
                "border-emerald-200"
        };
    }

    /**
     * Formata uma nota para apresentação.
     *
     * O traço representa uma nota não lançada.
     *
     * @param {number|null} nota Nota acadêmica.
     * @returns {string} Nota formatada.
     */
    function formatarNota(nota) {
        if (nota == null) {
            return "—";
        }

        return nota.toLocaleString(
            "pt-BR",
            {
                minimumFractionDigits: 1,
                maximumFractionDigits: 2
            }
        );
    }

    /**
     * Renderiza uma linha do boletim acadêmico.
     *
     * @param {Object} registro Disciplina consolidada.
     * @returns {string} HTML da linha.
     */
    function renderizarLinhaBoletim(registro) {
        const notas = registro.notas;

        const situacao = obterSituacaoAcademica(
            registro.media
        );

        return `
            <tr class="hover:bg-slate-50 transition
                       border-b border-slate-100 text-xs">

                <td class="p-3 font-semibold
                           text-slate-800">

                    ${escaparHTML(registro.nome)}

                </td>

                <td class="p-3 font-medium">
                    ${formatarNota(notas.p1)}
                </td>

                <td class="p-3 font-medium">
                    ${formatarNota(notas.p2)}
                </td>

                <td class="p-3 font-medium">
                    ${formatarNota(notas.trabalho)}
                </td>

                <td class="p-3 font-medium">
                    ${formatarNota(notas.quizzes)}
                </td>

                <td class="p-3 text-center
                           font-extrabold text-brand-blue">

                    ${formatarNota(registro.media)}

                </td>

                <td class="p-3 text-center">

                    <span class="${situacao.classe}
                                 px-2.5 py-1 rounded-full
                                 text-[10px] font-bold
                                 border whitespace-nowrap">

                        ${situacao.texto}

                    </span>

                </td>

            </tr>
        `;
    }

    /**
     * Define a cor do indicador de faltas.
     *
     * @param {number} faltas Quantidade de faltas.
     * @returns {string} Classe visual.
     */
    function obterClasseFaltas(faltas) {
        if (faltas >= CONFIGURACAO.limiteFaltas) {
            return "text-red-600";
        }

        if (faltas >= CONFIGURACAO.alertaFaltas) {
            return "text-amber-600";
        }

        return "text-emerald-600";
    }

    /**
     * Renderiza o cartão de frequência da disciplina.
     *
     * Diferencia a ausência de registros de uma
     * disciplina que possui zero faltas.
     *
     * @param {Object} registro Disciplina consolidada.
     * @returns {string} HTML do cartão.
     */
    function renderizarCartaoFrequencia(registro) {
        const frequencia = registro.frequencia;

        const classeFaltas = obterClasseFaltas(
            frequencia.faltas
        );

        const possuiRegistros =
            frequencia.totalAulas > 0;

        const percentual = possuiRegistros
            ? frequencia.percentual.toLocaleString(
                "pt-BR",
                {
                    maximumFractionDigits: 1
                }
            ) + "%"
            : "—";

        const larguraBarra = possuiRegistros
            ? frequencia.percentual
            : 0;

        const classeBarra =
            frequencia.percentual >= 75
                ? "bg-emerald-500"
                : "bg-red-500";

        const descricao = possuiRegistros
            ? "Frequência nas aulas registradas"
            : "Nenhuma frequência registrada.";

        return `
            <article class="p-4 bg-slate-50
                            rounded-xl border
                            border-slate-200 space-y-3">

                <header class="flex justify-between
                               items-start gap-3">

                    <div>

                        <h4 class="text-sm font-bold
                                   text-slate-800">

                            ${escaparHTML(registro.nome)}

                        </h4>

                        <p class="text-[11px]
                                  text-slate-500 mt-1">

                            ${escaparHTML(registro.turma)}

                        </p>

                    </div>

                    <span class="${classeFaltas}
                                 text-sm font-bold
                                 whitespace-nowrap">

                        ${possuiRegistros
                ? frequencia.faltas + " Falta(s)"
                : "—"}

                    </span>

                </header>

                <div class="grid grid-cols-3 gap-2
                            text-center">

                    <div class="bg-white rounded-lg p-2">

                        <p class="text-[10px]
                                  text-slate-500">
                            Aulas
                        </p>

                        <p class="text-sm font-bold
                                  text-slate-800">

                            ${frequencia.totalAulas}

                        </p>

                    </div>

                    <div class="bg-white rounded-lg p-2">

                        <p class="text-[10px]
                                  text-slate-500">
                            Presenças
                        </p>

                        <p class="text-sm font-bold
                                  text-emerald-600">

                            ${frequencia.presencas}

                        </p>

                    </div>

                    <div class="bg-white rounded-lg p-2">

                        <p class="text-[10px]
                                  text-slate-500">
                            Frequência
                        </p>

                        <p class="text-sm font-bold
                                  text-brand-blue">

                            ${percentual}

                        </p>

                    </div>

                </div>

                <div class="h-2 w-full bg-slate-200
                            rounded-full overflow-hidden">

                    <div class="${classeBarra}
                                h-full rounded-full
                                transition-all"
                         style="width: ${larguraBarra}%">
                    </div>

                </div>

                <p class="text-[10px] text-slate-500">

                    ${descricao}

                </p>

                <p class="text-[10px] text-slate-500">

                    Limite de faltas:
                    ${CONFIGURACAO.limiteFaltas}

                </p>

            </article>
        `;
    }

    /**
     * Renderiza o alerta de frequência crítica.
     *
     * @param {Array} boletim Disciplinas consolidadas.
     * @returns {string} HTML do alerta.
     */
    function renderizarAlertaFaltas(boletim) {
        const possuiFaltasCriticas = boletim.some(
            registro =>
                registro.frequencia.faltas >=
                CONFIGURACAO.limiteFaltas
        );

        if (!possuiFaltasCriticas) {
            return "";
        }

        return `
            <aside class="bg-red-50 border-l-4
                          border-red-500 p-4 rounded-xl
                          flex items-start gap-3"
                   role="alert">

                <i class="fa-solid
                          fa-triangle-exclamation
                          text-red-500 text-xl">
                </i>

                <div>

                    <h4 class="text-sm font-bold
                               text-red-800">

                        Atenção: limite de faltas atingido!

                    </h4>

                    <p class="text-xs text-red-700 mt-1">

                        Você acumulou
                        ${CONFIGURACAO.limiteFaltas}
                        ou mais faltas em uma ou mais
                        disciplinas. Procure a coordenação.

                    </p>

                </div>

            </aside>
        `;
    }

    /**
     * Renderiza o extrato de faltas por disciplina.
     *
     * @param {Array} boletim Disciplinas consolidadas.
     * @returns {string} HTML da seção.
     */
    function renderizarExtratoFaltas(boletim) {
        const cartoes = boletim.map(
            registro =>
                renderizarCartaoFrequencia(registro)
        ).join("");

        return `
            <section class="bg-white p-6 rounded-2xl
                            border border-slate-100
                            shadow-sm">

                <h3 class="text-base font-bold
                           text-slate-800 mb-4
                           flex items-center gap-2">

                    <i class="fa-solid fa-clipboard-user
                              text-brand-blue">
                    </i>

                    Extrato de Faltas por Disciplina

                </h3>

                <div id="extrato-faltas-cards"
                     class="grid grid-cols-1
                            sm:grid-cols-2 gap-4">

                    ${cartoes}

                </div>

            </section>
        `;
    }

    /**
     * Renderiza a tabela de notas acadêmicas.
     *
     * @param {Array} boletim Disciplinas consolidadas.
     * @returns {string} HTML da tabela.
     */
    function renderizarTabelaBoletim(boletim) {
        const linhas = boletim.map(
            registro =>
                renderizarLinhaBoletim(registro)
        ).join("");

        return `
            <section class="bg-white p-6 rounded-2xl
                            border border-slate-100
                            shadow-sm">

                <h3 class="text-base font-bold
                           text-slate-800 mb-4
                           flex items-center gap-2">

                    <i class="fa-solid fa-graduation-cap
                              text-brand-blue">
                    </i>

                    Boletim Acadêmico

                </h3>

                <div class="overflow-x-auto rounded-xl
                            border border-slate-200">

                    <table class="w-full min-w-[700px]
                                  text-left border-collapse
                                  boletim-table">

                        <thead>

                            <tr class="bg-blue-50
                                       text-slate-600
                                       text-xs font-bold
                                       uppercase border-b
                                       border-blue-100">

                                <th scope="col" class="p-3">
                                    Disciplina
                                </th>

                                <th scope="col"
                                    class="p-3 w-20">
                                    P1
                                </th>

                                <th scope="col"
                                    class="p-3 w-20">
                                    P2
                                </th>

                                <th scope="col"
                                    class="p-3 w-24">
                                    Trabalho
                                </th>

                                <th scope="col"
                                    class="p-3 w-20">
                                    Quizzes
                                </th>

                                <th scope="col"
                                    class="p-3 w-20
                                           text-center">
                                    Média
                                </th>

                                <th scope="col"
                                    class="p-3 w-28
                                           text-center">
                                    Situação
                                </th>

                            </tr>

                        </thead>

                        <tbody>

                            ${linhas}

                        </tbody>

                    </table>

                </div>

                <p class="text-xs text-slate-500 mt-4">

                    Média ponderada:
                    P1 (30%), P2 (30%),
                    Trabalho (25%) e Quizzes (15%).

                </p>

                <p class="text-[11px]
                          text-slate-400 mt-2">

                    O símbolo — indica uma nota
                    ainda não registrada.

                </p>

            </section>
        `;
    }

    /**
     * Atualiza a identificação da página.
     */
    function atualizarCabecalho() {
        const titulo = document.getElementById(
            "page-title"
        );

        const subtitulo = document.getElementById(
            "page-subtitle"
        );

        if (titulo) {
            titulo.textContent =
                "Boletim Acadêmico";
        }

        if (subtitulo) {
            subtitulo.textContent =
                "Acompanhe suas notas e frequências.";
        }
    }

    /**
     * Destaca a opção de boletim no menu lateral.
     */
    function selecionarMenuBoletim() {
        const botoes = document.querySelectorAll(
            "#sidebar nav button"
        );

        for (const botao of botoes) {
            botao.classList.remove(
                "bg-brand-blue",
                "text-white",
                "shadow-md"
            );

            botao.classList.add(
                "text-slate-300"
            );
        }

        const botaoBoletim = document.getElementById(
            "nav-boletim"
        );

        if (!botaoBoletim) {
            return;
        }

        botaoBoletim.classList.remove(
            "text-slate-300"
        );

        botaoBoletim.classList.add(
            "bg-brand-blue",
            "text-white",
            "shadow-md"
        );
    }

    /**
     * Exibe uma mensagem de carregamento ou erro.
     *
     * @param {string} mensagem Texto apresentado.
     * @param {boolean} erro Indica falha na consulta.
     */
    function exibirMensagem(
        mensagem,
        erro = false
    ) {
        const area = document.getElementById(
            "main-content-area"
        );

        if (!area) {
            return;
        }

        const elemento = document.createElement(
            "div"
        );

        elemento.className = erro
            ? "p-5 rounded-xl bg-red-50 text-red-700"
            : "p-5 rounded-xl bg-white text-slate-500";

        elemento.textContent = mensagem;

        area.replaceChildren(elemento);
    }

    /**
     * Consulta e apresenta o boletim acadêmico.
     *
     * A interface é atualizada somente quando
     * a requisição ainda pertence à tela atual.
     *
     * @returns {Promise<void>}
     */
    async function carregarBoletim() {
        const requisicao = ++estado.requisicao;

        selecionarMenuBoletim();

        atualizarCabecalho();

        exibirMensagem(
            "Carregando notas e frequências..."
        );

        try {
            const dados =
                await consultarDadosAcademicos();

            // Impede que consultas antigas alterem
            // outras funcionalidades do painel.
            if (requisicao !== estado.requisicao) {
                return;
            }

            const boletim = consolidarBoletim(
                dados
            );

            if (boletim.length === 0) {
                exibirMensagem(
                    "Nenhuma disciplina vinculada ao aluno."
                );

                return;
            }

            const area = document.getElementById(
                "main-content-area"
            );

            if (!area) {
                return;
            }

            area.innerHTML = `
                <div class="space-y-6">

                    ${renderizarAlertaFaltas(boletim)}

                    ${renderizarExtratoFaltas(boletim)}

                    ${renderizarTabelaBoletim(boletim)}

                </div>
            `;

        } catch (erro) {
            if (requisicao !== estado.requisicao) {
                return;
            }

            console.error(
                "Erro ao carregar boletim:",
                erro
            );

            exibirMensagem(
                erro.message,
                true
            );
        }
    }

    /**
     * Substitui as funções antigas que utilizavam
     * registros acadêmicos armazenados localmente.
     */
    function desativarBoletimLocal() {
        if (typeof renderBoletim === "function") {
            renderBoletim = carregarBoletim;
        }

        /*
         * Desativa a verificação antiga de faltas.
         * Os alertas reais são apresentados pelo
         * novo boletim após a consulta à API.
         */
        if (
            typeof verificarAlertaFaltasCriticas ===
            "function"
        ) {
            verificarAlertaFaltasCriticas =
                function () { };
        }
    }

    /**
     * Controla a navegação para impedir que uma
     * consulta antiga sobrescreva outra página.
     */
    function configurarNavegacao() {
        document.addEventListener(
            "click",
            evento => {
                const alvo = evento.target;

                if (!(alvo instanceof Element)) {
                    return;
                }

                const menu = alvo.closest(
                    "#sidebar nav button"
                );

                if (!menu) {
                    return;
                }

                // Invalida consultas ao trocar de página.
                estado.requisicao++;

                if (menu.id !== "nav-boletim") {
                    return;
                }

                evento.preventDefault();

                evento.stopImmediatePropagation();

                carregarBoletim();
            },
            true
        );
    }

    /**
     * Inicializa a integração do boletim acadêmico.
     */
    function inicializar() {
        configurarNavegacao();

        console.info(
            "TechAcademy: integração do boletim carregada."
        );
    }

    /*
     * Substitui a lógica local antes da inicialização
     * do painel principal.
     */
    desativarBoletimLocal();

    // Aguarda o carregamento do documento.
    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            inicializar
        );

    } else {
        inicializar();
    }
})();