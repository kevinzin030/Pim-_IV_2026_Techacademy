/**
 * TechAcademy - Painel do Professor
 *
 * Gerencia disciplinas, aulas, PDFs, quizzes,
 * notas, faltas, métricas e perfil.
 *
 * A autenticação utiliza a sessão criada pelo login.js.
 * Os demais dados continuam demonstrativos e locais.
 */

function renderHtmlTemplate(templateId, values = {}) {
    const template = document.getElementById(templateId);

    if (!template) {
        console.error(`Template não encontrado: ${templateId}`);
        return '';
    }

    let html = template.innerHTML;

    for (const [chave, valor] of Object.entries(values)) {
        html = html.replaceAll(
            `__TA_SLOT_${chave}__`,
            valor == null ? '' : String(valor)
        );
    }

    return html;
}

/**
 * Estado central do painel.
 * A identidade do professor não contém dados fictícios.
 */
const state = {
    user: {
        nome: '',
        email: '',
        matricula: '',
        telefone: ''
    },

    turmas: [
        { id: 't1', nome: 'Turma A - Noturno (2024.1)' },
        { id: 't2', nome: 'Turma B - Matutino (2024.1)' }
    ],

    disciplinas: [
        {
            id: 'disc-1',
            codigo: 'TEC-301',
            nome: 'Desenvolvimento Web Front-End & UX',
            turmaId: 't1',
            aulas: [
                {
                    id: 'a1',
                    titulo: 'Aula 01: Fundamentos de UX/UI e Heurísticas de Nielsen',
                    pdfNome: 'Aula_01_Heuristicas.pdf',
                    data: '12/03/2024'
                },
                {
                    id: 'a2',
                    titulo: 'Aula 02: HTML5 Semântico e CSS Responsivo',
                    pdfNome: 'Aula_02_HTML_CSS.pdf',
                    data: '19/03/2024'
                }
            ],
            questionarios: [
                {
                    id: 'q1',
                    titulo: 'Quiz 01: Aplicação de Heurísticas',
                    xp: 100,
                    pergunta: 'Qual heurística aborda a visibilidade do estado do sistema?',
                    opcoes: [
                        'A) Prevenção de Erros',
                        'B) Visibilidade do Estado do Sistema',
                        'C) Estética Minimalista'
                    ],
                    correta: 1
                }
            ],
        },
        {
            id: 'disc-2',
            codigo: 'ENG-402',
            nome: 'Engenharia de Software e Arquitetura',
            turmaId: 't1',
            aulas: [
                {
                    id: 'a3',
                    titulo: 'Aula 01: Padrões de Projeto (Design Patterns)',
                    pdfNome: 'Design_Patterns_Intro.pdf',
                    data: '10/03/2024'
                }
            ],
            questionarios: [
                {
                    id: 'q2',
                    titulo: 'Quiz 01: Singleton & Factory',
                    xp: 120,
                    pergunta: 'Qual padrão de projeto garante apenas uma instância de uma classe?',
                    opcoes: [
                        'A) Factory Method',
                        'B) Singleton',
                        'C) Observer'
                    ],
                    correta: 1
                }
            ],

        }
    ],

    alunosNotas: [
        {
            id: 'al-1',
            nome: 'Lucas Mendes Santos',
            matricula: '2023001',
            p1: 8.5,
            p2: 7.0,
            trabalho: 9.0,
            quizzes: 8.0
        },
        {
            id: 'al-2',
            nome: 'Mariana Oliveira Rocha',
            matricula: '2023002',
            p1: 6.0,
            p2: 5.5,
            trabalho: 7.0,
            quizzes: 6.5
        },
        {
            id: 'al-3',
            nome: 'Gabriel Costa Lima',
            matricula: '2023003',
            p1: 4.0,
            p2: 3.5,
            trabalho: 5.0,
            quizzes: 4.0
        },
        {
            id: 'al-4',
            nome: 'Beatriz Souza Alves',
            matricula: '2023004',
            p1: 9.5,
            p2: 9.0,
            trabalho: 10.0,
            quizzes: 9.5
        }
    ],

    selecaoLancarNotas: {
        turmaId: '',
        disciplinaId: ''
    },

    notificacoes: []
};

const STORAGE_KEY = 'techacademy_shared_v1';
const CHAVE_FALTAS = 'techacademy_faltas';

const ALUNOS_NOTAS_PADRAO = state.alunosNotas.map(
    aluno => ({ ...aluno })
);

/**
 * Lê os dados locais compartilhados entre os painéis.
 */
function obterDadosCompartilhados() {
    try {
        return JSON.parse(
            localStorage.getItem(STORAGE_KEY) || '{}'
        );
    } catch (erro) {
        console.warn('Erro ao ler dados locais:', erro);
        return {};
    }
}

/**
 * Salva os dados compartilhados no navegador.
 */
function salvarDadosCompartilhados() {
    try {
        const dadosAtuais = obterDadosCompartilhados();

        localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify({
                ...dadosAtuais,
                disciplinas: state.disciplinas,
                notificacoes:
                    state.notificacoes.length
                        ? state.notificacoes
                        : dadosAtuais.notificacoes || []
            })
        );
    } catch (erro) {
        console.error('Erro ao salvar dados:', erro);

        showToast(
            'Não foi possível salvar. Verifique o espaço disponível no navegador.'
        );
    }
}

/**
 * Registra uma notificação de aula ou quiz.
 */
function registrarNotificacao(tipo, titulo, disciplina) {
    const dadosAtuais = obterDadosCompartilhados();

    const notificacoes = Array.isArray(
        dadosAtuais.notificacoes
    )
        ? dadosAtuais.notificacoes
        : [];

    const novaNotificacao = {
        id: `n-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
        tipo,
        titulo,
        disciplina,
        dataHora: new Date().toISOString(),
        lida: false
    };

    notificacoes.unshift(novaNotificacao);

    state.notificacoes = notificacoes.slice(0, 100);

    salvarDadosCompartilhados();
}

/**
 * Recupera disciplinas, aulas e quizzes do navegador.
 */
function carregarDadosCompartilhados() {
    const dadosSalvos = obterDadosCompartilhados();

    if (
        Array.isArray(dadosSalvos.disciplinas) &&
        dadosSalvos.disciplinas.length
    ) {
        state.disciplinas = dadosSalvos.disciplinas.map(
            disciplina => ({
                ...disciplina,
                aulas: disciplina.aulas || [],
                questionarios: (
                    disciplina.questionarios || []
                ).map(questionario => ({
                    ...questionario,
                    perguntas: questionario.perguntas || [
                        {
                            pergunta: questionario.pergunta || '',
                            opcoes: questionario.opcoes || [],
                            correta: Number(
                                questionario.correta || 0
                            )
                        }
                    ]
                }))
            })
        );
    } else {
        salvarDadosCompartilhados();
    }

    state.notificacoes = Array.isArray(
        dadosSalvos.notificacoes
    )
        ? dadosSalvos.notificacoes
        : [];
}

/**
 * Recupera o professor autenticado pelo login.js.
 * O login já consulta a API e o Azure SQL.
 */
function carregarProfessorAutenticado() {
    let usuario = null;

    try {
        usuario = JSON.parse(
            sessionStorage.getItem('usuario_logado') || 'null'
        );
    } catch (erro) {
        console.error('Sessão inválida:', erro);
    }

    if (
        !usuario ||
        (
            Number(usuario.idPerfil) !== 3 &&
            usuario.role !== 'professor'
        )
    ) {
        window.location.replace('index.html');
        return false;
    }

    state.user.nome =
        usuario.nomeCompleto ||
        usuario.nome ||
        usuario.login ||
        '';

    state.user.email = usuario.email || '';

    state.user.matricula =
        usuario.registroProfessor ||
        usuario.matricula ||
        usuario.login ||
        '';

    state.user.telefone = usuario.telefone || '';

    atualizarIdentificacaoProfessor();

    return true;
}

/**
 * Atualiza a identificação da barra lateral.
 */
function atualizarIdentificacaoProfessor() {
    const nome = document.getElementById(
        'prof-name-sidebar'
    );

    const email = document.getElementById(
        'prof-email-sidebar'
    );

    if (nome) {
        nome.textContent = state.user.nome;
    }

    if (email) {
        email.textContent =
            state.user.email || state.user.matricula;
    }
}

/**
 * Inicializa o painel após o carregamento do HTML.
 */
window.addEventListener('DOMContentLoaded', () => {
    if (!carregarProfessorAutenticado()) {
        return;
    }

    carregarDadosCompartilhados();
    bindNavEvents();
    renderDisciplinas();
});

/**
 * Configura a navegação entre as telas do professor.
 */
function bindNavEvents() {
    const itens = [
        ['nav-disciplinas', renderDisciplinas, 'Minhas Disciplinas'],
        ['nav-postar-aula', renderPostarAula, 'Postar Slides / Aulas'],
        ['nav-criar-quiz', renderCriarQuiz, 'Criar Quizzes'],
        ['nav-lancar-notas', renderLancarNotas, 'Lançar & Consultar Notas'],
        ['nav-lancar-faltas', renderizarSecaoFaltas, 'Lançar & Consultar Faltas'],
        ['nav-metricas', renderMetricas, 'Métricas da Turma'],
        ['nav-perfil', renderPerfil, 'Meu Perfil']
    ];

    itens.forEach(([id, renderizar, titulo]) => {
        const botao = document.getElementById(id);

        if (botao) {
            botao.addEventListener('click', evento => {
                setActiveNav(
                    evento.currentTarget,
                    renderizar,
                    titulo
                );
            });
        }
    });
}

/**
 * Atualiza a opção ativa e renderiza a tela selecionada.
 */
function setActiveNav(botao, renderizar, titulo) {
    document.querySelectorAll('nav button').forEach(item => {
        item.className =
            'w-full flex items-center gap-3 px-3 py-2.5 ' +
            'rounded-xl text-sm font-medium transition-all ' +
            'text-slate-300 hover:bg-slate-800 hover:text-white';
    });

    botao.className =
        'w-full flex items-center gap-3 px-3 py-2.5 ' +
        'rounded-xl text-sm font-medium transition-all ' +
        'bg-brand-blue text-white shadow-md';

    const tituloPagina = document.getElementById(
        'page-title'
    );

    if (tituloPagina) {
        tituloPagina.textContent = titulo;
    }

    renderizar();
}

/**
 * Calcula a média ponderada do aluno.
 */
function calcularMedia(p1, p2, trabalho, quiz) {
    const media =
        Number(p1) * 0.30 +
        Number(p2) * 0.30 +
        Number(trabalho) * 0.25 +
        Number(quiz) * 0.15;

    return media.toFixed(1);
}

/**
 * Identifica a situação acadêmica conforme a média.
 */
function obterSituacao(media) {
    if (media >= 7) {
        return {
            status: 'Aprovado',
            badge: 'bg-emerald-50 text-emerald-700 border-emerald-200'
        };
    }

    if (media >= 5) {
        return {
            status: 'Exame',
            badge: 'bg-amber-50 text-amber-700 border-amber-200'
        };
    }

    return {
        status: 'Reprovado',
        badge: 'bg-red-50 text-red-700 border-red-200'
    };
}

/**
 * Renderiza os cartões das disciplinas do professor.
 */
function renderDisciplinas() {
    const main = document.getElementById(
        'main-content-area'
    );

    const cartoes = state.disciplinas.map(disciplina => {
        const turma = state.turmas.find(
            item => item.id === disciplina.turmaId
        );

        return renderHtmlTemplate(
            'ta-prof-fragment-029',
            {
                '0': disciplina.codigo,
                '1': turma?.nome || '',
                '2': disciplina.nome,
                '3': disciplina.aulas.length,
                '4': disciplina.questionarios.length
            }
        );
    }).join('');

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-028',
        { '0': cartoes }
    );
}

/**
 * Converte um PDF selecionado em Data URL.
 * Usado somente para armazenamento demonstrativo local.
 */
function lerArquivoPdf(arquivo) {
    return new Promise((resolve, reject) => {
        const leitor = new FileReader();

        leitor.onload = () => resolve(leitor.result);
        leitor.onerror = () => reject(
            new Error('Falha ao ler o PDF.')
        );

        leitor.readAsDataURL(arquivo);
    });
}

/**
 * Localiza uma aula entre as disciplinas cadastradas.
 */
function localizarAula(aulaId) {
    for (const disciplina of state.disciplinas) {
        const indice = disciplina.aulas.findIndex(
            aula => aula.id === aulaId
        );

        if (indice !== -1) {
            return {
                disciplina,
                indice,
                aula: disciplina.aulas[indice]
            };
        }
    }

    return null;
}

/**
 * Exibe o formulário de postagem e edição de aulas.
 */
function renderPostarAula() {
    const main = document.getElementById(
        'main-content-area'
    );

    const opcoesDisciplinas = state.disciplinas.map(
        disciplina => renderHtmlTemplate(
            'ta-prof-fragment-025',
            {
                '0': disciplina.id,
                '1': disciplina.codigo,
                '2': disciplina.nome
            }
        )
    ).join('');

    const aulasPublicadas = state.disciplinas.flatMap(
        disciplina => disciplina.aulas.map(
            aula => renderHtmlTemplate(
                'ta-prof-fragment-027',
                {
                    '0': aula.titulo,
                    '1': disciplina.codigo,
                    '2': aula.pdfNome || 'Sem PDF',
                    '3': aula.data || 'Data não registrada',
                    '4': aula.id,
                    '5': disciplina.id
                }
            )
        )
    ).join('');

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-024',
        {
            '0': opcoesDisciplinas,
            '1': aulasPublicadas ||
                renderHtmlTemplate('ta-prof-fragment-026')
        }
    );

    const formulario = document.getElementById(
        'form-postar-aula'
    );

    const campoArquivo = document.getElementById(
        'aula-pdf'
    );

    /**
     * Limpa o formulário após salvar ou cancelar a edição.
     */
    function limparFormularioAula() {
        formulario.reset();

        document.getElementById('aula-edit-id').value = '';
        campoArquivo.required = true;

        document.getElementById(
            'titulo-form-aula'
        ).innerHTML = renderHtmlTemplate(
            'ta-prof-fragment-023'
        );

        document.getElementById(
            'btn-salvar-aula'
        ).innerHTML = renderHtmlTemplate(
            'ta-prof-fragment-022'
        );

        document.getElementById(
            'cancelar-edicao-aula'
        ).classList.add('hidden');
    }

    /**
     * Preenche o formulário com os dados da aula escolhida.
     */
    document.querySelectorAll(
        '[data-editar-aula]'
    ).forEach(botao => {
        botao.addEventListener('click', () => {
            const disciplina = state.disciplinas.find(
                item => item.id === botao.dataset.disc
            );

            const aula = disciplina?.aulas.find(
                item => item.id === botao.dataset.editarAula
            );

            if (!aula) {
                return;
            }

            document.getElementById(
                'aula-edit-id'
            ).value = aula.id;

            document.getElementById(
                'aula-disciplina'
            ).value = disciplina.id;

            document.getElementById(
                'aula-titulo'
            ).value = aula.titulo;

            campoArquivo.required = false;

            document.getElementById(
                'titulo-form-aula'
            ).innerHTML = renderHtmlTemplate(
                'ta-prof-fragment-021'
            );

            document.getElementById(
                'btn-salvar-aula'
            ).textContent = 'Salvar alterações';

            document.getElementById(
                'cancelar-edicao-aula'
            ).classList.remove('hidden');

            formulario.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        });
    });

    document.getElementById(
        'cancelar-edicao-aula'
    ).addEventListener('click', limparFormularioAula);

    /**
     * Valida e salva a aula no armazenamento local.
     */
    formulario.addEventListener('submit', async evento => {
        evento.preventDefault();

        const disciplinaId = document.getElementById(
            'aula-disciplina'
        ).value;

        const titulo = document.getElementById(
            'aula-titulo'
        ).value.trim();

        const arquivo = campoArquivo.files[0];

        const aulaEditadaId = document.getElementById(
            'aula-edit-id'
        ).value;

        const disciplinaDestino = state.disciplinas.find(
            item => item.id === disciplinaId
        );

        if (!disciplinaDestino) {
            showToast('Selecione uma disciplina.');
            return;
        }

        if (
            arquivo &&
            arquivo.type !== 'application/pdf' &&
            !arquivo.name.toLowerCase().endsWith('.pdf')
        ) {
            showToast('Selecione um arquivo PDF válido.');
            return;
        }

        if (arquivo && arquivo.size > 3 * 1024 * 1024) {
            showToast('Use PDFs de até 3 MB.');
            return;
        }

        let conteudoPdf = null;

        try {
            if (arquivo) {
                conteudoPdf = await lerArquivoPdf(arquivo);
            }
        } catch (erro) {
            console.error(erro);
            showToast('Não foi possível ler o PDF.');
            return;
        }

        const agora = new Date();

        if (aulaEditadaId) {
            const registro = localizarAula(aulaEditadaId);

            if (!registro) {
                showToast('Aula não encontrada.');
                return;
            }

            const aula = registro.aula;

            if (registro.disciplina.id !== disciplinaId) {
                registro.disciplina.aulas.splice(
                    registro.indice,
                    1
                );

                disciplinaDestino.aulas.push(aula);
            }

            aula.titulo = titulo;
            aula.data = agora.toLocaleDateString('pt-BR');
            aula.dataHora = agora.toISOString();

            if (arquivo) {
                aula.pdfNome = arquivo.name;
                aula.pdfData = conteudoPdf;
            }

            showToast('Postagem atualizada com sucesso!');
        } else {
            if (!arquivo) {
                showToast('Selecione um PDF.');
                return;
            }

            disciplinaDestino.aulas.push({
                id: `a-${Date.now()}`,
                titulo,
                pdfNome: arquivo.name,
                pdfData: conteudoPdf,
                data: agora.toLocaleDateString('pt-BR'),
                dataHora: agora.toISOString()
            });

            registrarNotificacao(
                'aula',
                titulo,
                disciplinaDestino.nome
            );

            showToast('Aula e PDF publicados para os alunos!');
        }

        salvarDadosCompartilhados();
        renderPostarAula();
    });
}

/**
 * Exibe a criação e edição de quizzes.
 */
function renderCriarQuiz() {
    const main = document.getElementById(
        'main-content-area'
    );

    const opcoesDisciplinas = state.disciplinas.map(
        disciplina => renderHtmlTemplate(
            'ta-prof-fragment-016',
            {
                '0': disciplina.id,
                '1': disciplina.codigo,
                '2': disciplina.nome
            }
        )
    ).join('');

    const opcoesAulas = state.disciplinas.flatMap(
        disciplina => disciplina.aulas.map(
            aula => renderHtmlTemplate(
                'ta-prof-fragment-017',
                {
                    '0': disciplina.id,
                    '1': aula.id,
                    '2': disciplina.codigo,
                    '3': aula.titulo
                }
            )
        )
    ).join('')

    const quizzesCadastrados = state.disciplinas.flatMap(
        disciplina => disciplina.questionarios.map(
            quiz => renderHtmlTemplate(
                'ta-prof-fragment-020',
                {
                    '0': quiz.titulo,
                    '1': disciplina.codigo,
                    '2': quiz.perguntas?.length ||
                        (quiz.pergunta ? 1 : 0),
                    '3': quiz.xp || 100,
                    '4': quiz.id,
                    '5': disciplina.id
                }
            )
        )
    ).join('');

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-015',
        {
            '0': opcoesDisciplinas,
            '1': opcoesAulas,
            '2': quizzesCadastrados ||
                renderHtmlTemplate('ta-prof-fragment-019')
        }
    );

    const containerPerguntas = document.getElementById(
        'quiz-perguntas-container'
    );

    const seletorAula = document.getElementById(
        'quiz-aula'
    );

    let quizEmEdicaoId = null;

    /**
     * Adiciona um novo conjunto de pergunta e alternativas.
     */
    function adicionarPergunta() {
        const numero =
            containerPerguntas.querySelectorAll(
                '.quiz-pergunta-item'
            ).length + 1;

        const elemento = document.createElement('div');

        elemento.className =
            'quiz-pergunta-item p-4 border border-slate-200 ' +
            'rounded-xl space-y-2';

        elemento.innerHTML = renderHtmlTemplate(
            'ta-prof-fragment-013',
            {
                '0': numero,
                '1': numero > 1
                    ? renderHtmlTemplate('ta-prof-fragment-014')
                    : '',
                '2': numero
            }
        );

        elemento.querySelector(
            '.remover-pergunta'
        )?.addEventListener('click', () => {
            elemento.remove();

            containerPerguntas.querySelectorAll(
                '.quiz-pergunta-item'
            ).forEach((item, indice) => {
                item.querySelector('strong').textContent =
                    `Pergunta ${indice + 1}`;
            });
        });

        containerPerguntas.appendChild(elemento);
    }

    /**
     * Filtra as aulas pela disciplina selecionada.
     */
    function atualizarAulasDoQuiz() {
        const disciplinaId = document.getElementById(
            'quiz-disciplina'
        ).value;

        const disciplina = state.disciplinas.find(
            item => item.id === disciplinaId
        );

        const opcoes = (disciplina?.aulas || []).map(
            aula => renderHtmlTemplate(
                'ta-prof-fragment-012',
                {
                    '0': disciplina.id,
                    '1': aula.id,
                    '2': aula.titulo
                }
            )
        ).join('');

        seletorAula.innerHTML = renderHtmlTemplate(
            'ta-prof-fragment-011',
            { '0': opcoes }
        );
    }

    adicionarPergunta();

    document.getElementById(
        'adicionar-pergunta'
    ).addEventListener('click', adicionarPergunta);

    document.getElementById(
        'quiz-disciplina'
    ).addEventListener('change', atualizarAulasDoQuiz);

    atualizarAulasDoQuiz();

    /**
     * Preenche o formulário para editar um quiz existente.
     */
    document.querySelectorAll(
        '[data-editar-quiz]'
    ).forEach(botao => {
        botao.addEventListener('click', () => {
            const disciplina = state.disciplinas.find(
                item => item.id === botao.dataset.disc
            );

            const quiz = disciplina?.questionarios.find(
                item => item.id === botao.dataset.editarQuiz
            );

            if (!quiz) {
                return;
            }

            quizEmEdicaoId = quiz.id;

            document.getElementById(
                'quiz-edit-id'
            ).value = quiz.id;

            document.getElementById(
                'quiz-disciplina'
            ).value = disciplina.id;

            atualizarAulasDoQuiz();

            const opcaoAula = [...seletorAula.options].find(
                opcao =>
                    opcao.value === `${disciplina.id}|${quiz.aulaId}`
            ) || [...seletorAula.options].find(
                opcao => opcao.value === 'geral|geral'
            );

            if (opcaoAula) {
                seletorAula.value = opcaoAula.value;
            }

            document.getElementById(
                'quiz-titulo'
            ).value = quiz.titulo || '';

            document.getElementById(
                'quiz-xp'
            ).value = quiz.xp || 100;

            containerPerguntas.innerHTML = '';

            const perguntas = quiz.perguntas || [
                {
                    pergunta: quiz.pergunta || '',
                    opcoes: quiz.opcoes || ['', '', '', ''],
                    correta: Number(quiz.correta || 0)
                }
            ];

            perguntas.forEach(pergunta => {
                adicionarPergunta();

                const elemento =
                    containerPerguntas.lastElementChild;

                elemento.querySelector(
                    '.pergunta-enunciado'
                ).value = pergunta.pergunta || '';

                elemento.querySelectorAll(
                    '.pergunta-opcao'
                ).forEach((campo, indice) => {
                    campo.value = (
                        pergunta.opcoes?.[indice] || ''
                    ).replace(/^[A-D]\)\s*/, '');
                });

                elemento.querySelector(
                    '.pergunta-correta'
                ).value = String(pergunta.correta || 0);
            });

            document.getElementById(
                'btn-salvar-quiz'
            ).textContent = 'Salvar alterações do quiz';

            document.getElementById(
                'cancelar-edicao-quiz'
            ).classList.remove('hidden');

            document.getElementById(
                'form-criar-quiz'
            ).scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        });
    });

    /**
     * Cancela a edição e restaura o formulário.
     */
    document.getElementById(
        'cancelar-edicao-quiz'
    ).addEventListener('click', () => {
        quizEmEdicaoId = null;

        document.getElementById(
            'quiz-edit-id'
        ).value = '';

        document.getElementById(
            'form-criar-quiz'
        ).reset();

        containerPerguntas.innerHTML = '';
        adicionarPergunta();

        document.getElementById(
            'btn-salvar-quiz'
        ).textContent = 'Cadastrar Quiz';

        document.getElementById(
            'cancelar-edicao-quiz'
        ).classList.add('hidden');

        atualizarAulasDoQuiz();
    });

    /**
     * Salva um quiz novo ou atualiza um quiz existente.
     */
    document.getElementById(
        'form-criar-quiz'
    ).addEventListener('submit', evento => {
        evento.preventDefault();

        const disciplinaId = document.getElementById(
            'quiz-disciplina'
        ).value;

        const disciplina = state.disciplinas.find(
            item => item.id === disciplinaId
        );

        if (!disciplina) {
            showToast('Selecione uma disciplina.');
            return;
        }

        const vinculo = document.getElementById(
            'quiz-aula'
        ).value.split('|');

        const perguntas = [
            ...document.querySelectorAll('.quiz-pergunta-item')
        ].map(elemento => ({
            pergunta: elemento.querySelector(
                '.pergunta-enunciado'
            ).value.trim(),

            opcoes: [
                ...elemento.querySelectorAll('.pergunta-opcao')
            ].map((campo, indice) =>
                `${String.fromCharCode(65 + indice)}) ${campo.value.trim()}`
            ),

            correta: Number(
                elemento.querySelector(
                    '.pergunta-correta'
                ).value
            )
        }));

        const perguntasInvalidas =
            !perguntas.length ||
            perguntas.some(pergunta =>
                !pergunta.pergunta ||
                pergunta.opcoes.some(
                    alternativa => !alternativa.slice(3).trim()
                )
            );

        if (perguntasInvalidas) {
            showToast(
                'Preencha todas as perguntas e alternativas.'
            );
            return;
        }

        const disciplinaDestino =
            vinculo[0] === 'geral'
                ? disciplina
                : state.disciplinas.find(
                    item => item.id === vinculo[0]
                );

        if (!disciplinaDestino) {
            showToast('Vínculo da aula inválido.');
            return;
        }

        const quizAnterior = state.disciplinas.flatMap(
            item => item.questionarios
        ).find(
            item => item.id === quizEmEdicaoId
        );

        const dadosQuiz = {
            id: quizEmEdicaoId || `q-${Date.now()}`,
            titulo: document.getElementById(
                'quiz-titulo'
            ).value.trim(),
            xp: parseInt(
                document.getElementById('quiz-xp').value,
                10
            ),
            aulaId:
                vinculo[0] === 'geral'
                    ? 'geral'
                    : vinculo[1],
            perguntas,
            respondido: false,
            dataHora:
                quizAnterior?.dataHora ||
                new Date().toISOString()
        };

        if (quizEmEdicaoId) {
            for (const item of state.disciplinas) {
                const indice = item.questionarios.findIndex(
                    quiz => quiz.id === quizEmEdicaoId
                );

                if (indice !== -1) {
                    item.questionarios.splice(indice, 1);
                    break;
                }
            }

            disciplinaDestino.questionarios.push(dadosQuiz);

            showToast('Quiz atualizado com sucesso!');
        } else {
            disciplinaDestino.questionarios.push(dadosQuiz);

            registrarNotificacao(
                'quiz',
                dadosQuiz.titulo,
                disciplinaDestino.nome
            );

            showToast('Quiz criado e vinculado à aula!');
        }

        salvarDadosCompartilhados();
        renderCriarQuiz();
    });

}
/**
 * Exibe o formulário de lançamento de notas.
 * Os dados ainda são demonstrativos e locais.
 */
function renderLancarNotas() {
    const main = document.getElementById(
        'main-content-area'
    );

    const { turmaId, disciplinaId } =
        state.selecaoLancarNotas;

    if (disciplinaId) {
        state.alunosNotas =
            obterNotasParaDisciplina(disciplinaId);
    }

    const opcoesTurmas = state.turmas.map(
        turma => renderHtmlTemplate(
            'ta-prof-fragment-008',
            {
                '0': turma.id,
                '1': turma.id === turmaId
                    ? 'selected'
                    : '',
                '2': turma.nome
            }
        )
    ).join('');

    const opcoesDisciplinas = state.disciplinas
        .filter(disciplina => disciplina.turmaId === turmaId)
        .map(disciplina => renderHtmlTemplate(
            'ta-prof-fragment-009',
            {
                '0': disciplina.id,
                '1': disciplina.id === disciplinaId
                    ? 'selected'
                    : '',
                '2': disciplina.codigo,
                '3': disciplina.nome
            }
        ))
        .join('');

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-007',
        {
            '0': opcoesTurmas,
            '1': turmaId ? '' : 'disabled',
            '2': opcoesDisciplinas,
            '3': turmaId && disciplinaId
                ? renderTabelaNotasHTML()
                : renderHtmlTemplate('ta-prof-fragment-010')
        }
    );

    document.getElementById(
        'select-turma'
    )?.addEventListener('change', evento => {
        state.selecaoLancarNotas.turmaId =
            evento.target.value;

        state.selecaoLancarNotas.disciplinaId = '';

        renderLancarNotas();
    });

    document.getElementById(
        'select-disciplina'
    )?.addEventListener('change', evento => {
        state.selecaoLancarNotas.disciplinaId =
            evento.target.value;

        renderLancarNotas();
    });

    bindTabelaNotasEvents();
}

/**
 * Recupera as notas locais de determinada disciplina.
 * Quando não há notas salvas, utiliza os valores de exemplo.
 */
function obterNotasParaDisciplina(disciplinaId) {
    const dados = obterDadosCompartilhados();

    const registros =
        dados.gradebook?.[disciplinaId] || {};

    return ALUNOS_NOTAS_PADRAO.map(aluno => ({
        ...aluno,
        ...(registros[aluno.matricula]?.notas || {})
    }));
}

/**
 * Valida e publica as notas no armazenamento local.
 */
function salvarNotasDaDisciplina() {
    const disciplinaId =
        state.selecaoLancarNotas.disciplinaId;

    if (!disciplinaId) {
        showToast(
            'Selecione uma turma e uma disciplina antes de salvar.'
        );
        return;
    }

    const camposNota = [
        ...document.querySelectorAll('.input-nota')
    ];

    for (const campo of camposNota) {
        const valor = campo.value.trim();
        const nota = Number(valor);

        if (
            valor === '' ||
            !Number.isFinite(nota) ||
            nota < 0 ||
            nota > 10
        ) {
            campo.focus();

            showToast(
                'Preencha todas as notas com valores entre 0 e 10.'
            );

            return;
        }
    }

    const dados = obterDadosCompartilhados();

    dados.gradebook = dados.gradebook || {};

    dados.gradebook[disciplinaId] =
        dados.gradebook[disciplinaId] || {};

    const disciplina = state.disciplinas.find(
        item => item.id === disciplinaId
    );

    state.alunosNotas.forEach(aluno => {
        const notas = {
            p1: Number(aluno.p1),
            p2: Number(aluno.p2),
            trabalho: Number(aluno.trabalho),
            quizzes: Number(aluno.quizzes)
        };

        dados.gradebook[disciplinaId][aluno.matricula] = {
            matricula: aluno.matricula,
            nome: aluno.nome,
            disciplinaId,
            disciplinaNome:
                disciplina?.nome || 'Disciplina',
            notas,
            media: calcularMedia(
                notas.p1,
                notas.p2,
                notas.trabalho,
                notas.quizzes
            ),
            publicado: true,
            atualizadoEm: new Date().toISOString()
        };
    });

    try {
        localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify({
                ...dados,
                disciplinas: state.disciplinas,
                notificacoes:
                    state.notificacoes.length
                        ? state.notificacoes
                        : dados.notificacoes || []
            })
        );

        const indicador = document.getElementById(
            'status-salvar-notas'
        );

        if (indicador) {
            indicador.className =
                'text-[11px] bg-emerald-50 text-emerald-700 ' +
                'px-3 py-1 rounded-full font-bold ' +
                'border border-emerald-200';

            indicador.innerHTML =
                '<i class="fa-solid fa-circle-check mr-1"></i> ' +
                'Notas salvas e publicadas';
        }

        showToast(
            'Notas salvas! Os alunos já podem consultar o boletim local.'
        );
    } catch (erro) {
        console.error('Erro ao salvar notas:', erro);

        showToast(
            'Não foi possível salvar as notas no navegador.'
        );
    }
}

/**
 * Renderiza a tabela de notas da disciplina selecionada.
 */
function renderTabelaNotasHTML() {
    const dados = obterDadosCompartilhados();

    const disciplinaId =
        state.selecaoLancarNotas.disciplinaId;

    const notasPublicadas =
        dados.gradebook?.[disciplinaId] || {};

    const existemNotasPublicadas =
        state.alunosNotas.some(
            aluno =>
                notasPublicadas[aluno.matricula]?.publicado
        );

    /**
     * Cria um campo numérico editável de nota.
     */
    function criarCampoNota(
        aluno,
        indice,
        campo,
        valor,
        rotulo
    ) {
        return `
            <td class="p-3">
                <label class="sr-only">
                    ${rotulo} de ${aluno.nome}
                </label>
                <input
                    aria-label="${rotulo} de ${aluno.nome}"
                    type="number"
                    step="0.1"
                    min="0"
                    max="10"
                    value="${valor}"
                    data-index="${indice}"
                    data-field="${campo}"
                    class="input-nota w-full min-w-[72px] p-2 border border-slate-200 rounded-lg text-center font-bold text-slate-700 focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                >
            </td>
        `;
    }

    const linhas = state.alunosNotas.map(
        (aluno, indice) => {
            const media = calcularMedia(
                aluno.p1,
                aluno.p2,
                aluno.trabalho,
                aluno.quizzes
            );

            const situacao = obterSituacao(
                Number(media)
            );

            return `
                <tr class="hover:bg-blue-50/50 transition border-b border-slate-100">
                    <td class="p-3 font-semibold text-slate-800 min-w-[210px]">
                        <div>${aluno.nome}</div>
                        <div class="text-[10px] text-slate-400 font-mono mt-1">
                            Matrícula: ${aluno.matricula}
                        </div>
                    </td>

                    ${criarCampoNota(
                aluno, indice, 'p1', aluno.p1, 'P1'
            )}

                    ${criarCampoNota(
                aluno, indice, 'p2', aluno.p2, 'P2'
            )}

                    ${criarCampoNota(
                aluno, indice, 'trabalho',
                aluno.trabalho, 'Trabalho'
            )}

                    ${criarCampoNota(
                aluno, indice, 'quizzes',
                aluno.quizzes, 'Quizzes'
            )}

                    <td
                        class="p-3 text-center font-extrabold text-sm text-brand-blue whitespace-nowrap"
                        id="media-${indice}"
                    >
                        ${media}
                    </td>

                    <td
                        class="p-3 text-center whitespace-nowrap"
                        id="situacao-${indice}"
                    >
                        <span class="inline-block px-2.5 py-1 rounded-full text-[10px] font-bold border ${situacao.badge}">
                            ${situacao.status}
                        </span>
                    </td>
                </tr>
            `;
        }
    ).join('');

    const classeStatus = existemNotasPublicadas
        ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
        : 'bg-amber-50 text-amber-700 border-amber-200';

    const textoStatus = existemNotasPublicadas
        ? 'Notas publicadas para os alunos'
        : 'Alterações ainda não salvas';

    return `
        <section class="bg-white p-4 sm:p-6 rounded-2xl border border-blue-100 shadow-sm">
            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-5">
                <div>
                    <h3 class="text-base font-bold text-slate-800 flex items-center gap-2">
                        <i class="fa-solid fa-list-check text-brand-blue"></i>
                        Diário de Notas dos Alunos
                    </h3>
                    <p class="text-xs text-slate-500 mt-1">
                        Edite as notas e clique em Salvar notas para publicar no boletim.
                    </p>
                </div>

                <span
                    id="status-salvar-notas"
                    class="self-start text-xs ${classeStatus} px-3 py-2 rounded-full font-bold border"
                >
                    <i class="fa-solid fa-circle-info mr-1"></i>
                    ${textoStatus}
                </span>
            </div>

            <div class="overflow-x-auto rounded-xl border border-slate-200">
                <table class="w-full min-w-[850px] text-left border-collapse">
                    <thead>
                        <tr class="bg-blue-50 text-slate-600 text-xs font-bold uppercase border-b border-blue-100">
                            <th class="p-3">Aluno</th>
                            <th class="p-3 w-24">P1 (30%)</th>
                            <th class="p-3 w-24">P2 (30%)</th>
                            <th class="p-3 w-28">Trabalho (25%)</th>
                            <th class="p-3 w-24">Quizzes (15%)</th>
                            <th class="p-3 w-20 text-center">Média</th>
                            <th class="p-3 w-28 text-center">Situação</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${linhas}
                    </tbody>
                </table>
            </div>

            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-5">
                <p class="text-xs text-slate-500">
                    As notas serão vinculadas à disciplina selecionada e à matrícula de cada aluno.
                </p>

                <button
                    id="btn-salvar-notas"
                    type="button"
                    class="inline-flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-bold text-sm shadow-md transition focus:outline-none focus:ring-4 focus:ring-blue-200"
                >
                    <i class="fa-solid fa-floppy-disk"></i>
                    Salvar notas
                </button>
            </div>
        </section>
    `;
}

/**
 * Atualiza a média e a situação conforme as notas são editadas.
 */
function bindTabelaNotasEvents() {
    document.querySelectorAll(
        '.input-nota'
    ).forEach(campo => {
        campo.addEventListener('input', evento => {
            const indice = Number(
                evento.target.dataset.index
            );

            const propriedade =
                evento.target.dataset.field;

            const valor = parseFloat(
                String(evento.target.value).replace(',', '.')
            ) || 0;

            const aluno = state.alunosNotas[indice];

            if (!aluno) {
                return;
            }

            aluno[propriedade] = valor;

            const indicador = document.getElementById(
                'status-salvar-notas'
            );

            if (indicador) {
                indicador.className =
                    'text-[11px] bg-amber-50 text-amber-700 ' +
                    'px-3 py-1 rounded-full font-bold ' +
                    'border border-amber-200';

                indicador.innerHTML =
                    '<i class="fa-solid fa-pen-to-square mr-1"></i> ' +
                    'Alterações não salvas';
            }

            const media = calcularMedia(
                aluno.p1,
                aluno.p2,
                aluno.trabalho,
                aluno.quizzes
            );

            const situacao = obterSituacao(
                Number(media)
            );

            document.getElementById(
                `media-${indice}`
            ).textContent = media;

            document.getElementById(
                `situacao-${indice}`
            ).innerHTML = renderHtmlTemplate(
                'ta-prof-fragment-004',
                {
                    '0': situacao.badge,
                    '1': situacao.status
                }
            );
        });
    });

    document.getElementById(
        'btn-salvar-notas'
    )?.addEventListener(
        'click',
        salvarNotasDaDisciplina
    );
}

/**
 * Calcula as métricas acadêmicas dos alunos demonstrativos.
 */
function renderMetricas() {
    const main = document.getElementById(
        'main-content-area'
    );

    const totalAlunos = state.alunosNotas.length;

    let somaMedias = 0;
    let somaQuizzes = 0;

    let aprovados = 0;
    let exame = 0;
    let reprovados = 0;

    state.alunosNotas.forEach(aluno => {
        const media = Number(
            calcularMedia(
                aluno.p1,
                aluno.p2,
                aluno.trabalho,
                aluno.quizzes
            )
        );

        somaMedias += media;
        somaQuizzes += Number(aluno.quizzes);

        if (media >= 7) {
            aprovados++;
        } else if (media >= 5) {
            exame++;
        } else {
            reprovados++;
        }
    });

    const dividirPorTotal = valor =>
        totalAlunos ? valor / totalAlunos : 0;

    const percentual = quantidade =>
        dividirPorTotal(quantidade) * 100;

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-003',
        {
            '0': dividirPorTotal(somaMedias).toFixed(1),
            '1': dividirPorTotal(somaQuizzes).toFixed(1),
            '2': Math.round(percentual(aprovados)),
            '3': totalAlunos,
            '4': aprovados,
            '5': percentual(aprovados),
            '6': exame,
            '7': percentual(exame),
            '8': reprovados,
            '9': percentual(reprovados)
        }
    );

    main.querySelectorAll(
        '[data-progress-percent]'
    ).forEach(barra => {
        const valor = Number(
            barra.dataset.progressPercent
        );

        const percentualLimitado = Math.max(
            0,
            Math.min(100, valor)
        );

        barra.style.setProperty(
            '--progress-width',
            `${percentualLimitado}%`
        );
    });
}

/**
 * Exibe o perfil do professor autenticado.
 *
 * As alterações feitas no formulário são apenas locais,
 * pois ainda não existe integração de edição de perfil.
 */
function renderPerfil() {
    const main = document.getElementById(
        'main-content-area'
    );

    const professor = state.user;

    main.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-002',
        {
            '0': professor.nome,
            '1': professor.email,
            '2': professor.matricula,
            '3': professor.telefone
        }
    );

    document.getElementById(
        'form-perfil'
    )?.addEventListener('submit', evento => {
        evento.preventDefault();

        professor.nome = document.getElementById(
            'perf-nome'
        ).value.trim();

        professor.email = document.getElementById(
            'perf-email'
        ).value.trim();

        professor.telefone = document.getElementById(
            'perf-tel'
        ).value.trim();

        atualizarIdentificacaoProfessor();

        showToast(
            'Perfil alterado nesta sessão. Os dados ainda não foram salvos no Azure.'
        );
    });
}

/**
 * Exibe mensagens temporárias no layout original.
 */
function showToast(mensagem) {
    const container = document.getElementById(
        'toast-container'
    );

    if (!container) {
        return;
    }

    const toast = document.createElement('div');

    toast.className =
        'bg-slate-900 text-white px-4 py-3 rounded-xl ' +
        'shadow-xl text-xs flex items-center gap-2 ' +
        'border border-slate-700 animate-fade-in';

    toast.innerHTML = renderHtmlTemplate(
        'ta-prof-fragment-001',
        {
            '0': mensagem
        }
    );

    container.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}

/**
 * Recupera os registros demonstrativos de frequência.
 */
function obterDadosFaltas() {
    try {
        return JSON.parse(
            localStorage.getItem(CHAVE_FALTAS) || '{}'
        );
    } catch (erro) {
        console.error('Erro ao recuperar faltas:', erro);
        return {};
    }
}

/**
 * Exibe a seleção de turma e disciplina para frequência.
 */
function renderizarSecaoFaltas() {
    const main = document.getElementById(
        'main-content-area'
    );

    const turmas = [
        {
            id: 'ADS-2026-2',
            nome: 'Análise e Desenvolvimento de Sistemas - 2º Semestre'
        }
    ];

    const opcoesTurmas = turmas.map(
        turma => `
            <option value="${turma.id}">
                ${turma.nome}
            </option>
        `
    ).join('');

    main.innerHTML = document.getElementById(
        'ta-prof-fragment-faltas-main'
    ).innerHTML
        .replace('__TA_SLOT_0__', opcoesTurmas)
        .replace('__TA_SLOT_1__', 'disabled')
        .replace('__TA_SLOT_2__', '')
        .replace(
            '__TA_SLOT_3__',
            document.getElementById(
                'ta-prof-fragment-faltas-aviso'
            ).innerHTML
        );

    /**
     * Habilita a seleção de disciplina após escolher turma.
     */
    document.getElementById(
        'select-turma-falta'
    )?.addEventListener('change', evento => {
        const turmaId = evento.target.value;

        const seletorDisciplina = document.getElementById(
            'select-disciplina-falta'
        );

        const containerTabela = document.getElementById(
            'container-tabela-faltas'
        );

        if (!turmaId) {
            seletorDisciplina.disabled = true;

            seletorDisciplina.innerHTML =
                '<option value="">-- Escolha uma Disciplina --</option>';

            containerTabela.innerHTML =
                document.getElementById(
                    'ta-prof-fragment-faltas-aviso'
                ).innerHTML;

            return;
        }

        seletorDisciplina.disabled = false;

        const disciplinas = [
            {
                id: 'web',
                nome: 'Desenvolvimento Web & UX'
            },
            {
                id: 'db',
                nome: 'Banco de Dados'
            }
        ];

        seletorDisciplina.innerHTML =
            '<option value="">-- Escolha uma Disciplina --</option>' +
            disciplinas.map(
                disciplina => `
                    <option value="${disciplina.id}">
                        ${disciplina.nome}
                    </option>
                `
            ).join('');
    });

    /**
     * Carrega a chamada da disciplina selecionada.
     */
    document.getElementById(
        'select-disciplina-falta'
    )?.addEventListener('change', evento => {
        const disciplinaId = evento.target.value;

        const containerTabela = document.getElementById(
            'container-tabela-faltas'
        );

        if (!disciplinaId) {
            containerTabela.innerHTML =
                document.getElementById(
                    'ta-prof-fragment-faltas-aviso'
                ).innerHTML;

            return;
        }

        carregarTabelaFaltas(
            disciplinaId,
            containerTabela
        );
    });
}

/**
 * Exibe os alunos e os registros de presença da disciplina.
 */
function carregarTabelaFaltas(
    disciplinaId,
    containerTabela
) {
    const alunosTurma = [
        {
            matricula: '2023001',
            nome: 'Lucas Mendes Santos'
        },
        {
            matricula: '2023002',
            nome: 'Mariana Oliveira Rocha'
        },
        {
            matricula: '2023003',
            nome: 'Gabriel Costa Lima'
        },
        {
            matricula: '2023004',
            nome: 'Beatriz Souza Alves'
        }
    ];

    const dadosFaltas = obterDadosFaltas();

    const dataHoje = new Date()
        .toISOString()
        .split('T')[0];

    containerTabela.innerHTML =
        document.getElementById(
            'ta-prof-fragment-faltas-tabela'
        ).innerHTML;

    const corpoTabela =
        containerTabela.querySelector('tbody');

    const linhas = alunosTurma.map(aluno => {
        const registro = dadosFaltas[disciplinaId]?.[
            aluno.matricula
        ] || {
            total: 0,
            historico: {}
        };

        const statusHoje =
            registro.historico?.[dataHoje] || 'presente';

        const presencaSelecionada =
            statusHoje === 'presente'
                ? 'selected'
                : '';

        const faltaSelecionada =
            statusHoje === 'falta'
                ? 'selected'
                : '';

        return document.getElementById(
            'ta-prof-fragment-faltas-linha'
        ).innerHTML
            .replaceAll('__TA_SLOT_0__', aluno.nome)
            .replaceAll(
                '__TA_SLOT_1__',
                aluno.matricula
            )
            .replace(
                '__TA_SLOT_2__',
                presencaSelecionada
            )
            .replace(
                '__TA_SLOT_3__',
                faltaSelecionada
            )
            .replace(
                '__TA_SLOT_4__',
                registro.total
            );
    }).join('');

    if (corpoTabela) {
        corpoTabela.innerHTML = linhas;
    }

    const campoData = document.getElementById(
        'data-chamada-atual'
    );

    if (campoData) {
        campoData.value = dataHoje;
    }

    /**
     * Registra presença ou falta sem duplicar a contagem
     * quando a mesma data é salva novamente.
     */
    document.getElementById(
        'btn-salvar-faltas'
    )?.addEventListener('click', () => {
        const dataSelecionada = document.getElementById(
            'data-chamada-atual'
        ).value;

        if (!dataSelecionada) {
            alert('Selecione a data da aula.');
            return;
        }

        const registros = obterDadosFaltas();

        registros[disciplinaId] =
            registros[disciplinaId] || {};

        document.querySelectorAll(
            '.select-status-falta'
        ).forEach(seletor => {
            const matricula =
                seletor.dataset.matricula;

            const statusAtual =
                seletor.value;

            if (!registros[disciplinaId][matricula]) {
                registros[disciplinaId][matricula] = {
                    total: 0,
                    historico: {}
                };
            }

            const registro =
                registros[disciplinaId][matricula];

            const statusAnterior =
                registro.historico[dataSelecionada] ||
                'presente';

            if (statusAnterior !== statusAtual) {
                if (statusAtual === 'falta') {
                    registro.total += 1;
                } else if (
                    statusAnterior === 'falta' &&
                    statusAtual === 'presente'
                ) {
                    registro.total = Math.max(
                        0,
                        registro.total - 1
                    );
                }
            }

            registro.historico[dataSelecionada] =
                statusAtual;
        });

        try {
            localStorage.setItem(
                CHAVE_FALTAS,
                JSON.stringify(registros)
            );

            alert('Chamada e faltas salvas com sucesso!');

            carregarTabelaFaltas(
                disciplinaId,
                containerTabela
            );
        } catch (erro) {
            console.error(
                'Erro ao salvar faltas:',
                erro
            );

            showToast(
                'Não foi possível salvar a chamada.'
            );
        }
    });
}