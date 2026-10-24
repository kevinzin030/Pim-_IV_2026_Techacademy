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

/** Estado em memória do painel do professor. Os dados compartilhados são sincronizados com localStorage. */
const state = {
    user: {
        nome: "Dr. Carlos Eduardo Silva",
        email: "carlos.silva@techacademy.edu",
        matricula: "PROF-2024-882",
        telefone: "(11) 98765-4321"
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
                { id: 'a1', titulo: 'Aula 01: Fundamentos de UX/UI e Heurísticas de Nielsen', pdfNome: 'Aula_01_Heuristicas.pdf', data: '12/03/2024' },
                { id: 'a2', titulo: 'Aula 02: HTML5 Semântico e CSS Responsivo', pdfNome: 'Aula_02_HTML_CSS.pdf', data: '19/03/2024' }
            ],
            questionarios: [
                {
                    id: 'q1',
                    titulo: 'Quiz 01: Aplicação de Heurísticas',
                    xp: 100,
                    pergunta: 'Qual heurística aborda a visibilidade do estado do sistema?',
                    opcoes: ['A) Prevenção de Erros', 'B) Visibilidade do Estado do Sistema', 'C) Estética Minimalista'],
                    correta: 1
                }
            ],
            desafioFinal: {
                titulo: 'Desafio Final: Protótipo de E-Commerce Responsivo',
                xp: 300,
                descricao: 'Desenvolver um protótipo navegável aplicando as 10 heurísticas de Nielsen.'
            }
        },
        {
            id: 'disc-2',
            codigo: 'ENG-402',
            nome: 'Engenharia de Software e Arquitetura',
            turmaId: 't1',
            aulas: [
                { id: 'a3', titulo: 'Aula 01: Padrões de Projeto (Design Patterns)', pdfNome: 'Design_Patterns_Intro.pdf', data: '10/03/2024' }
            ],
            questionarios: [
                {
                    id: 'q2',
                    titulo: 'Quiz 01: Singleton & Factory',
                    xp: 120,
                    pergunta: 'Qual padrão de projeto garante apenas uma instância de uma classe?',
                    opcoes: ['A) Factory Method', 'B) Singleton', 'C) Observer'],
                    correta: 1
                }
            ],
            desafioFinal: {
                titulo: 'Desafio Final: Arquitetura em Microserviços',
                xp: 350,
                descricao: 'Projetar a arquitetura lógica para um sistema bancário descentralizado.'
            }
        }
    ],
    alunosNotas: [
        { id: 'al-1', nome: 'Lucas Mendes Santos', matricula: '2023001', p1: 8.5, p2: 7.0, trabalho: 9.0, quizzes: 8.0 },
        { id: 'al-2', nome: 'Mariana Oliveira Rocha', matricula: '2023002', p1: 6.0, p2: 5.5, trabalho: 7.0, quizzes: 6.5 },
        { id: 'al-3', nome: 'Gabriel Costa Lima', matricula: '2023003', p1: 4.0, p2: 3.5, trabalho: 5.0, quizzes: 4.0 },
        { id: 'al-4', nome: 'Beatriz Souza Alves', matricula: '2023004', p1: 9.5, p2: 9.0, trabalho: 10.0, quizzes: 9.5 }
    ],
    selecaoLancarNotas: {
        turmaId: '',
        disciplinaId: ''
    }
};

// Chave única compartilhada entre os painéis do professor e do aluno.
const STORAGE_KEY = 'techacademy_shared_v1';
state.notificacoes = [];
/**
 * Registra uma ocorrência da plataforma e atualiza os dados compartilhados.
 */
function registrarNotificacao(tipo, titulo, disciplina) {
    const item = { id: 'n-' + Date.now() + '-' + Math.random().toString(36).slice(2, 7), tipo, titulo, disciplina, dataHora: new Date().toISOString(), lida: false };
    const atual = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
    const notificacoes = Array.isArray(atual.notificacoes) ? atual.notificacoes : [];
    notificacoes.unshift(item);
    state.notificacoes = notificacoes.slice(0, 100);
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...atual, disciplinas: state.disciplinas, notificacoes: notificacoes.slice(0, 100) }));
}
/**
 * Persiste o estado atual no armazenamento local do navegador.
 */
function salvarDadosCompartilhados() {
    try { const atual = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...atual, disciplinas: state.disciplinas, notificacoes: state.notificacoes || atual.notificacoes || [] })); }
    catch (e) { showToast('Não foi possível salvar. O PDF pode ser grande demais para o armazenamento local.'); }
}
/**
 * Recupera dados persistidos no navegador e prepara o estado da aplicação.
 */
function carregarDadosCompartilhados() {
    try {
        const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
        if (saved && Array.isArray(saved.disciplinas) && saved.disciplinas.length) {
            state.disciplinas = saved.disciplinas.map(d => ({ ...d, aulas: d.aulas || [], questionarios: (d.questionarios || []).map(q => ({ ...q, perguntas: q.perguntas || [{ pergunta: q.pergunta || '', opcoes: q.opcoes || [], correta: Number(q.correta || 0) }] })) }));
        } else salvarDadosCompartilhados();
    } catch (e) { console.warn('Falha ao carregar dados locais.', e); }
}

window.addEventListener('DOMContentLoaded', () => {
    carregarDadosCompartilhados();
    bindNavEvents();
    renderDisciplinas();
});

/**
 * Associa eventos da interface às funções que implementam o comportamento.
 */
function bindNavEvents() {
    document.getElementById('nav-disciplinas').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderDisciplinas, 'Minhas Disciplinas'));
    document.getElementById('nav-postar-aula').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderPostarAula, 'Postar Slides / Aulas'));
    document.getElementById('nav-criar-quiz').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderCriarQuiz, 'Criar Quizzes & Desafio'));
    document.getElementById('nav-lancar-notas').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderLancarNotas, 'Lançar & Consultar Notas'));
    document.getElementById('nav-metricas').addEventListener('click', (e) => setActiveNav(e.currentTarget, renderMetricas, 'Métricas da Turma'));
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
 * Calcula o resultado numérico usado pelas regras acadêmicas da plataforma.
 */
function calcularMedia(p1, p2, trab, quiz) {
    const media = (p1 * 0.3) + (p2 * 0.3) + (trab * 0.25) + (quiz * 0.15);
    return media.toFixed(1);
}

/**
 * Obtém os dados derivados necessários para exibir a situação acadêmica.
 */
function obterSituacao(media) {
    if (media < 5.0) return { status: 'Reprovado', badge: 'bg-red-100 text-red-800 border-red-200' };
    if (media < 7.0) return { status: 'Exame', badge: 'bg-amber-100 text-amber-800 border-amber-200' };
    return { status: 'Aprovado', badge: 'bg-emerald-100 text-emerald-800 border-emerald-200' };
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderDisciplinas() {
    const main = document.getElementById('main-content-area');
    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-028', { "0": (state.disciplinas.map(d => renderHtmlTemplate('ta-prof-fragment-029', { "0": (d.codigo), "1": (state.turmas.find(t=>t.id===d.turmaId)?.nome || ''), "2": (d.nome), "3": (d.aulas.length), "4": (d.questionarios.length), "5": (d.desafioFinal ? d.desafioFinal.titulo : 'Não cadastrado') })).join('')) });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderPostarAula() {
    const main = document.getElementById('main-content-area');
    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-024', { "0": (state.disciplinas.map(d => renderHtmlTemplate('ta-prof-fragment-025', { "0": (d.id), "1": (d.codigo), "2": (d.nome) })).join('')), "1": (state.disciplinas.flatMap(d => d.aulas.map(a => renderHtmlTemplate('ta-prof-fragment-027', { "0": (a.titulo), "1": (d.codigo), "2": (a.pdfNome || "Sem PDF"), "3": (a.data || "Data não registrada"), "4": (a.id), "5": (d.id) }))).join("") || renderHtmlTemplate('ta-prof-fragment-026')) });

    const formAula = document.getElementById('form-postar-aula');
    const resetEdicaoAula = () => { document.getElementById('aula-edit-id').value=''; formAula.reset(); document.getElementById('titulo-form-aula').innerHTML=renderHtmlTemplate('ta-prof-fragment-023'); document.getElementById('btn-salvar-aula').innerHTML=renderHtmlTemplate('ta-prof-fragment-022'); document.getElementById('cancelar-edicao-aula').classList.add('hidden'); };
    document.querySelectorAll('[data-editar-aula]').forEach(btn => btn.addEventListener('click', () => {
        const disc = state.disciplinas.find(d=>d.id===btn.dataset.disc); const aula = disc?.aulas.find(a=>a.id===btn.dataset.editarAula); if(!aula) return;
        document.getElementById('aula-edit-id').value=aula.id; document.getElementById('aula-disciplina').value=disc.id; document.getElementById('aula-titulo').value=aula.titulo; document.getElementById('aula-pdf').required=false;
        document.getElementById('titulo-form-aula').innerHTML=renderHtmlTemplate('ta-prof-fragment-021'); document.getElementById('btn-salvar-aula').innerHTML='Salvar alterações'; document.getElementById('cancelar-edicao-aula').classList.remove('hidden'); formAula.scrollIntoView({behavior:'smooth',block:'start'});
    }));
    document.getElementById('cancelar-edicao-aula').addEventListener('click', () => { document.getElementById('aula-pdf').required=true; resetEdicaoAula(); });
    formAula.addEventListener('submit', async (e) => {
        e.preventDefault(); const discId=document.getElementById('aula-disciplina').value; const titulo=document.getElementById('aula-titulo').value.trim(); const file=document.getElementById('aula-pdf').files[0]; const editId=document.getElementById('aula-edit-id').value;
        const disc=state.disciplinas.find(d=>d.id===discId); if(!disc) return;
        if(file && (file.type!=='application/pdf' && !file.name.toLowerCase().endsWith('.pdf'))) { showToast('Selecione um arquivo PDF válido.'); return; }
        if(file && file.size>3*1024*1024) { showToast('Para armazenamento local, use PDFs de até 3 MB.'); return; }
        let pdfData=null; if(file) pdfData=await new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(reader.result);reader.onerror=reject;reader.readAsDataURL(file);});
        if(editId) {
            let aulaAtual=null; for(const d of state.disciplinas){const idx=d.aulas.findIndex(a=>a.id===editId);if(idx>=0){aulaAtual=d.aulas[idx];if(d.id!==discId){d.aulas.splice(idx,1);disc.aulas.push(aulaAtual);}break;}}
            if(!aulaAtual){showToast('Não foi possível localizar esta aula.');return;} aulaAtual.titulo=titulo; if(file){aulaAtual.pdfNome=file.name;aulaAtual.pdfData=pdfData;} aulaAtual.data=new Date().toLocaleDateString('pt-BR'); aulaAtual.dataHora=new Date().toISOString();
            showToast('Postagem atualizada com sucesso!');
        } else { if(!file){showToast('Selecione um PDF para publicar uma nova aula.');return;} const agora=new Date(); disc.aulas.push({id:'a-'+Date.now(),titulo,pdfNome:file.name,pdfData,data:agora.toLocaleDateString('pt-BR'),dataHora:agora.toISOString()}); registrarNotificacao('aula', titulo, disc.nome); showToast('Aula e PDF publicados para os alunos!'); }
        salvarDadosCompartilhados(); renderPostarAula();
    });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderCriarQuiz() {
    const main = document.getElementById('main-content-area');
    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-015', { "0": (state.disciplinas.map(d => renderHtmlTemplate('ta-prof-fragment-016', { "0": (d.id), "1": (d.codigo), "2": (d.nome) })).join('')), "1": (state.disciplinas.flatMap(d => d.aulas.map(a => renderHtmlTemplate('ta-prof-fragment-017', { "0": (d.id), "1": (a.id), "2": (d.codigo), "3": (a.titulo) }))).join('')), "2": (state.disciplinas.map(d => renderHtmlTemplate('ta-prof-fragment-018', { "0": (d.id), "1": (d.codigo), "2": (d.nome) })).join('')), "3": (state.disciplinas.flatMap(d => d.questionarios.map(q => renderHtmlTemplate('ta-prof-fragment-020', { "0": (q.titulo), "1": (d.codigo), "2": ((q.perguntas||[]).length || (q.pergunta?1:0)), "3": (q.xp||100), "4": (q.id), "5": (d.id) }))).join('') || renderHtmlTemplate('ta-prof-fragment-019')) });

    const perguntasContainer = document.getElementById('quiz-perguntas-container');
    /**
 * Adiciona um novo elemento de formulário ou conteúdo à interface.
 */
function adicionarPergunta() {
        const n = perguntasContainer.querySelectorAll('.quiz-pergunta-item').length + 1;
        const el = document.createElement('div'); el.className = 'quiz-pergunta-item p-4 border border-slate-200 rounded-xl space-y-2';
        el.innerHTML = renderHtmlTemplate('ta-prof-fragment-013', { "0": (n), "1": (n > 1 ? renderHtmlTemplate('ta-prof-fragment-014') : ''), "2": (n) });
        el.querySelector('.remover-pergunta')?.addEventListener('click', () => { el.remove(); [...perguntasContainer.querySelectorAll('.quiz-pergunta-item')].forEach((item,i)=>item.querySelector('strong').textContent=`Pergunta ${i+1}`); });
        perguntasContainer.appendChild(el);
    }
    adicionarPergunta(); document.getElementById('adicionar-pergunta').addEventListener('click', adicionarPergunta);
    const selectAula = document.getElementById('quiz-aula');
    /**
 * Atualiza os elementos da interface com os dados mais recentes.
 */
function atualizarAulasDoQuiz() {
        const selecionada = document.getElementById('quiz-disciplina').value;
        const discAtual = state.disciplinas.find(d => d.id === selecionada);
        selectAula.innerHTML = renderHtmlTemplate('ta-prof-fragment-011', { "0": ((discAtual?.aulas || []).map(a => renderHtmlTemplate('ta-prof-fragment-012', { "0": (discAtual.id), "1": (a.id), "2": (a.titulo) })).join('')) });
    }
    document.getElementById('quiz-disciplina').addEventListener('change', atualizarAulasDoQuiz);
    atualizarAulasDoQuiz();
    let quizEmEdicaoId = null;
    document.querySelectorAll('[data-editar-quiz]').forEach(btn => btn.addEventListener('click', () => {
        const disc=state.disciplinas.find(d=>d.id===btn.dataset.disc); const quiz=disc?.questionarios.find(q=>q.id===btn.dataset.editarQuiz); if(!quiz) return;
        quizEmEdicaoId=quiz.id; document.getElementById('quiz-edit-id').value=quiz.id; document.getElementById('quiz-disciplina').value=disc.id; atualizarAulasDoQuiz();
        const aulaOpt=[...selectAula.options].find(o=>o.value===`${disc.id}|${quiz.aulaId}`) || [...selectAula.options].find(o=>o.value==='geral|geral'); if(aulaOpt) selectAula.value=aulaOpt.value;
        document.getElementById('quiz-titulo').value=quiz.titulo||''; document.getElementById('quiz-xp').value=quiz.xp||100; perguntasContainer.innerHTML='';
        const perguntas=quiz.perguntas||[{pergunta:quiz.pergunta||'',opcoes:quiz.opcoes||['','','',''],correta:Number(quiz.correta||0)}];
        perguntas.forEach(p=>{adicionarPergunta();const el=perguntasContainer.lastElementChild;el.querySelector('.pergunta-enunciado').value=p.pergunta||'';[...el.querySelectorAll('.pergunta-opcao')].forEach((input,i)=>input.value=(p.opcoes?.[i]||'').replace(/^[A-D]\)\s*/,''));el.querySelector('.pergunta-correta').value=String(p.correta||0);});
        document.getElementById('btn-salvar-quiz').textContent='Salvar alterações do quiz'; document.getElementById('cancelar-edicao-quiz').classList.remove('hidden'); document.getElementById('form-criar-quiz').scrollIntoView({behavior:'smooth',block:'start'});
    }));
    document.getElementById('cancelar-edicao-quiz').addEventListener('click',()=>{quizEmEdicaoId=null;document.getElementById('quiz-edit-id').value='';document.getElementById('form-criar-quiz').reset();perguntasContainer.innerHTML='';adicionarPergunta();document.getElementById('btn-salvar-quiz').textContent='Cadastrar Quiz';document.getElementById('cancelar-edicao-quiz').classList.add('hidden');atualizarAulasDoQuiz();});

    document.getElementById('form-criar-quiz').addEventListener('submit', (e) => {
        e.preventDefault();
        const discId = document.getElementById('quiz-disciplina').value;
        const disc = state.disciplinas.find(d => d.id === discId);

        const vinculo = document.getElementById('quiz-aula').value.split('|');
        const perguntas = [...document.querySelectorAll('.quiz-pergunta-item')].map((el) => ({
            pergunta: el.querySelector('.pergunta-enunciado').value.trim(),
            opcoes: [...el.querySelectorAll('.pergunta-opcao')].map((input, i) => `${String.fromCharCode(65+i)}) ${input.value.trim()}`),
            correta: Number(el.querySelector('.pergunta-correta').value)
        }));
        if (!perguntas.length || perguntas.some(p => !p.pergunta || p.opcoes.some(o => !o.slice(3).trim()))) { showToast('Preencha todas as perguntas e alternativas.'); return; }
        const alvoDisc = vinculo[0] === 'geral' ? disc : state.disciplinas.find(d => d.id === vinculo[0]);
        const dadosQuiz = { id: quizEmEdicaoId || 'q-' + Date.now(), titulo: document.getElementById('quiz-titulo').value.trim(), xp: parseInt(document.getElementById('quiz-xp').value), aulaId: vinculo[0] === 'geral' ? 'geral' : vinculo[1], perguntas, respondido: false, dataHora: quizEmEdicaoId ? (state.disciplinas.flatMap(d=>d.questionarios).find(q=>q.id===quizEmEdicaoId)?.dataHora || new Date().toISOString()) : new Date().toISOString() };
        if (quizEmEdicaoId) { for (const d of state.disciplinas) { const idx=d.questionarios.findIndex(q=>q.id===quizEmEdicaoId); if(idx>=0){d.questionarios.splice(idx,1);break;} } alvoDisc.questionarios.push(dadosQuiz); showToast('Quiz atualizado com sucesso!'); }
        else { alvoDisc.questionarios.push(dadosQuiz); registrarNotificacao('quiz', dadosQuiz.titulo, alvoDisc.nome); showToast('Quiz criado e vinculado à aula!'); }
        salvarDadosCompartilhados(); renderCriarQuiz();
    });

    document.getElementById('form-desafio-final').addEventListener('submit', (e) => {
        e.preventDefault();
        const discId = document.getElementById('desafio-disciplina').value;
        const disc = state.disciplinas.find(d => d.id === discId);

        const tituloDesafio = document.getElementById('desafio-titulo').value.trim();
        const eraNovo = !disc.desafioFinal || !disc.desafioFinal.dataHora;
        disc.desafioFinal = { titulo: tituloDesafio, xp: parseInt(document.getElementById('desafio-xp').value), descricao: document.getElementById('desafio-desc').value, dataHora: eraNovo ? new Date().toISOString() : disc.desafioFinal.dataHora };
        if (eraNovo) registrarNotificacao('desafio', tituloDesafio, disc.nome);
        salvarDadosCompartilhados();
        showToast(eraNovo ? 'Desafio final publicado e alunos notificados!' : 'Desafio final atualizado para esta disciplina!');
    });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderLancarNotas() {
    const main = document.getElementById('main-content-area');
    const { turmaId, disciplinaId } = state.selecaoLancarNotas;

    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-007', { "0": (state.turmas.map(t => renderHtmlTemplate('ta-prof-fragment-008', { "0": (t.id), "1": (t.id === turmaId ? 'selected' : ''), "2": (t.nome) })).join('')), "1": (!turmaId ? 'disabled' : ''), "2": (state.disciplinas.filter(d => d.turmaId === turmaId).map(d => renderHtmlTemplate('ta-prof-fragment-009', { "0": (d.id), "1": (d.id === disciplinaId ? 'selected' : ''), "2": (d.codigo), "3": (d.nome) })).join('')), "3": ((turmaId && disciplinaId) ? renderTabelaNotasHTML() : renderHtmlTemplate('ta-prof-fragment-010')) });

    // Eventos dos Selects
    document.getElementById('select-turma').addEventListener('change', (e) => {
        state.selecaoLancarNotas.turmaId = e.target.value;
        state.selecaoLancarNotas.disciplinaId = ''; // reseta disciplina ao trocar turma
        renderLancarNotas();
    });

    const selectDisc = document.getElementById('select-disciplina');
    if (selectDisc) {
        selectDisc.addEventListener('change', (e) => {
            state.selecaoLancarNotas.disciplinaId = e.target.value;
            renderLancarNotas();
        });
    }

    bindTabelaNotasEvents();
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderTabelaNotasHTML() {
    return renderHtmlTemplate('ta-prof-fragment-005', { "0": (state.alunosNotas.map((aluno, index) => {
                        const media = calcularMedia(aluno.p1, aluno.p2, aluno.trabalho, aluno.quizzes);
                        const sit = obterSituacao(parseFloat(media));
                        return renderHtmlTemplate('ta-prof-fragment-006', { "0": (aluno.nome), "1": (aluno.matricula), "2": (aluno.p1), "3": (index), "4": (aluno.p2), "5": (index), "6": (aluno.trabalho), "7": (index), "8": (aluno.quizzes), "9": (index), "10": (index), "11": (media), "12": (index), "13": (sit.badge), "14": (sit.status) });
                    }).join('')) });
}

/**
 * Associa eventos da interface às funções que implementam o comportamento.
 */
function bindTabelaNotasEvents() {
    document.querySelectorAll('.input-nota').forEach(input => {
        input.addEventListener('input', (e) => {
            const idx = e.target.getAttribute('data-index');
            const field = e.target.getAttribute('data-field');
            const val = parseFloat(e.target.value) || 0;

            state.alunosNotas[idx][field] = val;

            const al = state.alunosNotas[idx];
            const novaMedia = calcularMedia(al.p1, al.p2, al.trabalho, al.quizzes);
            const novaSit = obterSituacao(parseFloat(novaMedia));

            document.getElementById(`media-${idx}`).textContent = novaMedia;
            document.getElementById(`situacao-${idx}`).innerHTML = renderHtmlTemplate('ta-prof-fragment-004', { "0": (novaSit.badge), "1": (novaSit.status) });
        });
    });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderMetricas() {
    const main = document.getElementById('main-content-area');
    
    // Cálculos estatísticos
    const totalAlunos = state.alunosNotas.length;
    let somaMedias = 0;
    let somaQuizzes = 0;
    let aprovados = 0, exame = 0, reprovados = 0;

    state.alunosNotas.forEach(al => {
        const m = parseFloat(calcularMedia(al.p1, al.p2, al.trabalho, al.quizzes));
        somaMedias += m;
        somaQuizzes += al.quizzes;
        if (m >= 7.0) aprovados++;
        else if (m >= 5.0) exame++;
        else reprovados++;
    });

    const mediaGeralTurma = (somaMedias / totalAlunos).toFixed(1);
    const mediaQuizzesTurma = (somaQuizzes / totalAlunos).toFixed(1);

    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-003', { "0": (mediaGeralTurma), "1": (mediaQuizzesTurma), "2": (Math.round((aprovados/totalAlunos)*100)), "3": (totalAlunos), "4": (aprovados), "5": ((aprovados/totalAlunos)*100), "6": (exame), "7": ((exame/totalAlunos)*100), "8": (reprovados), "9": ((reprovados/totalAlunos)*100) });

    // Os percentuais são dados calculados; o CSS define cor, altura, animação e acabamento das barras.
    main.querySelectorAll('[data-progress-percent]').forEach((barra) => {
        const percentual = Number(barra.dataset.progressPercent);
        const percentualLimitado = Math.max(0, Math.min(100, percentual));
        barra.style.setProperty('--progress-width', `${percentualLimitado}%`);
    });
}

/**
 * Renderiza a seção da interface indicada e atualiza a área principal da página.
 */
function renderPerfil() {
    const main = document.getElementById('main-content-area');
    const u = state.user;
    main.innerHTML = renderHtmlTemplate('ta-prof-fragment-002', { "0": (u.nome), "1": (u.email), "2": (u.matricula), "3": (u.telefone) });

    document.getElementById('form-perfil').addEventListener('submit', (e) => {
        e.preventDefault();
        u.nome = document.getElementById('perf-nome').value;
        u.email = document.getElementById('perf-email').value;
        u.telefone = document.getElementById('perf-tel').value;

        // Atualiza elementos do sidebar
        document.getElementById('prof-name-sidebar').textContent = u.nome;
        document.getElementById('prof-email-sidebar').textContent = u.email;

        showToast('Perfil atualizado com sucesso!');
    });
}

/**
 * Exibe uma mensagem ou informação ao usuário.
 */
function showToast(msg) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'bg-slate-900 text-white px-4 py-3 rounded-xl shadow-xl text-xs flex items-center gap-2 border border-slate-700 animate-fade-in';
    toast.innerHTML = renderHtmlTemplate('ta-prof-fragment-001', { "0": (msg) });
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}