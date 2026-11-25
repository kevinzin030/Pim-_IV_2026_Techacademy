

const CONFIGURACAO_ALUNO = {
    chaveUsuario: 'usuario_logado',
    chaveToken: 'techacademy_token',
    chaveDadosCompartilhados: 'techacademy_shared_v1',
    chaveFaltas: 'techacademy_faltas',
    paginaLogin: 'index.html',
    perfilAluno: 4
};

/**
 * Estado central do painel acadêmico.
 *
 * Os dados pessoais são preenchidos após a autenticação.
 * As disciplinas e os questionários são carregados
    * pela integração com a API REST.
 *
 * Nenhum registro acadêmico fictício é inicializado.
 */
const estadoAluno = {
    aluno: {
        nome: '',
        email: '',
        matricula: '',
        curso: 'Análise e Desenvolvimento de Sistemas',
        xp: 0,
        streak: 0
    },

    disciplinas: [],

    notificacoes: []
};

/**
 * Mantém o nome "state" como referência ao estado central.
 *
 * Isso garante compatibilidade com eventuais scripts existentes
 * que já utilizem esse identificador.
 */
const state = estadoAluno;

/**
 * Substitui os marcadores de um template HTML.
 *
 * Os templates já estão definidos no dashboard-aluno.html.
 * Portanto, esta função não cria componentes visuais novos.
 */
function renderHtmlTemplate(templateId, valores = {}) {
    const template = document.getElementById(templateId);

    if (!template) {
        console.error(
            `Template HTML não encontrado: ${templateId}`
        );
        return '';
    }

    let html = template.innerHTML;

    for (const [chave, valor] of Object.entries(valores)) {
        html = html.replaceAll(
            `__TA_SLOT_${chave}__`,
            valor == null ? '' : String(valor)
        );
    }

    return html;
}

/**
 * Recupera o usuário salvo pelo processo de login.
 *
 * Aceita tanto "idPerfil" quanto "role", pois o login
 * pode armazenar essas informações em formatos diferentes.
 */
function obterUsuarioAutenticado() {
    try {
        const usuarioJson = sessionStorage.getItem(
            CONFIGURACAO_ALUNO.chaveUsuario
        );

        if (!usuarioJson) {
            return null;
        }

        return JSON.parse(usuarioJson);
    } catch (erro) {
        console.error(
            'Erro ao recuperar usuário autenticado:',
            erro
        );

        return null;
    }
}

/**
 * Verifica se a sessão pertence a um aluno.
 *
 * A validação definitiva de autorização deve continuar
 * sendo realizada pela API nos endpoints protegidos.
 */
function usuarioPossuiPerfilAluno(usuario) {
    if (!usuario) {
        return false;
    }

    const perfilNumerico = Number(
        usuario.idPerfil ?? usuario.usuario?.idPerfil
    );

    const perfilTexto = String(
        usuario.role ?? usuario.perfil ?? ''
    ).toLowerCase();

    return (
        perfilNumerico === CONFIGURACAO_ALUNO.perfilAluno ||
        perfilTexto === 'aluno'
    );
}

/**
 * Preenche o estado com os dados da sessão autenticada.
 *
 * Não utiliza nomes, emails ou matrículas fictícios.
 */
function carregarIdentidadeAluno() {
    const usuario = obterUsuarioAutenticado();

    if (!usuarioPossuiPerfilAluno(usuario)) {
        console.warn(
            'Acesso ao painel do aluno sem sessão válida.'
        );

        window.location.replace(
            CONFIGURACAO_ALUNO.paginaLogin
        );

        return false;
    }

    const dadosUsuario = usuario.usuario || usuario;

    estadoAluno.aluno.nome = String(
        dadosUsuario.nomeCompleto ||
        dadosUsuario.nome ||
        dadosUsuario.login ||
        ''
    );

    estadoAluno.aluno.email = String(
        dadosUsuario.email || ''
    );

    estadoAluno.aluno.matricula = String(
        dadosUsuario.matricula ||
        dadosUsuario.login ||
        ''
    );

    return true;
}

/**
 * Atualiza os dados pessoais apresentados na barra lateral.
 *
 * Os elementos já existem no HTML original.
 */
function atualizarIdentificacaoAluno() {
    const nomeBarraLateral = document.getElementById(
        'aluno-name-sidebar'
    );

    const emailBarraLateral = document.getElementById(
        'aluno-email-sidebar'
    );

    if (nomeBarraLateral) {
        nomeBarraLateral.textContent =
            estadoAluno.aluno.nome;
    }

    if (emailBarraLateral) {
        emailBarraLateral.textContent =
            estadoAluno.aluno.email ||
            estadoAluno.aluno.matricula;
    }
}

/**
 * Converte uma data para o formato brasileiro.
 */
function dataHoraPt(valor) {
    if (!valor) {
        return 'Data não registrada';
    }

    const data = new Date(valor);

    if (Number.isNaN(data.getTime())) {
        return String(valor);
    }

    return data.toLocaleString('pt-BR', {
        dateStyle: 'short',
        timeStyle: 'short'
    });
}

/**
 * Recupera o objeto compartilhado entre os painéis Web.
 *
 * Este armazenamento é local ao navegador.
 * Ele ainda não representa dados persistidos no Azure SQL.
 */
function obterDadosCompartilhados() {
    try {
        return JSON.parse(
            localStorage.getItem(
                CONFIGURACAO_ALUNO.chaveDadosCompartilhados
            ) || '{}'
        );
    } catch (erro) {
        console.warn(
            'Não foi possível recuperar dados compartilhados:',
            erro
        );

        return {};
    }
}

/**
 * Salva o objeto compartilhado no navegador.
 */
function persistirDadosCompartilhados(dados) {
    try {
        localStorage.setItem(
            CONFIGURACAO_ALUNO.chaveDadosCompartilhados,
            JSON.stringify(dados)
        );

        return true;
    } catch (erro) {
        console.error(
            'Erro ao salvar dados compartilhados:',
            erro
        );

        return false;
    }
}

/**
 * Recupera as notificações criadas no navegador.
 *
 * O sino de notificações já faz parte do HTML original.
 * Esta função apenas recupera os avisos exibidos nele.
 */
function carregarNotificacoes() {
    const dadosCompartilhados =
        obterDadosCompartilhados();

    estadoAluno.notificacoes = Array.isArray(
        dadosCompartilhados.notificacoes
    )
        ? dadosCompartilhados.notificacoes
        : [];

    atualizarSinoNotificacoes();
}

/**
 * Atualiza o número de notificações não lidas.
 */
function atualizarSinoNotificacoes() {
    const contador = document.getElementById(
        'notification-count'
    );

    if (!contador) {
        return;
    }

    const quantidadeNaoLidas =
        estadoAluno.notificacoes.filter(
            notificacao => !notificacao.lida
        ).length;

    contador.textContent =
        quantidadeNaoLidas > 99
            ? '99+'
            : quantidadeNaoLidas;

    contador.classList.toggle(
        'hidden',
        quantidadeNaoLidas === 0
    );
}

/**
 * Identifica o título apropriado para cada notificação.
 */
function obterTituloTipoNotificacao(tipo) {
    switch (tipo) {
        case 'quiz':
            return 'Novo quiz';
        default:
            return 'Nova aula publicada';
    }
}

/**
 * Exibe as notificações no painel existente.
 */
function renderNotificacoes() {
    const painel = document.getElementById(
        'notification-panel'
    );

    if (!painel) {
        return;
    }

    const notificacoesOrdenadas = [
        ...estadoAluno.notificacoes
    ].sort(
        (primeira, segunda) =>
            new Date(segunda.dataHora) -
            new Date(primeira.dataHora)
    );

    const listaHtml = notificacoesOrdenadas.length
        ? notificacoesOrdenadas.map(notificacao =>
            renderHtmlTemplate(
                'ta-aluno-fragment-029',
                {
                    '0': notificacao.lida
                        ? 'bg-slate-50'
                        : 'bg-blue-50',

                    '1': obterTituloTipoNotificacao(
                        notificacao.tipo
                    ),

                    '2': notificacao.titulo,

                    '3': notificacao.disciplina || '',

                    '4': dataHoraPt(
                        notificacao.dataHora
                    )
                }
            )
        ).join('')
        : renderHtmlTemplate(
            'ta-aluno-fragment-028'
        );

    painel.innerHTML = renderHtmlTemplate(
        'ta-aluno-fragment-027',
        {
            '0': listaHtml
        }
    );

    const botaoMarcarLidas = document.getElementById(
        'marcar-notificacoes-lidas'
    );

    if (botaoMarcarLidas) {
        botaoMarcarLidas.addEventListener(
            'click',
            marcarTodasNotificacoesComoLidas
        );
    }
}

/**
 * Marca todas as notificações como lidas.
 */
function marcarTodasNotificacoesComoLidas() {
    estadoAluno.notificacoes =
        estadoAluno.notificacoes.map(
            notificacao => ({
                ...notificacao,
                lida: true
            })
        );

    const dadosCompartilhados =
        obterDadosCompartilhados();

    persistirDadosCompartilhados({
        ...dadosCompartilhados,
        notificacoes: estadoAluno.notificacoes
    });

    renderNotificacoes();
    atualizarSinoNotificacoes();
}

/**
 * Salva disciplinas e notificações demonstrativas.
 *
 * A persistência definitiva deverá ser feita pela API.
 */
function salvarDadosCompartilhados() {
    const dadosAtuais = obterDadosCompartilhados();

    persistirDadosCompartilhados({
        ...dadosAtuais,
        disciplinas: estadoAluno.disciplinas,
        notificacoes: estadoAluno.notificacoes
    });
}

/**
 * Normaliza os questionários carregados do navegador.
 *
 * Aceita o formato antigo de pergunta única e o formato
 * com várias perguntas, preservando a compatibilidade.
 */
function normalizarQuestionario(questionario) {
    const perguntas = Array.isArray(
        questionario.perguntas
    ) && questionario.perguntas.length
        ? questionario.perguntas
        : [
            {
                pergunta: questionario.pergunta || '',
                opcoes:
                    questionario.opcoes ||
                    questionario.opcoesResposta ||
                    [],
                correta: Number(
                    questionario.correta ?? 0
                )
            }
        ];

    return {
        ...questionario,
        perguntas
    };
}

/**
 * Recupera as disciplinas compartilhadas pelo painel Web.
 */
function carregarDadosCompartilhados() {
    const dadosCompartilhados =
        obterDadosCompartilhados();

    if (
        !Array.isArray(
            dadosCompartilhados.disciplinas
        ) ||
        dadosCompartilhados.disciplinas.length === 0
    ) {
        return;
    }

    estadoAluno.disciplinas =
        dadosCompartilhados.disciplinas.map(
            (disciplina, indice) => ({
                ...disciplina,

                nome:
                    disciplina.nome ||
                    `Disciplina ${indice + 1}`,

                codigo:
                    disciplina.codigo ||
                    `TEC-${indice + 1}`,

                notas: disciplina.notas || {
                    p1: 0,
                    p2: 0,
                    trabalho: 0,
                    quizzes: 0
                },

                aulas: Array.isArray(
                    disciplina.aulas
                )
                    ? disciplina.aulas
                    : [],

                questionarios: Array.isArray(
                    disciplina.questionarios
                )
                    ? disciplina.questionarios.map(
                        normalizarQuestionario
                    )
                    : []
            })
        );
}

/**
 * Calcula a média ponderada do boletim.
 *
 * P1: 30%
 * P2: 30%
 * Trabalho: 25%
 * Quiz: 15%
 */
function calcularMedia(
    primeiraProva,
    segundaProva,
    trabalho,
    quiz
) {
    const media =
        Number(primeiraProva) * 0.30 +
        Number(segundaProva) * 0.30 +
        Number(trabalho) * 0.25 +
        Number(quiz) * 0.15;

    return media.toFixed(1);
}

/**
 * Inicializa os recursos do painel após o HTML carregar.
 */
window.addEventListener(
    'DOMContentLoaded',
    inicializarPainelAluno
);

/**
 * Organiza a sequência de inicialização do painel.
 */
function inicializarPainelAluno() {
    const sessaoValida = carregarIdentidadeAluno();

    if (!sessaoValida) {
        return;
    }

    atualizarIdentificacaoAluno();
    carregarDadosCompartilhados();
    carregarNotificacoes();

    configurarEventosNavegacao();
    configurarEventosConteudo();
    configurarSinoNotificacoes();

    renderDisciplinas();
    updateGamificationWidget();

    verificarAlertaFaltasCriticas();
}

/**
 * Configura os botões principais da barra lateral.
 */
function configurarEventosNavegacao() {
    const itensNavegacao = [
        {
            id: 'nav-disciplinas',
            titulo: 'Disciplinas & Aulas',
            renderizar: renderDisciplinas
        },
        {
            id: 'nav-quizzes',
            titulo: 'Questionários',
            renderizar: renderQuizzes
        },
        {
            id: 'nav-boletim',
            titulo: 'Meu Boletim / Notas & Faltas',
            renderizar: renderBoletim
        },
        {
            id: 'nav-perfil',
            titulo: 'Meu Perfil',
            renderizar: renderPerfil
        }
    ];

    itensNavegacao.forEach(item => {
        const botao = document.getElementById(
            item.id
        );

        if (!botao) {
            console.warn(
                `Botão de navegação não encontrado: ${item.id}`
            );

            return;
        }

        botao.addEventListener(
            'click',
            evento => {
                setActiveNav(
                    evento.currentTarget,
                    item.renderizar,
                    item.titulo
                );
            }
        );
    });
}

/**
 * Mantém compatibilidade com o nome usado anteriormente.
 */
function bindNavEvents() {
    configurarEventosNavegacao();
}

/**
 * Configura ações de disciplinas, aulas e questionários.
 *
 * A delegação de eventos permite que os botões continuem
 * funcionando mesmo quando o HTML é renderizado novamente.
 */
function configurarEventosConteudo() {
    document.addEventListener(
        'click',
        evento => {
            const elementoAcao =
                evento.target.closest(
                    '[data-action]'
                );

            if (elementoAcao) {
                const disciplinaId =
                    elementoAcao.dataset.disciplinaId;

                switch (
                elementoAcao.dataset.action
                ) {
                    case 'open-discipline':
                        abrirDetalheDisciplina(
                            disciplinaId
                        );
                        return;

                    case 'back-to-disciplines':
                        renderDisciplinas();
                        return;

                    case 'back-to-discipline':
                        evento.preventDefault();

                        setActiveNav(
                            document.getElementById(
                                'nav-disciplinas'
                            ),
                            renderDisciplinas,
                            'Disciplinas & Aulas'
                        );

                        window.setTimeout(
                            () => {
                                abrirDetalheDisciplina(
                                    disciplinaId
                                );
                            },
                            0
                        );

                        return;
                }
            }

            const botaoQuiz =
                evento.target.closest(
                    '[data-abrir-quiz="1"]'
                );

            if (!botaoQuiz) {
                return;
            }

            evento.preventDefault();
            evento.stopPropagation();

            abrirQuiz(
                botaoQuiz.dataset.disciplinaId,
                botaoQuiz.dataset.quizId
            );
        }
    );
}

/**
 * Configura o sino de notificações já presente no HTML.
 */
function configurarSinoNotificacoes() {
    const botaoSino = document.getElementById(
        'notification-bell'
    );

    const painelNotificacoes = document.getElementById(
        'notification-panel'
    );

    if (!botaoSino || !painelNotificacoes) {
        return;
    }

    botaoSino.addEventListener(
        'click',
        () => {
            painelNotificacoes.classList.toggle(
                'hidden'
            );

            const painelAberto =
                !painelNotificacoes.classList.contains(
                    'hidden'
                );

            if (painelAberto) {
                renderNotificacoes();
            }
        }
    );
}

/**
 * Atualiza o item selecionado na navegação.
 *
 * As classes CSS são as mesmas do código original.
 */
function setActiveNav(
    botaoSelecionado,
    funcaoRenderizacao,
    tituloPagina
) {
    if (!botaoSelecionado) {
        return;
    }

    document.querySelectorAll(
        'nav button'
    ).forEach(botao => {
        botao.className =
            'w-full flex items-center gap-3 px-3 py-2.5 ' +
            'rounded-xl text-sm font-medium transition-all ' +
            'text-slate-300 hover:bg-slate-800 hover:text-white';
    });

    botaoSelecionado.className =
        'w-full flex items-center gap-3 px-3 py-2.5 ' +
        'rounded-xl text-sm font-medium transition-all ' +
        'bg-brand-blue text-white shadow-md';

    const elementoTitulo = document.getElementById(
        'page-title'
    );

    if (elementoTitulo) {
        elementoTitulo.textContent = tituloPagina;
    }

    funcaoRenderizacao();
}

/**
 * Atualiza os indicadores demonstrativos de XP e sequência.
 *
 * O valor definitivo de XP deverá vir da API,
 * utilizando a tabela AlunoXP do Azure SQL.
 */
function updateGamificationWidget() {
    const widgetXp = document.getElementById(
        'widget-xp'
    );

    const contadorSequencia = document.getElementById(
        'streak-count'
    );

    if (widgetXp) {
        widgetXp.innerHTML = renderHtmlTemplate(
            'ta-aluno-fragment-026',
            {
                '0': estadoAluno.aluno.xp
            }
        );
    }

    if (contadorSequencia) {
        contadorSequencia.textContent =
            estadoAluno.aluno.streak;
    }
}

/**
 * Renderiza a lista de disciplinas.
 */
function renderDisciplinas() {
    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (!conteudoPrincipal) {
        return;
    }

    const disciplinasHtml =
        estadoAluno.disciplinas.map(
            disciplina => {
                return renderHtmlTemplate(
                    'ta-aluno-fragment-025',
                    {
                        '0': disciplina.id,
                        '1': disciplina.codigo,
                        '2': disciplina.nome,
                        '3': disciplina.aulas.length,
                        '4': disciplina.questionarios.length
                    }
                );
            }
        ).join('');

    conteudoPrincipal.innerHTML =
        renderHtmlTemplate(
            'ta-aluno-fragment-024',
            {
                '0': disciplinasHtml
            }
        );
}

/**
 * Renderiza um questionário associado a uma aula.
 */
function renderizarQuizDaAula(
    disciplina,
    questionario
) {
    const quantidadePerguntas =
        questionario.perguntas?.length || 1;

    return renderHtmlTemplate(
        'ta-aluno-fragment-023',
        {
            '0': questionario.titulo,
            '1': quantidadePerguntas,
            '2': questionario.respondido
                ? 'Concluído'
                : 'Ainda não iniciado',
            '3': questionario.xp,
            '4': dataHoraPt(
                questionario.dataHora
            ),
            '5': disciplina.id,
            '6': questionario.id,
            '7': questionario.respondido
                ? 'Ver resultado'
                : 'Acessar quiz'
        }
    );
}

/**
 * Renderiza uma aula, seus materiais acadêmicos
 * e os questionários vinculados.
 *
 * @param {Object} disciplina Disciplina selecionada.
 * @param {Object} aula Aula acadêmica.
 * @param {number} indiceAula Posição da aula.
 * @returns {string} HTML da aula.
 */
function renderizarAula(
    disciplina,
    aula,
    indiceAula
) {
    // Seleciona exclusivamente os quizzes da aula.
    const questionariosDaAula =
        disciplina.questionarios.filter(
            questionario => {
                const identificadorAula =
                    questionario.idAula ??
                    questionario.aulaId;

                return (
                    identificadorAula != null &&
                    String(identificadorAula) ===
                    String(aula.id)
                );
            }
        );

    const quantidadeMateriais = Number(
        aula.totalMateriais ?? 0
    );

    const descricaoMateriais =
        quantidadeMateriais === 1
            ? "1 material acadêmico"
            : `${quantidadeMateriais} materiais acadêmicos`;

    const situacao = aula.realizada
        ? "Realizada"
        : "Planejada";

    // Formata a data sem deslocamento de fuso horário.
    const dataAula = aula.data
        ? String(aula.data)
            .slice(0, 10)
            .split("-")
            .reverse()
            .join("/")
        : "Data não registrada";

    // Os materiais são consultados ao abrir a disciplina.
    const materiaisHtml = `
        <div data-materiais-aula="${Number(aula.id)}"
             class="space-y-3">

            <p class="text-xs text-slate-500">

                Consultando materiais acadêmicos...

            </p>

        </div>
    `;

    const questionariosHtml =
        questionariosDaAula.length > 0
            ? questionariosDaAula.map(
                questionario =>
                    renderizarQuestionarioGeral(
                        disciplina,
                        questionario
                    )
            ).join("")
            : renderHtmlTemplate(
                "ta-aluno-fragment-022"
            );

    // Preserva o template original da aula.
    return renderHtmlTemplate(
        "ta-aluno-fragment-018",
        {
            "0": `Aula ${indiceAula + 1} - ${aula.titulo}`,

            "1": descricaoMateriais,

            "2": `${dataAula} · ${situacao}`,

            "3": materiaisHtml,

            "4": "",

            "5": questionariosHtml
        }
    );
}

/**
 * Renderiza um questionário da disciplina,
 * diferenciando os pendentes dos concluídos.
 *
 * @param {Object} disciplina Disciplina vinculada.
 * @param {Object} questionario Questionário acadêmico.
 * @returns {string} HTML do cartão.
 */
function renderizarQuestionarioGeral(
    disciplina,
    questionario
) {
    const concluido = Boolean(
        questionario.respondido
    );

    const xpMaximo = Number(
        questionario.xpMaximo ??
        questionario.xp ??
        0
    );

    const xpGanho = Number(
        questionario.xpGanho ??
        questionario.pontuacao ??
        0
    );

    const acertos = Number(
        questionario.acertos ?? 0
    );

    const totalPerguntas = Number(
        questionario.totalPerguntas ??
        questionario.perguntas?.length ??
        0
    );

    // Define a descrição conforme a situação do quiz.
    const descricao = concluido
        ? `${acertos} de ${totalPerguntas} acertos · +${xpGanho} XP`
        : `Recompensa disponível: ${xpMaximo} XP`;

    const classeDescricao = concluido
        ? 'text-emerald-700'
        : 'text-amber-700';

    const classeBotao = concluido
        ? 'bg-emerald-600 hover:bg-emerald-700 text-white'
        : 'bg-brand-blue hover:bg-brand-navy text-white';

    return renderHtmlTemplate(
        'ta-aluno-fragment-016',
        {
            '0': questionario.titulo,
            '1': disciplina.id,
            '2': questionario.id,
            '3': classeDescricao,
            '4': descricao,
            '5': classeBotao,
            '6': concluido
                ? 'Ver resultado'
                : 'Acessar quiz'
        }
    );
}

/**
 * Renderiza um grupo de questionários conforme
 * sua situação de conclusão.
 *
 * @param {Object} disciplina Disciplina selecionada.
 * @param {Array} questionarios Questionários do grupo.
 * @param {boolean} concluido Situação do grupo.
 * @returns {string} HTML do agrupamento.
 */
function renderizarGrupoQuestionarios(
    disciplina,
    questionarios,
    concluido
) {
    const quantidade = questionarios.length;

    const titulo = concluido
        ? `Questionários concluídos (${quantidade})`
        : `Questionários pendentes (${quantidade})`;

    const classeTitulo = concluido
        ? 'text-emerald-700'
        : 'text-amber-700';

    const icone = concluido
        ? 'fa-solid fa-circle-check'
        : 'fa-solid fa-clock';

    const mensagemVazia = concluido
        ? 'Nenhum questionário concluído.'
        : 'Nenhum questionário pendente.';

    // Renderiza os cartões ou informa que o grupo está vazio.
    const questionariosHtml = quantidade > 0
        ? questionarios.map(
            questionario =>
                renderizarQuestionarioGeral(
                    disciplina,
                    questionario
                )
        ).join('')
        : `
            <p class="text-sm text-slate-400">
                ${mensagemVazia}
            </p>
        `;

    return renderHtmlTemplate(
        'ta-aluno-fragment-015',
        {
            '0': questionariosHtml,
            '1': classeTitulo,
            '2': icone,
            '3': titulo
        }
    );
}

/**
 * Abre os detalhes da disciplina selecionada.
 *
 * Apresenta aulas, materiais acadêmicos e
 * questionários separados por situação.
 *
 * @param {string} disciplinaId Identificador da disciplina.
 */
function abrirDetalheDisciplina(disciplinaId) {
    const disciplina = estadoAluno.disciplinas.find(
        item =>
            String(item.id) === String(disciplinaId)
    );

    if (!disciplina) {
        console.warn(
            'Disciplina não encontrada:',
            disciplinaId
        );

        return;
    }

    const containerDetalhes = document.getElementById(
        'detalhes-disciplina-container'
    );

    if (!containerDetalhes) {
        return;
    }

    const aulas = Array.isArray(disciplina.aulas)
        ? disciplina.aulas
        : [];

    const questionarios = Array.isArray(
        disciplina.questionarios
    )
        ? disciplina.questionarios
        : [];

    // Renderiza as aulas e os materiais disponíveis.
    const aulasHtml = aulas.length > 0
        ? aulas.map(
            (aula, indice) =>
                renderizarAula(
                    disciplina,
                    aula,
                    indice
                )
        ).join('')
        : renderHtmlTemplate(
            'ta-aluno-fragment-017'
        );

    // Seleciona os questionários gerais da disciplina.
    const questionariosGerais = questionarios.filter(
        questionario => {
            const identificadorAula =
                questionario.idAula ??
                questionario.aulaId;

            return (
                identificadorAula == null ||
                identificadorAula === '' ||
                identificadorAula === 'geral'
            );
        }
    );

    // Separa os questionários pela situação de conclusão.
    const questionariosPendentes =
        questionariosGerais.filter(
            questionario => !questionario.respondido
        );

    const questionariosConcluidos =
        questionariosGerais.filter(
            questionario => Boolean(
                questionario.respondido
            )
        );

    // Renderiza primeiro os questionários pendentes.
    const pendentesHtml = renderizarGrupoQuestionarios(
        disciplina,
        questionariosPendentes,
        false
    );

    // Renderiza os questionários já concluídos.
    const concluidosHtml = renderizarGrupoQuestionarios(
        disciplina,
        questionariosConcluidos,
        true
    );

    // Atualiza os detalhes preservando o layout original.
    containerDetalhes.innerHTML = renderHtmlTemplate(
        'ta-aluno-fragment-014',
        {
            '0': disciplina.codigo,
            '1': disciplina.nome,
            '2': aulas.length,
            '3': aulasHtml,
            '4': pendentesHtml + concluidosHtml
        }
    );

    // Consulta os materiais das aulas exibidas.
    carregarMateriaisDisciplina(
        containerDetalhes
    );

    // Posiciona a visualização no início da disciplina.
    containerDetalhes.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}

/**
 * Obtém as perguntas de um questionário.
 *
 * Preserva compatibilidade com questionários antigos.
 */
function obterPerguntasQuestionario(questionario) {
    if (
        Array.isArray(questionario.perguntas) &&
        questionario.perguntas.length > 0
    ) {
        return questionario.perguntas;
    }

    return [
        {
            pergunta: questionario.pergunta || '',
            opcoes: questionario.opcoes || [],
            correta: Number(
                questionario.correta ?? 0
            )
        }
    ];
}

/**
 * Renderiza as alternativas de uma pergunta.
 */
function renderizarAlternativas(
    pergunta,
    indicePergunta
) {
    return pergunta.opcoes.map(
        (alternativa, indiceAlternativa) =>
            renderHtmlTemplate(
                'ta-aluno-fragment-012',
                {
                    '0': indicePergunta,
                    '1': indiceAlternativa,
                    '2': alternativa
                }
            )
    ).join('');
}

/**
 * Renderiza as perguntas do questionário.
 */
function renderizarPerguntas(perguntas) {
    return perguntas.map(
        (pergunta, indice) =>
            renderHtmlTemplate(
                'ta-aluno-fragment-011',
                {
                    '0': indice + 1,
                    '1': pergunta.pergunta,
                    '2': renderizarAlternativas(
                        pergunta,
                        indice
                    )
                }
            )
    ).join('');
}

/**
 * Abre o questionário selecionado.
 */
function abrirQuiz(disciplinaId, questionarioId) {
    const disciplina =
        estadoAluno.disciplinas.find(
            item => item.id === disciplinaId
        );

    const questionario =
        disciplina?.questionarios.find(
            item => item.id === questionarioId
        );

    if (!disciplina || !questionario) {
        console.error(
            'Questionário não encontrado:',
            {
                disciplinaId,
                questionarioId
            }
        );

        showToast(
            'Não foi possível abrir este quiz.'
        );

        return;
    }

    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (!conteudoPrincipal) {
        return;
    }

    const perguntas =
        obterPerguntasQuestionario(
            questionario
        );

    const conteudoQuestionario =
        questionario.respondido
            ? renderHtmlTemplate(
                'ta-aluno-fragment-013',
                {
                    '0': questionario.pontuacao ?? 0,
                    '1': perguntas.length
                }
            )
            : renderHtmlTemplate(
                'ta-aluno-fragment-010',
                {
                    '0': renderizarPerguntas(
                        perguntas
                    )
                }
            );

    conteudoPrincipal.innerHTML =
        renderHtmlTemplate(
            'ta-aluno-fragment-009',
            {
                '0': disciplina.id,
                '1': questionario.titulo,
                '2': disciplina.nome,
                '3': perguntas.length,
                '4': questionario.xp,
                '5': conteudoQuestionario
            }
        );

    const formulario = document.getElementById(
        'form-responder-quiz'
    );

    if (formulario) {
        formulario.addEventListener(
            'submit',
            evento => {
                evento.preventDefault();

                finalizarQuestionario(
                    formulario,
                    disciplina,
                    questionario,
                    perguntas
                );
            }
        );
    }
}

/**
 * Calcula a quantidade de respostas corretas.
 */
function calcularAcertos(
    respostas,
    perguntas
) {
    return respostas.reduce(
        (totalAcertos, resposta, indice) => {
            const alternativaCorreta =
                Number(
                    perguntas[indice].correta
                );

            return totalAcertos +
                (
                    resposta === alternativaCorreta
                        ? 1
                        : 0
                );
        },
        0
    );
}

/**
 * Finaliza o questionário demonstrativo.
 *
 * A pontuação é calculada localmente apenas para preservar
 * o comportamento atual. A API deverá validar respostas
 * e impedir XP duplicado na integração definitiva.
 */
function finalizarQuestionario(
    formulario,
    disciplina,
    questionario,
    perguntas
) {
    if (questionario.respondido) {
        showToast(
            'Este questionário já foi respondido.'
        );
        return;
    }

    const dadosFormulario =
        new FormData(formulario);

    const respostas = perguntas.map(
        (_, indice) => {
            const valor = dadosFormulario.get(
                `pergunta-${indice}`
            );

            return valor === null
                ? null
                : Number(valor);
        }
    );

    const existemRespostasPendentes =
        respostas.some(
            resposta => resposta === null
        );

    if (existemRespostasPendentes) {
        showToast(
            'Responda todas as perguntas.'
        );
        return;
    }

    const quantidadeAcertos =
        calcularAcertos(
            respostas,
            perguntas
        );

    const xpGanho = Math.round(
        Number(questionario.xp || 0) *
        quantidadeAcertos /
        perguntas.length
    );

    questionario.respondido = true;
    questionario.pontuacao =
        quantidadeAcertos;

    estadoAluno.aluno.xp += xpGanho;
    estadoAluno.aluno.streak += 1;

    salvarDadosCompartilhados();
    updateGamificationWidget();

    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (conteudoPrincipal) {
        conteudoPrincipal.innerHTML =
            renderHtmlTemplate(
                'ta-aluno-fragment-008',
                {
                    '0': quantidadeAcertos,
                    '1': perguntas.length,
                    '2': xpGanho,
                    '3': disciplina.id
                }
            );
    }
}

/**
 * Mantém compatibilidade com botões do HTML original.
 */
function responderQuiz(
    disciplinaId,
    questionarioId
) {
    abrirQuiz(
        disciplinaId,
        questionarioId
    );
}

/**
 * Protege os dados dos materiais acadêmicos.
 *
 * @param {*} valor Conteúdo original.
 * @returns {string} Conteúdo protegido.
 */
function escaparHtmlMaterial(valor) {
    return String(valor ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

/**
 * Consulta um recurso acadêmico utilizando o JWT.
 *
 * @param {string} endpoint Rota da API.
 * @returns {Promise<Response>} Resposta HTTP.
 */
async function consultarRecursoAula(endpoint) {
    const token = sessionStorage.getItem(
        CONFIGURACAO_ALUNO.chaveToken
    );

    if (!token) {
        throw new Error(
            "Sessão não encontrada."
        );
    }

    const resposta = await fetch(
        `http://localhost:3000/api${endpoint}`,
        {
            method: "GET",
            cache: "no-store",

            headers: {
                Authorization: `Bearer ${token}`
            }
        }
    );

    if (!resposta.ok) {
        const dados = await resposta.json()
            .catch(() => ({}));

        throw new Error(
            dados.mensagem ||
            "Não foi possível consultar o material."
        );
    }

    return resposta;
}

/**
 * Realiza o download autenticado de um PDF.
 *
 * @param {number} idMaterial Identificador do material.
 * @param {string} nomeArquivo Nome original do PDF.
 */
async function fazerDownloadPdf(
    idMaterial,
    nomeArquivo
) {
    let urlTemporaria = null;
    let link = null;

    try {
        const identificador = Number(idMaterial);

        if (
            !Number.isInteger(identificador) ||
            identificador <= 0
        ) {
            throw new Error(
                "Material acadêmico inválido."
            );
        }

        const resposta = await consultarRecursoAula(
            `/alunos/me/materiais/${identificador}/download`
        );

        const arquivo = await resposta.blob();

        if (arquivo.type !== "application/pdf") {
            throw new Error(
                "O arquivo recebido não é um PDF."
            );
        }

        urlTemporaria = URL.createObjectURL(
            arquivo
        );

        link = document.createElement("a");

        link.href = urlTemporaria;
        link.download = nomeArquivo || "material.pdf";

        document.body.appendChild(link);

        link.click();

    } catch (erro) {
        console.error(
            "Erro ao baixar material:",
            erro
        );

        showToast(erro.message);

    } finally {
        if (link) {
            link.remove();
        }

        if (urlTemporaria) {
            const url = urlTemporaria;

            window.setTimeout(
                () => URL.revokeObjectURL(url),
                30000
            );
        }
    }
}

/**
 * Renderiza um material acadêmico.
 *
 * @param {Object} material Material retornado pela API.
 * @returns {string} HTML do cartão.
 */
function renderizarMaterialAcademico(material) {
    const idMaterial = Number(
        material.idMaterial
    );

    const titulo = escaparHtmlMaterial(
        material.titulo
    );

    const nomeArquivo = escaparHtmlMaterial(
        material.nomeArquivo
    );

    const tamanho = (
        Number(material.tamanhoBytes ?? 0) / 1024
    ).toFixed(1);

    return `
        <article class="flex items-center
                        justify-between gap-3
                        p-3 bg-white rounded-xl
                        border border-slate-200">

            <div class="flex items-center gap-3
                        min-w-0">

                <i class="fa-solid fa-file-pdf
                          text-red-500 text-2xl">
                </i>

                <div class="min-w-0">

                    <p class="text-sm font-semibold
                              text-slate-800 break-words">

                        ${titulo}

                    </p>

                    <p class="text-xs text-slate-500
                              break-words">

                        ${nomeArquivo} · ${tamanho} KB

                    </p>

                </div>

            </div>

            <button type="button"
                    data-baixar-material="${idMaterial}"
                    data-nome-arquivo="${nomeArquivo}"
                    class="bg-brand-blue text-white
                           hover:bg-brand-navy
                           rounded-lg px-3 py-2
                           text-xs font-semibold
                           transition shrink-0">

                <i class="fa-solid fa-download"></i>

                Baixar PDF

            </button>

        </article>
    `;
}

/**
 * Consulta e apresenta os materiais de uma aula.
 *
 * @param {HTMLElement} container Área dos materiais.
 */
async function carregarMateriaisAula(container) {
    const idAula = Number(
        container.dataset.materiaisAula
    );

    try {
        const resposta = await consultarRecursoAula(
            `/alunos/me/aulas/${idAula}/materiais`
        );

        const dados = await resposta.json();

        if (!Array.isArray(dados.materiais)) {
            throw new Error(
                "Lista de materiais inválida."
            );
        }

        // Evita atualizar uma tela que já foi fechada.
        if (!container.isConnected) {
            return;
        }

        container.innerHTML = dados.materiais.length
            ? dados.materiais.map(
                renderizarMaterialAcademico
            ).join("")
            : renderHtmlTemplate(
                "ta-aluno-fragment-019"
            );

        // Configura os botões de download.
        container.querySelectorAll(
            "[data-baixar-material]"
        ).forEach(botao => {
            botao.addEventListener(
                "click",
                async () => {
                    botao.disabled = true;

                    try {
                        await fazerDownloadPdf(
                            botao.dataset.baixarMaterial,
                            botao.dataset.nomeArquivo
                        );

                    } finally {
                        botao.disabled = false;
                    }
                }
            );
        });

    } catch (erro) {
        if (!container.isConnected) {
            return;
        }

        container.textContent = erro.message;

        console.error(
            "Erro ao consultar materiais:",
            erro
        );
    }
}

/**
 * Carrega os materiais das aulas exibidas
 * na disciplina selecionada.
 *
 * @param {HTMLElement} container Área da disciplina.
 */
function carregarMateriaisDisciplina(container) {
    const containersMateriais =
        container.querySelectorAll(
            "[data-materiais-aula]"
        );

    for (const elemento of containersMateriais) {
        carregarMateriaisAula(elemento);
    }
}

/**
 * Renderiza o cartão de um questionário.
 */
function renderizarCartaoQuestionario(
    disciplina,
    questionario
) {
    const quantidadePerguntas =
        obterPerguntasQuestionario(
            questionario
        ).length;

    return renderHtmlTemplate(
        'ta-aluno-fragment-007',
        {
            '0': questionario.titulo,
            '1': disciplina.nome,
            '2': quantidadePerguntas,
            '3': questionario.xp,
            '4': dataHoraPt(
                questionario.dataHora
            ),
            '5': questionario.respondido
                ? 'text-emerald-700'
                : 'text-amber-700',
            '6': questionario.respondido
                ? 'Concluído'
                : 'Pendente',
            '7': disciplina.id,
            '8': questionario.id,
            '9': questionario.respondido
                ? 'Ver resultado'
                : 'Acessar quiz'
        }
    );
}

/**
 * Renderiza todos os questionários disponíveis.
 */
function renderQuizzes() {
    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (!conteudoPrincipal) {
        return;
    }

    const questionarios =
        estadoAluno.disciplinas.flatMap(
            disciplina =>
                disciplina.questionarios.map(
                    questionario => ({
                        disciplina,
                        questionario
                    })
                )
        );

    const questionariosHtml =
        questionarios.length
            ? questionarios.map(
                item =>
                    renderizarCartaoQuestionario(
                        item.disciplina,
                        item.questionario
                    )
            ).join('')
            : renderHtmlTemplate(
                'ta-aluno-fragment-006'
            );

    conteudoPrincipal.innerHTML =
        renderHtmlTemplate(
            'ta-aluno-fragment-005',
            {
                '0': questionariosHtml
            }
        );
}

/**
 * Mantém compatibilidade com quizzes gerais.
 */
function responderQuizGeral(
    disciplinaId,
    questionarioId
) {
    abrirQuiz(
        disciplinaId,
        questionarioId
    );
}

/**
 * Recupera os registros de faltas do navegador.
 */
function obterRegistrosFaltas() {
    try {
        return JSON.parse(
            localStorage.getItem(
                CONFIGURACAO_ALUNO.chaveFaltas
            ) || '{}'
        );
    } catch (erro) {
        console.warn(
            'Erro ao recuperar faltas:',
            erro
        );

        return {};
    }
}

/**
 * Determina a situação acadêmica pela média.
 */
function obterSituacaoAcademica(media) {
    if (media < 5) {
        return {
            texto: 'Reprovado',
            classe:
                'status-reprovado text-red-700 bg-red-50 px-2.5 py-1 rounded-full text-[10px] font-bold border border-red-200'
        };
    }

    if (media < 7) {
        return {
            texto: 'Exame',
            classe:
                'status-exame text-amber-700 bg-amber-50 px-2.5 py-1 rounded-full text-[10px] font-bold border border-amber-200'
        };
    }

    return {
        texto: 'Aprovado',
        classe:
            'status-aprovado text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-full text-[10px] font-bold border border-emerald-200'
    };
}

/**
 * Cria uma célula da tabela de notas.
 *
 * textContent evita interpretar dados acadêmicos como HTML.
 */
function criarCelulaTabela(
    texto,
    classeCss
) {
    const celula = document.createElement(
        'td'
    );

    celula.textContent = texto;
    celula.className = classeCss;

    return celula;
}

/**
 * Renderiza uma linha do boletim acadêmico.
 */
function criarLinhaBoletim(registro) {
    const notas = registro.notas || {};

    const primeiraProva =
        Number(notas.p1) || 0;

    const segundaProva =
        Number(notas.p2) || 0;

    const trabalho =
        Number(notas.trabalho) || 0;

    const quizzes =
        Number(notas.quizzes) || 0;

    const media = Number(
        calcularMedia(
            primeiraProva,
            segundaProva,
            trabalho,
            quizzes
        )
    );

    const situacao =
        obterSituacaoAcademica(media);

    const linha = document.createElement(
        'tr'
    );

    linha.className =
        'hover:bg-slate-50 transition border-b border-slate-100 text-xs';

    const celulas = [
        {
            texto:
                registro.disciplinaNome ||
                'Disciplina',
            classe:
                'p-3 font-semibold text-slate-800'
        },
        {
            texto: primeiraProva.toFixed(1),
            classe: 'p-3 font-medium'
        },
        {
            texto: segundaProva.toFixed(1),
            classe: 'p-3 font-medium'
        },
        {
            texto: trabalho.toFixed(1),
            classe: 'p-3 font-medium'
        },
        {
            texto: quizzes.toFixed(1),
            classe: 'p-3 font-medium'
        },
        {
            texto: media.toFixed(1),
            classe:
                'p-3 text-center font-extrabold text-brand-blue'
        },
        {
            texto: situacao.texto,
            classe:
                `p-3 text-center ${situacao.classe}`
        }
    ];

    celulas.forEach(celula => {
        linha.appendChild(
            criarCelulaTabela(
                celula.texto,
                celula.classe
            )
        );
    });

    return linha;
}

/**
 * Renderiza o boletim e o extrato de faltas.
 *
 * Mantém o layout original, mas os dados ainda são locais.
 */
function renderBoletim() {
    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (!conteudoPrincipal) {
        return;
    }

    const dadosCompartilhados =
        obterDadosCompartilhados();

    const registrosFaltas =
        obterRegistrosFaltas();

    const matricula = String(
        estadoAluno.aluno.matricula
    );

    const livroNotas =
        dadosCompartilhados.gradebook || {};

    const notasPublicadas =
        Object.values(livroNotas)
            .map(
                registros =>
                    registros?.[matricula]
            )
            .filter(
                registro =>
                    registro &&
                    registro.publicado &&
                    registro.notas
            );

    if (
        notasPublicadas.length === 0 &&
        Object.keys(registrosFaltas).length === 0
    ) {
        conteudoPrincipal.innerHTML =
            renderHtmlTemplate(
                'ta-aluno-fragment-030'
            );

        return;
    }

    conteudoPrincipal.innerHTML = `
        <div class="space-y-6">
            <div id="alerta-faltas-critico"
                class="hidden bg-red-50 border-l-4 border-red-500 p-4 rounded-xl shadow-sm flex items-start gap-3">
                <i class="fa-solid fa-triangle-exclamation text-red-500 text-xl mt-0.5"></i>
                <div>
                    <h4 class="text-sm font-bold text-red-800">
                        Atenção Crítica: Limite de Faltas Excedido!
                    </h4>
                    <p class="text-xs text-red-700 mt-0.5">
                        Você acumulou 15 ou mais faltas em uma ou mais disciplinas.
                        Procure a coordenação pedagógica.
                    </p>
                </div>
            </div>

            <div class="bg-white p-6 rounded-2xl border border-slate-100 shadow-sm">
                <h3 class="text-base font-bold text-slate-800 mb-4 flex items-center gap-2">
                    <i class="fa-solid fa-clipboard-user text-brand-blue"></i>
                    Extrato de Faltas por Disciplina
                </h3>
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4"
                    id="extrato-faltas-cards">
                </div>
            </div>

            <div class="bg-white p-6 rounded-2xl border border-slate-100 shadow-sm">
                <h3 class="text-base font-bold text-slate-800 mb-4 flex items-center gap-2">
                    <i class="fa-solid fa-graduation-cap text-brand-blue"></i>
                    Boletim Acadêmico
                </h3>
                <div class="overflow-x-auto rounded-xl border border-slate-200">
                    <table class="w-full min-w-[700px] text-left border-collapse boletim-table">
                        <thead>
                            <tr class="bg-blue-50 text-slate-600 text-xs font-bold uppercase border-b border-blue-100">
                                <th class="p-3">Disciplina</th>
                                <th class="p-3 w-20">P1</th>
                                <th class="p-3 w-20">P2</th>
                                <th class="p-3 w-24">Trabalho</th>
                                <th class="p-3 w-20">Quizzes</th>
                                <th class="p-3 w-20 text-center">Média</th>
                                <th class="p-3 w-28 text-center">Situação</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    renderizarExtratoFaltas(
        registrosFaltas,
        matricula
    );

    const corpoTabela =
        conteudoPrincipal.querySelector(
            '.boletim-table tbody'
        );

    if (corpoTabela) {
        notasPublicadas.forEach(
            registro => {
                corpoTabela.appendChild(
                    criarLinhaBoletim(
                        registro
                    )
                );
            }
        );
    }

    verificarAlertaFaltasCriticas();
}

/**
 * Renderiza os cartões de faltas.
 *
 * Os identificadores demonstrativos são mantidos até
 * a integração das disciplinas reais do Azure.
 */
function renderizarExtratoFaltas(
    registrosFaltas,
    matricula
) {
    const container = document.getElementById(
        'extrato-faltas-cards'
    );

    if (!container) {
        return;
    }

    const disciplinasDemonstrativas = {
        web: 'Desenvolvimento Web & UX',
        db: 'Banco de Dados'
    };

    const htmlCartoes =
        Object.entries(
            disciplinasDemonstrativas
        ).map(
            ([disciplinaId, nomeDisciplina]) => {
                const registroAluno =
                    registrosFaltas[disciplinaId]?.[matricula] ||
                    { total: 0 };

                const quantidadeFaltas =
                    Number(
                        registroAluno.total || 0
                    );

                const classeIndicador =
                    quantidadeFaltas >= 15
                        ? 'bg-red-100 text-red-800 border-red-200 font-bold animate-pulse'
                        : quantidadeFaltas >= 10
                            ? 'bg-amber-100 text-amber-800 border-amber-200'
                            : 'bg-blue-50 text-brand-blue border-blue-100';

                return `
                    <div class="p-4 rounded-xl border border-slate-100 bg-slate-50 flex items-center justify-between">
                        <div>
                            <h4 class="font-bold text-slate-800 text-xs">
                                ${nomeDisciplina}
                            </h4>
                            <p class="text-[11px] text-slate-400 mt-0.5">
                                Limite máximo permitido: 15 faltas
                            </p>
                        </div>
                        <div>
                            <span class="px-3 py-1 rounded-full text-xs border ${classeIndicador}">
                                ${quantidadeFaltas} Falta(s)
                            </span>
                        </div>
                    </div>
                `;
            }
        ).join('');

    container.innerHTML = htmlCartoes;
}

/**
 * Verifica se o aluno atingiu o limite crítico de faltas.
 */
function verificarAlertaFaltasCriticas() {
    const matricula =
        estadoAluno.aluno.matricula;

    if (!matricula) {
        return;
    }

    const registrosFaltas =
        obterRegistrosFaltas();

    const existeFaltaCritica =
        Object.values(
            registrosFaltas
        ).some(
            registrosDisciplina => {
                const registroAluno =
                    registrosDisciplina?.[matricula];

                return (
                    registroAluno &&
                    Number(registroAluno.total) >= 15
                );
            }
        );

    const alerta = document.getElementById(
        'alerta-faltas-critico'
    );

    if (alerta) {
        alerta.classList.toggle(
            'hidden',
            !existeFaltaCritica
        );
    }
}

/**
 * Renderiza o perfil do aluno autenticado.
 */
function renderPerfil() {
    const conteudoPrincipal =
        document.getElementById(
            'main-content-area'
        );

    if (!conteudoPrincipal) {
        return;
    }

    const aluno = estadoAluno.aluno;

    conteudoPrincipal.innerHTML =
        renderHtmlTemplate(
            'ta-aluno-fragment-002',
            {
                '0': aluno.nome,
                '1': aluno.email,
                '2': aluno.curso
            }
        );

    const formulario = document.getElementById(
        'form-perfil-aluno'
    );

    if (!formulario) {
        return;
    }

    formulario.addEventListener(
        'submit',
        evento => {
            evento.preventDefault();

            /**
             * A atualização de perfil ainda não possui
             * persistência confirmada na API.
             *
             * Por isso, não mostramos uma mensagem falsa
             * informando que o Azure foi atualizado.
             */
            showToast(
                'A edição do perfil ainda não está integrada ao servidor.'
            );
        }
    );
}

/**
 * Exibe uma mensagem temporária na interface.
 */
function showToast(mensagem) {
    const container = document.getElementById(
        'toast-container'
    );

    if (!container) {
        return;
    }

    const notificacao = document.createElement(
        'div'
    );

    notificacao.className =
        'bg-slate-900 text-white px-4 py-3 rounded-xl shadow-xl text-xs flex items-center gap-2 border border-slate-700';

    notificacao.innerHTML =
        renderHtmlTemplate(
            'ta-aluno-fragment-001',
            {
                '0': mensagem
            }
        );

    container.appendChild(
        notificacao
    );

    window.setTimeout(
        () => {
            notificacao.remove();
        },
        3000
    );
}

/**
 * Atualiza informações locais quando outra aba
 * modifica os dados compartilhados.
 */
window.addEventListener(
    'storage',
    evento => {
        const chaveAlterada = evento.key;

        const alterouDadosCompartilhados =
            chaveAlterada ===
            CONFIGURACAO_ALUNO.chaveDadosCompartilhados;

        const alterouFaltas =
            chaveAlterada ===
            CONFIGURACAO_ALUNO.chaveFaltas;

        if (
            !alterouDadosCompartilhados &&
            !alterouFaltas
        ) {
            return;
        }

        if (alterouDadosCompartilhados) {
            carregarDadosCompartilhados();
            carregarNotificacoes();
        }

        const tituloPagina =
            document.getElementById(
                'page-title'
            );

        if (
            tituloPagina?.textContent.includes(
                'Boletim'
            )
        ) {
            renderBoletim();
        }

        verificarAlertaFaltasCriticas();
    }
);
