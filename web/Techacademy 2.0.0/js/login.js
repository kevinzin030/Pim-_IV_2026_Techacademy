let selectedRole = 'professor';

        const btnProf = document.getElementById('role-btn-professor');
        const btnAluno = document.getElementById('role-btn-aluno');
        const emailInput = document.getElementById('login-email');

        btnProf.addEventListener('click', () => {
            selectedRole = 'professor';
            btnProf.className = "flex-1 py-2 text-xs font-semibold rounded-lg transition-all shadow-sm bg-white text-brand-dark";
            btnAluno.className = "flex-1 py-2 text-xs font-semibold rounded-lg text-slate-500 hover:text-brand-dark transition-all";
            emailInput.value = "carlos.silva@techacademy.edu";
        });

        btnAluno.addEventListener('click', () => {
            selectedRole = 'aluno';
            btnAluno.className = "flex-1 py-2 text-xs font-semibold rounded-lg transition-all shadow-sm bg-white text-brand-dark";
            btnProf.className = "flex-1 py-2 text-xs font-semibold rounded-lg text-slate-500 hover:text-brand-dark transition-all";
            emailInput.value = "lucas.mendes@aluno.techacademy.edu";
        });

        document.getElementById('login-form').addEventListener('submit', (e) => {
            e.preventDefault();
            if (selectedRole === 'professor') {
                window.location.href = 'dashboard-professor.html';
            } else {
                window.location.href = 'dashboard-aluno.html';
            }
        });

        document.getElementById('forgot-password').addEventListener('click', (e) => {
            e.preventDefault();
            alert('A funcionalidade de recuperação de senha foi simulada com sucesso!');
        });

        // Set default value
        emailInput.value = "carlos.silva@techacademy.edu";
