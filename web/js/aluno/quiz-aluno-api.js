/**
 * TechAcademy Web
 * Integração dos questionários e da gamificação do aluno.
 *
 * Responsável pela consulta dos questionários,
 * organização por situação, envio das respostas,
 * apresentação dos resultados e acompanhamento
 * do progresso acadêmico.
 *
 * A correção das respostas e a atribuição de XP
 * são realizadas exclusivamente pela API.
 */
(() => {
    "use strict";

    const API_URL = "http://localhost:3000/api";

    /**
     * Estado de navegação e controle das requisições.
     */
    const estado = {
        tela: null,
        requisicao: 0,
        enviando: false,
        disciplinaOrigem: null
    };

    /**
     * Protege os dados exibidos contra injeção de HTML.
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
     * Preenche os templates existentes no HTML.
     *
     * @param {string} numero Identificador do template.
     * @param {Array} valores Valores utilizados no template.
     * @returns {string} HTML preenchido.
     */
    function renderizarTemplate(numero, valores = []) {
        const identificador =
            `ta-aluno-fragment-${numero}`;

        const template = document.getElementById(
            identificador
        );

        if (!template) {
            throw new Error(
                `Template não encontrado: ${identificador}`
            );
        }

        return template.innerHTML.replace(
            /__TA_SLOT_(\d+)__/g,
            (_, indice) => valores[Number(indice)] ?? ""
        );
    }

    /**
     * Realiza requisições autenticadas à API REST.
     *
     * @param {string} caminho Endpoint da API.
     * @param {string} metodo Método HTTP.
     * @param {Object|null} dados Corpo da requisição.
     * @returns {Promise<Object>} Resposta da API.
     */
    async function requisitar(
        caminho,
        metodo = "GET",
        dados = null
    ) {
        const token = sessionStorage.getItem(
            "techacademy_token"
        );

        if (!token) {
            window.location.replace("../../index.html");

            throw new Error("Sessão não encontrada.");
        }

        const configuracao = {
            method: metodo,
            cache: "no-store",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`
            }
        };

        if (dados !== null) {
            configuracao.body = JSON.stringify(dados);
        }

        const resposta = await fetch(
            `${API_URL}${caminho}`,
            configuracao
        );

        const resultado = await resposta.json()
            .catch(() => ({}));

        // Encerra a sessão quando o token é inválido.
        if (resposta.status === 401) {
            sessionStorage.removeItem(
                "techacademy_token"
            );

            sessionStorage.removeItem(
                "usuario_logado"
            );

            window.location.replace("../../index.html");
        }

        if (!resposta.ok) {
            const erro = new Error(
                resultado.mensagem ||
                "Não foi possível concluir a operação."
            );

            erro.status = resposta.status;

            throw erro;
        }

        return resultado;
    }

    /**
     * Retorna a área principal do painel.
     *
     * @returns {HTMLElement} Área de conteúdo.
     */
    function obterAreaPrincipal() {
        return document.getElementById(
            "main-content-area"
        );
    }

    /**
     * Atualiza o título e o subtítulo da página.
     *
     * @param {string} titulo Título da página.
     * @param {string} subtitulo Descrição da página.
     */
    function atualizarCabecalho(titulo, subtitulo) {
        document.getElementById(
            "page-title"
        ).textContent = titulo;

        document.getElementById(
            "page-subtitle"
        ).textContent = subtitulo;
    }

    /**
     * Destaca a opção de questionários no menu lateral.
     */
    function selecionarMenuQuizzes() {
        const botoes = document.querySelectorAll(
            "#sidebar nav button"
        );

        for (const botao of botoes) {
            botao.classList.remove(
                "bg-brand-blue",
                "text-white",
                "shadow-md"
            );

            botao.classList.add("text-slate-300");
        }

        const botaoQuizzes = document.getElementById(
            "nav-quizzes"
        );

        if (!botaoQuizzes) {
            return;
        }

        botaoQuizzes.classList.remove(
            "text-slate-300"
        );

        botaoQuizzes.classList.add(
            "bg-brand-blue",
            "text-white",
            "shadow-md"
        );
    }

    /**
     * Exibe uma mensagem na área principal.
     *
     * @param {string} mensagem Texto apresentado.
     * @param {boolean} erro Indica uma mensagem de erro.
     */
    function exibirMensagem(mensagem, erro = false) {
        const area = obterAreaPrincipal();

        const elemento = document.createElement("div");

        elemento.className = erro
            ? "p-5 rounded-xl bg-red-50 text-red-700"
            : "p-5 rounded-xl bg-white text-slate-500";

        elemento.textContent = mensagem;

        area.replaceChildren(elemento);
    }

    /**
     * Formata as datas recebidas da API.
     *
     * @param {string} valor Data original.
     * @returns {string} Data no formato brasileiro.
     */
    function formatarData(valor) {
        if (!valor) {
            return "Não informada";
        }

        const data = new Date(valor);

        if (Number.isNaN(data.getTime())) {
            return "Não informada";
        }

        return data.toLocaleDateString("pt-BR");
    }

    /**
     * Atualiza o widget de gamificação com os
     * dados oficiais retornados pelo servidor.
     *
     * @param {Object} progresso Progresso do aluno.
     */
    function renderizarProgresso(progresso) {
        const widgetXP = document.getElementById(
            "widget-xp"
        );

        const widgetStreak = document.getElementById(
            "widget-streak"
        );

        if (!widgetXP || !widgetStreak) {
            return;
        }

        const xpTotal = Number(
            progresso.xpTotal ?? 0
        );

        const nivel = Number(
            progresso.nivel ?? 1
        );

        const nivelMaximo = Number(
            progresso.nivelMaximo ?? 50
        );

        const percentual = Math.min(
            100,
            Math.max(
                0,
                Number(
                    progresso.percentualProgresso ?? 0
                )
            )
        );

        // Atualiza o total de XP acumulado.
        widgetXP.innerHTML = renderizarTemplate(
            "026",
            [xpTotal.toLocaleString("pt-BR")]
        );

        // Atualiza a identificação do nível atual.
        const rotuloNivel =
            widgetStreak.previousElementSibling;

        if (rotuloNivel) {
            rotuloNivel.textContent = "Nível Atual";
        }

        widgetStreak.className =
            "text-lg font-extrabold text-blue-400 " +
            "flex items-center justify-center gap-1";

        widgetStreak.innerHTML = `
            <i class="fa-solid fa-star"></i>
            <span>${nivel}</span>
        `;

        const container =
            widgetXP.parentElement.parentElement;

        let barra = document.getElementById(
            "progresso-nivel-aluno"
        );

        // Cria a barra de progresso quando necessário.
        if (!barra) {
            barra = document.createElement("div");

            barra.id = "progresso-nivel-aluno";
            barra.className = "mt-4 space-y-2";

            container.after(barra);
        }

        const descricao = progresso.atingiuNivelMaximo
            ? "Nível máximo alcançado!"
            : `${progresso.xpProximoNivel} XP ` +
            `para o nível ${nivel + 1}`;

        barra.innerHTML = `
            <div class="flex justify-between
                        text-[10px] text-slate-400">

                <span>Nível ${nivel} de ${nivelMaximo}</span>

                <span>${percentual}%</span>
            </div>

            <div class="w-full h-2 bg-slate-900
                        rounded-full overflow-hidden"
                 role="progressbar"
                 aria-label="Progresso do nível"
                 aria-valuemin="0"
                 aria-valuemax="100"
                 aria-valuenow="${percentual}">

                <div class="h-full bg-blue-500
                            rounded-full transition-all"
                     style="width: ${percentual}%">
                </div>

            </div>

            <p class="text-[10px] text-slate-400
                      text-center">

                ${escaparHTML(descricao)}

            </p>
        `;
    }

    /**
     * Consulta o XP acumulado no Azure SQL.
     *
     * @returns {Promise<void>}
     */
    async function carregarProgresso() {
        try {
            const progresso = await requisitar(
                "/alunos/me/xp"
            );

            renderizarProgresso(progresso);

        } catch (erro) {
            console.error(
                "Erro ao consultar XP:",
                erro
            );

            const widget = document.getElementById(
                "widget-xp"
            );

            if (widget) {
                widget.textContent = "Indisponível";
            }
        }
    }

    /**
     * Renderiza um cartão da listagem geral.
     *
     * A situação, o resultado e a recompensa são
     * definidos pelos dados retornados pela API.
     *
     * @param {Object} quiz Questionário acadêmico.
     * @returns {string} HTML do cartão.
     */
    function renderizarCartaoQuiz(quiz) {
        const concluido = Boolean(
            quiz.respondido
        );

        const acertos = Number(
            quiz.acertos ?? 0
        );

        const totalPerguntas = Number(
            quiz.totalPerguntas ?? 0
        );

        const xpGanho = Number(
            quiz.xpGanho ?? 0
        );

        const xpMaximo = Number(
            quiz.xpMaximo ?? 0
        );

        // Define a descrição conforme a situação.
        const situacao = concluido
            ? `Concluído · ${acertos} de ` +
            `${totalPerguntas} acertos · ` +
            `+${xpGanho} XP`
            : "Disponível para responder";

        const classeSituacao = concluido
            ? "text-emerald-600 font-semibold"
            : "text-amber-600 font-semibold";

        const textoBotao = concluido
            ? "Ver resultado"
            : "Responder quiz";

        // Preserva o template original do dashboard.
        const cartaoHTML = renderizarTemplate(
            "007",
            [
                escaparHTML(quiz.titulo),

                escaparHTML(
                    `${quiz.nomeDisciplina} · ` +
                    `${quiz.nomeTurma}`
                ),

                quiz.totalPerguntas == null
                    ? "Consulte as"
                    : totalPerguntas,

                xpMaximo,

                escaparHTML(
                    formatarData(quiz.dataCriacao)
                ),

                classeSituacao,

                escaparHTML(situacao),

                escaparHTML(
                    quiz.idDisciplina ?? ""
                ),

                escaparHTML(quiz.idQuiz),

                textoBotao
            ]
        );

        /*
         * Personaliza o botão conforme a situação
         * sem modificar o template do dashboard.
         */
        const template = document.createElement(
            "template"
        );

        template.innerHTML = cartaoHTML;

        const botao = template.content.querySelector(
            "[data-abrir-quiz]"
        );

        if (botao) {
            botao.classList.remove(
                "quiz-access-button",
                "quiz-access-button--standard"
            );

            botao.classList.add(
                "transition",
                "shrink-0"
            );

            if (concluido) {
                botao.classList.add(
                    "bg-emerald-600",
                    "hover:bg-emerald-700"
                );

            } else {
                botao.classList.add(
                    "bg-brand-blue",
                    "hover:bg-brand-navy"
                );
            }
        }

        return template.innerHTML;
    }

    /**
     * Renderiza um grupo de questionários.
     *
     * Apresenta o título, a quantidade de registros
     * e os cartões correspondentes à situação.
     *
     * @param {Array} quizzes Questionários do grupo.
     * @param {boolean} concluidos Situação do grupo.
     * @returns {string} HTML da seção.
     */
    function renderizarGrupoQuizzes(
        quizzes,
        concluidos
    ) {
        const quantidade = quizzes.length;

        const titulo = concluidos
            ? `Questionários concluídos (${quantidade})`
            : `Questionários pendentes (${quantidade})`;

        const classeTitulo = concluidos
            ? "text-emerald-700"
            : "text-amber-700";

        const icone = concluidos
            ? "fa-solid fa-circle-check"
            : "fa-solid fa-clock";

        const mensagemVazia = concluidos
            ? "Nenhum questionário concluído."
            : "Nenhum questionário pendente.";

        // Renderiza os cartões pertencentes ao grupo.
        const cartoesHTML = quantidade > 0
            ? quizzes.map(
                quiz => renderizarCartaoQuiz(quiz)
            ).join("")
            : `
                <div class="bg-white p-4 rounded-xl
                            border border-slate-200">

                    <p class="text-sm text-slate-400">
                        ${mensagemVazia}
                    </p>

                </div>
            `;

        // Reutiliza o template de agrupamento do painel.
        return renderizarTemplate(
            "015",
            [
                cartoesHTML,
                classeTitulo,
                icone,
                titulo
            ]
        );
    }

    /**
     * Sincroniza os questionários concluídos com
     * o estado das disciplinas do painel.
     *
     * A API permanece como fonte oficial dos resultados.
     *
     * @param {Array} quizzes Questionários da API.
     */
    function sincronizarQuestionarios(quizzes) {
        for (const quiz of quizzes) {
            if (!quiz.respondido) {
                continue;
            }

            atualizarQuestionarioLocal(
                quiz.idQuiz,
                quiz
            );
        }
    }

    /**
     * Consulta e apresenta todos os questionários
     * disponíveis ao aluno autenticado.
     *
     * Os registros são organizados em dois grupos:
     * questionários pendentes e concluídos.
     *
     * @returns {Promise<void>}
     */
    async function carregarQuizzes() {
        const requisicao = ++estado.requisicao;

        estado.tela = "quizzes";
        estado.disciplinaOrigem = null;

        selecionarMenuQuizzes();

        atualizarCabecalho(
            "Questionários",
            "Responda aos quizzes e evolua seu nível."
        );

        exibirMensagem(
            "Carregando questionários..."
        );

        try {
            const resultado = await requisitar(
                "/alunos/me/quizzes"
            );

            /*
             * Impede que uma requisição antiga
             * sobrescreva uma tela mais recente.
             */
            if (requisicao !== estado.requisicao) {
                return;
            }

            if (!Array.isArray(resultado.quizzes)) {
                throw new Error(
                    "A API retornou uma listagem inválida."
                );
            }

            const quizzes = resultado.quizzes;

            // Sincroniza os resultados com as disciplinas.
            sincronizarQuestionarios(quizzes);

            // Seleciona os questionários ainda não respondidos.
            const quizzesPendentes = quizzes.filter(
                quiz => !quiz.respondido
            );

            // Seleciona os questionários já respondidos.
            const quizzesConcluidos = quizzes.filter(
                quiz => Boolean(quiz.respondido)
            );

            // Renderiza o grupo de questionários pendentes.
            const pendentesHTML = renderizarGrupoQuizzes(
                quizzesPendentes,
                false
            );

            // Renderiza o grupo de questionários concluídos.
            const concluidosHTML = renderizarGrupoQuizzes(
                quizzesConcluidos,
                true
            );

            const area = obterAreaPrincipal();

            /*
             * Os questionários pendentes são exibidos
             * primeiro para facilitar a identificação
             * das atividades que precisam ser realizadas.
             */
            area.innerHTML = `
                <section class="space-y-5"
                         aria-label="Questionários acadêmicos">

                    <p class="text-sm text-slate-500">

                        Acompanhe seus questionários pendentes
                        e consulte os resultados das atividades
                        já concluídas.

                    </p>

                    ${pendentesHTML}

                    ${concluidosHTML}

                </section>
            `;

        } catch (erro) {
            if (requisicao !== estado.requisicao) {
                return;
            }

            exibirMensagem(
                erro.message,
                true
            );
        }
    }

    /**
     * Configura o botão de retorno do questionário.
     *
     * O texto é definido conforme a origem
     * da navegação do aluno.
     */
    function configurarBotaoVoltar() {
        const area = obterAreaPrincipal();

        const botaoVoltar = area.querySelector(
            '[data-action="back-to-discipline"]'
        );

        if (!botaoVoltar) {
            return;
        }

        botaoVoltar.textContent = estado.disciplinaOrigem
            ? "Voltar à disciplina"
            : "Voltar aos questionários";
    }

    /**
     * Retorna à disciplina de origem ou à listagem
     * geral de questionários.
     */
    function voltarOrigemQuiz() {
        const disciplinaOrigem =
            estado.disciplinaOrigem;

        estado.disciplinaOrigem = null;

        /*
         * Retorna à listagem geral quando o quiz
         * foi acessado pelo menu de questionários.
         */
        if (!disciplinaOrigem) {
            carregarQuizzes();

            return;
        }

        estado.tela = null;
        estado.requisicao++;

        const menuDisciplinas = document.getElementById(
            "nav-disciplinas"
        );

        if (!menuDisciplinas) {
            carregarQuizzes();

            return;
        }

        // Reabre a listagem de disciplinas.
        menuDisciplinas.click();

        // Localiza o cartão da disciplina de origem.
        const cartoes = document.querySelectorAll(
            '[data-action="open-discipline"]'
        );

        const cartaoDisciplina = Array.from(cartoes)
            .find(cartao =>
                String(cartao.dataset.disciplinaId) ===
                String(disciplinaOrigem)
            );

        // Utiliza a navegação original do painel.
        if (cartaoDisciplina) {
            cartaoDisciplina.click();
        }
    }

    /**
     * Exibe o resultado oficial de um questionário.
     *
     * @param {Object} resultado Resultado da API.
     */
    function exibirResultado(resultado) {
        estado.tela = "resultado";

        const area = obterAreaPrincipal();

        area.innerHTML = renderizarTemplate(
            "008",
            [
                Number(resultado.acertos ?? 0),

                Number(resultado.totalPerguntas ?? 0),

                Number(resultado.xpGanho ?? 0),

                escaparHTML(
                    estado.disciplinaOrigem ?? ""
                )
            ]
        );

        configurarBotaoVoltar();

        /*
         * Informa ao aluno quando um novo nível
         * é conquistado após concluir o questionário.
         */
        if (resultado.subiuDeNivel) {
            const mensagem = document.createElement("p");

            mensagem.className =
                "mt-4 font-bold text-blue-600";

            mensagem.textContent =
                `Parabéns! Você alcançou o nível ` +
                `${resultado.nivel}!`;

            area.firstElementChild.appendChild(
                mensagem
            );
        }
    }

    /**
     * Renderiza as alternativas de uma pergunta.
     *
     * Nenhuma informação sobre o gabarito é
     * utilizada ou armazenada pelo navegador.
     *
     * @param {Object} pergunta Pergunta acadêmica.
     * @returns {string} HTML das alternativas.
     */
    function renderizarAlternativas(pergunta) {
        return pergunta.alternativas.map(
            alternativa =>
                renderizarTemplate(
                    "012",
                    [
                        Number(
                            pergunta.idPergunta
                        ),

                        Number(
                            alternativa.idAlternativa
                        ),

                        escaparHTML(
                            alternativa.texto
                        )
                    ]
                )
        ).join("");
    }

    /**
     * Renderiza uma pergunta e suas alternativas.
     *
     * @param {Object} pergunta Pergunta acadêmica.
     * @param {number} indice Posição da pergunta.
     * @returns {string} HTML da pergunta.
     */
    function renderizarPergunta(
        pergunta,
        indice
    ) {
        const alternativasHTML =
            renderizarAlternativas(pergunta);

        return renderizarTemplate(
            "011",
            [
                indice + 1,

                escaparHTML(
                    pergunta.enunciado
                ),

                alternativasHTML
            ]
        );
    }

    /**
     * Configura o formulário de resolução do quiz.
     *
     * Impede a execução da lógica antiga que
     * realizava a correção e atribuição local de XP.
     *
     * @param {string} idQuiz Identificador do quiz.
     * @param {Array} perguntas Perguntas apresentadas.
     */
    function configurarFormularioQuiz(
        idQuiz,
        perguntas
    ) {
        const formulario = document.getElementById(
            "form-responder-quiz"
        );

        if (!formulario) {
            throw new Error(
                "Formulário do questionário não encontrado."
            );
        }

        formulario.addEventListener(
            "submit",
            evento => {
                evento.preventDefault();

                evento.stopImmediatePropagation();

                enviarRespostas(
                    formulario,
                    idQuiz,
                    perguntas
                );
            },
            true
        );
    }

    /**
     * Consulta e abre diretamente um questionário.
     *
     * As perguntas são recebidas sem o gabarito.
     * Caso o quiz já tenha sido respondido, apresenta
     * somente o resultado registrado no banco.
     *
     * @param {string} idQuiz Identificador do quiz.
     * @returns {Promise<void>}
     */
    async function abrirQuiz(idQuiz) {
        if (!idQuiz) {
            exibirMensagem(
                "Questionário não identificado.",
                true
            );

            return;
        }

        const requisicao = ++estado.requisicao;

        estado.tela = "quiz";

        exibirMensagem(
            "Carregando perguntas..."
        );

        try {
            const quiz = await requisitar(
                `/alunos/me/quizzes/` +
                encodeURIComponent(idQuiz)
            );

            if (requisicao !== estado.requisicao) {
                return;
            }

            /*
             * Questionários concluídos não podem
             * ser respondidos novamente.
             */
            if (quiz.respondido) {
                if (!quiz.resultado) {
                    throw new Error(
                        "Resultado do questionário indisponível."
                    );
                }

                // Sincroniza o resultado com a disciplina.
                atualizarQuestionarioLocal(
                    idQuiz,
                    quiz.resultado
                );

                exibirResultado(
                    quiz.resultado
                );

                return;
            }

            const perguntas = quiz.perguntas || [];

            if (perguntas.length === 0) {
                throw new Error(
                    "Este quiz não possui perguntas."
                );
            }

            // Constrói as perguntas e alternativas.
            const perguntasHTML = perguntas.map(
                (pergunta, indice) =>
                    renderizarPergunta(
                        pergunta,
                        indice
                    )
            ).join("");

            const formularioHTML = renderizarTemplate(
                "010",
                [perguntasHTML]
            );

            const area = obterAreaPrincipal();

            // Preenche a estrutura de resolução do quiz.
            area.innerHTML = renderizarTemplate(
                "009",
                [
                    escaparHTML(
                        estado.disciplinaOrigem ?? ""
                    ),

                    escaparHTML(quiz.titulo),

                    "Quiz de fixação",

                    perguntas.length,

                    Number(quiz.xpMaximo),

                    formularioHTML
                ]
            );

            configurarBotaoVoltar();

            configurarFormularioQuiz(
                idQuiz,
                perguntas
            );

        } catch (erro) {
            if (requisicao !== estado.requisicao) {
                return;
            }

            exibirMensagem(
                erro.message,
                true
            );
        }
    }

    /**
     * Atualiza o questionário armazenado no estado
     * das disciplinas após a conclusão.
     *
     * Os valores utilizados são exclusivamente
     * aqueles retornados pela API.
     *
     * A atualização ocorre apenas na memória da
     * interface, sem persistência no localStorage.
     *
     * @param {string} idQuiz Identificador do quiz.
     * @param {Object} resultado Resultado oficial.
     */
    function atualizarQuestionarioLocal(
        idQuiz,
        resultado
    ) {
        if (typeof estadoAluno === "undefined") {
            return;
        }

        const disciplinas =
            estadoAluno.disciplinas || [];

        for (const disciplina of disciplinas) {
            const questionarios =
                disciplina.questionarios || [];

            const questionario = questionarios.find(
                item =>
                    String(item.id) === String(idQuiz)
            );

            if (!questionario) {
                continue;
            }

            const xpGanho = Number(
                resultado.xpGanho ?? 0
            );

            // Atualiza a situação do questionário.
            questionario.respondido = true;

            // Preserva a compatibilidade com o painel.
            questionario.pontuacao = xpGanho;
            questionario.xpGanho = xpGanho;

            // Atualiza o desempenho acadêmico.
            questionario.acertos = Number(
                resultado.acertos ?? 0
            );

            questionario.totalPerguntas = Number(
                resultado.totalPerguntas ?? 0
            );
        }
    }

    /**
     * Coleta as alternativas selecionadas pelo aluno.
     *
     * A validação do navegador verifica somente
     * se todas as perguntas foram respondidas.
     *
     * A correção permanece sob responsabilidade da API.
     *
     * @param {HTMLFormElement} formulario Formulário.
     * @param {Array} perguntas Perguntas apresentadas.
     * @returns {Array|null} Respostas selecionadas.
     */
    function coletarRespostas(
        formulario,
        perguntas
    ) {
        const respostas = [];

        for (const pergunta of perguntas) {
            const idPergunta = Number(
                pergunta.idPergunta
            );

            const selecionada = formulario.querySelector(
                `input[name="pergunta-${idPergunta}"]:checked`
            );

            if (!selecionada) {
                return null;
            }

            respostas.push({
                idPergunta,

                idAlternativa: Number(
                    selecionada.value
                )
            });
        }

        return respostas;
    }

    /**
     * Envia as respostas selecionadas para a API.
     *
     * O servidor valida as alternativas, calcula
     * os acertos e realiza a atribuição de XP.
     *
     * @param {HTMLFormElement} formulario Formulário.
     * @param {string} idQuiz Identificador do quiz.
     * @param {Array} perguntas Perguntas apresentadas.
     * @returns {Promise<void>}
     */
    async function enviarRespostas(
        formulario,
        idQuiz,
        perguntas
    ) {
        // Impede envios simultâneos pelo navegador.
        if (estado.enviando) {
            return;
        }

        const respostas = coletarRespostas(
            formulario,
            perguntas
        );

        if (!respostas) {
            alert(
                "Responda todas as perguntas."
            );

            return;
        }

        const botao = formulario.querySelector(
            'button[type="submit"]'
        );

        if (!botao) {
            return;
        }

        const requisicao = estado.requisicao;

        estado.enviando = true;

        botao.disabled = true;

        botao.textContent =
            "Enviando respostas...";

        try {
            const resultado = await requisitar(
                `/alunos/me/quizzes/` +
                `${encodeURIComponent(idQuiz)}/respostas`,
                "POST",
                { respostas }
            );

            /*
             * Sincroniza o estado da interface com
             * o resultado oficial do servidor.
             */
            atualizarQuestionarioLocal(
                idQuiz,
                resultado
            );

            // Atualiza o XP e o nível do aluno.
            await carregarProgresso();

            /*
             * Apresenta o resultado somente quando
             * o aluno permanece na tela do quiz.
             */
            if (requisicao === estado.requisicao) {
                exibirResultado(resultado);
            }

        } catch (erro) {
            /*
             * A API retorna 409 quando o questionário
             * já possui um resultado registrado.
             */
            if (erro.status === 409) {
                if (requisicao === estado.requisicao) {
                    alert(
                        "Este quiz já foi respondido. " +
                        "Nenhum XP adicional será atribuído."
                    );
                }

                await carregarProgresso();

                /*
                 * Consulta novamente o questionário
                 * para apresentar o resultado existente.
                 */
                if (requisicao === estado.requisicao) {
                    await abrirQuiz(idQuiz);
                }

                return;
            }

            if (requisicao === estado.requisicao) {
                alert(erro.message);
            }

        } finally {
            // Libera o controle de envio.
            estado.enviando = false;

            botao.disabled = false;

            botao.textContent =
                "Enviar respostas";
        }
    }

    /**
     * Gerencia a navegação dos questionários.
     *
     * Permite abrir um quiz diretamente pela
     * disciplina ou pela listagem geral.
     *
     * Preserva a origem da navegação e impede
     * que a lógica antiga atribua XP localmente.
     */
    function configurarEventos() {
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

                // Identifica a navegação pelo menu lateral.
                if (menu) {
                    estado.disciplinaOrigem = null;

                    if (menu.id === "nav-quizzes") {
                        evento.preventDefault();

                        evento.stopImmediatePropagation();

                        carregarQuizzes();

                        return;
                    }

                    /*
                     * Invalida requisições anteriores
                     * ao acessar outra funcionalidade.
                     */
                    estado.tela = null;
                    estado.requisicao++;

                    return;
                }

                const botaoQuiz = alvo.closest(
                    "[data-abrir-quiz]"
                );

                /*
                 * Abre diretamente o questionário
                 * independentemente da tela de origem.
                 */
                if (botaoQuiz) {
                    evento.preventDefault();

                    evento.stopImmediatePropagation();

                    estado.disciplinaOrigem =
                        estado.tela === "quizzes"
                            ? null
                            : botaoQuiz.dataset.disciplinaId
                            || null;

                    abrirQuiz(
                        botaoQuiz.dataset.quizId
                    );

                    return;
                }

                const botaoVoltar = alvo.closest(
                    '[data-action="back-to-discipline"]'
                );

                /*
                 * Retorna à disciplina ou à listagem
                 * geral conforme a origem do acesso.
                 */
                if (botaoVoltar && estado.tela) {
                    evento.preventDefault();

                    evento.stopImmediatePropagation();

                    voltarOrigemQuiz();
                }
            },
            true
        );
    }

    /**
     * Inicializa a integração dos questionários
     * e da gamificação do aluno.
     */
    function inicializar() {
        console.info(
            "TechAcademy: integração do aluno carregada."
        );

        configurarEventos();

        carregarProgresso();
    }

    // Aguarda o carregamento completo do documento.
    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            inicializar
        );

    } else {
        inicializar();
    }
})();