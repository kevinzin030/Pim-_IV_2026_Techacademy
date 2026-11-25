/**
 * TechAcademy Web
 * Integração dos quizzes do professor com a API REST.
 *
 * Responsabilidades:
 * - Consultar turmas e disciplinas reais.
 * - Filtrar disciplinas pela turma selecionada.
 * - Validar perguntas e pontuação XP.
 * - Publicar e consultar quizzes no Azure SQL.
 */

(() => {
    "use strict";

    const API_URL = "http://localhost:3000/api";

    const CONFIGURACAO = Object.freeze({
        xpMinimo: 10,
        xpMaximo: 1000,
        intervaloXP: 10,
        maximoPerguntas: 50,
        chaveToken: "techacademy_token",
        chaveSessao: "usuario_logado"
    });

    /**
     * Realiza requisições autenticadas com JWT.
     */
    async function requisitar(
        caminho,
        metodo = "GET",
        dados = null
    ) {
        const token = sessionStorage.getItem(
            CONFIGURACAO.chaveToken
        );

        if (!token) {
            window.location.replace("index.html");

            throw new Error("Sessão não encontrada.");
        }

        const configuracao = {
            method: metodo,
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

        if (resposta.status === 401) {
            sessionStorage.removeItem(
                CONFIGURACAO.chaveToken
            );

            sessionStorage.removeItem(
                CONFIGURACAO.chaveSessao
            );

            window.location.replace("index.html");
        }

        if (!resposta.ok) {
            throw new Error(
                resultado.mensagem ||
                "Não foi possível concluir a operação."
            );
        }

        return resultado;
    }

    /**
     * Cria o seletor de turma mantendo a identidade
     * visual do formulário original.
     */
    function criarSeletorTurma(seletorDisciplina) {
        const grupoDisciplina =
            seletorDisciplina.parentElement;

        const grupoTurma = document.createElement("div");

        grupoTurma.id = "grupo-quiz-turma";

        const rotulo = document.createElement("label");

        rotulo.htmlFor = "quiz-turma";

        rotulo.className =
            "block font-semibold text-slate-700 mb-1";

        rotulo.textContent = "Turma";

        const seletorTurma = document.createElement(
            "select"
        );

        seletorTurma.id = "quiz-turma";
        seletorTurma.required = true;

        seletorTurma.className =
            seletorDisciplina.className;

        seletorTurma.add(
            new Option("Selecione uma turma", "")
        );

        grupoTurma.append(rotulo, seletorTurma);

        grupoDisciplina.before(grupoTurma);

        return seletorTurma;
    }

    /**
     * Apresenta somente as disciplinas vinculadas
     * à turma selecionada pelo professor.
     */
    function atualizarDisciplinas(
        seletorTurma,
        seletorDisciplina,
        vinculos
    ) {
        const idTurma = Number(seletorTurma.value);

        seletorDisciplina.replaceChildren();

        seletorDisciplina.add(
            new Option("Selecione uma disciplina", "")
        );

        if (!idTurma) {
            seletorDisciplina.disabled = true;
            return;
        }

        const disciplinas = vinculos.filter(
            vinculo =>
                Number(vinculo.idTurma) === idTurma
        );

        disciplinas.sort((primeira, segunda) =>
            primeira.nomeDisciplina.localeCompare(
                segunda.nomeDisciplina,
                "pt-BR"
            )
        );

        for (const disciplina of disciplinas) {
            const descricao =
                `${disciplina.codigoDisciplina} - ` +
                `${disciplina.nomeDisciplina}`;

            seletorDisciplina.add(
                new Option(
                    descricao,
                    disciplina.idTurmaDisciplina
                )
            );
        }

        seletorDisciplina.disabled =
            disciplinas.length === 0;
    }

    /**
     * Consulta os vínculos acadêmicos reais.
     * Remove turmas duplicadas antes de exibi-las.
     */
    async function carregarVinculos(
        seletorTurma,
        seletorDisciplina
    ) {
        const resultado = await requisitar(
            "/professores/me/turmas"
        );

        const vinculos = resultado.turmas || [];

        const turmasUnicas = new Map();

        for (const vinculo of vinculos) {
            if (!turmasUnicas.has(vinculo.idTurma)) {
                turmasUnicas.set(
                    vinculo.idTurma,
                    {
                        idTurma: vinculo.idTurma,
                        codigoTurma: vinculo.codigoTurma,
                        nomeTurma: vinculo.nomeTurma
                    }
                );
            }
        }

        const turmas = Array.from(
            turmasUnicas.values()
        );

        turmas.sort((primeira, segunda) =>
            primeira.nomeTurma.localeCompare(
                segunda.nomeTurma,
                "pt-BR"
            )
        );

        seletorTurma.replaceChildren();

        seletorTurma.add(
            new Option("Selecione uma turma", "")
        );

        for (const turma of turmas) {
            const descricao =
                `${turma.codigoTurma} - ` +
                `${turma.nomeTurma}`;

            seletorTurma.add(
                new Option(
                    descricao,
                    turma.idTurma
                )
            );
        }

        seletorTurma.addEventListener(
            "change",
            () => {
                atualizarDisciplinas(
                    seletorTurma,
                    seletorDisciplina,
                    vinculos
                );
            }
        );

        atualizarDisciplinas(
            seletorTurma,
            seletorDisciplina,
            vinculos
        );

        return vinculos;
    }

    /**
     * Apresenta os quizzes persistidos no Azure SQL.
     */
    async function carregarQuizzes(container) {
        const resultado = await requisitar(
            "/professores/me/quizzes"
        );

        const quizzes = resultado.quizzes || [];

        container.replaceChildren();

        if (quizzes.length === 0) {
            const mensagem = document.createElement("p");

            mensagem.className =
                "text-slate-500 text-sm";

            mensagem.textContent =
                "Nenhum quiz publicado.";

            container.appendChild(mensagem);

            return;
        }

        for (const quiz of quizzes) {
            const card = document.createElement("div");

            card.className =
                "p-4 border border-slate-200 " +
                "rounded-xl bg-white";

            const titulo = document.createElement("div");

            titulo.className =
                "font-bold text-slate-800";

            titulo.textContent = quiz.titulo;

            const disciplina = document.createElement(
                "p"
            );

            disciplina.className =
                "text-slate-600 mt-1";

            disciplina.textContent =
                `${quiz.nomeTurma} - ` +
                `${quiz.nomeDisciplina}`;

            const detalhes = document.createElement("p");

            detalhes.className =
                "text-slate-500 text-xs mt-2";

            detalhes.textContent = [
                `${quiz.totalPerguntas} pergunta(s)`,
                `${quiz.xpMaximo} XP`,
                `${quiz.totalRespostas} resposta(s)`
            ].join(" · ");

            const situacao = document.createElement("p");

            situacao.className = quiz.ativo
                ? "text-green-600 text-xs font-bold mt-2"
                : "text-red-600 text-xs font-bold mt-2";

            situacao.textContent = quiz.ativo
                ? "Publicado"
                : "Inativo";

            card.append(
                titulo,
                disciplina,
                detalhes,
                situacao
            );

            container.appendChild(card);
        }
    }

    /**
     * Obtém e valida as perguntas do formulário.
     */
    function obterPerguntas(formulario) {
        const elementos = Array.from(
            formulario.querySelectorAll(
                ".quiz-pergunta-item"
            )
        );

        if (
            elementos.length < 1 ||
            elementos.length >
            CONFIGURACAO.maximoPerguntas
        ) {
            throw new Error(
                "O quiz deve possuir entre " +
                "1 e 50 perguntas."
            );
        }

        return elementos.map((elemento, indice) => {
            const enunciado = elemento.querySelector(
                ".pergunta-enunciado"
            ).value.trim();

            const indiceCorreto = Number(
                elemento.querySelector(
                    ".pergunta-correta"
                ).value
            );

            const alternativas = Array.from(
                elemento.querySelectorAll(
                    ".pergunta-opcao"
                )
            ).map((campo, indiceAlternativa) => ({
                texto: campo.value.trim(),
                correta:
                    indiceAlternativa === indiceCorreto
            }));

            if (!enunciado || enunciado.length > 10000) {
                throw new Error(
                    `Informe um enunciado válido ` +
                    `na pergunta ${indice + 1}.`
                );
            }

            if (
                alternativas.length < 2 ||
                alternativas.length > 6
            ) {
                throw new Error(
                    `A pergunta ${indice + 1} deve ` +
                    `possuir entre 2 e 6 alternativas.`
                );
            }

            if (
                alternativas.some(
                    alternativa =>
                        !alternativa.texto ||
                        alternativa.texto.length > 1000
                )
            ) {
                throw new Error(
                    `Preencha corretamente as alternativas ` +
                    `da pergunta ${indice + 1}.`
                );
            }

            const totalCorretas = alternativas.filter(
                alternativa => alternativa.correta
            ).length;

            if (totalCorretas !== 1) {
                throw new Error(
                    `Selecione uma resposta correta ` +
                    `na pergunta ${indice + 1}.`
                );
            }

            return {
                enunciado,
                alternativas
            };
        });
    }

    /**
     * Converte os dados do formulário para o formato
     * esperado pelo QuizController.
     */
    function obterDadosQuiz(formulario, vinculos) {
        const idTurma = Number(
            formulario.querySelector(
                "#quiz-turma"
            ).value
        );

        const idTurmaDisciplina = Number(
            formulario.querySelector(
                "#quiz-disciplina"
            ).value
        );

        const titulo = formulario.querySelector(
            "#quiz-titulo"
        ).value.trim();

        const xpMaximo = Number(
            formulario.querySelector(
                "#quiz-xp"
            ).value
        );

        if (
            !Number.isInteger(idTurma) ||
            idTurma <= 0
        ) {
            throw new Error("Selecione uma turma.");
        }

        if (
            !Number.isInteger(idTurmaDisciplina) ||
            idTurmaDisciplina <= 0
        ) {
            throw new Error("Selecione uma disciplina.");
        }

        const vinculoValido = vinculos.some(
            vinculo =>
                Number(vinculo.idTurma) === idTurma &&
                Number(vinculo.idTurmaDisciplina) ===
                idTurmaDisciplina
        );

        if (!vinculoValido) {
            throw new Error(
                "A disciplina não pertence à turma selecionada."
            );
        }

        if (!titulo || titulo.length > 300) {
            throw new Error("Informe um título válido.");
        }

        if (
            !Number.isInteger(xpMaximo) ||
            xpMaximo < CONFIGURACAO.xpMinimo ||
            xpMaximo > CONFIGURACAO.xpMaximo ||
            xpMaximo % CONFIGURACAO.intervaloXP !== 0
        ) {
            throw new Error(
                "Informe um XP entre 10 e 1000, " +
                "em intervalos de 10."
            );
        }

        const perguntas = obterPerguntas(formulario);

        return {
            idTurmaDisciplina,
            idAula: null,
            titulo,
            xpMaximo,
            perguntas
        };
    }

    /**
     * Limpa o formulário após uma publicação.
     */
    function limparFormulario(
        formulario,
        seletorTurma,
        seletorDisciplina,
        vinculos
    ) {
        const turmaSelecionada = seletorTurma.value;

        formulario.reset();

        seletorTurma.value = turmaSelecionada;

        atualizarDisciplinas(
            seletorTurma,
            seletorDisciplina,
            vinculos
        );

        const perguntas = formulario.querySelectorAll(
            ".quiz-pergunta-item"
        );

        perguntas.forEach((pergunta, indice) => {
            if (indice > 0) {
                pergunta.remove();
            }
        });

        formulario.querySelector("#quiz-xp").value =
            "100";
    }

    /**
     * Integra o formulário original com a API.
     */
    async function integrarFormulario() {
        const formulario = document.getElementById(
            "form-criar-quiz"
        );

        if (
            !formulario ||
            formulario.dataset.integradoApi === "true"
        ) {
            return;
        }

        const seletorDisciplina = formulario.querySelector(
            "#quiz-disciplina"
        );

        const seletorAula = formulario.querySelector(
            "#quiz-aula"
        );

        const campoXP = formulario.querySelector(
            "#quiz-xp"
        );

        const campoTitulo = formulario.querySelector(
            "#quiz-titulo"
        );

        const botaoSalvar = formulario.querySelector(
            "#btn-salvar-quiz"
        ) || formulario.querySelector(
            'button[type="submit"]'
        );

        if (
            !seletorDisciplina ||
            !campoXP ||
            !campoTitulo ||
            !botaoSalvar
        ) {
            console.error(
                "Elementos do formulário não encontrados."
            );

            return;
        }

        formulario.dataset.integradoApi = "true";

        const seletorTurma = criarSeletorTurma(
            seletorDisciplina
        );

        const secaoQuizzes = document.querySelector(
            "#main-content-area > section"
        );

        let containerQuizzes = secaoQuizzes?.querySelector(
            ".space-y-3"
        );

        if (secaoQuizzes && !containerQuizzes) {
            containerQuizzes = document.createElement(
                "div"
            );

            containerQuizzes.className = "space-y-3";

            secaoQuizzes.appendChild(containerQuizzes);
        }

        let vinculos = [];

        campoXP.min = String(CONFIGURACAO.xpMinimo);
        campoXP.max = String(CONFIGURACAO.xpMaximo);
        campoXP.step = String(CONFIGURACAO.intervaloXP);

        campoTitulo.maxLength = 300;

        seletorTurma.disabled = true;
        seletorDisciplina.disabled = true;

        botaoSalvar.textContent = "Carregando...";
        botaoSalvar.disabled = true;

        // Oculta a vinculação com aulas.
        // O quiz permanece vinculado à turma e à disciplina.
        if (seletorAula) {
            seletorAula.disabled = true;
            seletorAula.parentElement.style.display = "none";
        }

        const tituloSecao = secaoQuizzes?.querySelector(
            "h2"
        );

        if (tituloSecao) {
            tituloSecao.textContent =
                "Quizzes publicados";
        }

        containerQuizzes?.replaceChildren();

        /**
         * Impede o salvamento antigo no localStorage.
         * A publicação passa a ocorrer pela API.
         */
        formulario.addEventListener(
            "submit",
            async evento => {
                evento.preventDefault();
                evento.stopImmediatePropagation();

                if (botaoSalvar.disabled) {
                    return;
                }

                botaoSalvar.disabled = true;
                botaoSalvar.textContent = "Publicando...";

                try {
                    const dados = obterDadosQuiz(
                        formulario,
                        vinculos
                    );

                    await requisitar(
                        "/professores/me/quizzes",
                        "POST",
                        dados
                    );

                    limparFormulario(
                        formulario,
                        seletorTurma,
                        seletorDisciplina,
                        vinculos
                    );

                    alert(
                        "Quiz publicado com sucesso!"
                    );

                    if (containerQuizzes) {
                        try {
                            await carregarQuizzes(
                                containerQuizzes
                            );
                        } catch (erro) {
                            console.error(erro);

                            alert(
                                "O quiz foi publicado, mas " +
                                "não foi possível atualizar a lista."
                            );
                        }
                    }
                } catch (erro) {
                    console.error(erro);

                    alert(erro.message);
                } finally {
                    botaoSalvar.disabled = false;

                    botaoSalvar.textContent =
                        "Publicar Quiz";
                }
            },
            true
        );

        try {
            vinculos = await carregarVinculos(
                seletorTurma,
                seletorDisciplina
            );

            if (!formulario.isConnected) {
                return;
            }

            seletorTurma.disabled =
                vinculos.length === 0;

            botaoSalvar.disabled =
                vinculos.length === 0;

            botaoSalvar.textContent = "Publicar Quiz";

            if (vinculos.length === 0) {
                alert(
                    "O professor não possui vínculos ativos."
                );
            }

            if (containerQuizzes) {
                try {
                    await carregarQuizzes(
                        containerQuizzes
                    );
                } catch (erro) {
                    console.error(erro);

                    containerQuizzes.textContent =
                        "Não foi possível consultar os quizzes.";
                }
            }
        } catch (erro) {
            console.error(erro);

            botaoSalvar.textContent =
                "Falha ao carregar vínculos";

            alert(erro.message);
        }
    }

    /**
     * Detecta a abertura da tela dinâmica de quizzes.
     */
    function inicializar() {
        console.info(
            "TechAcademy: integração de quizzes carregada."
        );

        const areaPrincipal = document.getElementById(
            "main-content-area"
        );

        if (!areaPrincipal) {
            return;
        }

        const observador = new MutationObserver(() => {
            integrarFormulario();
        });

        observador.observe(areaPrincipal, {
            childList: true,
            subtree: true
        });

        integrarFormulario();
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            inicializar
        );
    } else {
        inicializar();
    }
})();