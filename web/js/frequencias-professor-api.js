(() => {
    "use strict";

    const API_URL = "http://localhost:3000/api";

    const estado = {
        turmas: [],
        alunos: [],
        aulas: [],
        frequencias: [],
        alteracoes: new Map(),
        idTurma: "",
        idTurmaDisciplina: "",
        idAula: "",
        requisicao: 0,
        telaAtiva: false,
        salvando: false
    };

    /**
     * Protege os valores inseridos no HTML.
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
     * Compara identificadores numéricos.
     */
    function mesmoId(primeiro, segundo) {
        return Number(primeiro) === Number(segundo);
    }

    /**
     * Formata datas acadêmicas sem alterar o dia
     * em razão do fuso horário do navegador.
     */
    function formatarData(valor) {
        if (!valor) {
            return "Não informada";
        }

        const [ano, mes, dia] =
            String(valor).slice(0, 10).split("-");

        return `${dia}/${mes}/${ano}`;
    }

    /**
     * Executa requisições autenticadas à API.
     */
    async function requisitar(endpoint, opcoes = {}) {
        const token = sessionStorage.getItem(
            "techacademy_token"
        );

        if (!token) {
            window.location.href = "index.html";

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

            window.location.href = "index.html";
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
     * Consulta os dados acadêmicos do professor.
     *
     * As requisições são sequenciais para reduzir
     * a concorrência no pool de conexões.
     */
    async function consultarDadosAcademicos() {
        const turmas = await requisitar(
            "/professores/me/turmas"
        );

        const alunos = await requisitar(
            "/professores/me/alunos"
        );

        const aulas = await requisitar(
            "/professores/me/aulas"
        );

        const frequencias = await requisitar(
            "/professores/me/frequencias"
        );

        return {
            turmas: turmas.turmas,
            alunos: alunos.alunos,
            aulas: aulas.aulas,
            frequencias: frequencias.frequencias
        };
    }

    /**
     * Obtém as turmas sem duplicar os vínculos
     * referentes às diferentes disciplinas.
     */
    function obterTurmas() {
        const turmas = new Map();

        estado.turmas.forEach(turma => {
            turmas.set(
                Number(turma.idTurma),
                turma
            );
        });

        return [...turmas.values()].sort(
            (primeira, segunda) =>
                primeira.nomeTurma.localeCompare(
                    segunda.nomeTurma,
                    "pt-BR"
                )
        );
    }

    /**
     * Obtém as disciplinas da turma selecionada.
     */
    function obterDisciplinas() {
        return estado.turmas.filter(
            disciplina => mesmoId(
                disciplina.idTurma,
                estado.idTurma
            )
        );
    }

    /**
     * Obtém exclusivamente as aulas realizadas
     * da disciplina selecionada.
     */
    function obterAulas() {
        return estado.aulas
            .filter(aula =>
                mesmoId(
                    aula.idTurmaDisciplina,
                    estado.idTurmaDisciplina
                ) &&
                aula.realizada === true
            )
            .sort(
                (primeira, segunda) =>
                    String(segunda.dataAula)
                        .localeCompare(
                            String(primeira.dataAula)
                        )
            );
    }

    /**
     * Obtém os alunos atualmente matriculados
     * na turma selecionada.
     */
    function obterAlunos() {
        return estado.alunos
            .filter(aluno =>
                mesmoId(
                    aluno.idTurma,
                    estado.idTurma
                ) &&
                aluno.situacaoMatricula === "Ativa"
            )
            .sort(
                (primeiro, segundo) =>
                    primeiro.nomeCompleto.localeCompare(
                        segundo.nomeCompleto,
                        "pt-BR"
                    )
            );
    }

    /**
     * Recupera uma frequência registrada no banco.
     */
    function obterFrequencia(idMatricula) {
        return estado.frequencias.find(
            frequencia =>
                mesmoId(
                    frequencia.idMatricula,
                    idMatricula
                ) &&
                mesmoId(
                    frequencia.idAula,
                    estado.idAula
                )
        );
    }

    /**
     * Recupera a situação atual da frequência.
     *
     * Uma frequência não registrada permanece
     * pendente e não é considerada uma falta.
     */
    function obterSituacao(idMatricula) {
        const identificador = Number(idMatricula);

        if (estado.alteracoes.has(identificador)) {
            return estado.alteracoes.get(
                identificador
            );
        }

        const frequencia = obterFrequencia(
            identificador
        );

        return frequencia
            ? frequencia.presente
            : null;
    }

    /**
     * Registra uma alteração temporária.
     *
     * Alterações iguais ao registro original
     * são removidas da lista de pendências.
     */
    function alterarFrequencia(idMatricula, presente) {
        const identificador = Number(idMatricula);

        const original = obterFrequencia(
            identificador
        );

        if (
            original &&
            original.presente === presente
        ) {
            estado.alteracoes.delete(
                identificador
            );

        } else {
            estado.alteracoes.set(
                identificador,
                presente
            );
        }
    }

    /**
     * Calcula os totais da aula selecionada.
     */
    function calcularTotais() {
        const alunos = obterAlunos();

        const totais = {
            alunos: alunos.length,
            presencas: 0,
            faltas: 0,
            pendentes: 0
        };

        alunos.forEach(aluno => {
            const situacao = obterSituacao(
                aluno.idMatricula
            );

            if (situacao === true) {
                totais.presencas++;

            } else if (situacao === false) {
                totais.faltas++;

            } else {
                totais.pendentes++;
            }
        });

        return totais;
    }

    /**
     * Renderiza uma opção dos seletores.
     */
    function renderizarOpcao(valor, texto, selecionado) {
        return `
            <option
                value="${escaparHtml(valor)}"
                ${mesmoId(valor, selecionado)
                ? "selected"
                : ""}
            >
                ${escaparHtml(texto)}
            </option>
        `;
    }

    /**
     * Renderiza os filtros acadêmicos.
     */
    function renderizarFiltros() {
        const turmas = obterTurmas()
            .map(turma =>
                renderizarOpcao(
                    turma.idTurma,
                    turma.nomeTurma,
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

        const aulas = obterAulas()
            .map(aula =>
                renderizarOpcao(
                    aula.idAula,
                    `${formatarData(aula.dataAula)} - ${aula.conteudo || "Aula"}`,
                    estado.idAula
                )
            )
            .join("");

        return `
            <section
                class="bg-white p-6 rounded-2xl
                       border border-blue-100 shadow-sm"
            >
                <h2 class="font-bold text-slate-800 mb-5">
                    <i
                        class="fa-solid fa-filter
                               text-blue-600 mr-2"
                    ></i>

                    Seleção de Turma, Disciplina e Aula
                </h2>

                <div
                    class="grid grid-cols-1
                           lg:grid-cols-3 gap-4"
                >
                    <div>
                        <label
                            for="frequencia-turma"
                            class="block text-sm font-bold
                                   text-slate-700 mb-1"
                        >
                            1º Passo: Selecione a Turma
                        </label>

                        <select
                            id="frequencia-turma"
                            class="w-full p-3 bg-white
                                   border border-slate-200
                                   rounded-xl outline-none
                                   focus:border-blue-500"
                        >
                            <option value="">
                                -- Escolha uma Turma --
                            </option>

                            ${turmas}
                        </select>
                    </div>

                    <div>
                        <label
                            for="frequencia-disciplina"
                            class="block text-sm font-bold
                                   text-slate-700 mb-1"
                        >
                            2º Passo: Selecione a Disciplina
                        </label>

                        <select
                            id="frequencia-disciplina"
                            ${estado.idTurma ? "" : "disabled"}
                            class="w-full p-3 bg-white
                                   border border-slate-200
                                   rounded-xl outline-none
                                   focus:border-blue-500
                                   disabled:opacity-50"
                        >
                            <option value="">
                                -- Escolha uma Disciplina --
                            </option>

                            ${disciplinas}
                        </select>
                    </div>

                    <div>
                        <label
                            for="frequencia-aula"
                            class="block text-sm font-bold
                                   text-slate-700 mb-1"
                        >
                            3º Passo: Selecione a Aula
                        </label>

                        <select
                            id="frequencia-aula"
                            ${estado.idTurmaDisciplina
                ? ""
                : "disabled"}
                            class="w-full p-3 bg-white
                                   border border-slate-200
                                   rounded-xl outline-none
                                   focus:border-blue-500
                                   disabled:opacity-50"
                        >
                            <option value="">
                                -- Escolha uma Aula --
                            </option>

                            ${aulas}
                        </select>
                    </div>
                </div>

                <p class="text-xs text-slate-500 mt-4">
                    Somente aulas realizadas estão disponíveis
                    para o lançamento de frequências.
                </p>
            </section>
        `;
    }

    /**
     * Renderiza um cartão de resumo.
     */
    function renderizarIndicador(
        titulo,
        valor,
        icone,
        classe
    ) {
        return `
            <article
                class="bg-white border border-blue-100
                       rounded-2xl p-5 shadow-sm"
            >
                <div class="flex items-center gap-3">
                    <div
                        class="w-11 h-11 rounded-xl
                               flex items-center
                               justify-center ${classe}"
                    >
                        <i class="${icone}"></i>
                    </div>

                    <div>
                        <p class="text-xs text-slate-500">
                            ${titulo}
                        </p>

                        <p class="text-2xl font-bold text-slate-800">
                            ${valor}
                        </p>
                    </div>
                </div>
            </article>
        `;
    }

    /**
     * Renderiza os indicadores da aula.
     */
    function renderizarIndicadores() {
        const totais = calcularTotais();

        return `
            <div
                class="grid grid-cols-2
                       xl:grid-cols-4 gap-4"
            >
                ${renderizarIndicador(
            "Total de alunos",
            totais.alunos,
            "fa-solid fa-users",
            "bg-blue-50 text-blue-600"
        )}

                ${renderizarIndicador(
            "Presenças",
            totais.presencas,
            "fa-solid fa-user-check",
            "bg-emerald-50 text-emerald-600"
        )}

                ${renderizarIndicador(
            "Faltas",
            totais.faltas,
            "fa-solid fa-user-xmark",
            "bg-red-50 text-red-600"
        )}

                ${renderizarIndicador(
            "Pendentes",
            totais.pendentes,
            "fa-solid fa-clock",
            "bg-amber-50 text-amber-600"
        )}
            </div>
        `;
    }

    /**
     * Renderiza a identificação da frequência.
     */
    function renderizarSituacao(situacao) {
        let texto;
        let classe;

        if (situacao === true) {
            texto = "Presente";

            classe =
                "bg-emerald-50 text-emerald-700 border-emerald-200";

        } else if (situacao === false) {
            texto = "Falta";

            classe =
                "bg-red-50 text-red-700 border-red-200";

        } else {
            texto = "Pendente";

            classe =
                "bg-amber-50 text-amber-700 border-amber-200";
        }

        return `
            <span
                class="inline-flex px-3 py-1 rounded-full
                       text-xs font-bold border ${classe}"
            >
                ${texto}
            </span>
        `;
    }

    /**
     * Renderiza um botão de presença ou falta.
     */
    function renderizarBotaoFrequencia(
        idMatricula,
        presente,
        situacao
    ) {
        const selecionado = situacao === presente;

        const texto = presente
            ? "Presente"
            : "Falta";

        const icone = presente
            ? "fa-check"
            : "fa-xmark";

        let classe;

        if (presente) {
            classe = selecionado
                ? "bg-emerald-600 text-white border-emerald-600"
                : "bg-white text-emerald-600 border-emerald-200 hover:bg-emerald-50";

        } else {
            classe = selecionado
                ? "bg-red-600 text-white border-red-600"
                : "bg-white text-red-600 border-red-200 hover:bg-red-50";
        }

        return `
            <button
                type="button"
                data-frequencia="${presente}"
                data-matricula="${idMatricula}"
                aria-pressed="${selecionado}"
                class="px-4 py-2 rounded-lg border
                       text-xs font-bold transition ${classe}"
            >
                <i class="fa-solid ${icone} mr-1"></i>

                ${texto}
            </button>
        `;
    }

    /**
     * Renderiza uma linha da tabela de frequências.
     */
    function renderizarAluno(aluno) {
        const situacao = obterSituacao(
            aluno.idMatricula
        );

        const alterado = estado.alteracoes.has(
            Number(aluno.idMatricula)
        );

        return `
            <tr
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

                <td class="px-4 py-4 text-center">
                    ${renderizarSituacao(situacao)}
                </td>

                <td class="px-4 py-4">
                    <div
                        class="flex items-center
                               justify-center gap-2"
                    >
                        ${renderizarBotaoFrequencia(
            aluno.idMatricula,
            true,
            situacao
        )}

                        ${renderizarBotaoFrequencia(
            aluno.idMatricula,
            false,
            situacao
        )}
                    </div>
                </td>

                <td
                    class="px-4 py-4 text-center
                           text-xs text-slate-500"
                >
                    ${alterado
                ? "Não publicado"
                : situacao === null
                    ? "Aguardando lançamento"
                    : "Registrado"}
                </td>
            </tr>
        `;
    }

    /**
     * Renderiza uma mensagem informativa.
     */
    function renderizarAviso(mensagem) {
        return `
            <div
                class="border border-blue-100
                       rounded-2xl p-8 text-center
                       text-slate-500"
            >
                <i
                    class="fa-solid fa-circle-info
                           text-blue-600 text-2xl mb-3"
                ></i>

                <p>${escaparHtml(mensagem)}</p>
            </div>
        `;
    }

    /**
     * Renderiza o diário de frequências.
     */
    function renderizarDiario() {
        if (!estado.idTurmaDisciplina) {
            return renderizarAviso(
                "Selecione a turma e a disciplina."
            );
        }

        if (!obterAulas().length) {
            return renderizarAviso(
                "Não existem aulas realizadas nesta disciplina."
            );
        }

        if (!estado.idAula) {
            return renderizarAviso(
                "Selecione uma aula para registrar as frequências."
            );
        }

        const alunos = obterAlunos();

        if (!alunos.length) {
            return renderizarAviso(
                "Nenhum aluno ativo nesta turma."
            );
        }

        const aula = obterAulas().find(
            registro => mesmoId(
                registro.idAula,
                estado.idAula
            )
        );

        return `
            <div class="space-y-5">
                <div id="indicadores-frequencia">
                    ${renderizarIndicadores()}
                </div>

                <section
                    class="bg-white p-6 rounded-2xl
                           border border-blue-100 shadow-sm"
                >
                    <div
                        class="flex flex-wrap items-center
                               justify-between gap-4 mb-5"
                    >
                        <div>
                            <h2 class="font-bold text-slate-800">
                                <i
                                    class="fa-solid fa-clipboard-list
                                           text-blue-600 mr-2"
                                ></i>

                                Diário de Frequências
                            </h2>

                            <p class="text-xs text-slate-500 mt-2">
                                ${formatarData(aula.dataAula)}
                                -
                                ${escaparHtml(aula.conteudo)}
                            </p>
                        </div>

                        <span
                            id="status-frequencias"
                            role="status"
                            aria-live="polite"
                            class="text-xs font-bold
                                   text-emerald-700 bg-emerald-50
                                   px-3 py-2 rounded-lg"
                        >
                            Frequências sincronizadas.
                        </span>
                    </div>

                    <div class="flex flex-wrap gap-3 mb-5">
                        <button
                            type="button"
                            id="btn-todos-presentes"
                            class="px-4 py-2 rounded-lg
                                   border border-emerald-200
                                   text-emerald-700 font-bold
                                   text-xs hover:bg-emerald-50"
                        >
                            <i class="fa-solid fa-check-double mr-1"></i>

                            Marcar todos presentes
                        </button>
                    </div>

                    <div class="overflow-x-auto">
                        <table
                            class="w-full min-w-[650px]
                                   border-collapse text-sm"
                        >
                            <thead
                                class="bg-blue-50 text-slate-700
                                       border-b border-blue-100"
                            >
                                <tr>
                                    <th
                                        scope="col"
                                        class="px-4 py-4 text-left"
                                    >
                                        ALUNO
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-4 py-4 text-center"
                                    >
                                        SITUAÇÃO
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-4 py-4 text-center"
                                    >
                                        FREQUÊNCIA
                                    </th>

                                    <th
                                        scope="col"
                                        class="px-4 py-4 text-center"
                                    >
                                        REGISTRO
                                    </th>
                                </tr>
                            </thead>

                            <tbody id="corpo-frequencias">
                                ${alunos.map(renderizarAluno).join("")}
                            </tbody>
                        </table>
                    </div>

                    <p class="text-xs text-slate-500 mt-4">
                        Frequências pendentes não são
                        consideradas faltas.
                    </p>
                </section>

                <section
                    class="bg-white p-5 rounded-2xl
                           border border-blue-100 shadow-sm"
                >
                    <div
                        class="flex flex-wrap
                               justify-end gap-3"
                    >
                        <button
                            type="button"
                            id="btn-atualizar-frequencias"
                            class="px-5 py-2.5 border
                                   border-slate-200 rounded-xl
                                   font-bold text-slate-600
                                   hover:bg-slate-50"
                        >
                            Atualizar
                        </button>

                        <button
                            type="button"
                            id="btn-salvar-frequencias"
                            class="px-5 py-2.5 bg-brand-blue
                                   text-white rounded-xl
                                   font-bold hover:opacity-90
                                   disabled:opacity-50"
                        >
                            <i class="fa-solid fa-floppy-disk mr-2"></i>

                            Salvar e publicar frequências
                        </button>
                    </div>
                </section>
            </div>
        `;
    }

    /**
     * Exibe mensagens de sincronização e erros.
     */
    function exibirMensagem(mensagem, erro = false) {
        const elemento = document.getElementById(
            "status-frequencias"
        );

        if (!elemento) {
            return;
        }

        elemento.textContent = mensagem;

        elemento.className = erro
            ? "text-xs font-bold text-red-700 bg-red-50 px-3 py-2 rounded-lg"
            : "text-xs font-bold text-emerald-700 bg-emerald-50 px-3 py-2 rounded-lg";
    }

    /**
     * Atualiza a tabela e os indicadores sem
     * recriar os seletores acadêmicos.
     */
    function atualizarDiario() {
        const corpo = document.getElementById(
            "corpo-frequencias"
        );

        const indicadores = document.getElementById(
            "indicadores-frequencia"
        );

        if (corpo) {
            corpo.innerHTML = obterAlunos()
                .map(renderizarAluno)
                .join("");
        }

        if (indicadores) {
            indicadores.innerHTML =
                renderizarIndicadores();
        }

        exibirMensagem(
            estado.alteracoes.size
                ? "Alterações aguardando publicação."
                : "Frequências sincronizadas."
        );
    }

    /**
     * Verifica se existem alterações não publicadas.
     */
    function confirmarDescarte() {
        if (estado.salvando) {
            return false;
        }

        if (!estado.alteracoes.size) {
            return true;
        }

        return window.confirm(
            "Descartar as frequências não publicadas?"
        );
    }

    /**
     * Configura os eventos dos seletores.
     */
    function configurarSeletores() {
        const turma = document.getElementById(
            "frequencia-turma"
        );

        const disciplina = document.getElementById(
            "frequencia-disciplina"
        );

        const aula = document.getElementById(
            "frequencia-aula"
        );

        turma.value = estado.idTurma;

        disciplina.value = estado.idTurmaDisciplina;

        aula.value = estado.idAula;

        turma.addEventListener(
            "change",
            evento => {
                if (!confirmarDescarte()) {
                    evento.target.value =
                        estado.idTurma;

                    return;
                }

                estado.alteracoes.clear();

                estado.idTurma =
                    evento.target.value;

                estado.idTurmaDisciplina = "";

                estado.idAula = "";

                renderizarTela();
            }
        );

        disciplina.addEventListener(
            "change",
            evento => {
                if (!confirmarDescarte()) {
                    evento.target.value =
                        estado.idTurmaDisciplina;

                    return;
                }

                estado.alteracoes.clear();

                estado.idTurmaDisciplina =
                    evento.target.value;

                estado.idAula = "";

                renderizarTela();
            }
        );

        aula.addEventListener(
            "change",
            evento => {
                if (!confirmarDescarte()) {
                    evento.target.value =
                        estado.idAula;

                    return;
                }

                estado.alteracoes.clear();

                estado.idAula =
                    evento.target.value;

                renderizarTela();
            }
        );
    }

    /**
     * Atualiza o registro local somente após
     * a confirmação do salvamento pela API.
     */
    function sincronizarFrequencia(resultado) {
        const registro = estado.frequencias.find(
            frequencia =>
                mesmoId(
                    frequencia.idMatricula,
                    resultado.idMatricula
                ) &&
                mesmoId(
                    frequencia.idAula,
                    resultado.idAula
                )
        );

        if (registro) {
            Object.assign(
                registro,
                resultado
            );

        } else {
            estado.frequencias.push(
                resultado
            );
        }

        estado.alteracoes.delete(
            Number(resultado.idMatricula)
        );
    }

    /**
     * Publica as frequências alteradas.
     *
     * Cada registro é salvo individualmente,
     * preservando as alterações que ainda não
     * foram confirmadas pela API.
     */
    async function salvarFrequencias() {
        if (
            estado.salvando ||
            !estado.idAula
        ) {
            return;
        }

        if (!estado.alteracoes.size) {
            exibirMensagem(
                "Nenhuma alteração para salvar."
            );

            return;
        }

        estado.salvando = true;

        const controles = [
            ...document.querySelectorAll(
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

        const botao = document.getElementById(
            "btn-salvar-frequencias"
        );

        botao.textContent = "Salvando...";

        let totalSalvas = 0;

        try {
            const alteracoes = [
                ...estado.alteracoes.entries()
            ];

            for (const [idMatricula, presente] of alteracoes) {
                const resultado = await requisitar(
                    "/professores/me/frequencias",
                    {
                        method: "POST",

                        body: JSON.stringify({
                            idMatricula,

                            idAula: Number(
                                estado.idAula
                            ),

                            presente
                        })
                    }
                );

                sincronizarFrequencia(
                    resultado
                );

                totalSalvas++;
            }

            // Recupera os registros oficiais do banco.
            const consulta = await requisitar(
                "/professores/me/frequencias"
            );

            estado.frequencias =
                consulta.frequencias;

            renderizarTela();

            exibirMensagem(
                `${totalSalvas} frequência(s) publicada(s) com sucesso.`
            );

        } catch (erro) {
            atualizarDiario();

            const mensagem = totalSalvas > 0
                ? `${totalSalvas} frequência(s) salva(s). ${erro.message}`
                : erro.message;

            exibirMensagem(
                mensagem,
                true
            );

        } finally {
            estado.salvando = false;

            controles.forEach(
                (controle, indice) => {
                    controle.disabled =
                        estadosAnteriores[indice];
                }
            );

            botao.innerHTML = `
                <i class="fa-solid fa-floppy-disk mr-2"></i>

                Salvar e publicar frequências
            `;
        }
    }

    /**
     * Configura os eventos do diário de frequências.
     */
    function configurarEventos() {
        configurarSeletores();

        const corpo = document.getElementById(
            "corpo-frequencias"
        );

        // A delegação de eventos preserva o funcionamento
        // dos botões após atualizar as linhas da tabela.
        corpo?.addEventListener(
            "click",
            evento => {
                const botao = evento.target.closest(
                    "[data-frequencia]"
                );

                if (!botao || estado.salvando) {
                    return;
                }

                alterarFrequencia(
                    Number(botao.dataset.matricula),
                    botao.dataset.frequencia === "true"
                );

                atualizarDiario();
            }
        );

        document.getElementById(
            "btn-todos-presentes"
        )?.addEventListener(
            "click",
            () => {
                if (estado.salvando) {
                    return;
                }

                obterAlunos().forEach(aluno => {
                    alterarFrequencia(
                        aluno.idMatricula,
                        true
                    );
                });

                atualizarDiario();
            }
        );

        document.getElementById(
            "btn-salvar-frequencias"
        )?.addEventListener(
            "click",
            salvarFrequencias
        );

        document.getElementById(
            "btn-atualizar-frequencias"
        )?.addEventListener(
            "click",
            () => {
                if (!confirmarDescarte()) {
                    return;
                }

                estado.alteracoes.clear();

                carregarTela();
            }
        );
    }

    /**
     * Renderiza a interface completa.
     */
    function renderizarTela() {
        const main = document.getElementById(
            "main-content-area"
        );

        main.innerHTML = `
            <div class="space-y-5">
                ${renderizarFiltros()}

                ${renderizarDiario()}
            </div>
        `;

        configurarEventos();
    }

    /**
     * Identifica o botão de navegação de frequências.
     *
     * A identificação também considera o texto
     * para manter compatibilidade com o painel.
     */
    function ehBotaoFrequencias(botao) {
        if (!botao) {
            return false;
        }

        return (
            botao.id === "nav-lancar-faltas" ||
            botao.id === "nav-lancar-frequencias" ||
            botao.textContent.includes(
                "Lançar & Consultar Faltas"
            )
        );
    }

    /**
     * Atualiza a navegação e o cabeçalho do painel.
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

            if (ehBotaoFrequencias(botao)) {
                botao.classList.remove(
                    "text-slate-300"
                );

                botao.classList.add(
                    "bg-brand-blue",
                    "text-white"
                );
            }
        });

        const titulo = document.getElementById(
            "page-title"
        );

        const subtitulo = document.getElementById(
            "page-subtitle"
        );

        if (titulo) {
            titulo.textContent =
                "Lançar & Consultar Faltas";
        }

        if (subtitulo) {
            subtitulo.textContent =
                "Gerencie as presenças e faltas dos alunos.";
        }
    }

    /**
     * Verifica se os vínculos selecionados continuam
     * disponíveis após atualizar os dados.
     */
    function validarSelecao() {
        const turmaValida = obterTurmas().some(
            turma => mesmoId(
                turma.idTurma,
                estado.idTurma
            )
        );

        if (!turmaValida) {
            estado.idTurma = "";

            estado.idTurmaDisciplina = "";

            estado.idAula = "";

            return;
        }

        const disciplinaValida =
            obterDisciplinas().some(
                disciplina => mesmoId(
                    disciplina.idTurmaDisciplina,
                    estado.idTurmaDisciplina
                )
            );

        if (!disciplinaValida) {
            estado.idTurmaDisciplina = "";

            estado.idAula = "";

            return;
        }

        const aulaValida = obterAulas().some(
            aula => mesmoId(
                aula.idAula,
                estado.idAula
            )
        );

        if (!aulaValida) {
            estado.idAula = "";
        }
    }

    /**
     * Carrega a tela com os dados oficiais da API.
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

                Carregando frequências dos alunos...
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
     * Protege alterações ao fechar ou atualizar
     * a página do professor.
     */
    window.addEventListener(
        "beforeunload",
        evento => {
            if (
                estado.telaAtiva &&
                (
                    estado.salvando ||
                    estado.alteracoes.size > 0
                )
            ) {
                evento.preventDefault();

                evento.returnValue = "";
            }
        }
    );

    /**
     * Protege a navegação enquanto existem
     * alterações ainda não publicadas.
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

            if (!confirmarDescarte()) {
                evento.preventDefault();

                evento.stopImmediatePropagation();

                return;
            }

            estado.alteracoes.clear();

            if (!ehBotaoFrequencias(botao)) {
                estado.telaAtiva = false;

                estado.requisicao++;
            }
        },
        true
    );

    /**
     * Intercepta exclusivamente a navegação
     * de lançamento e consulta de frequências.
     */
    document.addEventListener(
        "click",
        evento => {
            const botao = evento.target.closest(
                "#sidebar nav button"
            );

            if (!ehBotaoFrequencias(botao)) {
                return;
            }

            evento.preventDefault();

            evento.stopImmediatePropagation();

            carregarTela();
        },
        true
    );
})();