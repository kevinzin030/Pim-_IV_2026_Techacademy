import { Router } from "express";

import { autenticarToken } from "../Middlewares/autenticarToken.js";
import { conectarBanco } from "../Config/database.js";

import {
    realizarLogin,
    alterarSenha,
    consultarPerfil
} from "../Controllers/AuthController.js";

import {
    listarDisciplinas
} from "../Controllers/DisciplinaController.js";

import {
    listarTurmas
} from "../Controllers/TurmaController.js";

import {
    consultarMinhasNotas,
    consultarMinhasFrequencias,
    consultarMinhasDisciplinas,
    consultarMinhaTurma
} from "../Controllers/AlunoController.js";

import {
    consultarMinhasTurmas,
    consultarMeusAlunos,
    consultarNotasDosAlunos,
    consultarFrequenciasDosAlunos,
    lancarNota
} from "../Controllers/ProfessorController.js";

import {
    criarQuiz,
    listarQuizzesProfessor,
    listarQuizzesAluno,
    consultarQuizAluno,
    responderQuiz,
    consultarMeuXP
} from "../Controllers/QuizController.js";

import {
    cadastrarAula,
    listarAulasProfessor,
    listarAulasAluno,
    receberMaterial,
    publicarMaterial,
    listarMateriaisProfessor,
    listarMateriaisAluno,
    baixarMaterialProfessor,
    baixarMaterialAluno
} from "../Controllers/AulaController.js";

import {
    consultarDesempenhoQuizzes
} from "../Controllers/DesempenhoController.js";

import {
    lancarFrequencia
} from "../Controllers/FrequenciaController.js";

import {
    listarNotificacoes,
    marcarNotificacaoComoLida,
    marcarTodasComoLidas
} from "../Controllers/NotificacaoController.js";

// Inicializa o roteador principal da API TechAcademy.
const router = Router();

// Verifica se a API está funcionando.
router.get("/health", (req, res) => {
    return res.json({
        sistema: "TechAcademy",
        status: "online",
        mensagem: "API funcionando corretamente"
    });
});

// Verifica a conexão com o banco de dados Azure SQL.
router.get("/status-banco", async (req, res) => {
    try {
        const conexao = await conectarBanco();

        const resultado = await conexao.request().query(`
            SELECT
                DB_NAME() AS banco,
                COUNT(*) AS totalTabelas
            FROM sys.tables
            WHERE is_ms_shipped = 0
        `);

        return res.json({
            sistema: "TechAcademy",
            status: "conectado",
            ...resultado.recordset[0]
        });

    } catch (erro) {
        console.error(
            "Erro ao consultar o banco:",
            erro.message
        );

        return res.status(500).json({
            status: "erro",
            mensagem:
                "Não foi possível consultar o banco de dados."
        });
    }
});

// Autentica usuários utilizando login e senha.
router.post(
    "/auth/login",
    realizarLogin
);

// Permite alterar a senha de um usuário autenticado.
router.post(
    "/auth/alterar-senha",
    autenticarToken,
    alterarSenha
);

// Retorna os dados do usuário autenticado por JWT.
router.get("/auth/me", autenticarToken, (req, res) => {
    return res.status(200).json({
        mensagem: "Token validado com sucesso.",
        usuario: req.usuario
    });
});

// Consulta os dados cadastrais atualizados no Azure SQL.
// A operação é exclusivamente de leitura.
router.get(
    "/auth/perfil",
    autenticarToken,
    consultarPerfil
);

// Consulta o cadastro geral de disciplinas.
router.get(
    "/disciplinas",
    autenticarToken,
    listarDisciplinas
);

// Consulta o cadastro geral de turmas.
router.get(
    "/turmas",
    autenticarToken,
    listarTurmas
);

// Consulta as notas do aluno autenticado.
router.get(
    "/alunos/me/notas",
    autenticarToken,
    consultarMinhasNotas
);

// Consulta as frequências do aluno autenticado.
router.get(
    "/alunos/me/frequencias",
    autenticarToken,
    consultarMinhasFrequencias
);

// Consulta as disciplinas vinculadas ao aluno.
router.get(
    "/alunos/me/disciplinas",
    autenticarToken,
    consultarMinhasDisciplinas
);

// Consulta as turmas vinculadas ao aluno.
router.get(
    "/alunos/me/turmas",
    autenticarToken,
    consultarMinhaTurma
);

// Consulta as aulas disponíveis para o aluno.
router.get(
    "/alunos/me/aulas",
    autenticarToken,
    listarAulasAluno
);

// Consulta os materiais acadêmicos de uma aula.
router.get(
    "/alunos/me/aulas/:idAula/materiais",
    autenticarToken,
    listarMateriaisAluno
);

// Permite ao aluno baixar um material autorizado.
router.get(
    "/alunos/me/materiais/:idMaterial/download",
    autenticarToken,
    baixarMaterialAluno
);

// Consulta os quizzes disponíveis para o aluno.
router.get(
    "/alunos/me/quizzes",
    autenticarToken,
    listarQuizzesAluno
);

// Consulta as perguntas e alternativas de um quiz.
router.get(
    "/alunos/me/quizzes/:idQuiz",
    autenticarToken,
    consultarQuizAluno
);

// Registra as respostas do aluno e calcula o XP.
router.post(
    "/alunos/me/quizzes/:idQuiz/respostas",
    autenticarToken,
    responderQuiz
);

// Consulta o XP e o nível do aluno.
router.get(
    "/alunos/me/xp",
    autenticarToken,
    consultarMeuXP
);

// Consulta as turmas e disciplinas do professor.
router.get(
    "/professores/me/turmas",
    autenticarToken,
    consultarMinhasTurmas
);

// Consulta os alunos vinculados ao professor.
router.get(
    "/professores/me/alunos",
    autenticarToken,
    consultarMeusAlunos
);

// Consulta as notas dos alunos.
router.get(
    "/professores/me/notas",
    autenticarToken,
    consultarNotasDosAlunos
);

// Registra ou atualiza as notas dos alunos.
router.post(
    "/professores/me/notas",
    autenticarToken,
    lancarNota
);

// Consulta as frequências dos alunos.
router.get(
    "/professores/me/frequencias",
    autenticarToken,
    consultarFrequenciasDosAlunos
);

// Registra ou atualiza presenças e faltas.
// Valida o vínculo acadêmico e o histórico escolar.
router.post(
    "/professores/me/frequencias",
    autenticarToken,
    lancarFrequencia
);

// Permite ao professor cadastrar uma aula.
router.post(
    "/professores/me/aulas",
    autenticarToken,
    cadastrarAula
);

// Consulta as aulas vinculadas ao professor.
router.get(
    "/professores/me/aulas",
    autenticarToken,
    listarAulasProfessor
);

// Permite publicar um material acadêmico em PDF.
router.post(
    "/professores/me/aulas/:idAula/materiais",
    autenticarToken,
    receberMaterial,
    publicarMaterial
);

// Consulta os materiais publicados em uma aula.
router.get(
    "/professores/me/aulas/:idAula/materiais",
    autenticarToken,
    listarMateriaisProfessor
);

// Permite ao professor baixar um material autorizado.
router.get(
    "/professores/me/materiais/:idMaterial/download",
    autenticarToken,
    baixarMaterialProfessor
);

// Permite ao professor cadastrar um quiz.
router.post(
    "/professores/me/quizzes",
    autenticarToken,
    criarQuiz
);

// Consulta os quizzes cadastrados pelo professor.
router.get(
    "/professores/me/quizzes",
    autenticarToken,
    listarQuizzesProfessor
);

// Consulta o desempenho individual nos quizzes.
router.get(
    "/professores/me/quizzes/desempenho",
    autenticarToken,
    consultarDesempenhoQuizzes
);

// Consulta as notificações do usuário autenticado.
router.get(
    "/notificacoes/me",
    autenticarToken,
    listarNotificacoes
);

// Marca todas as notificações do usuário como lidas.
router.patch(
    "/notificacoes/me/todas/lida",
    autenticarToken,
    marcarTodasComoLidas
);

// Marca uma notificação específica como lida.
// A operação é restrita ao destinatário autenticado.
router.patch(
    "/notificacoes/me/:idNotificacao/lida",
    autenticarToken,
    marcarNotificacaoComoLida
);

// Exporta o roteador para o servidor Express.
export default router;