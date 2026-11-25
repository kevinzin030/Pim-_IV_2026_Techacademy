/**
 * TechAcademy - Métricas da Turma
 *
 * Apresenta indicadores acadêmicos consolidados
 * utilizando os dados reais da API REST.
 */

(() => {
    "use strict";

    const CONFIGURACAO = {
        api: "http://localhost:3000/api",
        chaveToken: "techacademy_token",

        pesos: {
            p1: 0.30,
            p2: 0.30,
            trabalho: 0.25,
            quizzes: 0.15
        }
    };

    const estado = {
        turmas: [],
        alunos: [],
        notas: [],
        frequencias: [],
        quizzes: [],
        desempenhos: [],

        idTurma: "",
        idTurmaDisciplina: "",

        requisicao: 0,
        telaAtiva: false
    };

    /**
     * Protege os textos inseridos no HTML.
     */
    function escaparHtml(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    }

    /**
     * Compara identificadores acadêmicos.
     */
    function mesmoId(primeiro, segundo) {
        return Number(primeiro) === Number(segundo);
    }

    /**
     * Consulta os registros autenticados da API.
     */
    async function consultarApi(endpoint, propriedade) {
        const token = sessionStorage.getItem(
            CONFIGURACAO.chaveToken
        );

        if (!token) {
            window.location.replace("index.html");

            throw new Error("Sessão não encontrada.");
        }

        const resposta = await fetch(
            `${CONFIGURACAO.api}/professores/me/${endpoint}`,
            {
                headers: {
                    Authorization: `Bearer ${token}`
                },

                cache: "no-store"
            }
        );

        if (resposta.status === 401) {
            sessionStorage.removeItem(
                CONFIGURACAO.chaveToken
            );

            sessionStorage.removeItem(
                "usuario_logado"
            );

            window.location.replace("index.html");

            throw new Error("Sessão expirada.");
        }

        const dados = await resposta.json();

        if (!resposta.ok) {
            throw new Error(
                dados.mensagem ||
                "Não foi possível consultar os dados."
            );
        }

        if (!Array.isArray(dados[propriedade])) {
            throw new Error(
                `Resposta inválida: ${propriedade}.`
            );
        }

        return dados[propriedade];
    }

    /**
     * Formata números no padrão brasileiro.
     */
    function formatarNumero(valor, casas = 2) {
        if (
            valor === null ||
            !Number.isFinite(valor)
        ) {
            return "—";
        }

        return valor.toLocaleString("pt-BR", {
            minimumFractionDigits: casas,
            maximumFractionDigits: casas
        });
    }

    /**
     * Calcula um percentual sem divisão por zero.
     */
    function calcularPercentual(valor, total) {
        return total > 0
            ? valor / total * 100
            : null;
    }

    /**
     * Formata valores percentuais.
     */
    function formatarPercentual(valor) {
        return valor === null
            ? "—"
            : `${formatarNumero(valor, 0)}%`;
    }

    /**
     * Normaliza os títulos das avaliações.
     */
    function normalizarTitulo(titulo) {
        return String(titulo ?? "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/\s+/g, "")
            .toLowerCase();
    }

    /**
     * Recupera as turmas vinculadas ao professor,
     * sem duplicar os registros.
     */
    function obterTurmas() {
        const turmas = new Map();

        estado.turmas.forEach(turma => {
            turmas.set(
                String(turma.idTurma),
                turma.nomeTurma
            );
        });

        return [...turmas.entries()].sort(
            (primeira, segunda) =>
                primeira[1].localeCompare(
                    segunda[1],
                    "pt-BR"
                )
        );
    }

    /**
     * Recupera as disciplinas da turma selecionada.
     */
    function obterDisciplinas() {
        return estado.turmas
            .filter(turma =>
                mesmoId(
                    turma.idTurma,
                    estado.idTurma
                )
            )
            .sort(
                (primeira, segunda) =>
                    primeira.nomeDisciplina.localeCompare(
                        segunda.nomeDisciplina,
                        "pt-BR"
                    )
            );
    }

    /**
     * Verifica se o registro pertence à disciplina
     * vinculada à turma selecionada.
     */
    function pertenceDisciplina(registro) {
        return mesmoId(
            registro.idTurmaDisciplina,
            estado.idTurmaDisciplina
        );
    }

    /**
     * Recupera os alunos ativos da turma.
     */
    function obterAlunos() {
        const alunos = new Map();

        estado.alunos
            .filter(aluno =>
                mesmoId(
                    aluno.idTurma,
                    estado.idTurma
                ) &&
                aluno.situacaoMatricula === "Ativa"
            )
            .forEach(aluno => {
                alunos.set(
                    Number(aluno.idMatricula),
                    aluno
                );
            });

        return [...alunos.values()];
    }

    /**
     * Recupera a nota mais recente de uma avaliação.
     *
     * A pontuação é normalizada para uma escala
     * de zero a dez.
     */
    function obterNota(aluno, titulo) {
        const registros = estado.notas
            .filter(nota =>
                pertenceDisciplina(nota) &&
                mesmoId(
                    nota.idMatricula,
                    aluno.idMatricula
                ) &&
                normalizarTitulo(
                    nota.tituloAvaliacao
                ) === normalizarTitulo(titulo)
            )
            .sort(
                (primeira, segunda) =>
                    new Date(segunda.dataAvaliacao) -
                    new Date(primeira.dataAvaliacao)
            );

        const nota = registros[0];

        if (
            !nota ||
            nota.valorNota == null ||
            Number(nota.valorMaximo) <= 0
        ) {
            return null;
        }

        const valor =
            Number(nota.valorNota) /
            Number(nota.valorMaximo) * 10;

        return (
            Number.isFinite(valor) &&
            valor >= 0 &&
            valor <= 10
        )
            ? valor
            : null;
    }

    /**
     * Recupera a nota dos quizzes calculada pela API.
     *
     * O desempenho é baseado nos acertos e não
     * na quantidade de XP acumulado.
     */
    function obterNotaQuizzes(aluno) {
        const desempenho = estado.desempenhos.find(
            registro =>
                pertenceDisciplina(registro) &&
                mesmoId(
                    registro.idAluno,
                    aluno.idAluno
                ) &&
                mesmoId(
                    registro.idMatricula,
                    aluno.idMatricula
                )
        );

        if (
            !desempenho ||
            desempenho.notaQuizzes == null ||
            Number(desempenho.totalPerguntas) <= 0
        ) {
            return null;
        }

        const nota = Number(
            desempenho.notaQuizzes
        );

        return (
            Number.isFinite(nota) &&
            nota >= 0 &&
            nota <= 10
        )
            ? nota
            : null;
    }

    /**
     * Calcula a média ponderada de um aluno.
     *
     * Avaliações não lançadas permanecem pendentes.
     */
    function calcularMediaAluno(aluno) {
        const p1 = obterNota(aluno, "P1");

        const p2 = obterNota(aluno, "P2");

        const trabalho = obterNota(
            aluno,
            "Trabalho"
        );

        const quizzes = obterNotaQuizzes(
            aluno
        );

        const notas = [
            p1,
            p2,
            trabalho,
            quizzes
        ];

        if (
            notas.some(
                nota =>
                    nota === null ||
                    !Number.isFinite(nota)
            )
        ) {
            return null;
        }

        const pesos = CONFIGURACAO.pesos;

        const media =
            p1 * pesos.p1 +
            p2 * pesos.p2 +
            trabalho * pesos.trabalho +
            quizzes * pesos.quizzes;

        return Math.round(media * 100) / 100;
    }

    /**
     * Classifica o desempenho acadêmico por nota.
     */
    function obterSituacao(media) {
        if (media === null) {
            return "pendentes";
        }

        if (media >= 7) {
            return "aprovados";
        }

        if (media >= 5) {
            return "exame";
        }

        return "reprovados";
    }

    /**
     * Calcula a frequência geral da turma.
     *
     * Registros pendentes não são considerados
     * presenças nem faltas.
     */
    function calcularFrequencia(alunos) {
        const matriculas = new Set(
            alunos.map(aluno =>
                Number(aluno.idMatricula)
            )
        );

        const registros = estado.frequencias.filter(
            frequencia =>
                pertenceDisciplina(frequencia) &&
                matriculas.has(
                    Number(frequencia.idMatricula)
                )
        );

        const presencas = registros.filter(
            registro => registro.presente === true
        ).length;

        const faltas = registros.filter(
            registro => registro.presente === false
        ).length;

        return calcularPercentual(
            presencas,
            presencas + faltas
        );
    }

    /**
     * Calcula a participação geral nos quizzes.
     *
     * Considera os quizzes ativos e as respostas
     * dos alunos atualmente matriculados.
     */
    function calcularParticipacao(alunos) {
        const quizzes = estado.quizzes.filter(
            quiz =>
                pertenceDisciplina(quiz) &&
                quiz.ativo === true
        );

        const matriculas = new Set(
            alunos.map(aluno =>
                Number(aluno.idMatricula)
            )
        );

        const desempenhos = estado.desempenhos.filter(
            desempenho =>
                pertenceDisciplina(desempenho) &&
                matriculas.has(
                    Number(desempenho.idMatricula)
                )
        );

        const respostas = desempenhos.reduce(
            (total, desempenho) =>
                total +
                Number(desempenho.totalQuizzes || 0),
            0
        );

        const totalEsperado =
            quizzes.length * alunos.length;

        if (
            totalEsperado === 0 ||
            respostas > totalEsperado
        ) {
            return null;
        }

        return calcularPercentual(
            respostas,
            totalEsperado
        );
    }

    /**
     * Consolida os indicadores gerais da turma.
     *
     * Nenhuma informação individual é exibida
     * na interface de métricas.
     */
    function calcularMetricas() {
        const alunos = obterAlunos();

        const situacoes = {
            aprovados: 0,
            exame: 0,
            reprovados: 0,
            pendentes: 0
        };

        const medias = [];

        alunos.forEach(aluno => {
            const media = calcularMediaAluno(
                aluno
            );

            const situacao = obterSituacao(
                media
            );

            situacoes[situacao]++;

            if (media !== null) {
                medias.push(media);
            }
        });

        const somaMedias = medias.reduce(
            (total, media) => total + media,
            0
        );

        const mediaGeral = medias.length
            ? somaMedias / medias.length
            : null;

        return {
            totalAlunos: alunos.length,

            mediaGeral,

            totalMedias: medias.length,

            frequencia: calcularFrequencia(
                alunos
            ),

            participacao: calcularParticipacao(
                alunos
            ),

            ...situacoes
        };
    }

    /**
     * Renderiza uma opção dos filtros acadêmicos.
     */
    function renderizarOpcao(
        valor,
        texto,
        selecionado
    ) {
        return `
            <option
                value="${escaparHtml(valor)}"
                ${String(valor) === String(selecionado)
                ? "selected"
                : ""}
            >
                ${escaparHtml(texto)}
            </option>
        `;
    }

    /**
     * Renderiza os filtros de turma e disciplina.
     */
    function renderizarFiltros() {
        const turmas = obterTurmas()
            .map(([id, nome]) =>
                renderizarOpcao(
                    id,
                    nome,
                    estado.idTurma
                )
            )
            .join("");

        const disciplinas = obterDisciplinas()
            .map(disciplina =>
                renderizarOpcao(
                    disciplina.idTurmaDisciplina,
                    disciplina.nomeDisciplina,
                    estado.idTurmaDisciplina
                )
            )
            .join("");

        return `
            <section class="
                bg-white p-6 rounded-2xl
                border border-blue-100 shadow-sm
            ">
                <h2 class="
                    font-bold text-slate-800 mb-5
                ">
                    <i class="
                        fa-solid fa-filter
                        text-blue-600 mr-2
                    "></i>

                    Visão geral da turma
                </h2>

                <div class="
                    grid grid-cols-1
                    md:grid-cols-2 gap-4
                ">
                    <div>
                        <label
                            for="metricas-turma"
                            class="
                                block text-xs font-semibold
                                text-slate-700 mb-2
                            "
                        >
                            Turma
                        </label>

                        <select
                            id="metricas-turma"
                            class="
                                w-full p-3 bg-white
                                border border-slate-200
                                rounded-xl text-sm
                                outline-none
                                focus:border-blue-500
                            "
                        >
                            <option value="">
                                -- Escolha uma Turma --
                            </option>

                            ${turmas}
                        </select>
                    </div>

                    <div>
                        <label
                            for="metricas-disciplina"
                            class="
                                block text-xs font-semibold
                                text-slate-700 mb-2
                            "
                        >
                            Disciplina
                        </label>

                        <select
                            id="metricas-disciplina"
                            ${estado.idTurma
                ? ""
                : "disabled"}
                            class="
                                w-full p-3 bg-white
                                border border-slate-200
                                rounded-xl text-sm
                                outline-none
                                focus:border-blue-500
                                disabled:opacity-50
                            "
                        >
                            <option value="">
                                -- Escolha uma Disciplina --
                            </option>

                            ${disciplinas}
                        </select>
                    </div>
                </div>
            </section>
        `;
    }

    /**
     * Renderiza um indicador geral da turma.
     */
    function renderizarIndicador(
        titulo,
        valor,
        icone,
        cor,
        fundo,
        descricao
    ) {
        return `
            <article class="
                bg-white p-5 rounded-2xl
                border border-blue-100 shadow-sm
            ">
                <div class="
                    flex items-center
                    justify-between gap-3 mb-4
                ">
                    <span class="
                        text-sm font-semibold
                        text-slate-600
                    ">
                        ${escaparHtml(titulo)}
                    </span>

                    <div class="
                        w-10 h-10 rounded-xl
                        flex items-center
                        justify-center ${fundo}
                    ">
                        <i class="
                            fa-solid ${icone} ${cor}
                        "></i>
                    </div>
                </div>

                <p class="
                    text-3xl font-extrabold ${cor}
                ">
                    ${escaparHtml(valor)}
                </p>

                <p class="
                    text-xs text-slate-400 mt-2
                ">
                    ${escaparHtml(descricao)}
                </p>
            </article>
        `;
    }

    /**
     * Renderiza os quatro indicadores principais.
     */
    function renderizarIndicadores(metricas) {
        return `
            <div class="
                grid grid-cols-1 sm:grid-cols-2
                xl:grid-cols-4 gap-4
            ">
                ${renderizarIndicador(
            "Total de alunos",
            metricas.totalAlunos,
            "fa-users",
            "text-blue-600",
            "bg-blue-50",
            "Matrículas ativas"
        )}

                ${renderizarIndicador(
            "Média da turma",
            formatarNumero(
                metricas.mediaGeral
            ),
            "fa-chart-line",
            "text-blue-600",
            "bg-blue-50",
            `${metricas.totalMedias} de ` +
            `${metricas.totalAlunos} médias completas`
        )}

                ${renderizarIndicador(
            "Frequência geral",
            formatarPercentual(
                metricas.frequencia
            ),
            "fa-calendar-check",
            "text-emerald-600",
            "bg-emerald-50",
            "Presenças registradas"
        )}

                ${renderizarIndicador(
            "Participação nos quizzes",
            formatarPercentual(
                metricas.participacao
            ),
            "fa-clipboard-check",
            "text-violet-600",
            "bg-violet-50",
            "Participação da turma"
        )}
            </div>
        `;
    }

    /**
     * Renderiza uma legenda do gráfico acadêmico.
     */
    function renderizarLegenda(
        titulo,
        quantidade,
        cor,
        percentual
    ) {
        return `
            <div class="
                p-4 rounded-xl bg-slate-50
            ">
                <div class="
                    flex items-center gap-2 mb-2
                ">
                    <span class="
                        w-2.5 h-2.5 rounded-full
                        ${cor}
                    "></span>

                    <span class="
                        text-xs font-semibold
                        text-slate-600
                    ">
                        ${titulo}
                    </span>
                </div>

                <div class="
                    flex items-end
                    justify-between gap-2
                ">
                    <span class="
                        text-2xl font-bold
                        text-slate-800
                    ">
                        ${quantidade}
                    </span>

                    <span class="
                        text-xs text-slate-400
                    ">
                        ${formatarPercentual(percentual)}
                    </span>
                </div>
            </div>
        `;
    }

    /**
     * Renderiza o gráfico consolidado da turma.
     *
     * As categorias pendentes são preservadas
     * para não apresentar resultados incorretos.
     */
    function renderizarDesempenho(metricas) {
        const categorias = [
            {
                titulo: "Aprovados",
                quantidade: metricas.aprovados,
                cor: "bg-emerald-500"
            },
            {
                titulo: "Em exame",
                quantidade: metricas.exame,
                cor: "bg-amber-500"
            },
            {
                titulo: "Reprovados",
                quantidade: metricas.reprovados,
                cor: "bg-red-500"
            }
        ];

        // Exibe pendências somente quando existirem.
        if (metricas.pendentes > 0) {
            categorias.push({
                titulo: "Pendentes",
                quantidade: metricas.pendentes,
                cor: "bg-slate-400"
            });
        }

        const segmentos = categorias
            .filter(
                categoria => categoria.quantidade > 0
            )
            .map(categoria => {
                const percentual =
                    calcularPercentual(
                        categoria.quantidade,
                        metricas.totalAlunos
                    );

                return `
                    <div
                        class="h-full ${categoria.cor}"
                        style="width: ${percentual}%"
                        title="${categoria.titulo}: ${categoria.quantidade}"
                    ></div>
                `;
            })
            .join("");

        const legendas = categorias
            .map(categoria =>
                renderizarLegenda(
                    categoria.titulo,
                    categoria.quantidade,
                    categoria.cor,

                    calcularPercentual(
                        categoria.quantidade,
                        metricas.totalAlunos
                    )
                )
            )
            .join("");

        const colunas = categorias.length === 4
            ? "md:grid-cols-4"
            : "md:grid-cols-3";

        return `
            <section class="
                bg-white p-6 rounded-2xl
                border border-blue-100 shadow-sm
                space-y-6
            ">
                <div>
                    <h2 class="
                        font-bold text-slate-800
                    ">
                        <i class="
                            fa-solid fa-chart-pie
                            text-blue-600 mr-2
                        "></i>

                        Desempenho geral da turma
                    </h2>

                    <p class="
                        text-xs text-slate-400 mt-2
                    ">
                        Distribuição dos alunos por média.
                    </p>
                </div>

                <div
                    class="
                        flex h-5 bg-slate-100
                        rounded-full overflow-hidden
                    "
                    role="img"
                    aria-label="Distribuição acadêmica da turma"
                >
                    ${segmentos}
                </div>

                <div class="
                    grid grid-cols-1
                    sm:grid-cols-2 ${colunas}
                    gap-3
                ">
                    ${legendas}
                </div>
            </section>
        `;
    }

    /**
     * Renderiza uma mensagem informativa.
     */
    function renderizarAviso(mensagem) {
        return `
            <div class="
                border border-blue-100
                rounded-2xl p-8
                text-center text-slate-500
            ">
                <i class="
                    fa-solid fa-circle-info
                    text-blue-600 text-xl mb-3
                "></i>

                <p class="text-sm">
                    ${escaparHtml(mensagem)}
                </p>
            </div>
        `;
    }

    /**
     * Renderiza o painel minimalista de métricas.
     */
    function renderizarTela() {
        const main = document.getElementById(
            "main-content-area"
        );

        let conteudo;

        if (
            !estado.idTurma ||
            !estado.idTurmaDisciplina
        ) {
            conteudo = renderizarAviso(
                "Selecione a turma e a disciplina " +
                "para visualizar os indicadores."
            );

        } else {
            const metricas = calcularMetricas();

            conteudo = `
                ${renderizarIndicadores(metricas)}

                ${metricas.totalAlunos > 0
                    ? renderizarDesempenho(metricas)
                    : renderizarAviso(
                        "Nenhum aluno matriculado nesta turma."
                    )}
            `;
        }

        main.innerHTML = `
            <div class="space-y-6">
                ${renderizarFiltros()}

                ${conteudo}
            </div>
        `;

        configurarFiltros();
    }

    /**
     * Configura os eventos dos filtros acadêmicos.
     */
    function configurarFiltros() {
        const turma = document.getElementById(
            "metricas-turma"
        );

        const disciplina = document.getElementById(
            "metricas-disciplina"
        );

        turma.value = estado.idTurma;

        disciplina.value =
            estado.idTurmaDisciplina;

        turma.addEventListener(
            "change",
            evento => {
                estado.idTurma =
                    evento.target.value;

                estado.idTurmaDisciplina = "";

                renderizarTela();
            }
        );

        disciplina.addEventListener(
            "change",
            evento => {
                estado.idTurmaDisciplina =
                    evento.target.value;

                renderizarTela();
            }
        );
    }

    /**
     * Atualiza o cabeçalho e o menu de navegação.
     */
    function configurarNavegacao(botaoAtivo) {
        document.querySelectorAll(
            "#sidebar nav button"
        ).forEach(botao => {
            botao.classList.remove(
                "bg-brand-blue",
                "bg-blue-600",
                "bg-slate-800",
                "text-white",
                "shadow-md"
            );

            botao.classList.add(
                "text-slate-300"
            );
        });

        botaoAtivo.classList.remove(
            "text-slate-300"
        );

        botaoAtivo.classList.add(
            "bg-brand-blue",
            "text-white",
            "shadow-md"
        );

        const titulo = document.getElementById(
            "page-title"
        );

        const subtitulo = document.getElementById(
            "page-subtitle"
        );

        if (titulo) {
            titulo.textContent =
                "Métricas da Turma";
        }

        if (subtitulo) {
            subtitulo.textContent =
                "Acompanhe o desempenho geral da turma.";
        }
    }

    /**
     * Valida os filtros após atualizar os dados.
     */
    function validarSelecao() {
        const turmaValida = obterTurmas().some(
            ([id]) =>
                id === estado.idTurma
        );

        if (!turmaValida) {
            estado.idTurma = "";

            estado.idTurmaDisciplina = "";

            return;
        }

        const disciplinaValida =
            obterDisciplinas().some(
                disciplina =>
                    mesmoId(
                        disciplina.idTurmaDisciplina,
                        estado.idTurmaDisciplina
                    )
            );

        if (!disciplinaValida) {
            estado.idTurmaDisciplina = "";
        }
    }

    /**
     * Verifica se a requisição continua válida.
     */
    function requisicaoValida(requisicao) {
        return (
            estado.telaAtiva &&
            requisicao === estado.requisicao
        );
    }

    /**
     * Consulta os registros oficiais do banco.
     *
     * As consultas são sequenciais para reduzir
     * a concorrência no pool de conexões.
     */
    async function carregarMetricas() {
        const requisicao = ++estado.requisicao;

        estado.telaAtiva = true;

        const main = document.getElementById(
            "main-content-area"
        );

        main.scrollTop = 0;

        main.innerHTML = `
            <div class="
                p-10 text-center text-slate-500
            ">
                <i class="
                    fa-solid fa-spinner fa-spin
                    text-blue-600 text-xl
                "></i>

                <p class="mt-3 text-sm">
                    Carregando métricas da turma...
                </p>
            </div>
        `;

        try {
            const consultas = [
                ["turmas", "turmas"],
                ["alunos", "alunos"],
                ["notas", "notas"],
                ["frequencias", "frequencias"],
                ["quizzes", "quizzes"],

                [
                    "quizzes/desempenho",
                    "desempenhos"
                ]
            ];

            for (
                const [endpoint, propriedade]
                of consultas
            ) {
                const dados = await consultarApi(
                    endpoint,
                    propriedade
                );

                if (!requisicaoValida(requisicao)) {
                    return;
                }

                estado[propriedade] = dados;
            }

            validarSelecao();

            renderizarTela();

        } catch (erro) {
            if (!requisicaoValida(requisicao)) {
                return;
            }

            console.error(
                "Erro ao carregar métricas:",
                erro
            );

            main.innerHTML = `
                <div class="
                    bg-white p-8 rounded-2xl
                    border border-red-100
                    text-center
                ">
                    <p class="text-sm text-red-600">
                        ${escaparHtml(erro.message)}
                    </p>

                    <button
                        id="metricas-tentar-novamente"
                        type="button"
                        class="
                            mt-4 px-5 py-3 rounded-xl
                            bg-blue-600 text-white
                            text-sm font-bold
                        "
                    >
                        Tentar novamente
                    </button>
                </div>
            `;

            document.getElementById(
                "metricas-tentar-novamente"
            ).addEventListener(
                "click",
                carregarMetricas
            );
        }
    }

    /**
     * Cancela consultas pendentes ao navegar
     * para outra funcionalidade do painel.
     */
    window.addEventListener(
        "click",
        evento => {
            const botao = evento.target.closest(
                "#sidebar nav button"
            );

            if (
                botao &&
                botao.id !== "nav-metricas"
            ) {
                estado.telaAtiva = false;

                estado.requisicao++;
            }
        },
        true
    );

    /**
     * Intercepta exclusivamente a navegação
     * para a tela de métricas.
     */
    document.addEventListener(
        "click",
        evento => {
            const botao = evento.target.closest(
                "#sidebar nav button"
            );

            if (
                !botao ||
                botao.id !== "nav-metricas"
            ) {
                return;
            }

            evento.preventDefault();

            evento.stopImmediatePropagation();

            configurarNavegacao(botao);

            carregarMetricas();
        },
        true
    );
})();