
/**
 * TechAcademy Web
 * Autenticação de alunos e professores pela API Azure.
 *
 * Responsabilidades:
 * - Selecionar o perfil de acesso.
 * - Validar os campos do formulário.
 * - Autenticar o usuário pela API.
 * - Exigir alteração de senha quando necessário.
 * - Armazenar a sessão e redirecionar o usuário.
 */

const API_BASE_URL = "https://techacademy-api-2026-h0ebgpcgechch0c4.brazilsouth-01.azurewebsites.net/api";

const PERFIS = Object.freeze({
    PROFESSOR: 3,
    ALUNO: 4
});

const CHAVE_SESSAO = "usuario_logado";
const CHAVE_TOKEN = "techacademy_token";

let perfilSelecionado = "professor";

// Referências aos elementos já existentes no index.html.
const botaoProfessor = document.getElementById("role-btn-professor");
const botaoAluno = document.getElementById("role-btn-aluno");
const rotuloLogin = document.getElementById("login-label");
const campoLogin = document.getElementById("login-identifier");
const campoSenha = document.getElementById("login-password");
const formularioLogin = document.getElementById("login-form");
const linkEsqueceuSenha = document.getElementById("forgot-password");

/**
 * Alterna o perfil selecionado sem modificar o HTML.
 */
function selecionarRole(perfil) {
    perfilSelecionado = perfil;

    const classeAtiva =
        "flex-1 py-2 text-xs font-semibold rounded-lg transition-all shadow-sm bg-white text-brand-dark cursor-pointer";

    const classeInativa =
        "flex-1 py-2 text-xs font-semibold rounded-lg text-slate-500 hover:text-brand-dark transition-all cursor-pointer";

    botaoProfessor.className =
        perfil === "professor" ? classeAtiva : classeInativa;

    botaoAluno.className =
        perfil === "aluno" ? classeAtiva : classeInativa;

    rotuloLogin.textContent =
        perfil === "professor"
            ? "RP (Registro de Professor)"
            : "RA (Registro Acadêmico)";

    campoLogin.placeholder =
        perfil === "professor"
            ? "Digite seu RP"
            : "Digite seu RA";

    campoLogin.value = "";
    campoSenha.value = "";
}

/**
 * Executa requisições HTTP e apresenta os erros
 * retornados pela API.
 */
async function enviarRequisicao(caminho, dados, token = null) {
    const cabecalhos = {
        "Content-Type": "application/json"
    };

    if (token) {
        cabecalhos.Authorization = `Bearer ${token}`;
    }

    const resposta = await fetch(`${API_BASE_URL}${caminho}`, {
        method: "POST",
        headers: cabecalhos,
        body: JSON.stringify(dados)
    });

    const resultado = await resposta.json().catch(() => ({}));

    if (!resposta.ok) {
        throw new Error(
            resultado.mensagem ||
            `Erro na comunicação com a API (${resposta.status}).`
        );
    }

    return resultado;
}

/**
 * Verifica se o usuário pertence ao perfil selecionado.
 * O ID do perfil vem da API, nunca do formulário.
 */
function validarPerfilUsuario(usuario) {
    const perfilEsperado =
        perfilSelecionado === "professor"
            ? PERFIS.PROFESSOR
            : PERFIS.ALUNO;

    if (Number(usuario.idPerfil) !== perfilEsperado) {
        throw new Error(
            "O usuário não pertence ao perfil selecionado."
        );
    }
}

/**
 * Abre uma janela obrigatória de alteração de senha.
 * O usuário não recebe acesso ao painel antes da
 * confirmação da API.
 */
async function solicitarAlteracaoObrigatoria(token) {
    return new Promise((resolve, reject) => {
        const sobreposicao = document.createElement("div");

        sobreposicao.style.cssText = `
            position: fixed;
            inset: 0;
            z-index: 9999;
            background: rgba(15, 23, 42, 0.9);
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 16px;
        `;

        const janela = document.createElement("form");

        janela.style.cssText = `
            width: 100%;
            max-width: 420px;
            background: white;
            border-radius: 16px;
            padding: 24px;
            display: flex;
            flex-direction: column;
            gap: 12px;
            color: #0f172a;
        `;

        const titulo = document.createElement("h2");
        titulo.textContent = "Alteração obrigatória de senha";
        titulo.style.cssText =
            "font-size: 20px; font-weight: 700;";

        const descricao = document.createElement("p");
        descricao.textContent =
            "Altere sua senha para continuar utilizando o TechAcademy.";
        descricao.style.fontSize = "14px";

        /**
         * Cria campos de senha com identificação
         * semântica e preenchimento obrigatório.
         */
        function criarCampo(nome, texto, autocomplete) {
            const campo = document.createElement("input");

            campo.name = nome;
            campo.type = "password";
            campo.placeholder = texto;
            campo.autocomplete = autocomplete;
            campo.required = true;
            campo.style.cssText = `
                padding: 12px;
                border: 1px solid #cbd5e1;
                border-radius: 8px;
                width: 100%;
            `;

            return campo;
        }

        const senhaAtual = criarCampo(
            "senhaAtual",
            "Senha atual",
            "current-password"
        );

        const novaSenha = criarCampo(
            "novaSenha",
            "Nova senha",
            "new-password"
        );

        const confirmarSenha = criarCampo(
            "confirmarNovaSenha",
            "Confirme a nova senha",
            "new-password"
        );

        const instrucoes = document.createElement("p");
        instrucoes.textContent =
            "A nova senha deve ter pelo menos 8 caracteres, incluindo maiúscula, minúscula, número e caractere especial.";
        instrucoes.style.cssText =
            "font-size: 12px; color: #475569;";

        const mensagemErro = document.createElement("p");
        mensagemErro.style.cssText =
            "font-size: 13px; color: #dc2626;";

        const botaoConfirmar = document.createElement("button");
        botaoConfirmar.type = "submit";
        botaoConfirmar.textContent = "Alterar senha";
        botaoConfirmar.style.cssText = `
            background: #2563eb;
            color: white;
            border: none;
            padding: 12px;
            border-radius: 8px;
            cursor: pointer;
            font-weight: 600;
        `;

        const botaoCancelar = document.createElement("button");
        botaoCancelar.type = "button";
        botaoCancelar.textContent = "Cancelar login";
        botaoCancelar.style.cssText = `
            padding: 10px;
            color: #475569;
            cursor: pointer;
        `;

        /**
         * Cancela a autenticação sem criar uma sessão.
         */
        botaoCancelar.addEventListener("click", () => {
            sobreposicao.remove();
            reject(new Error("Alteração de senha cancelada."));
        });

        /**
         * Envia a alteração para a API.
         * O servidor valida a senha e atualiza o Azure SQL.
         */
        janela.addEventListener("submit", async (evento) => {
            evento.preventDefault();
            mensagemErro.textContent = "";

            if (novaSenha.value !== confirmarSenha.value) {
                mensagemErro.textContent =
                    "A confirmação da senha não corresponde.";
                return;
            }

            botaoConfirmar.disabled = true;
            botaoConfirmar.textContent = "Alterando senha...";

            try {
                await enviarRequisicao(
                    "/auth/alterar-senha",
                    {
                        senhaAtual: senhaAtual.value,
                        novaSenha: novaSenha.value,
                        confirmarNovaSenha: confirmarSenha.value
                    },
                    token
                );

                sobreposicao.remove();
                resolve();
            } catch (erro) {
                mensagemErro.textContent = erro.message;
            } finally {
                botaoConfirmar.disabled = false;
                botaoConfirmar.textContent = "Alterar senha";
            }
        });

        janela.append(
            titulo,
            descricao,
            senhaAtual,
            novaSenha,
            confirmarSenha,
            instrucoes,
            mensagemErro,
            botaoConfirmar,
            botaoCancelar
        );

        sobreposicao.appendChild(janela);
        document.body.appendChild(sobreposicao);
        senhaAtual.focus();
    });
}

/**
 * Guarda a sessão no formato utilizado
 * pelos painéis existentes do TechAcademy.
 */
function salvarSessao(usuario, token) {
    const perfil =
        Number(usuario.idPerfil) === PERFIS.PROFESSOR
            ? "professor"
            : "aluno";

    const dadosSessao = {
        idUsuario: usuario.idUsuario,
        idPerfil: Number(usuario.idPerfil),
        role: perfil,
        nome: usuario.nomeCompleto,
        nomeCompleto: usuario.nomeCompleto,
        login: usuario.login,
        matricula: usuario.login,
        deveAlterarSenha: false
    };

    sessionStorage.setItem(
        CHAVE_SESSAO,
        JSON.stringify(dadosSessao)
    );

    // O token fica na sessão da aba, não no localStorage.
    sessionStorage.setItem(CHAVE_TOKEN, token);
}

/**
 * Redireciona o usuário para o painel do perfil
 * confirmado pela API.
 */
function redirecionarUsuario(usuario) {
    const destino =
        Number(usuario.idPerfil) === PERFIS.PROFESSOR
            ? "pages/professor/dashboard-professor.html"
            : "pages/aluno/dashboard-aluno.html";

    window.location.assign(destino);
}

/**
 * Autentica o usuário com login e senha reais.
 */
async function realizarLogin(evento) {
    evento.preventDefault();

    const login = campoLogin.value.trim();
    const senha = campoSenha.value;

    if (!login || !senha) {
        alert("Informe seu RA/RP e sua senha.");
        return;
    }

    const botaoEntrar =
        formularioLogin.querySelector('button[type="submit"]');

    if (botaoEntrar) {
        botaoEntrar.disabled = true;
    }

    // Elimina dados de autenticações anteriores.
    sessionStorage.removeItem(CHAVE_SESSAO);
    sessionStorage.removeItem(CHAVE_TOKEN);

    try {
        const resposta = await enviarRequisicao(
            "/auth/login",
            { login, senha }
        );

        if (!resposta.token || !resposta.usuario) {
            throw new Error(
                "A API retornou uma resposta de login incompleta."
            );
        }

        validarPerfilUsuario(resposta.usuario);

        // Bloqueia o acesso até a senha ser alterada.
        if (resposta.usuario.deveAlterarSenha) {
            await solicitarAlteracaoObrigatoria(resposta.token);
        }

        salvarSessao(resposta.usuario, resposta.token);
        redirecionarUsuario(resposta.usuario);
    } catch (erro) {
        console.error("Falha na autenticação:", erro.message);

        if (erro instanceof TypeError) {
            alert(
                "Não foi possível conectar à API. Verifique a conexão ou a configuração de CORS."
            );
        } else {
            alert(erro.message);
        }
    } finally {
        if (botaoEntrar) {
            botaoEntrar.disabled = false;
        }
    }
}

/**
 * Inicializa os eventos da página de autenticação.
 */
function inicializarLogin() {
    botaoProfessor?.addEventListener("click", () => {
        selecionarRole("professor");
    });

    botaoAluno?.addEventListener("click", () => {
        selecionarRole("aluno");
    });

    formularioLogin?.addEventListener("submit", realizarLogin);

    linkEsqueceuSenha?.addEventListener("click", (evento) => {
        evento.preventDefault();

        alert(
            "Entre em contato com o administrador para redefinir sua senha."
        );
    });

    selecionarRole("professor");
}

inicializarLogin();
