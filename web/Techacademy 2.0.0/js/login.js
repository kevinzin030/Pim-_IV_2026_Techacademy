// Sistema de Autenticação - TechAcademy
let selectedRole = 'professor';

// Base de Usuários Cadastrados
const usuariosCadastrados = {
    professores: {
        'PROF-2024-882': { nome: "Dr. Carlos Eduardo Silva", email: "carlos.silva@techacademy.edu", matricula: "PROF-2024-882", telefone: "(11) 98765-4321" },
        'PROF-2024-901': { nome: "Dra. Ana Beatris Souza", email: "ana.souza@techacademy.edu", matricula: "PROF-2024-901", telefone: "(11) 97654-3210" },
        'PROF-2024-905': { nome: "Prof. Roberto Mendes", email: "roberto.mendes@techacademy.edu", matricula: "PROF-2024-905", telefone: "(11) 96543-2109" }
    },
    alunos: {
        '2023001': { nome: "Lucas Mendes Santos", email: "lucas.mendes@aluno.techacademy.edu", matricula: "2023001", curso: "Análise e Desenvolvimento de Sistemas" },
        '2023002': { nome: "Mariana Oliveira Rocha", email: "mariana.rocha@aluno.techacademy.edu", matricula: "2023002", curso: "Engenharia de Software" },
        '2023003': { nome: "Gabriel Costa Lima", email: "gabriel.lima@aluno.techacademy.edu", matricula: "2023003", curso: "Sistemas de Informação" },
        '2023004': { nome: "Beatriz Souza Alves", email: "beatriz.alves@aluno.techacademy.edu", matricula: "2023004", curso: "Análise e Desenvolvimento de Sistemas" }
    }
};

const btnProf = document.getElementById('role-btn-professor');
const btnAluno = document.getElementById('role-btn-aluno');
const loginLabel = document.getElementById('login-label');
const loginInput = document.getElementById('login-identifier');
const loginPassword = document.getElementById('login-password');

function selecionarRole(role) {
    selectedRole = role;
    if (role === 'professor') {
        btnProf.className = "flex-1 py-2 text-xs font-semibold rounded-lg transition-all shadow-sm bg-white text-brand-dark";
        btnAluno.className = "flex-1 py-2 text-xs font-semibold rounded-lg text-slate-500 hover:text-brand-dark transition-all";
        loginLabel.textContent = "RP (Registro de Professor)";
        loginInput.placeholder = "Digite seu RP";
    } else {
        btnAluno.className = "flex-1 py-2 text-xs font-semibold rounded-lg transition-all shadow-sm bg-white text-brand-dark";
        btnProf.className = "flex-1 py-2 text-xs font-semibold rounded-lg text-slate-500 hover:text-brand-dark transition-all";
        loginLabel.textContent = "RA (Registro Acadêmico)";
        loginInput.placeholder = "Digite seu RA";
    }
    // Limpa os campos ao alternar o perfil
    loginInput.value = "";
    loginPassword.value = "";
}

btnProf?.addEventListener('click', () => selecionarRole('professor'));
btnAluno?.addEventListener('click', () => selecionarRole('aluno'));

document.getElementById('login-form')?.addEventListener('submit', (e) => {
    e.preventDefault();
    const identifier = loginInput.value.trim().toUpperCase();
    const password = loginPassword.value;

    if (!identifier || !password) {
        alert('Por favor, preencha o registro e a senha.');
        return;
    }

    if (selectedRole === 'professor') {
        const prof = usuariosCadastrados.professores[identifier];
        if (!prof) {
            alert('RP não encontrado ou credenciais inválidas.');
            return;
        }
        sessionStorage.setItem('usuario_logado', JSON.stringify({ role: 'professor', ...prof }));
        window.location.href = 'dashboard-professor.html';
    } else {
        const aluno = usuariosCadastrados.alunos[identifier];
        if (!aluno) {
            alert('RA não encontrado ou credenciais inválidas.');
            return;
        }
        sessionStorage.setItem('usuario_logado', JSON.stringify({ role: 'aluno', ...aluno }));
        window.location.href = 'dashboard-aluno.html';
    }
});

document.getElementById('forgot-password')?.addEventListener('click', (e) => {
    e.preventDefault();
    alert('Entre em contato com o suporte acadêmico para redefinir sua senha.');
});

// Inicialização limpa (campos vazios)
loginInput.value = "";
loginPassword.value = "";