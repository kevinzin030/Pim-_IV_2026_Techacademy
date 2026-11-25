(() => {
    "use strict";

    const API_URL = "https://techacademy-api-2026-h0ebgpcgechch0c4.brazilsouth-01.azurewebsites.net/api";

    const PESOS = {
        P1: 0.30,
        P2: 0.30,
        Trabalho: 0.25,
        Quizzes: 0.15
    };

    const CAMPOS_AVALIACAO = {
        p1: "P1",
        p2: "P2",
        trabalho: "Trabalho"
    };

    const estado = {
        turmas: [],
        alunos: [],
        notas: [],
        desempenhos: [],
        idTurma: "",
        idTurmaDisciplina: "",
        requisicao: 0,
        salvando: false,
        telaAtiva: false
    };

    /**
     * Protege os conteúdos inseridos no HTML.
     */
    function escaparHtml(valor) {
        return String(valor ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    /**
     * Reutiliza os templates originais do painel.
     */
    function renderizarTemplate(numero, dados = {}) {
        const identificador =
            `ta-prof-fragment-${numero}`;

        const template =
            document.getElementById(identificador);

        if (!template) {
            throw new Error(
                `Template não encontrado: ${identificador}`
            );
        }

        return template.innerHTML.replace(
            /__TA_SLOT_(\d+)__/g,
            (_, chave) => dados[chave] ?? ""
        );
    }

    /**
     * Executa requisições autenticadas à API.
     */
    async function requisitar(endpoint, opcoes = {}) {
        const token = sessionStorage.getItem(
            "techacademy_token"
        );

        if (!token) {
            window.location.href = "../../index.html";

            throw new Error("Sessão expirada.");
        }

        const resposta = await fetch(
            `${API_URL}${endpoint}`,
            {
                ...opcoes,

                cache: "no-store",

                headers: {
                    Authorization: `Bearer ${token}`,
                    "Content-Type": "application/json",
                    ...opcoes.headers
                }
            }
        );

        const dados = await resposta.json();

        if (resposta.status === 401) {
            sessionStorage.removeItem(
                "techacademy_token"
            );

            sessionStorage.removeItem(
                "usuario_logado"
            );

            window.location.href = "../../index.html";
        }

        if (!resposta.ok) {
            throw new Error(
                dados.mensagem ||
                "Não foi possível concluir a operação."
            );
        }

        return dados;
    }

    /**
     * Recupera os dados acadêmicos reais do Azure SQL.
     *
     * As consultas são sequenciais para reduzir
     * a concorrência no pool de conexões.
     */
    async function consultarDadosAcademicos() {
        const turmas = await requisitar(
            "/professores/me/turmas"
        );

        const alunos = await requisitar(
            "/professores/me/alunos"
        );

        const notas = await requisitar(
            "/professores/me/notas"
        );

        const desempenhos = await requisitar(
            "/professores/me/quizzes/desempenho"
        );

        return {
            turmas: turmas.turmas,
            alunos: alunos.alunos,
            notas: notas.notas,
            desempenhos: desempenhos.desempenhos
        };
    }

    /**
     * Obtém as turmas sem repetir registros.
     */
    function obterTurmas() {
        const turmas = new Map();

        estado.turmas.forEach(turma => {
            turmas.set(turma.idTurma, turma);
        });

        return [...turmas.values()];
    }

    /**
     * Localiza uma nota registrada para determinada
     * matrícula, disciplina e avaliação.
     */
    function obterNota(idMatricula, tipoAvaliacao) {
        return estado.notas.find(nota =>
            Number(nota.idMatricula) ===
            Number(idMatricula) &&
            Number(nota.idTurmaDisciplina) ===
            Number(estado.idTurmaDisciplina) &&
            nota.tituloAvaliacao === tipoAvaliacao
        );
    }

    /**
     * Recupera o valor numérico de uma nota existente.
     */
    function obterValorNota(idMatricula, tipoAvaliacao) {
        const nota = obterNota(
            idMatricula,
            tipoAvaliacao
        );

        if (
            !nota ||
            nota.valorNota === null ||
            nota.valorNota === undefined
        ) {
            return null;
        }

        return Number(nota.valorNota);
    }

    /**
     * Obtém a nota automática dos quizzes.
     *
     * A ausência de respostas não é considerada zero.
     */
    function obterNotaQuizzes(idMatricula) {
        const desempenho = estado.desempenhos.find(
            registro =>
                Number(registro.idMatricula) ===
                Number(idMatricula) &&
                Number(registro.idTurmaDisciplina) ===
                Number(estado.idTurmaDisciplina)
        );

        if (
            !desempenho ||
            desempenho.notaQuizzes === null ||
            desempenho.notaQuizzes === undefined
        ) {
            return null;
        }

        return Number(desempenho.notaQuizzes);
    }

    /**
     * Calcula a média ponderada quando todas
     * as avaliações possuem notas válidas.
     */
    function calcularMedia(p1, p2, trabalho, quizzes) {
        const valores = [
            p1,
            p2,
            trabalho,
            quizzes
        ];

        if (
            valores.some(valor =>
                valor === null ||
                !Number.isFinite(valor) ||
                valor < 0 ||
                valor > 10
            )
        ) {
            return null;
        }

        const media =
            p1 * PESOS.P1 +
            p2 * PESOS.P2 +
            trabalho * PESOS.Trabalho +
            quizzes * PESOS.Quizzes;

        return Math.round(
            (media + Number.EPSILON) * 100
        ) / 100;
    }

    /**
     * Define a situação acadêmica conforme a média.
     */
    function obterSituacao(media) {
        if (media === null) {
            return {
                texto: "Pendente",
                classe:
                    "bg-slate-100 text-slate-600 border-slate-200"
            };
        }

        if (media >= 7) {
            return {
                texto: "Aprovado",
                classe:
                    "bg-emerald-50 text-emerald-700 border-emerald-200"
            };
        }

        if (media >= 5) {
            return {
                texto: "Exame",
                classe:
                    "bg-amber-50 text-amber-700 border-amber-200"
            };
        }

        return {
            texto: "Reprovado",
            classe:
                "bg-red-50 text-red-700 border-red-200"
        };
    }

    /**
     * Formata uma nota no padrão brasileiro.
     */
    function formatarNota(nota) {
        if (
            nota === null ||
            !Number.isFinite(nota)
        ) {
            return "—";
        }

        return nota.toLocaleString("pt-BR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    /**
     * Recupera o valor de um campo numérico.
     */
    function lerNota(campo) {
        if (!campo) {
            return null;
        }

        if (campo.validity.badInput) {
            return NaN;
        }

        if (campo.value.trim() === "") {
            return null;
        }

        return Number(campo.value);
    }

    /**
     * Exibe mensagens de sincronização e validação.
     */
    function exibirMensagem(mensagem, erro = false) {
        const status = document.getElementById(
            "status-salvar-notas"
        );

        if (!status) {
            return;
        }

        status.textContent = mensagem;

        status.className = erro
            ? "text-xs font-bold text-red-700 bg-red-50 px-3 py-2 rounded-lg"
            : "text-xs font-bold text-emerald-700 bg-emerald-50 px-3 py-2 rounded-lg";
    }

    /**
     * Renderiza o indicador da situação acadêmica.
     */
    function renderizarSituacao(situacao) {
        return `
            <span
                class="inline-flex items-center justify-center
                       px-2.5 py-1 rounded-full text-[10px]
                       font-bold border ${situacao.classe}"
            >
                ${situacao.texto}
            </span>
        `;
    }

    /**
     * Renderiza um campo individual de nota.
     *
     * Os campos possuem identificação acessível,
     * limites de 0 a 10 e duas casas decimais.
     */
    function renderizarCampoNota(
        campo,
        rotulo,
        valor,
        indice,
        nomeAluno,
        somenteLeitura = false
    ) {
        const identificador =
            `nota-${campo}-${indice}`;

        const classeCampo = somenteLeitura
            ? "bg-slate-100 text-slate-700 cursor-not-allowed"
            : "bg-white text-slate-800 focus:border-blue-500";

        return `
            <label
                for="${identificador}"
                class="sr-only"
            >
                ${escaparHtml(rotulo)}
            </label>

            <input
                type="number"
                id="${identificador}"
                data-field="${campo}"
                data-index="${indice}"
                min="0"
                max="10"
                step="0.01"
                value="${escaparHtml(valor ?? "")}"
                placeholder="—"
                aria-label="${escaparHtml(
            `${rotulo} de ${nomeAluno}`
        )}"
                ${somenteLeitura ? "readonly" : ""}
                ${somenteLeitura
                ? 'title="Nota calculada automaticamente pelos quizzes."'
                : ""}
                class="input-nota w-20 px-2 py-2
                       border border-slate-200 rounded-lg
                       text-center text-sm font-bold
                       outline-none transition ${classeCampo}"
            >
        `;
    }

    /**
     * Renderiza uma linha completa do diário de notas.
     *
     * A estrutura semântica mantém cada campo
     * alinhado ao seu respectivo cabeçalho.
     */
    function renderizarAluno(aluno, indice) {
        const idMatricula = aluno.idMatricula;

        const p1 = obterValorNota(
            idMatricula,
            "P1"
        );

        const p2 = obterValorNota(
            idMatricula,
            "P2"
        );

        const trabalho = obterValorNota(
            idMatricula,
            "Trabalho"
        );

        const quizzes = obterNotaQuizzes(
            idMatricula
        );

        const media = calcularMedia(
            p1,
            p2,
            trabalho,
            quizzes
        );

        const situacao = obterSituacao(media);

        return `
            <tr
                data-id-matricula="${idMatricula}"
                data-indice="${indice}"
                class="border-b border-slate-100
                       hover:bg-blue-50/40 transition"
            >
                <th
                    scope="row"
                    class="px-4 py-4 text-left font-normal"
                >
                    <p class="font-bold text-slate-800">
                        ${escaparHtml(aluno.nomeCompleto)}
                    </p>

                    <p class="text-xs text-slate-400 mt-1">
                        Mat: ${escaparHtml(aluno.matriculaAluno)}
                    </p>
                </th>

                <td class="px-3 py-4 text-center">
                    ${renderizarCampoNota(
            "p1",
            "P1",
            p1,
            indice,
            aluno.nomeCompleto
        )}
                </td>

                <td class="px-3 py-4 text-center">
                    ${renderizarCampoNota(
            "p2",
            "P2",
            p2,
            indice,
            aluno.nomeCompleto
        )}
                </td>

                <td class="px-3 py-4 text-center">
                    ${renderizarCampoNota(
            "trabalho",
            "Trabalho",
            trabalho,
            indice,
            aluno.nomeCompleto
        )}
                </td>

                <td class="px-3 py-4 text-center">
                    ${renderizarCampoNota(
            "quizzes",
            "Quizzes",
            quizzes,
            indice,
            aluno.nomeCompleto,
            true
        )}
                </td>

                <td
                    id="media-${indice}"
                    class="px-3 py-4 text-center
                           font-bold text-slate-800"
                >
                    ${formatarNota(media)}
                </td>

                <td
                    id="situacao-${indice}"
                    class="px-4 py-4 text-center"
                >
                    ${renderizarSituacao(situacao)}
                </td>
            </tr>
        `;
    }

    /**
     * Atualiza a média e a situação imediatamente
     * após a alteração de uma nota.
     */
    function atualizarMediaLinha(linha) {
        if (!linha) {
            return;
        }

        const p1 = lerNota(
            linha.querySelector('[data-field="p1"]')
        );

        const p2 = lerNota(
            linha.querySelector('[data-field="p2"]')
        );

        const trabalho = lerNota(
            linha.querySelector('[data-field="trabalho"]')
        );

        const quizzes = lerNota(
            linha.querySelector('[data-field="quizzes"]')
        );

        const media = calcularMedia(
            p1,
            p2,
            trabalho,
            quizzes
        );

        const situacao = obterSituacao(media);

        const indice = linha.dataset.indice;

        const elementoMedia = document.getElementById(
            `media-${indice}`
        );

        const elementoSituacao = document.getElementById(
            `situacao-${indice}`
        );

        if (elementoMedia) {
            elementoMedia.textContent =
                formatarNota(media);
        }

        if (elementoSituacao) {
            elementoSituacao.innerHTML =
                renderizarSituacao(situacao);
        }
    }

    /**
     * Renderiza o campo de data de uma avaliação.
     *
     * As datas existentes são preservadas.
     */
    function renderizarCampoData(tipo, identificador) {
        const avaliacao = estado.notas.find(
            nota =>
                Number(nota.idTurmaDisciplina) ===
                Number(estado.idTurmaDisciplina) &&
                nota.tituloAvaliacao === tipo
        );

        const data = avaliacao?.dataAvaliacao
            ?.slice(0, 10) ?? "";

        return `
            <div>
                <label
                    for="data-avaliacao-${identificador}"
                    class="block font-bold text-slate-700 mb-1"
                >
                    Data da avaliação: ${tipo}
                </label>

                <input
                    type="date"
                    id="data-avaliacao-${identificador}"
                    value="${escaparHtml(data)}"
                    ${data ? "readonly" : ""}
                    class="w-full p-2.5 border
                           border-slate-200 rounded-xl
                           ${data ? "bg-slate-50" : "bg-white"}"
                >
            </div>
        `;
    }

    /**
     * Renderiza as datas e os botões de publicação.
     */
    function renderizarControlesPublicacao() {
        return `
            <section
                class="bg-white p-5 rounded-2xl
                       border border-blue-100 shadow-sm
                       space-y-4"
            >
                <div
                    class="grid grid-cols-1
                           md:grid-cols-3 gap-4"
                >
                    ${renderizarCampoData("P1", "p1")}

                    ${renderizarCampoData("P2", "p2")}

                    ${renderizarCampoData(
            "Trabalho",
            "trabalho"
        )}
                </div>

                <p class="text-xs text-slate-500">
                    As datas são utilizadas no primeiro
                    lançamento de cada avaliação.
                    As datas já cadastradas serão preservadas.
                </p>

                <div class="flex flex-wrap justify-end gap-3">
                    <button
                        type="button"
                        id="btn-recarregar-notas-api"
                        class="px-5 py-2.5 border
                               border-slate-200 rounded-xl
                               font-bold text-slate-600
                               hover:bg-slate-50 transition"
                    >
                        Atualizar
                    </button>

                    <button
                        type="button"
                        id="btn-salvar-notas-api"
                        class="px-5 py-2.5 bg-brand-blue
                               text-white rounded-xl font-bold
                               hover:opacity-90 transition
                               disabled:opacity-50"
                    >
                        <i class="fa-solid fa-floppy-disk mr-2"></i>

                        Salvar e publicar notas
                    </button>
                </div>
            </section>
        `;
    }

    /**
     * Renderiza o diário de notas utilizando uma
     * tabela HTML semântica e responsiva.
     *
     * O cabeçalho fica acima das notas e cada
     * avaliação ocupa sua própria coluna.
     */
    function renderizarTabela() {
        if (!estado.idTurmaDisciplina) {
            return renderizarTemplate("010");
        }

        const alunos = estado.alunos.filter(
            aluno =>
                Number(aluno.idTurma) ===
                Number(estado.idTurma)
        );

        if (!alunos.length) {
            return `
                <div
                    class="bg-white p-8 rounded-2xl
                           border border-blue-100
                           text-center text-slate-500"
                >
                    Nenhum aluno matriculado nesta turma.
                </div>
            `;
        }

        const linhas = alunos.map(
            renderizarAluno
        ).join("");

        return `
            <div class="space-y-5">
                <section
                    class="bg-white p-6 rounded-2xl
                           border border-blue-100 shadow-sm"
                    aria-labelledby="titulo-diario-notas"
                >
                    <div
                        class="flex flex-wrap items-center
                               justify-between gap-3 mb-5"
                    >
                        <h2
                            id="titulo-diario-notas"
                            class="font-bold text-slate-800"
                        >
                            <i
                                class="fa-solid fa-list-check
                                       text-blue-600 mr-2"
                            ></i>

                            Diário de Notas dos Alunos
                        </h2>

                        <span
                            id="status-salvar-notas"
                            role="status"
                            aria-live="polite"
                            class="text-xs font-bold
                                   text-emerald-700 bg-emerald-50
                                   px-3 py-2 rounded-lg"
                        ></span>
                    </div>

                    <div class="overflow-x-auto">
                        <table
                            id="tabela-notas-api"
                            class="w-full min-w-[800px]
                                   border-collapse text-sm"
                        >
                            <thead
                                class="bg-blue-50
                                       text-slate-700
                                       border-b border-blue-100"
                            >
                                <tr>
                                    <th
                                        scope="col"
                                        class="px-4 py-4
                                               text-left
                                               min-w-[200px]"
                                    >
                                        ALUNO
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-3 py-4
                                               text-center"
                                    >
                                        P1 (30%)
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-3 py-4
                                               text-center"
                                    >
                                        P2 (30%)
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-3 py-4
                                               text-center"
                                    >
                                        TRABALHO (25%)
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-3 py-4
                                               text-center"
                                    >
                                        QUIZZES (15%)
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-3 py-4
                                               text-center"
                                    >
                                        MÉDIA
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-4 py-4
                                               text-center"
                                    >
                                        SITUAÇÃO
                                    </th>
                                </tr>
                            </thead>

                            <tbody id="corpo-tabela-notas-api">
                                ${linhas}
                            </tbody>
                        </table>
                    </div>

                    <p class="text-xs text-slate-400 mt-4">
                        A nota dos quizzes é calculada
                        automaticamente pelo sistema.
                    </p>
                </section>

                ${renderizarControlesPublicacao()}
            </div>
        `;
    }

    /**
     * Renderiza os seletores e o diário de notas.
     */
    function renderizarTela() {
        const main = document.getElementById(
            "main-content-area"
        );

        const opcoesTurmas = obterTurmas().map(
            turma => renderizarTemplate("008", {
                0: turma.idTurma,

                1: Number(turma.idTurma) ===
                    Number(estado.idTurma)
                    ? "selected"
                    : "",

                2: escaparHtml(turma.nomeTurma)
            })
        ).join("");

        const opcoesDisciplinas = estado.turmas
            .filter(turma =>
                Number(turma.idTurma) ===
                Number(estado.idTurma)
            )
            .map(disciplina =>
                renderizarTemplate("009", {
                    0: disciplina.idTurmaDisciplina,

                    1: Number(disciplina.idTurmaDisciplina) ===
                        Number(estado.idTurmaDisciplina)
                        ? "selected"
                        : "",

                    2: escaparHtml(
                        disciplina.codigoDisciplina
                    ),

                    3: escaparHtml(
                        disciplina.nomeDisciplina
                    )
                })
            )
            .join("");

        main.innerHTML = renderizarTemplate(
            "007",
            {
                0: opcoesTurmas,
                1: estado.idTurma ? "" : "disabled",
                2: opcoesDisciplinas,
                3: renderizarTabela()
            }
        );

        // Preserva a turma selecionada.
        const selectTurma = document.getElementById(
            "select-turma"
        );

        if (selectTurma) {
            selectTurma.value = estado.idTurma;
        }

        // Preserva a disciplina selecionada.
        const selectDisciplina = document.getElementById(
            "select-disciplina"
        );

        if (selectDisciplina) {
            selectDisciplina.value =
                estado.idTurmaDisciplina;

            selectDisciplina.disabled =
                !estado.idTurma;
        }

        configurarEventos();

        exibirMensagem(
            "Notas sincronizadas com o banco de dados."
        );
    }

    /**
     * Recupera as linhas da tabela de notas.
     */
    function obterLinhasTabela() {
        return [
            ...document.querySelectorAll(
                "#corpo-tabela-notas-api tr[data-id-matricula]"
            )
        ];
    }

    /**
     * Identifica alterações ainda não publicadas.
     */
    function existemAlteracoes() {
        return obterLinhasTabela().some(linha => {
            const idMatricula =
                Number(linha.dataset.idMatricula);

            return Object.entries(
                CAMPOS_AVALIACAO
            ).some(([campo, tipo]) => {
                const elemento = linha.querySelector(
                    `[data-field="${campo}"]`
                );

                const atual = lerNota(elemento);

                const anterior = obterValorNota(
                    idMatricula,
                    tipo
                );

                return atual !== anterior;
            });
        });
    }

    /**
     * Coleta somente notas novas ou alteradas.
     */
    function coletarAlteracoes() {
        const alteracoes = [];

        obterLinhasTabela().forEach(linha => {
            const idMatricula =
                Number(linha.dataset.idMatricula);

            Object.entries(CAMPOS_AVALIACAO).forEach(
                ([campo, tipoAvaliacao]) => {
                    const elemento = linha.querySelector(
                        `[data-field="${campo}"]`
                    );

                    const valorNota = lerNota(elemento);

                    const notaExistente = obterNota(
                        idMatricula,
                        tipoAvaliacao
                    );

                    if (!elemento.validity.valid) {
                        throw new Error(
                            "Informe notas válidas entre 0 e 10."
                        );
                    }

                    if (valorNota === null) {
                        if (notaExistente) {
                            throw new Error(
                                "Uma nota publicada não pode ser apagada. Informe um novo valor."
                            );
                        }

                        return;
                    }

                    if (
                        !Number.isFinite(valorNota) ||
                        valorNota < 0 ||
                        valorNota > 10 ||
                        Math.abs(
                            valorNota * 100 -
                            Math.round(valorNota * 100)
                        ) > 0.000001
                    ) {
                        throw new Error(
                            "Informe notas entre 0 e 10, com até duas casas decimais."
                        );
                    }

                    if (
                        notaExistente &&
                        Number(notaExistente.valorNota) ===
                        valorNota
                    ) {
                        return;
                    }

                    const dataExistente =
                        notaExistente?.dataAvaliacao
                            ?.slice(0, 10);

                    const campoData =
                        document.getElementById(
                            `data-avaliacao-${campo}`
                        );

                    const dataAvaliacao =
                        dataExistente || campoData?.value;

                    if (!dataAvaliacao) {
                        throw new Error(
                            `Informe a data da avaliação ${tipoAvaliacao}.`
                        );
                    }

                    alteracoes.push({
                        idMatricula,

                        idTurmaDisciplina: Number(
                            estado.idTurmaDisciplina
                        ),

                        tipoAvaliacao,
                        valorNota,
                        dataAvaliacao
                    });
                }
            );
        });

        return alteracoes;
    }

    /**
     * Atualiza o estado local após a confirmação
     * de uma nota pela API.
     */
    function sincronizarNota(nota, resultado) {
        const anterior = obterNota(
            nota.idMatricula,
            nota.tipoAvaliacao
        );

        const registro = {
            ...anterior,
            ...resultado,

            tituloAvaliacao:
                nota.tipoAvaliacao,

            valorMaximo: 10
        };

        if (anterior) {
            Object.assign(
                anterior,
                registro
            );
        } else {
            estado.notas.push(registro);
        }
    }

    /**
     * Persiste as notas alteradas no Azure SQL.
     *
     * Cada lançamento é confirmado individualmente
     * pela API antes de atualizar o estado local.
     */
    async function salvarNotas() {
        if (estado.salvando) {
            return;
        }

        const botao = document.getElementById(
            "btn-salvar-notas-api"
        );

        let totalSalvas = 0;

        try {
            const alteracoes = coletarAlteracoes();

            if (!alteracoes.length) {
                exibirMensagem(
                    "Nenhuma alteração para salvar."
                );

                return;
            }

            estado.salvando = true;

            const controles = [
                ...document.querySelectorAll(
                    "#main-content-area input, " +
                    "#main-content-area select, " +
                    "#main-content-area button"
                )
            ];

            const estadosAnteriores = controles.map(
                controle => controle.disabled
            );

            controles.forEach(
                controle => controle.disabled = true
            );

            botao.textContent = "Salvando...";

            try {
                // Grava as notas sequencialmente.
                for (const nota of alteracoes) {
                    const resultado = await requisitar(
                        "/professores/me/notas",
                        {
                            method: "POST",

                            body: JSON.stringify(nota)
                        }
                    );

                    sincronizarNota(
                        nota,
                        resultado
                    );

                    totalSalvas++;
                }

                // Recupera os registros oficiais do banco.
                const notas = await requisitar(
                    "/professores/me/notas"
                );

                estado.notas = notas.notas;

                renderizarTela();

                exibirMensagem(
                    `${totalSalvas} nota(s) publicada(s) com sucesso.`
                );

            } finally {
                controles.forEach(
                    (controle, indice) => {
                        controle.disabled =
                            estadosAnteriores[indice];
                    }
                );
            }

        } catch (erro) {
            const mensagem = totalSalvas > 0
                ? `${totalSalvas} nota(s) salva(s). ${erro.message}`
                : erro.message;

            exibirMensagem(
                mensagem,
                true
            );

        } finally {
            estado.salvando = false;

            if (botao) {
                botao.disabled = false;

                botao.innerHTML = `
                    <i class="fa-solid fa-floppy-disk mr-2"></i>

                    Salvar e publicar notas
                `;
            }
        }
    }

    /**
     * Configura os eventos dos seletores.
     */
    function configurarSeletores() {
        const selectTurma = document.getElementById(
            "select-turma"
        );

        const selectDisciplina = document.getElementById(
            "select-disciplina"
        );

        selectTurma?.addEventListener(
            "change",
            evento => {
                if (
                    existemAlteracoes() &&
                    !confirm(
                        "Descartar alterações não salvas?"
                    )
                ) {
                    evento.target.value =
                        estado.idTurma;

                    return;
                }

                estado.idTurma =
                    evento.target.value;

                estado.idTurmaDisciplina = "";

                renderizarTela();
            }
        );

        selectDisciplina?.addEventListener(
            "change",
            evento => {
                if (
                    existemAlteracoes() &&
                    !confirm(
                        "Descartar alterações não salvas?"
                    )
                ) {
                    evento.target.value =
                        estado.idTurmaDisciplina;

                    return;
                }

                estado.idTurmaDisciplina =
                    evento.target.value;

                renderizarTela();
            }
        );
    }

    /**
     * Configura os eventos dos campos de notas
     * e dos botões de publicação.
     */
    function configurarEventos() {
        configurarSeletores();

        document.querySelectorAll(
            "#corpo-tabela-notas-api .input-nota:not([readonly])"
        ).forEach(campo => {
            campo.addEventListener(
                "input",
                () => {
                    atualizarMediaLinha(
                        campo.closest("tr")
                    );

                    exibirMensagem(
                        "Alterações aguardando publicação."
                    );
                }
            );
        });

        document.getElementById(
            "btn-salvar-notas-api"
        )?.addEventListener(
            "click",
            salvarNotas
        );

        document.getElementById(
            "btn-recarregar-notas-api"
        )?.addEventListener(
            "click",
            () => {
                if (
                    existemAlteracoes() &&
                    !confirm(
                        "Descartar alterações não salvas?"
                    )
                ) {
                    return;
                }

                carregarTela();
            }
        );
    }

    /**
     * Atualiza a identificação visual da navegação.
     */
    function configurarNavegacao() {
        document.querySelectorAll(
            "#sidebar nav button"
        ).forEach(botao => {
            botao.classList.remove(
                "bg-brand-blue",
                "bg-blue-600",
                "bg-slate-800",
                "text-white"
            );

            botao.classList.add(
                "text-slate-300"
            );
        });

        const botaoAtivo = document.getElementById(
            "nav-lancar-notas"
        );

        botaoAtivo?.classList.remove(
            "text-slate-300"
        );

        botaoAtivo?.classList.add(
            "bg-brand-blue",
            "text-white"
        );

        const titulo = document.getElementById(
            "page-title"
        );

        const subtitulo = document.getElementById(
            "page-subtitle"
        );

        if (titulo) {
            titulo.textContent =
                "Lançar & Consultar Notas";
        }

        if (subtitulo) {
            subtitulo.textContent =
                "Gerencie o desempenho acadêmico dos alunos.";
        }
    }

    /**
     * Valida se os vínculos selecionados continuam
     * disponíveis após uma nova consulta à API.
     */
    function validarSelecao() {
        const turmaValida = estado.turmas.some(
            turma =>
                Number(turma.idTurma) ===
                Number(estado.idTurma)
        );

        if (!turmaValida) {
            estado.idTurma = "";
            estado.idTurmaDisciplina = "";

            return;
        }

        const disciplinaValida = estado.turmas.some(
            disciplina =>
                Number(disciplina.idTurma) ===
                Number(estado.idTurma) &&
                Number(disciplina.idTurmaDisciplina) ===
                Number(estado.idTurmaDisciplina)
        );

        if (!disciplinaValida) {
            estado.idTurmaDisciplina = "";
        }
    }

    /**
     * Carrega a tela exclusivamente com dados da API.
     */
    async function carregarTela() {
        const requisicao = ++estado.requisicao;

        estado.telaAtiva = true;

        configurarNavegacao();

        const main = document.getElementById(
            "main-content-area"
        );

        main.innerHTML = `
            <div
                class="bg-white p-8 rounded-2xl
                       border border-blue-100
                       text-center text-slate-500"
            >
                <i class="fa-solid fa-spinner fa-spin mr-2"></i>

                Carregando notas dos alunos...
            </div>
        `;

        try {
            const dados =
                await consultarDadosAcademicos();

            if (
                requisicao !== estado.requisicao ||
                !estado.telaAtiva
            ) {
                return;
            }

            Object.assign(
                estado,
                dados
            );

            validarSelecao();

            renderizarTela();

        } catch (erro) {
            if (
                requisicao !== estado.requisicao ||
                !estado.telaAtiva
            ) {
                return;
            }

            main.innerHTML = `
                <div
                    class="bg-red-50 border border-red-200
                           rounded-2xl p-6 text-red-700"
                >
                    ${escaparHtml(erro.message)}
                </div>
            `;
        }
    }

    /**
     * Protege alterações não publicadas ao atualizar
     * ou fechar a página.
     */
    window.addEventListener(
        "beforeunload",
        evento => {
            if (
                estado.telaAtiva &&
                (
                    estado.salvando ||
                    existemAlteracoes()
                )
            ) {
                evento.preventDefault();

                evento.returnValue = "";
            }
        }
    );

    /**
     * Protege a navegação antes dos demais scripts
     * do painel processarem o clique.
     */
    window.addEventListener(
        "click",
        evento => {
            const botao = evento.target.closest(
                "#sidebar nav button"
            );

            if (!botao || !estado.telaAtiva) {
                return;
            }

            if (estado.salvando) {
                evento.preventDefault();

                evento.stopImmediatePropagation();

                return;
            }

            if (
                existemAlteracoes() &&
                !confirm(
                    "Descartar alterações não salvas?"
                )
            ) {
                evento.preventDefault();

                evento.stopImmediatePropagation();

                return;
            }

            if (botao.id !== "nav-lancar-notas") {
                estado.telaAtiva = false;

                estado.requisicao++;
            }
        },
        true
    );

    /**
     * Intercepta somente a navegação de notas.
     *
     * As demais funcionalidades continuam sendo
     * processadas pelos scripts correspondentes.
     */
    document.addEventListener(
        "click",
        evento => {
            const botao = evento.target.closest(
                "#sidebar nav button"
            );

            if (
                !botao ||
                botao.id !== "nav-lancar-notas"
            ) {
                return;
            }

            evento.preventDefault();

            evento.stopImmediatePropagation();

            carregarTela();
        },
        true
    );
})();