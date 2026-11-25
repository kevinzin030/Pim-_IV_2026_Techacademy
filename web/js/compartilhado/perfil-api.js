(() => {
    "use strict";

    // Configurações da integração com a API.
    const CONFIGURACAO = {
        api: "http://localhost:3000/api",
        chaveToken: "techacademy_token",
        perfilProfessor: 3,
        perfilAluno: 4
    };

    // Controla as consultas e a navegação entre as telas.
    const estado = {
        requisicao: 0,
        telaAtiva: false
    };

    /**
     * Protege os dados recebidos antes da renderização HTML.
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
     * Encerra a sessão quando a autenticação é inválida.
     */
    function encerrarSessao() {
        sessionStorage.removeItem(
            CONFIGURACAO.chaveToken
        );

        sessionStorage.removeItem("usuario_logado");

        window.location.replace("../../index.html");
    }

    /**
     * Consulta um endpoint protegido pelo JWT.
     */
    async function consultarApi(endpoint) {
        const token = sessionStorage.getItem(
            CONFIGURACAO.chaveToken
        );

        if (!token) {
            encerrarSessao();

            throw new Error("Sessão não encontrada.");
        }

        const resposta = await fetch(
            `${CONFIGURACAO.api}${endpoint}`,
            {
                method: "GET",
                cache: "no-store",
                headers: {
                    Authorization: `Bearer ${token}`
                }
            }
        );

        const dados = await resposta.json();

        if (resposta.status === 401) {
            encerrarSessao();

            throw new Error("Sessão expirada.");
        }

        if (!resposta.ok) {
            throw new Error(
                dados.mensagem ||
                "Não foi possível consultar os dados."
            );
        }

        return dados;
    }

    /**
     * Identifica o registro acadêmico conforme o perfil.
     */
    function obterRegistro(usuario) {
        if (
            Number(usuario.idPerfil) ===
            CONFIGURACAO.perfilProfessor
        ) {
            return usuario.registroProfessor;
        }

        return usuario.matriculaAluno;
    }

    /**
     * Atualiza a identificação exibida na barra lateral.
     */
    function atualizarIdentificacao(usuario) {
        const professor =
            Number(usuario.idPerfil) ===
            CONFIGURACAO.perfilProfessor;

        const prefixo = professor ? "prof" : "aluno";

        const nome = document.getElementById(
            `${prefixo}-name-sidebar`
        );

        const registro = document.getElementById(
            `${prefixo}-email-sidebar`
        );

        if (nome) {
            nome.textContent = usuario.nomeCompleto;
        }

        if (registro) {
            registro.textContent =
                obterRegistro(usuario) || usuario.login;
        }
    }

    /**
     * Renderiza uma informação cadastral somente para leitura.
     */
    function renderizarCampo(rotulo, valor, icone) {
        return `
            <div class="space-y-2">

                <p class="text-xs font-semibold text-slate-500">
                    <i class="${icone} mr-1"></i>
                    ${escaparHtml(rotulo)}
                </p>

                <p class="p-3 rounded-xl bg-slate-50
                          border border-slate-200
                          text-sm font-medium text-slate-800
                          break-words">

                    ${escaparHtml(valor || "Não informado")}

                </p>

            </div>
        `;
    }

    /**
     * Agrupa as disciplinas conforme a turma vinculada.
     * Evita apresentar registros duplicados.
     */
    function agruparTurmas(vinculos) {
        const turmas = new Map();

        vinculos.forEach(vinculo => {
            const idTurma = String(vinculo.idTurma);

            if (!turmas.has(idTurma)) {
                turmas.set(idTurma, {
                    nome: vinculo.nomeTurma,
                    disciplinas: new Map()
                });
            }

            const turma = turmas.get(idTurma);

            if (vinculo.idDisciplina != null) {
                turma.disciplinas.set(
                    String(vinculo.idDisciplina),
                    vinculo.nomeDisciplina
                );
            }
        });

        return Array.from(turmas.values())
            .sort((primeira, segunda) =>
                String(primeira.nome).localeCompare(
                    String(segunda.nome),
                    "pt-BR"
                )
            );
    }

    /**
     * Renderiza os vínculos acadêmicos do usuário.
     */
    function renderizarVinculos(vinculos) {
        const turmas = agruparTurmas(vinculos);

        if (turmas.length === 0) {
            return `
                <p class="text-sm text-slate-500">
                    Nenhum vínculo acadêmico encontrado.
                </p>
            `;
        }

        return turmas.map(turma => {
            const disciplinas = Array.from(
                turma.disciplinas.values()
            ).sort((primeira, segunda) =>
                String(primeira).localeCompare(
                    String(segunda),
                    "pt-BR"
                )
            );

            const lista = disciplinas.map(disciplina => `
                <li class="flex items-center gap-2
                           text-sm text-slate-600 py-2">

                    <i class="fa-solid fa-book-open
                              text-brand-blue text-xs">
                    </i>

                    ${escaparHtml(disciplina)}

                </li>
            `).join("");

            return `
                <article class="p-4 bg-slate-50
                                border border-slate-200
                                rounded-xl">

                    <h3 class="font-semibold text-sm
                               text-slate-800 mb-2">

                        <i class="fa-solid fa-users
                                  text-brand-blue mr-1">
                        </i>

                        ${escaparHtml(turma.nome)}

                    </h3>

                    <ul class="divide-y divide-slate-200">
                        ${lista}
                    </ul>

                </article>
            `;
        }).join("");
    }

    /**
     * Renderiza o perfil com os dados oficiais do Azure SQL.
     * Não disponibiliza operações de edição.
     */
    function renderizarPerfil(usuario, vinculos) {
        const main = document.getElementById(
            "main-content-area"
        );

        const professor =
            Number(usuario.idPerfil) ===
            CONFIGURACAO.perfilProfessor;

        const perfil = professor ? "Professor" : "Aluno";

        const rotuloRegistro = professor
            ? "Registro do Professor (RP)"
            : "Registro Acadêmico (RA)";

        const corPerfil = professor
            ? "bg-blue-100 text-blue-700"
            : "bg-emerald-100 text-emerald-700";

        const iniciais = String(usuario.nomeCompleto || "")
            .trim()
            .split(/\s+/)
            .filter(Boolean)
            .slice(0, 2)
            .map(nome => nome.charAt(0).toUpperCase())
            .join("");

        main.innerHTML = `
            <div class="max-w-3xl mx-auto space-y-5">

                <section class="bg-white p-6 rounded-2xl
                                border border-blue-100 shadow-sm">

                    <div class="flex items-center gap-4
                                pb-5 mb-5 border-b
                                border-slate-100">

                        <div class="w-14 h-14 rounded-full
                                    flex items-center
                                    justify-center font-bold
                                    text-xl shrink-0
                                    ${corPerfil}">

                            ${escaparHtml(iniciais)}

                        </div>

                        <div class="min-w-0">

                            <h2 class="text-lg font-bold
                                       text-slate-800 break-words">

                                ${escaparHtml(usuario.nomeCompleto)}

                            </h2>

                            <p class="text-sm text-slate-500 mt-1">
                                ${perfil}
                            </p>

                        </div>

                    </div>

                    <h3 class="text-sm font-bold
                               text-slate-800 mb-4">

                        Dados cadastrais

                    </h3>

                    <div class="grid grid-cols-1
                                sm:grid-cols-2 gap-4">

                        ${renderizarCampo(
            "Nome completo",
            usuario.nomeCompleto,
            "fa-solid fa-user"
        )}

                        ${renderizarCampo(
            rotuloRegistro,
            obterRegistro(usuario),
            "fa-solid fa-id-card"
        )}

                        ${renderizarCampo(
            "Perfil de acesso",
            usuario.perfil,
            "fa-solid fa-user-shield"
        )}

                        ${renderizarCampo(
            "Situação da conta",
            usuario.ativo ? "Ativa" : "Inativa",
            "fa-solid fa-circle-check"
        )}

                    </div>

                </section>

                <section class="bg-white p-6 rounded-2xl
                                border border-blue-100 shadow-sm">

                    <h3 class="text-sm font-bold
                               text-slate-800 mb-4">

                        <i class="fa-solid fa-graduation-cap
                                  text-brand-blue mr-1">
                        </i>

                        Vínculos acadêmicos

                    </h3>

                    <div class="space-y-3">

                        ${renderizarVinculos(vinculos)}

                    </div>

                </section>

            </div>
        `;

        atualizarIdentificacao(usuario);
    }

    /**
     * Atualiza o menu e o cabeçalho da página.
     */
    function configurarNavegacao(botao) {
        document.querySelectorAll(
            "#sidebar nav button"
        ).forEach(item => {
            item.classList.remove(
                "bg-brand-blue",
                "text-white",
                "shadow-md"
            );

            item.classList.add(
                "text-slate-300",
                "hover:bg-slate-800",
                "hover:text-white"
            );
        });

        botao.classList.remove(
            "text-slate-300",
            "hover:bg-slate-800",
            "hover:text-white"
        );

        botao.classList.add(
            "bg-brand-blue",
            "text-white",
            "shadow-md"
        );

        document.getElementById(
            "page-title"
        ).textContent = "Meu Perfil";

        document.getElementById(
            "page-subtitle"
        ).textContent =
            "Consulte seus dados cadastrais e acadêmicos.";
    }

    /**
     * Verifica se a consulta ainda pertence à tela atual.
     */
    function consultaAtiva(requisicao) {
        return (
            estado.telaAtiva &&
            estado.requisicao === requisicao
        );
    }

    /**
     * Consulta o perfil e seus vínculos acadêmicos.
     */
    async function carregarPerfil() {
        estado.telaAtiva = true;

        const requisicao = ++estado.requisicao;

        const main = document.getElementById(
            "main-content-area"
        );

        main.innerHTML = `
            <div class="py-12 text-center text-slate-500">

                <i class="fa-solid fa-spinner fa-spin
                          text-brand-blue text-2xl">
                </i>

                <p class="text-sm mt-3">
                    Carregando perfil...
                </p>

            </div>
        `;

        try {
            const dadosPerfil = await consultarApi(
                "/auth/perfil"
            );

            if (!consultaAtiva(requisicao)) {
                return;
            }

            const usuario = dadosPerfil.usuario;

            if (!usuario) {
                throw new Error("Perfil não encontrado.");
            }

            const idPerfil = Number(usuario.idPerfil);

            let vinculos;

            if (
                idPerfil === CONFIGURACAO.perfilProfessor
            ) {
                const dados = await consultarApi(
                    "/professores/me/turmas"
                );

                vinculos = dados.turmas;

            } else if (
                idPerfil === CONFIGURACAO.perfilAluno
            ) {
                const dados = await consultarApi(
                    "/alunos/me/disciplinas"
                );

                vinculos = dados.disciplinas;

            } else {
                throw new Error(
                    "Perfil incompatível com este painel."
                );
            }

            if (!Array.isArray(vinculos)) {
                throw new Error(
                    "Não foi possível obter os vínculos acadêmicos."
                );
            }

            if (!consultaAtiva(requisicao)) {
                return;
            }

            renderizarPerfil(usuario, vinculos);

        } catch (erro) {
            if (!consultaAtiva(requisicao)) {
                return;
            }

            console.error(
                "Erro ao consultar perfil:",
                erro
            );

            main.innerHTML = `
                <section class="max-w-md mx-auto
                                bg-white border
                                border-red-100
                                rounded-2xl p-6 text-center">

                    <i class="fa-solid fa-circle-exclamation
                              text-red-500 text-3xl mb-3">
                    </i>

                    <p class="text-sm text-slate-600">

                        ${escaparHtml(erro.message)}

                    </p>

                    <button id="btn-recarregar-perfil"
                            type="button"
                            class="mt-4 px-4 py-2
                                   bg-brand-blue text-white
                                   rounded-lg text-sm font-bold">

                        Tentar novamente

                    </button>

                </section>
            `;

            document.getElementById(
                "btn-recarregar-perfil"
            ).addEventListener(
                "click",
                carregarPerfil
            );
        }
    }

    /**
     * Cancela consultas pendentes ao sair da tela.
     */
    window.addEventListener("click", evento => {
        const botao = evento.target.closest(
            "#sidebar nav button"
        );

        if (botao && botao.id !== "nav-perfil") {
            estado.telaAtiva = false;
            estado.requisicao++;
        }
    }, true);

    /**
     * Substitui a tela antiga de edição pela consulta via API.
     * Preserva as demais funcionalidades dos painéis.
     */
    document.addEventListener("click", evento => {
        const botao = evento.target.closest(
            "#nav-perfil"
        );

        if (!botao) {
            return;
        }

        evento.preventDefault();
        evento.stopImmediatePropagation();

        configurarNavegacao(botao);
        carregarPerfil();

    }, true);

})();