/**
 * Renderiza um fragmento declarado no HTML.
 * @param {string} templateId Identificador do elemento <template> da página.
 * @param {Record<string, unknown>} values Valores dinâmicos que substituem marcadores.
 * @returns {string} Marcação HTML pronta para ser inserida no DOM.
 */
function renderHtmlTemplate(templateId, values = {}) {
    const template = document.getElementById(templateId);
    if (!template) {
        console.error(`Template HTML não encontrado: ${templateId}`);
        return '';
    }
    let markup = template.innerHTML;
    for (const [key, value] of Object.entries(values)) {
        markup = markup.replaceAll(`__TA_SLOT_${key}__`, value == null ? '' : String(value));
    }
    return markup;
}

/** Estado em memória do painel do aluno; XP, sequência diária e conteúdos podem ser sincronizados localmente. */
const state = {
    aluno: {
        nome: "Lucas Mendes Santos",
        email: "lucas.mendes@aluno.techacademy.edu",
        curso: "Análise e Desenvolvimento de Sistemas",
        xp: 450,
        streak: 5
    },
    disciplinas: [
        {
            id: 'disc-1',
            nome: 'Desenvolvimento Web Front-End & UX',
            codigo: 'TEC-301',
            aulas: [
                { id: 'a1', titulo: 'Aula 01: Fundamentos de UX/UI e Heurísticas de Nielsen', pdfNome: 'Aula_01_Heuristicas.pdf' },
                { id: 'a2', titulo: 'Aula 02: HTML5 Semântico e CSS Responsivo', pdfNome: 'Aula_02_HTML_CSS.pdf' }
            ],
            questionarios: [
                {
                    id: 'q1',
                    titulo: 'Quiz 01: Aplicação de Heurísticas',
                    xp: 100,
                    respondido: false,
                    pergunta: 'Qual heurística aborda a visibilidade do estado do sistema?',
                    opcoes: ['A) Prevenção de Erros', 'B) Visibilidade do Estado do Sistema', 'C) Estética Minimalista'],
                    correta: 1
                }
            ],
            notas: { p1: 8.5, p2: 7.0, trabalho: 9.0, quizzes: 8.0 }
        },
        {
            id: 'disc-2',
            nome: 'Engenharia de Software e Arquitetura',
            codigo: 'ENG-402',
            aulas: [
                { id: 'a3', titulo: 'Aula 01: Padrões de Projeto (Design Patterns)', pdfNome: 'Design_Patterns_Intro.pdf' }
            ],
            questionarios: [
                {
                    id: 'q3',
                    titulo: 'Quiz 01: Singleton & Factory',
                    xp: 120,
                    respondido: false,
                    pergunta: 'Qual padrão de projeto garante apenas uma instância de uma classe?',
                    opcoes: ['A) Factory Method', 'B) Singleton', 'C) Observer'],
                    correta: 1
                }
            ],
            notas: { p1: 7.5, p2: 8.0, trabalho: 8.5, quizzes: 7.0 }
        }
    ]
};

// Usa a mesma chave do painel do professor para compartilhar aulas, quizzes e notificações neste navegador.
const STORAGE_KEY = 'techacademy_shared_v1';
state.notificacoes = [];
/**
 * Formata valores para apresentação consistente na interface.
 */
function dataHoraPt(valor) { if (!valor) return 'Data não registrada'; const d = new Date(valor); return Number.isNaN(d.getTime()) ? valor : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }); }
/**
 * Recupera dados persistidos no navegador e prepara o estado da aplicação.
 */
function carregarNotificacoes() { try { const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); state.notificacoes = Array.isArray(saved.notificacoes) ? saved.notificacoes : []; } catch { state.notificacoes = []; } atualizarSinoNotificacoes(); }
/**
 * Atualiza os elementos da interface com os dados mais recentes.
 */
function atualizarSinoNotificacoes() { const badge = document.getElementById('notification-count'); if (badge) { const n = (state.notificacoes || []).filter(x => !x.lida).length; badge.textContent = n > 99 ? '99+' : n; badge.classList.toggle('hidden', n === 0); } }
/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderNotificacoes() { const painel = document.getElementById('notification-panel'); if (!painel) return; const lista = (state.notificacoes || []).slice().sort((a,b)=>new Date(b.dataHora)-new Date(a.dataHora)); painel.innerHTML = renderHtmlTemplate('ta-aluno-fragment-027', { "0": (lista.length ? lista.map(n=>renderHtmlTemplate('ta-aluno-fragment-029', { "0": (n.lida?'bg-slate-50':'bg-blue-50'), "1": (n.tipo==='quiz'?'Novo quiz':n.tipo==='desafio'?'Novo desafio final':'Nova aula publicada'), "2": (n.titulo), "3": (n.disciplina || ''), "4": (dataHoraPt(n.dataHora)) })).join('') : renderHtmlTemplate('ta-aluno-fragment-028')) }); const marcar=document.getElementById('marcar-notificacoes-lidas'); if(marcar) marcar.onclick=()=>{state.notificacoes=(state.notificacoes||[]).map(n=>({...n,lida:true})); const saved=JSON.parse(localStorage.getItem(STORAGE_KEY)||'{}'); localStorage.setItem(STORAGE_KEY,JSON.stringify({...saved,notificacoes:state.notificacoes})); renderNotificacoes(); atualizarSinoNotificacoes();}; }
/**
 * Persiste o estado atual no armazenamento local do navegador.
 */
function salvarDadosCompartilhados() {
    try { const atual = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...atual, disciplinas: state.disciplinas, notificacoes: state.notificacoes || atual.notificacoes || [] })); }
    catch (e) { console.warn('Não foi possível salvar os dados locais.', e); }
}
/**
 * Recupera dados persistidos no navegador e prepara o estado da aplicação.
 */
function carregarDadosCompartilhados() {
    try {
        const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
        if (saved && Array.isArray(saved.disciplinas) && saved.disciplinas.length) {
            state.disciplinas = saved.disciplinas.map((d, i) => ({ ...d, nome: d.nome || `Disciplina ${i+1}`, codigo: d.codigo || `TEC-${i+1}`, notas: d.notas || { p1: 0, p2: 0, trabalho: 0, quizzes: 0 }, aulas: d.aulas || [], questionarios: (d.questionarios || []).map(q => ({ ...q, perguntas: q.perguntas || [{ pergunta: q.pergunta || '', opcoes: q.opcoes || q.opcoesResposta || [], correta: Number(q.correta || 0) }] })) }));
        }
    } catch (e) { console.warn('Falha ao ler dados locais.', e); }
}

/**
 * Calcula o resultado numérico usado pelas regras acadêmicas da plataforma.
 */
function calcularMedia(p1, p2, trab, quiz) {
    const media = (p1 * 0.3) + (p2 * 0.3) + (trab * 0.25) + (quiz * 0.15);
    return media.toFixed(1);
}

window.addEventListener('DOMContentLoaded', () => {
    carregarDadosCompartilhados();
    carregarNotificacoes();
    bindNavEvents();
    // Delegação de eventos: os elementos podem vir de templates HTML inseridos depois do carregamento.
    // Assim, o HTML descreve os controles e este arquivo concentra toda a lógica de interação.
    document.addEventListener('click', (event) => {
        const alvoAcao = event.target.closest('[data-action]');
        if (alvoAcao) {
            const disciplinaId = alvoAcao.dataset.disciplinaId;
            switch (alvoAcao.dataset.action) {
                case 'open-discipline':
                    abrirDetalheDisciplina(disciplinaId);
                    return;
                case 'back-to-disciplines':
                    renderDisciplinas();
                    return;
                case 'back-to-discipline':
                    event.preventDefault();
                    setActiveNav(document.getElementById('nav-disciplinas'), renderDisciplinas, 'Disciplinas & Aulas');
                    window.setTimeout(() => abrirDetalheDisciplina(disciplinaId), 0);
                    return;
            }
        }

        const botaoQuiz = event.target.closest('[data-abrir-quiz="1"]');
        if (!botaoQuiz) return;
        event.preventDefault();
        event.stopPropagation();
        abrirQuiz(botaoQuiz.dataset.disciplinaId, botaoQuiz.dataset.quizId);
    });
    renderDisciplinas();
    updateGamificationWidget();
});

/**
 * Associa eventos da interface às funções que implementam o comportamento.
 */
function bindNavEvents() {
    document.getElementById('nav-disciplinas').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderDisciplinas, 'Disciplinas & Aulas'));
    document.getElementById('nav-quizzes').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderQuizzes, 'Questionários & Desafios'));
    document.getElementById('nav-boletim').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderBoletim, 'Meu Boletim / Notas'));
    document.getElementById('nav-perfil').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderPerfil, 'Meu Perfil'));
}

/**
 * Atualiza a navegação ativa, o título da página e o conteúdo exibido.
 */
function setActiveNav(button, renderFn, title) {
    document.querySelectorAll('nav button').forEach(b => {
        b.className = "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all text-slate-300 hover:bg-slate-800 hover:text-white";
    });
    button.className = "w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all bg-brand-blue text-white shadow-md";
    document.getElementById('page-title').textContent = title;
    renderFn();
}

/**
 * Atualiza os elementos da interface com os dados mais recentes.
 */
function updateGamificationWidget() {
    document.getElementById('widget-xp').innerHTML = renderHtmlTemplate('ta-aluno-fragment-026', { "0": (state.aluno.xp) });
    document.getElementById('streak-count').textContent = state.aluno.streak;
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderDisciplinas() {
    const main = document.getElementById('main-content-area');
    main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-024', { "0": (state.disciplinas.map(d => renderHtmlTemplate('ta-aluno-fragment-025', { "0": (d.id), "1": (d.codigo), "2": (d.nome), "3": (d.aulas.length), "4": (d.questionarios.length) })).join('')) });
}

/**
 * Abre o conteúdo solicitado e prepara os controles e eventos necessários.
 */
function abrirDetalheDisciplina(discId) {
    const disc = state.disciplinas.find(d => d.id === discId);
    if (!disc) return;
    const container = document.getElementById('detalhes-disciplina-container');
    const aulasHtml = disc.aulas.length ? disc.aulas.map((a, i) => {
        const quizzes = disc.questionarios.filter(q => q.aulaId === a.id || (!q.aulaId && i === 0));
        const pdfUrl = a.pdfData || '';
        return renderHtmlTemplate('ta-aluno-fragment-018', { "0": (a.titulo), "1": (a.pdfNome || 'Material da aula'), "2": (dataHoraPt(a.dataHora || a.data)), "3": (pdfUrl ? renderHtmlTemplate('ta-aluno-fragment-020', { "0": (a.titulo), "1": (pdfUrl) }) : renderHtmlTemplate('ta-aluno-fragment-019')), "4": (pdfUrl ? renderHtmlTemplate('ta-aluno-fragment-021', { "0": (pdfUrl), "1": (a.pdfNome || 'slides.pdf') }) : ''), "5": (quizzes.length ? quizzes.map(q => renderHtmlTemplate('ta-aluno-fragment-023', { "0": (q.titulo), "1": ((q.perguntas || [{pergunta:q.pergunta}]).length), "2": (q.respondido ? 'Concluído' : 'Ainda não iniciado'), "3": (q.xp), "4": (dataHoraPt(q.dataHora)), "5": (disc.id), "6": (q.id), "7": (q.respondido ? 'Ver resultado' : 'Acessar quiz') })).join('') : renderHtmlTemplate('ta-aluno-fragment-022')) });
    }).join('') : renderHtmlTemplate('ta-aluno-fragment-017');
    const quizzesGerais = disc.questionarios.filter(q => q.aulaId === 'geral');
    container.innerHTML = renderHtmlTemplate('ta-aluno-fragment-014', { "0": (disc.codigo), "1": (disc.nome), "2": (disc.aulas.length), "3": (aulasHtml), "4": (quizzesGerais.length ? renderHtmlTemplate('ta-aluno-fragment-015', { "0": (quizzesGerais.map(q=>renderHtmlTemplate('ta-aluno-fragment-016', { "0": (q.titulo), "1": (disc.id), "2": (q.id) })).join('')) }) : '') });
    container.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/**
 * Abre o conteúdo solicitado e prepara os controles e eventos necessários.
 */
function abrirQuiz(discId, quizId) {
    const disc = state.disciplinas.find(d => d.id === discId);
    const quiz = disc && disc.questionarios.find(q => q.id === quizId);
    if (!disc || !quiz) {
        console.error('Quiz não encontrado:', { discId, quizId, disciplinas: state.disciplinas });
        showToast('Não foi possível abrir este quiz. Atualize a página e tente novamente.');
        return;
    }
    const main = document.getElementById('main-content-area');
    const perguntas = quiz.perguntas && quiz.perguntas.length ? quiz.perguntas : [{ pergunta: quiz.pergunta || '', opcoes: quiz.opcoes || [], correta: quiz.correta || 0 }];
    main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-009', { "0": (disc.id), "1": (quiz.titulo), "2": (disc.nome), "3": (perguntas.length), "4": (quiz.xp), "5": (quiz.respondido ? renderHtmlTemplate('ta-aluno-fragment-013', { "0": (quiz.pontuacao ?? 0), "1": (perguntas.length) }) : renderHtmlTemplate('ta-aluno-fragment-010', { "0": (perguntas.map((p,i)=>renderHtmlTemplate('ta-aluno-fragment-011', { "0": (i+1), "1": (p.pergunta), "2": (p.opcoes.map((op,j)=>renderHtmlTemplate('ta-aluno-fragment-012', { "0": (i), "1": (j), "2": (op) })).join('')) })).join('')) })) });
    const form = document.getElementById('form-responder-quiz');
    if (form) form.addEventListener('submit', e => { e.preventDefault(); const respostas = perguntas.map((p,i)=>Number(new FormData(form).get(`pergunta-${i}`))); const acertos = respostas.reduce((n,r,i)=>n+(r===Number(perguntas[i].correta)?1:0),0); quiz.respondido = true; quiz.pontuacao = acertos; const xpGanho = Math.round(quiz.xp * acertos / perguntas.length); state.aluno.xp += xpGanho; state.aluno.streak += 1; salvarDadosCompartilhados(); updateGamificationWidget(); main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-008', { "0": (acertos), "1": (perguntas.length), "2": (xpGanho), "3": (disc.id) }); });
}

/**
 * Processa as respostas fornecidas pelo aluno e calcula o resultado.
 */
function responderQuiz(discId, quizId) { abrirQuiz(discId, quizId); }
/**
 * Executa uma etapa específica da lógica da plataforma; os elementos visuais correspondentes estão definidos no HTML.
 */
function fazerDownloadPdf(nomeArquivo) { showToast(`O PDF '${nomeArquivo}' ainda não está anexado a esta aula.`); }

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderQuizzes() {
    const main = document.getElementById('main-content-area');
    const items = state.disciplinas.flatMap(d => d.questionarios.map(q => ({ d, q })));
    main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-005', { "0": (items.length ? items.map(({d,q}) => renderHtmlTemplate('ta-aluno-fragment-007', { "0": (q.titulo), "1": (d.nome), "2": ((q.perguntas || []).length || 1), "3": (q.xp), "4": (dataHoraPt(q.dataHora)), "5": (q.respondido ? 'text-emerald-700' : 'text-amber-700'), "6": (q.respondido ? 'Concluído' : 'Pendente'), "7": (d.id), "8": (q.id), "9": (q.respondido ? 'Ver resultado' : 'Acessar quiz') })).join('') : renderHtmlTemplate('ta-aluno-fragment-006')) });
}

/**
 * Processa as respostas fornecidas pelo aluno e calcula o resultado.
 */
function responderQuizGeral(discId, quizId) { abrirQuiz(discId, quizId); }

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderBoletim() {
    const main = document.getElementById('main-content-area');
    main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-003', { "0": (state.disciplinas.map(d => {
                        const m = calcularMedia(d.notas.p1, d.notas.p2, d.notas.trabalho, d.notas.quizzes);
                        let statusBg = 'bg-emerald-100 text-emerald-800 border-emerald-200';
                        let statusTxt = 'Aprovado';
                        if (m < 5.0) {
                            statusBg = 'bg-red-100 text-red-800 border-red-200';
                            statusTxt = 'Reprovado';
                        } else if (m < 7.0) {
                            statusBg = 'bg-amber-100 text-amber-800 border-amber-200';
                            statusTxt = 'Exame';
                        }
                        return renderHtmlTemplate('ta-aluno-fragment-004', { "0": (d.nome), "1": (d.notas.p1.toFixed(1)), "2": (d.notas.p2.toFixed(1)), "3": (d.notas.trabalho.toFixed(1)), "4": (d.notas.quizzes.toFixed(1)), "5": (m), "6": (statusBg), "7": (statusTxt) });
                    }).join('')) });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderPerfil() {
    const main = document.getElementById('main-content-area');
    const a = state.aluno;
    main.innerHTML = renderHtmlTemplate('ta-aluno-fragment-002', { "0": (a.nome), "1": (a.email), "2": (a.curso) });

    document.getElementById('form-perfil-aluno').addEventListener('submit', (e) => {
        e.preventDefault();
        a.nome = document.getElementById('aluno-nome').value;
        a.email = document.getElementById('aluno-email').value;
        
        // Atualiza sidebar
        const nameSidebar = document.getElementById('aluno-name-sidebar');
        const emailSidebar = document.getElementById('aluno-email-sidebar');
        if (nameSidebar) nameSidebar.textContent = a.nome;
        if (emailSidebar) emailSidebar.textContent = a.email;

        showToast('Perfil do estudante atualizado com sucesso!');
    });
}

/**
 * Exibe uma mensagem ou informação ao usuário.
 */
function showToast(msg) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'bg-slate-900 text-white px-4 py-3 rounded-xl shadow-xl text-xs flex items-center gap-2 border border-slate-700';
    toast.innerHTML = renderHtmlTemplate('ta-aluno-fragment-001', { "0": (msg) });
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}
document.addEventListener('DOMContentLoaded', () => { const b=document.getElementById('notification-bell'); const p=document.getElementById('notification-panel'); if(b&&p)b.addEventListener('click',()=>{p.classList.toggle('hidden');if(!p.classList.contains('hidden')&&typeof renderNotificacoes==='function')renderNotificacoes();}); });
