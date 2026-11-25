/**
 * Integração das disciplinas, aulas e materiais
 * acadêmicos do professor com a API REST.
 */
(() => {
    "use strict";

    const API_URL = "https://techacademy-api-2026-h0ebgpcgechch0c4.brazilsouth-01.azurewebsites.net/api";
    const TAMANHO_MAXIMO_PDF = 5 * 1024 * 1024;

    const estado = {
        turmas: [],
        aulas: [],
        quizzes: [],
        tela: null,
        requisicao: 0,
        salvando: false
    };

    /**
     * Protege os dados inseridos nos templates HTML.
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
     * Renderiza os templates existentes no dashboard.
     */
    function renderizarTemplate(numero, valores = {}) {
        const identificador =
            String(numero).padStart(3, "0");

        const template = document.getElementById(
            `ta-prof-fragment-${identificador}`
        );

        if (!template) {
            throw new Error(
                "Template HTML não encontrado."
            );
        }

        return template.innerHTML.replace(
            /__TA_SLOT_(\d+)__/g,
            (_, chave) => valores[chave] ?? ""
        );
    }

    /**
     * Realiza uma requisição autenticada à API.
     */
    async function requisitar(
        endpoint,
        metodo = "GET",
        dados = null
    ) {
        const token = sessionStorage.getItem(
            "techacademy_token"
        );

        if (!token) {
            throw new Error(
                "Sessão expirada. Realize o login novamente."
            );
        }

        const configuracao = {
            method: metodo,
            cache: "no-store",

            headers: {
                Authorization: `Bearer ${token}`
            }
        };

        if (dados instanceof FormData) {
            configuracao.body = dados;

        } else if (dados !== null) {
            configuracao.headers["Content-Type"] =
                "application/json";

            configuracao.body = JSON.stringify(dados);
        }

        const resposta = await fetch(
            API_URL + endpoint,
            configuracao
        );

        const resultado = await resposta.json()
            .catch(() => ({}));

        if (!resposta.ok) {
            throw new Error(
                resultado.mensagem ||
                "Não foi possível concluir a operação."
            );
        }

        return resultado;
    }

    /**
     * Recupera a área principal do painel.
     */
    function obterAreaPrincipal() {
        return document.getElementById(
            "main-content-area"
        );
    }

    /**
     * Atualiza o cabeçalho e o menu selecionado.
     */
    function configurarTela(menu, titulo, subtitulo) {
        document.getElementById(
            "page-title"
        ).textContent = titulo;

        document.getElementById(
            "page-subtitle"
        ).textContent = subtitulo;

        document.querySelectorAll(
            "#sidebar nav button"
        ).forEach(botao => {
            const selecionado = botao.id === menu;

            botao.classList.toggle(
                "bg-brand-blue",
                selecionado
            );

            botao.classList.toggle(
                "text-white",
                selecionado
            );

            botao.classList.toggle(
                "shadow-md",
                selecionado
            );

            botao.classList.toggle(
                "text-slate-300",
                !selecionado
            );

            botao.classList.toggle(
                "hover:bg-slate-800",
                !selecionado
            );
        });
    }

    /**
     * Apresenta uma mensagem na área principal.
     */
    function exibirMensagem(mensagem) {
        obterAreaPrincipal().innerHTML = `
            <div class="bg-white p-6 rounded-2xl
                        border border-blue-100
                        text-center text-sm
                        text-slate-500">

                ${escaparHtml(mensagem)}

            </div>
        `;
    }

    /**
     * Consulta os vínculos acadêmicos do professor.
     */
    async function consultarTurmas() {
        const dados = await requisitar(
            "/professores/me/turmas"
        );

        if (!Array.isArray(dados.turmas)) {
            throw new Error(
                "Lista de turmas inválida."
            );
        }

        estado.turmas = dados.turmas;
    }

    /**
     * Consulta as aulas reais do professor.
     */
    async function consultarAulas() {
        const dados = await requisitar(
            "/professores/me/aulas"
        );

        if (!Array.isArray(dados.aulas)) {
            throw new Error(
                "Lista de aulas inválida."
            );
        }

        estado.aulas = dados.aulas;
    }

    /**
     * Consulta os quizzes publicados pelo professor.
     */
    async function consultarQuizzes() {
        const dados = await requisitar(
            "/professores/me/quizzes"
        );

        if (!Array.isArray(dados.quizzes)) {
            throw new Error(
                "Lista de quizzes inválida."
            );
        }

        estado.quizzes = dados.quizzes;
    }

    /**
     * Formata datas acadêmicas sem alterar o fuso.
     */
    function formatarData(data) {
        if (!data) {
            return "Não informada";
        }

        return String(data)
            .slice(0, 10)
            .split("-")
            .reverse()
            .join("/");
    }

    /**
     * Retorna a data atual no horário local.
     */
    function obterDataAtual() {
        const data = new Date();

        const ano = data.getFullYear();

        const mes = String(
            data.getMonth() + 1
        ).padStart(2, "0");

        const dia = String(
            data.getDate()
        ).padStart(2, "0");

        return `${ano}-${mes}-${dia}`;
    }

    /**
     * Renderiza uma disciplina vinculada ao professor.
     */
    function renderizarDisciplina(vinculo) {
        const identificador = Number(
            vinculo.idTurmaDisciplina
        );

        const aulas = estado.aulas.filter(
            aula =>
                Number(aula.idTurmaDisciplina) ===
                identificador
        );

        const quizzes = estado.quizzes.filter(
            quiz =>
                Number(quiz.idTurmaDisciplina) ===
                identificador &&
                Boolean(quiz.ativo)
        );

        return renderizarTemplate(29, {
            "0": escaparHtml(
                vinculo.codigoDisciplina
            ),

            "1": escaparHtml(
                vinculo.nomeTurma
            ),

            "2": escaparHtml(
                vinculo.nomeDisciplina
            ),

            "3": aulas.length,

            "4": quizzes.length
        });
    }

    /**
     * Apresenta as disciplinas reais do professor.
     */
    async function carregarDisciplinas() {
        estado.tela = "disciplinas";

        const requisicao = ++estado.requisicao;

        configurarTela(
            "nav-disciplinas",
            "Minhas Disciplinas",
            "Consulte suas turmas e disciplinas vinculadas"
        );

        exibirMensagem(
            "Carregando disciplinas..."
        );

        try {
            await Promise.all([
                consultarTurmas(),
                consultarAulas(),
                consultarQuizzes()
            ]);

            if (requisicao !== estado.requisicao) {
                return;
            }

            if (!estado.turmas.length) {
                exibirMensagem(
                    "Nenhuma disciplina vinculada."
                );

                return;
            }

            const cartoes = estado.turmas.map(
                renderizarDisciplina
            ).join("");

            obterAreaPrincipal().innerHTML =
                renderizarTemplate(28, {
                    "0": cartoes
                });

        } catch (erro) {
            if (requisicao === estado.requisicao) {
                exibirMensagem(erro.message);
            }
        }
    }

    /**
     * Cria uma opção de seleção com texto seguro.
     */
    function criarOpcao(valor, texto) {
        const opcao = document.createElement(
            "option"
        );

        opcao.value = String(valor);
        opcao.textContent = texto;

        return opcao;
    }

    /**
     * Preenche as turmas vinculadas ao professor.
     */
    function preencherTurmas(select) {
        const turmas = new Map();

        for (const vinculo of estado.turmas) {
            turmas.set(
                vinculo.idTurma,
                vinculo.nomeTurma
            );
        }

        select.replaceChildren(
            criarOpcao(
                "",
                "-- Selecione uma turma --"
            )
        );

        for (const [idTurma, nome] of turmas) {
            select.appendChild(
                criarOpcao(idTurma, nome)
            );
        }
    }

    /**
     * Preenche as disciplinas da turma selecionada.
     */
    function preencherDisciplinas(
        select,
        idTurma
    ) {
        select.replaceChildren(
            criarOpcao(
                "",
                "-- Selecione uma disciplina --"
            )
        );

        const vinculos = estado.turmas.filter(
            vinculo =>
                Number(vinculo.idTurma) ===
                Number(idTurma)
        );

        for (const vinculo of vinculos) {
            select.appendChild(
                criarOpcao(
                    vinculo.idTurmaDisciplina,
                    vinculo.nomeDisciplina
                )
            );
        }

        select.disabled = !vinculos.length;
    }

    /**
     * Renderiza uma aula publicada.
     */
    function renderizarAulaPublicada(aula) {
        const template = document.createElement(
            "template"
        );

        template.innerHTML = renderizarTemplate(
            27,
            {
                "0": escaparHtml(
                    aula.conteudo || "Aula sem conteúdo"
                ),

                "1": escaparHtml(
                    aula.nomeDisciplina
                ),

                "2": escaparHtml(
                    aula.nomeTurma
                ),

                "3": formatarData(
                    aula.dataAula
                ),

                "4": Number(aula.idAula),

                "5": Number(
                    aula.idTurmaDisciplina
                )
            }
        );

        const botao = template.content.querySelector(
            "[data-editar-aula]"
        );

        botao.removeAttribute(
            "data-editar-aula"
        );

        botao.removeAttribute(
            "data-disc"
        );

        botao.dataset.adicionarMaterial =
            String(aula.idAula);

        botao.textContent = "Adicionar PDF";

        const informacao = document.createElement(
            "p"
        );

        informacao.className =
            "text-xs text-slate-500 mt-1";

        const situacao = aula.realizada
            ? "Realizada"
            : "Planejada";

        informacao.textContent =
            `${situacao} · ${aula.totalMateriais ?? 0} material(is)`;

        template.content.querySelector(
            ".font-bold"
        ).parentElement.appendChild(informacao);

        return template.innerHTML;
    }

    /**
     * Atualiza a lista de aulas publicadas.
     */
    function atualizarListaAulas() {
        const container = document.getElementById(
            "lista-aulas-api"
        );

        if (!container) {
            return;
        }

        container.innerHTML = estado.aulas.length
            ? estado.aulas.map(
                renderizarAulaPublicada
            ).join("")
            : renderizarTemplate(26);
    }

    /**
     * Adiciona os campos necessários ao formulário
     * existente, preservando sua identidade visual.
     */
    function configurarFormulario() {
        const formulario = document.getElementById(
            "form-postar-aula"
        );

        const disciplina = document.getElementById(
            "aula-disciplina"
        );

        const titulo = document.getElementById(
            "aula-titulo"
        );

        const arquivo = document.getElementById(
            "aula-pdf"
        );

        const campoTurma = document.createElement(
            "div"
        );

        campoTurma.innerHTML = `
            <label for="aula-turma"
                   class="block font-bold
                          text-slate-700 mb-1">

                Selecione a Turma

            </label>

            <select id="aula-turma"
                    required
                    class="w-full p-2.5 border
                           border-slate-200 rounded-xl
                           focus:border-brand-blue
                           focus:outline-none">
            </select>
        `;

        formulario.insertBefore(
            campoTurma,
            disciplina.parentElement
        );

        const campoData = document.createElement(
            "div"
        );

        campoData.innerHTML = `
            <label for="aula-data"
                   class="block font-bold
                          text-slate-700 mb-1">

                Data da Aula

            </label>

            <input type="date"
                   id="aula-data"
                   required
                   class="w-full p-2.5 border
                          border-slate-200 rounded-xl
                          focus:border-brand-blue
                          focus:outline-none">
        `;

        formulario.insertBefore(
            campoData,
            arquivo.parentElement
        );

        const campoSituacao = document.createElement(
            "div"
        );

        campoSituacao.innerHTML = `
            <label class="flex items-center gap-2
                          font-semibold text-slate-700">

                <input type="checkbox"
                       id="aula-realizada">

                Aula já realizada

            </label>
        `;

        formulario.insertBefore(
            campoSituacao,
            arquivo.parentElement
        );

        const campoMaterial = document.createElement(
            "div"
        );

        campoMaterial.innerHTML = `
            <label for="material-titulo"
                   class="block font-bold
                          text-slate-700 mb-1">

                Título do Material (opcional)

            </label>

            <input type="text"
                   id="material-titulo"
                   maxlength="150"
                   placeholder="Ex: Slides de SQL"
                   class="w-full p-2.5 border
                          border-slate-200 rounded-xl
                          focus:border-brand-blue
                          focus:outline-none">
        `;

        formulario.insertBefore(
            campoMaterial,
            arquivo.parentElement
        );

        const mensagem = formulario.querySelector(
            "p"
        );

        mensagem.id = "aula-mensagem";

        mensagem.textContent =
            "O PDF é opcional e deve possuir no máximo 5 MB.";

        mensagem.setAttribute(
            "aria-live",
            "polite"
        );

        titulo.maxLength = 300;

        arquivo.accept = ".pdf,application/pdf";

        document.getElementById(
            "aula-data"
        ).value = obterDataAtual();

        const turma = document.getElementById(
            "aula-turma"
        );

        preencherTurmas(turma);

        preencherDisciplinas(
            disciplina,
            ""
        );

        turma.addEventListener(
            "change",
            () => {
                preencherDisciplinas(
                    disciplina,
                    turma.value
                );
            }
        );

        document.getElementById(
            "cancelar-edicao-aula"
        ).addEventListener(
            "click",
            carregarTelaAulas
        );
    }

    /**
     * Exibe uma mensagem de sucesso ou erro.
     */
    function informar(mensagem, erro = false) {
        const elemento = document.getElementById(
            "aula-mensagem"
        );

        if (!elemento) {
            return;
        }

        elemento.textContent = mensagem;

        elemento.className = erro
            ? "text-red-600 font-semibold"
            : "text-emerald-600 font-semibold";
    }

    /**
     * Valida o arquivo antes de enviá-lo à API.
     */
    async function validarPdf(arquivo) {
        if (!arquivo) {
            return;
        }

        if (
            !arquivo.name.toLowerCase().endsWith(".pdf") ||
            arquivo.type !== "application/pdf"
        ) {
            throw new Error(
                "Selecione um arquivo PDF válido."
            );
        }

        if (
            arquivo.size <= 0 ||
            arquivo.size > TAMANHO_MAXIMO_PDF
        ) {
            throw new Error(
                "O PDF deve possuir no máximo 5 MB."
            );
        }

        const assinatura = await arquivo
            .slice(0, 5)
            .text();

        if (assinatura !== "%PDF-") {
            throw new Error(
                "O arquivo selecionado não é um PDF válido."
            );
        }
    }

    /**
     * Publica um PDF vinculado a uma aula existente.
     */
    async function publicarPdf(
        idAula,
        titulo,
        arquivo
    ) {
        const dados = new FormData();

        dados.append(
            "titulo",
            titulo
        );

        dados.append(
            "arquivo",
            arquivo
        );

        return requisitar(
            `/professores/me/aulas/${idAula}/materiais`,
            "POST",
            dados
        );
    }

    /**
     * Configura o formulário para adicionar um PDF
     * a uma aula já cadastrada.
     */
    function selecionarAulaExistente(idAula) {
        const aula = estado.aulas.find(
            item =>
                Number(item.idAula) ===
                Number(idAula)
        );

        if (!aula || estado.salvando) {
            return;
        }

        const turma = document.getElementById(
            "aula-turma"
        );

        const disciplina = document.getElementById(
            "aula-disciplina"
        );

        const titulo = document.getElementById(
            "aula-titulo"
        );

        const data = document.getElementById(
            "aula-data"
        );

        const realizada = document.getElementById(
            "aula-realizada"
        );

        document.getElementById(
            "aula-edit-id"
        ).value = aula.idAula;

        turma.value = aula.idTurma;

        preencherDisciplinas(
            disciplina,
            aula.idTurma
        );

        disciplina.value =
            aula.idTurmaDisciplina;

        titulo.value = aula.conteudo ?? "";

        data.value = String(
            aula.dataAula
        ).slice(0, 10);

        realizada.checked = Boolean(
            aula.realizada
        );

        // Impede a alteração de dados acadêmicos.
        turma.disabled = true;
        disciplina.disabled = true;
        titulo.disabled = true;
        data.disabled = true;
        realizada.disabled = true;

        document.getElementById(
            "aula-pdf"
        ).required = true;

        document.getElementById(
            "titulo-form-aula"
        ).textContent = "Adicionar Material à Aula";

        document.getElementById(
            "btn-salvar-aula"
        ).textContent = "Publicar PDF";

        document.getElementById(
            "cancelar-edicao-aula"
        ).classList.remove("hidden");

        informar(
            "Selecione um PDF para adicionar à aula existente."
        );

        document.getElementById(
            "form-postar-aula"
        ).scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    }

    /**
     * Cadastra a aula e publica o material selecionado.
     */
    async function salvarAula(formulario) {
        if (estado.salvando) {
            return;
        }

        const botao = document.getElementById(
            "btn-salvar-aula"
        );

        const requisicao = estado.requisicao;

        const campoAula = document.getElementById(
            "aula-edit-id"
        );

        let idAula = Number(campoAula.value);

        const aulaExistente = idAula > 0;

        const idTurmaDisciplina = Number(
            document.getElementById(
                "aula-disciplina"
            ).value
        );

        const conteudo = document.getElementById(
            "aula-titulo"
        ).value.trim();

        const dataAula = document.getElementById(
            "aula-data"
        ).value;

        const realizada = document.getElementById(
            "aula-realizada"
        ).checked;

        const arquivo = document.getElementById(
            "aula-pdf"
        ).files[0];

        const tituloMaterial = document.getElementById(
            "material-titulo"
        ).value.trim();

        estado.salvando = true;
        botao.disabled = true;

        let aulaCadastrada = false;
        let operacaoConcluida = false;

        try {
            if (
                !aulaExistente &&
                (!idTurmaDisciplina || !conteudo || !dataAula)
            ) {
                throw new Error(
                    "Preencha todos os dados da aula."
                );
            }

            if (aulaExistente && !arquivo) {
                throw new Error(
                    "Selecione um arquivo PDF."
                );
            }

            await validarPdf(arquivo);

            if (!aulaExistente) {
                informar(
                    "Cadastrando aula..."
                );

                const resultado = await requisitar(
                    "/professores/me/aulas",
                    "POST",
                    {
                        idTurmaDisciplina,
                        dataAula,
                        conteudo,
                        realizada
                    }
                );

                idAula = Number(
                    resultado.aula.idAula
                );

                aulaCadastrada = true;

                // Mantém o ID para evitar cadastrar
                // outra aula caso o upload falhe.
                campoAula.value = idAula;
            }

            if (arquivo) {
                informar(
                    "Publicando material PDF..."
                );

                await publicarPdf(
                    idAula,
                    tituloMaterial ||
                    arquivo.name.slice(0, 150),
                    arquivo
                );
            }

            operacaoConcluida = true;

            await consultarAulas();

            if (
                requisicao !== estado.requisicao ||
                !formulario.isConnected
            ) {
                return;
            }

            renderizarTelaAulas();

            informar(
                aulaExistente
                    ? "Material publicado com sucesso!"
                    : "Aula publicada com sucesso!"
            );

        } catch (erro) {
            if (
                requisicao !== estado.requisicao ||
                !formulario.isConnected
            ) {
                return;
            }

            if (operacaoConcluida) {
                informar(
                    "Registro salvo. Reabra a tela para atualizar a lista.",
                    true
                );

                return;
            }

            if (aulaCadastrada) {
                // A aula permanece salva no banco.
                // Uma nova tentativa enviará apenas o PDF.
                document.getElementById(
                    "aula-turma"
                ).disabled = true;

                document.getElementById(
                    "aula-disciplina"
                ).disabled = true;

                document.getElementById(
                    "aula-titulo"
                ).disabled = true;

                document.getElementById(
                    "aula-data"
                ).disabled = true;

                document.getElementById(
                    "aula-realizada"
                ).disabled = true;

                document.getElementById(
                    "aula-pdf"
                ).required = true;

                document.getElementById(
                    "cancelar-edicao-aula"
                ).classList.remove("hidden");

                botao.textContent = "Publicar PDF";

                informar(
                    `Aula cadastrada. Falha no PDF: ${erro.message}`,
                    true
                );

            } else {
                informar(
                    erro.message,
                    true
                );
            }

        } finally {
            estado.salvando = false;
            botao.disabled = operacaoConcluida;
        }
    }

    /**
     * Renderiza a tela de publicação de aulas.
     */
    function renderizarTelaAulas() {
        obterAreaPrincipal().innerHTML =
            renderizarTemplate(24, {
                "0": "",

                "1": `
                    <div id="lista-aulas-api"
                         class="space-y-3">
                    </div>
                `
            });

        const secao = obterAreaPrincipal()
            .querySelector("section h2");

        secao.textContent = "Aulas já publicadas";

        configurarFormulario();

        atualizarListaAulas();
    }

    /**
     * Carrega os dados necessários à publicação.
     */
    async function carregarTelaAulas() {
        if (estado.salvando) {
            return;
        }

        estado.tela = "aulas";

        const requisicao = ++estado.requisicao;

        configurarTela(
            "nav-postar-aula",
            "Postar Slides / Aulas",
            "Publique aulas e materiais acadêmicos"
        );

        exibirMensagem(
            "Carregando aulas..."
        );

        try {
            await Promise.all([
                consultarTurmas(),
                consultarAulas()
            ]);

            if (requisicao !== estado.requisicao) {
                return;
            }

            renderizarTelaAulas();

        } catch (erro) {
            if (requisicao === estado.requisicao) {
                exibirMensagem(erro.message);
            }
        }
    }

    /**
     * Intercepta exclusivamente os eventos relacionados
     * às disciplinas e aulas integradas.
     */
    function configurarEventos() {
        document.addEventListener(
            "click",
            evento => {
                const menu = evento.target.closest(
                    "#sidebar nav button"
                );

                if (menu) {
                    if (estado.salvando) {
                        evento.preventDefault();
                        evento.stopImmediatePropagation();

                        return;
                    }

                    if (menu.id === "nav-disciplinas") {
                        evento.preventDefault();
                        evento.stopImmediatePropagation();

                        carregarDisciplinas();

                        return;
                    }

                    if (menu.id === "nav-postar-aula") {
                        evento.preventDefault();
                        evento.stopImmediatePropagation();

                        carregarTelaAulas();

                        return;
                    }

                    // Preserva os demais menus.
                    estado.tela = null;
                    estado.requisicao++;

                    return;
                }

                const botao = evento.target.closest(
                    "[data-adicionar-material]"
                );

                if (botao) {
                    evento.preventDefault();
                    evento.stopImmediatePropagation();

                    selecionarAulaExistente(
                        botao.dataset.adicionarMaterial
                    );
                }
            },
            true
        );

        document.addEventListener(
            "submit",
            evento => {
                if (
                    evento.target.id !== "form-postar-aula"
                ) {
                    return;
                }

                evento.preventDefault();
                evento.stopImmediatePropagation();

                salvarAula(evento.target);
            },
            true
        );
    }

    /**
     * Inicializa a integração com a API REST.
     */
    function inicializar() {
        configurarEventos();

        carregarDisciplinas();
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