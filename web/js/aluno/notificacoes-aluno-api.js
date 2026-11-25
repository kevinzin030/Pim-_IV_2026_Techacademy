(() => {
    "use strict";

    const CONFIGURACAO = {
        api: "https://techacademy-api-2026-h0ebgpcgechch0c4.brazilsouth-01.azurewebsites.net/api",
        chaveToken: "techacademy_token",
        intervaloAtualizacao: 30000
    };

    // Estado das notificações retornadas pela API.
    const estado = {
        notificacoes: [],
        totalNaoLidas: 0,
        carregando: false,
        atualizando: false,
        erro: ""
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
     * Realiza uma requisição autenticada à API.
     */
    async function consultarApi(
        endpoint,
        metodo = "GET"
    ) {
        const token = sessionStorage.getItem(
            CONFIGURACAO.chaveToken
        );

        if (!token) {
            throw new Error("Sessão não encontrada.");
        }

        const resposta = await fetch(
            `${CONFIGURACAO.api}${endpoint}`,
            {
                method: metodo,

                headers: {
                    Authorization: `Bearer ${token}`
                },

                cache: "no-store"
            }
        );

        const dados = await resposta.json();

        if (resposta.status === 401) {
            sessionStorage.removeItem(
                CONFIGURACAO.chaveToken
            );

            sessionStorage.removeItem(
                "usuario_logado"
            );

            window.location.replace("../../index.html");

            throw new Error("Sessão expirada.");
        }

        if (!resposta.ok) {
            throw new Error(
                dados.mensagem ||
                "Não foi possível consultar as notificações."
            );
        }

        return dados;
    }

    /**
     * Formata a data no horário local do usuário.
     */
    function formatarData(valor) {
        const data = new Date(valor);

        if (
            !valor ||
            Number.isNaN(data.getTime())
        ) {
            return "Data não registrada";
        }

        return data.toLocaleString("pt-BR", {
            dateStyle: "short",
            timeStyle: "short"
        });
    }

    /**
     * Define o ícone correspondente ao tipo de aviso.
     */
    function obterIcone(tipo) {
        switch (tipo) {
            case "Aula":
                return "fa-solid fa-book-open text-blue-600";

            case "Material":
                return "fa-solid fa-file-pdf text-red-600";

            case "Quiz":
                return "fa-solid fa-gamepad text-amber-600";

            case "Nota":
                return "fa-solid fa-award text-emerald-600";

            case "Frequencia":
                return "fa-solid fa-triangle-exclamation text-red-600";

            default:
                return "fa-solid fa-bell text-blue-600";
        }
    }

    /**
     * Atualiza o contador com o total real de avisos
     * não lidos, independentemente do limite da listagem.
     */
    function atualizarSino() {
        const contador = document.getElementById(
            "notification-count"
        );

        const sino = document.getElementById(
            "notification-bell"
        );

        if (!contador || !sino) {
            return;
        }

        contador.textContent =
            estado.totalNaoLidas > 99
                ? "99+"
                : String(estado.totalNaoLidas);

        contador.classList.toggle(
            "hidden",
            estado.totalNaoLidas === 0
        );

        sino.setAttribute(
            "aria-label",
            `Notificações: ${estado.totalNaoLidas} não lidas`
        );
    }

    /**
     * Verifica se o painel está aberto.
     */
    function painelEstaAberto() {
        const painel = document.getElementById(
            "notification-panel"
        );

        return Boolean(
            painel &&
            !painel.classList.contains("hidden")
        );
    }

    /**
     * Renderiza uma notificação individual.
     *
     * Os avisos não lidos possuem destaque azul
     * e permitem a confirmação individual de leitura.
     */
    function renderizarNotificacao(notificacao) {
        const id = Number(
            notificacao.idNotificacao
        );

        const lida = Boolean(
            notificacao.lida
        );

        const classeFundo = lida
            ? "bg-slate-50 border-slate-100"
            : "bg-blue-50 border-blue-100";

        const botaoLeitura = lida
            ? `
                <span class="text-[10px] text-slate-400">
                    <i class="fa-solid fa-check"></i>
                    Lida
                </span>
            `
            : `
                <button
                    type="button"
                    data-ler-notificacao="${id}"
                    class="text-[11px] font-semibold
                           text-brand-blue hover:underline">

                    Marcar como lida
                </button>
            `;

        return `
            <article class="p-3 rounded-lg mb-2
                            border ${classeFundo}">

                <div class="flex items-start gap-2">

                    <i class="${obterIcone(notificacao.tipo)}
                              mt-1"
                       aria-hidden="true">
                    </i>

                    <div class="flex-1 min-w-0">

                        <p class="font-bold text-sm
                                  text-slate-800 break-words">

                            ${escaparHtml(notificacao.titulo)}
                        </p>

                        <p class="text-xs text-slate-600
                                  mt-1 break-words">

                            ${escaparHtml(notificacao.mensagem)}
                        </p>

                        <div class="flex flex-wrap items-center
                                    justify-between gap-2 mt-2">

                            <span class="text-[10px]
                                         text-slate-500">

                                ${escaparHtml(
            formatarData(
                notificacao.dataCriacao
            )
        )}
                            </span>

                            ${botaoLeitura}

                        </div>

                    </div>

                </div>

            </article>
        `;
    }

    /**
     * Preenche o painel utilizando o cabeçalho
     * e o template vazio existentes no HTML.
     */
    function renderizarNotificacoes() {
        const painel = document.getElementById(
            "notification-panel"
        );

        if (!painel || !painelEstaAberto()) {
            return;
        }

        let listaHtml = "";

        if (estado.erro) {
            listaHtml += `
                <div class="p-3 mb-2 rounded-lg
                            bg-red-50 text-red-700 text-xs"
                     role="alert">

                    ${escaparHtml(estado.erro)}

                    <button
                        type="button"
                        data-recarregar-notificacoes
                        class="block mt-2 font-bold
                               hover:underline">

                        Tentar novamente
                    </button>

                </div>
            `;
        }

        if (estado.notificacoes.length > 0) {
            listaHtml += estado.notificacoes
                .map(renderizarNotificacao)
                .join("");
        } else if (estado.carregando) {
            listaHtml += `
                <p class="text-sm text-slate-500 py-3">
                    Consultando notificações...
                </p>
            `;
        } else if (!estado.erro) {
            listaHtml += renderHtmlTemplate(
                "ta-aluno-fragment-028"
            );
        }

        // Preserva a posição da rolagem durante atualizações.
        const posicaoRolagem = painel.scrollTop;

        painel.innerHTML = renderHtmlTemplate(
            "ta-aluno-fragment-027",
            {
                "0": listaHtml
            }
        );

        painel.scrollTop = posicaoRolagem;

        const botaoMarcarTodas = document.getElementById(
            "marcar-notificacoes-lidas"
        );

        if (botaoMarcarTodas) {
            botaoMarcarTodas.textContent =
                "Marcar todas como lidas";

            botaoMarcarTodas.disabled =
                estado.totalNaoLidas === 0 ||
                estado.atualizando;

            botaoMarcarTodas.classList.toggle(
                "opacity-50",
                botaoMarcarTodas.disabled
            );
        }

        painel.querySelectorAll(
            "[data-ler-notificacao]"
        ).forEach(botao => {
            botao.disabled = estado.atualizando;
        });
    }

    /**
     * Consulta as notificações persistidas no Azure SQL.
     *
     * Impede consultas simultâneas e evita sobrescrever
     * o estado durante uma atualização de leitura.
     */
    async function carregarNotificacoesApi() {
        if (
            estado.carregando ||
            estado.atualizando
        ) {
            return;
        }

        estado.carregando = true;
        estado.erro = "";

        renderizarNotificacoes();

        try {
            const dados = await consultarApi(
                "/notificacoes/me"
            );

            if (!Array.isArray(dados.notificacoes)) {
                throw new Error(
                    "Resposta de notificações inválida."
                );
            }

            const totalNaoLidas = Number(
                dados.totalNaoLidas
            );

            if (
                !Number.isInteger(totalNaoLidas) ||
                totalNaoLidas < 0
            ) {
                throw new Error(
                    "Contador de notificações inválido."
                );
            }

            estado.notificacoes =
                dados.notificacoes;

            estado.totalNaoLidas =
                totalNaoLidas;

            atualizarSino();

        } catch (erro) {
            estado.erro = erro.message;

            console.error(
                "Erro ao consultar notificações:",
                erro
            );

        } finally {
            estado.carregando = false;

            renderizarNotificacoes();
        }
    }

    /**
     * Confirma a leitura de uma ou de todas as notificações.
     *
     * A atualização é persistida pela API antes de
     * modificar o estado apresentado ao aluno.
     */
    async function atualizarLeitura(endpoint) {
        if (
            estado.atualizando ||
            estado.carregando
        ) {
            return;
        }

        estado.atualizando = true;
        estado.erro = "";

        renderizarNotificacoes();

        try {
            await consultarApi(
                endpoint,
                "PATCH"
            );

        } catch (erro) {
            estado.erro = erro.message;

            console.error(
                "Erro ao atualizar notificação:",
                erro
            );

        } finally {
            estado.atualizando = false;
        }

        if (estado.erro) {
            renderizarNotificacoes();

            return;
        }

        // Recupera a situação definitiva registrada no banco.
        await carregarNotificacoesApi();
    }

    /**
     * Marca uma notificação específica como lida.
     */
    async function marcarNotificacaoComoLida(id) {
        const idNotificacao = Number(id);

        if (
            !Number.isInteger(idNotificacao) ||
            idNotificacao <= 0 ||
            idNotificacao > 2147483647
        ) {
            return;
        }

        await atualizarLeitura(
            `/notificacoes/me/${idNotificacao}/lida`
        );
    }

    /**
     * Marca todas as notificações do usuário como lidas.
     */
    async function marcarTodasComoLidas() {
        await atualizarLeitura(
            "/notificacoes/me/todas/lida"
        );
    }

    /**
     * Configura a abertura e o fechamento do painel.
     */
    function configurarSinoApi() {
        const sino = document.getElementById(
            "notification-bell"
        );

        const painel = document.getElementById(
            "notification-panel"
        );

        if (!sino || !painel) {
            return;
        }

        sino.setAttribute(
            "aria-controls",
            "notification-panel"
        );

        sino.setAttribute(
            "aria-expanded",
            "false"
        );

        sino.addEventListener("click", () => {
            painel.classList.toggle("hidden");

            const aberto = painelEstaAberto();

            sino.setAttribute(
                "aria-expanded",
                String(aberto)
            );

            if (aberto) {
                renderizarNotificacoes();

                carregarNotificacoesApi();
            }
        });

        // Delegação de eventos para os botões dinâmicos.
        painel.addEventListener("click", evento => {
            const botaoIndividual =
                evento.target.closest(
                    "[data-ler-notificacao]"
                );

            if (botaoIndividual) {
                marcarNotificacaoComoLida(
                    botaoIndividual.dataset.lerNotificacao
                );

                return;
            }

            const botaoTodas =
                evento.target.closest(
                    "#marcar-notificacoes-lidas"
                );

            if (botaoTodas) {
                marcarTodasComoLidas();

                return;
            }

            const botaoRecarregar =
                evento.target.closest(
                    "[data-recarregar-notificacoes]"
                );

            if (botaoRecarregar) {
                carregarNotificacoesApi();
            }
        });

        // Fecha o painel ao clicar fora da central.
        document.addEventListener("click", evento => {
            if (
                !sino.contains(evento.target) &&
                !painel.contains(evento.target)
            ) {
                painel.classList.add("hidden");

                sino.setAttribute(
                    "aria-expanded",
                    "false"
                );
            }
        });

        // Permite fechar a central utilizando Escape.
        document.addEventListener("keydown", evento => {
            if (
                evento.key === "Escape" &&
                painelEstaAberto()
            ) {
                painel.classList.add("hidden");

                sino.setAttribute(
                    "aria-expanded",
                    "false"
                );

                sino.focus();
            }
        });
    }

    /**
     * Substitui as funções legadas de notificações
     * antes da inicialização do painel acadêmico.
     */
    window.carregarNotificacoes =
        carregarNotificacoesApi;

    window.atualizarSinoNotificacoes =
        atualizarSino;

    window.renderNotificacoes =
        renderizarNotificacoes;

    window.marcarTodasNotificacoesComoLidas =
        marcarTodasComoLidas;

    window.configurarSinoNotificacoes =
        configurarSinoApi;

    /**
     * Preserva a compatibilidade com os scripts antigos,
     * sem armazenar notificações acadêmicas localmente.
     */
    window.salvarDadosCompartilhados = function () {
        const dadosAtuais = obterDadosCompartilhados();

        delete dadosAtuais.notificacoes;

        persistirDadosCompartilhados({
            ...dadosAtuais,
            disciplinas: estadoAluno.disciplinas
        });
    };

    /**
     * Atualiza periodicamente as notificações enquanto
     * o painel estiver visível no navegador.
     */
    window.addEventListener("DOMContentLoaded", () => {
        window.setInterval(() => {
            if (
                !document.hidden &&
                sessionStorage.getItem(
                    CONFIGURACAO.chaveToken
                )
            ) {
                carregarNotificacoesApi();
            }
        }, CONFIGURACAO.intervaloAtualizacao);

        document.addEventListener(
            "visibilitychange",
            () => {
                if (
                    !document.hidden &&
                    sessionStorage.getItem(
                        CONFIGURACAO.chaveToken
                    )
                ) {
                    carregarNotificacoesApi();
                }
            }
        );
    });
})();