import { Router } from "express";
import { conectarBanco } from "../Config/database.js";
import { autenticarToken } from "../Middlewares/autenticarToken.js";

import {
    consultarInicializacao
} from "../Controllers/InicializacaoController.js";

import {
    realizarLogin,
    alterarSenha,
    consultarPerfil
} from "../Controllers/AuthController.js";

import {
    listarUsuarios,
    listarPerfisCadastro,
    verificarLogin,
    cadastrarUsuario,
    editarUsuario,
    alterarStatusUsuario
} from "../Controllers/UsuarioController.js";

import {
    redefinirSenha
} from "../Controllers/RedefinicaoSenhaController.js";

import {
    consultarAutorizacaoAuditoria,
    listarAuditoria
} from "../Controllers/AuditoriaController.js";

import {
    consultarMinhasPermissoes,
    listarPermissoes,
    listarPermissoesDoPerfil
} from "../Controllers/PermissaoController.js";

import {
    listarPerfis,
    atualizarPerfil
} from "../Controllers/PerfilController.js";

import {
    listarRegistrosAcademicos,
    salvarRegistroAcademico,
    alterarStatusAcademico
} from "../Controllers/GestaoAcademicaController.js";

import {
    listarOpcoesVinculo,
    listarVinculosTurma,
    vincularDisciplina,
    desvincularDisciplina
} from "../Controllers/VinculoDisciplinaController.js";

import {
    listarOpcoesAlocacao,
    listarDisciplinasAlocacao,
    consultarProfessorResponsavel,
    listarAlocacoes,
    alocarProfessor,
    substituirProfessor
} from "../Controllers/AlocacaoProfessorController.js";

import {
    listarOpcoesMatricula,
    listarMatriculas,
    matricularAluno,
    transferirMatricula,
    alterarSituacaoMatricula
} from "../Controllers/MatriculaController.js";

import {
    listarOpcoesConsultaNotas,
    consultarNotasCoordenador
} from "../Controllers/ConsultaNotasController.js";

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

const router = Router();

// Disponibilidade da API.

router.get("/health", (req, res) => {
    return res.status(200).json({
        sistema: "TechAcademy",
        status: "online",
        mensagem: "API funcionando corretamente"
    });
});

// Disponibilidade do banco de dados.

router.get("/status-banco", async (req, res) => {
    try {
        const conexao = await conectarBanco();

        const resultado = await conexao.request().query(`
            SELECT
                DB_NAME() AS banco,
                COUNT(*) AS totalTabelas
            FROM sys.tables
            WHERE is_ms_shipped = 0;
        `);

        return res.status(200).json({
            sistema: "TechAcademy",
            status: "conectado",
            ...resultado.recordset[0]
        });

    } catch (erro) {
        console.error("Erro na conexão com o banco:", erro);

        return res.status(500).json({
            sistema: "TechAcademy",
            status: "desconectado",
            mensagem: "Não foi possível conectar ao banco."
        });
    }
});

// Inicialização do sistema.

router.get(
    "/inicializacao/status",
    consultarInicializacao
);

// Autenticação e perfil.

router.post(
    "/auth/login",
    realizarLogin
);

router.post(
    "/auth/alterar-senha",
    autenticarToken,
    alterarSenha
);

router.get(
    "/auth/me",
    autenticarToken,
    (req, res) => {
        return res.status(200).json({
            mensagem: "Token validado com sucesso.",
            usuario: req.usuario
        });
    }
);

router.get(
    "/auth/perfil",
    autenticarToken,
    consultarPerfil
);

// Gerenciamento de usuários.

router.get(
    "/usuarios",
    autenticarToken,
    listarUsuarios
);

router.get(
    "/usuarios/perfis",
    autenticarToken,
    listarPerfisCadastro
);

router.get(
    "/usuarios/verificar-login",
    autenticarToken,
    verificarLogin
);

router.post(
    "/usuarios",
    autenticarToken,
    cadastrarUsuario
);

router.put(
    "/usuarios/:idUsuario",
    autenticarToken,
    editarUsuario
);

router.patch(
    "/usuarios/:idUsuario/status",
    autenticarToken,
    alterarStatusUsuario
);

router.post(
    "/usuarios/:idUsuario/redefinir-senha",
    autenticarToken,
    redefinirSenha
);

// Permissões e controle de acessos.

router.get(
    "/permissoes/me",
    autenticarToken,
    consultarMinhasPermissoes
);

router.get(
    "/permissoes",
    autenticarToken,
    listarPermissoes
);

router.get(
    "/perfis/:idPerfil/permissoes",
    autenticarToken,
    listarPermissoesDoPerfil
);

router.get(
    "/perfis",
    autenticarToken,
    listarPerfis
);

router.put(
    "/perfis/:idPerfil",
    autenticarToken,
    atualizarPerfil
);

// Auditoria administrativa.

router.get(
    "/auditoria/autorizacao",
    autenticarToken,
    consultarAutorizacaoAuditoria
);

router.get(
    "/auditoria",
    autenticarToken,
    listarAuditoria
);

// Gerenciamento acadêmico.

router.get(
    "/academico/:entidade",
    autenticarToken,
    listarRegistrosAcademicos
);

router.post(
    "/academico/:entidade",
    autenticarToken,
    salvarRegistroAcademico
);

router.put(
    "/academico/:entidade/:id",
    autenticarToken,
    salvarRegistroAcademico
);

router.patch(
    "/academico/:entidade/:id/status",
    autenticarToken,
    alterarStatusAcademico
);

// Vinculação de disciplinas.

router.get(
    "/vinculos/opcoes",
    autenticarToken,
    listarOpcoesVinculo
);

router.get(
    "/vinculos/turmas/:idTurma",
    autenticarToken,
    listarVinculosTurma
);

router.post(
    "/vinculos/turmas/:idTurma",
    autenticarToken,
    vincularDisciplina
);

router.delete(
    "/vinculos/turmas/:idTurma/:idVinculo",
    autenticarToken,
    desvincularDisciplina
);

// Alocação de professores.

router.get(
    "/alocacoes/opcoes",
    autenticarToken,
    listarOpcoesAlocacao
);

router.get(
    "/alocacoes/turmas/:idTurma/disciplinas",
    autenticarToken,
    listarDisciplinasAlocacao
);

router.get(
    "/alocacoes/vinculos/:idVinculo/responsavel",
    autenticarToken,
    consultarProfessorResponsavel
);

router.get(
    "/alocacoes",
    autenticarToken,
    listarAlocacoes
);

router.post(
    "/alocacoes",
    autenticarToken,
    alocarProfessor
);

router.put(
    "/alocacoes/vinculos/:idVinculo",
    autenticarToken,
    substituirProfessor
);

// Gerenciamento de matrículas.

router.get(
    "/matriculas/opcoes",
    autenticarToken,
    listarOpcoesMatricula
);

router.get(
    "/matriculas",
    autenticarToken,
    listarMatriculas
);

router.post(
    "/matriculas",
    autenticarToken,
    matricularAluno
);

router.put(
    "/matriculas/:idMatricula/transferencia",
    autenticarToken,
    transferirMatricula
);

router.patch(
    "/matriculas/:idMatricula/situacao",
    autenticarToken,
    alterarSituacaoMatricula
);

// Consulta de notas pelo coordenador.

router.get(
    "/coordenadores/notas/opcoes",
    autenticarToken,
    listarOpcoesConsultaNotas
);

router.get(
    "/coordenadores/notas",
    autenticarToken,
    consultarNotasCoordenador
);

// Consultas acadêmicas gerais.

router.get(
    "/disciplinas",
    autenticarToken,
    listarDisciplinas
);

router.get(
    "/turmas",
    autenticarToken,
    listarTurmas
);

// Funcionalidades do aluno.

router.get(
    "/alunos/me/notas",
    autenticarToken,
    consultarMinhasNotas
);

router.get(
    "/alunos/me/frequencias",
    autenticarToken,
    consultarMinhasFrequencias
);

router.get(
    "/alunos/me/disciplinas",
    autenticarToken,
    consultarMinhasDisciplinas
);

router.get(
    "/alunos/me/turmas",
    autenticarToken,
    consultarMinhaTurma
);

router.get(
    "/alunos/me/aulas",
    autenticarToken,
    listarAulasAluno
);

router.get(
    "/alunos/me/aulas/:idAula/materiais",
    autenticarToken,
    listarMateriaisAluno
);

router.get(
    "/alunos/me/materiais/:idMaterial/download",
    autenticarToken,
    baixarMaterialAluno
);

router.get(
    "/alunos/me/quizzes",
    autenticarToken,
    listarQuizzesAluno
);

router.get(
    "/alunos/me/quizzes/:idQuiz",
    autenticarToken,
    consultarQuizAluno
);

router.post(
    "/alunos/me/quizzes/:idQuiz/respostas",
    autenticarToken,
    responderQuiz
);

router.get(
    "/alunos/me/xp",
    autenticarToken,
    consultarMeuXP
);

// Funcionalidades do professor.

router.get(
    "/professores/me/turmas",
    autenticarToken,
    consultarMinhasTurmas
);

router.get(
    "/professores/me/alunos",
    autenticarToken,
    consultarMeusAlunos
);

router.get(
    "/professores/me/notas",
    autenticarToken,
    consultarNotasDosAlunos
);

router.post(
    "/professores/me/notas",
    autenticarToken,
    lancarNota
);

router.get(
    "/professores/me/frequencias",
    autenticarToken,
    consultarFrequenciasDosAlunos
);

router.post(
    "/professores/me/frequencias",
    autenticarToken,
    lancarFrequencia
);

router.post(
    "/professores/me/aulas",
    autenticarToken,
    cadastrarAula
);

router.get(
    "/professores/me/aulas",
    autenticarToken,
    listarAulasProfessor
);

router.post(
    "/professores/me/aulas/:idAula/materiais",
    autenticarToken,
    receberMaterial,
    publicarMaterial
);

router.get(
    "/professores/me/aulas/:idAula/materiais",
    autenticarToken,
    listarMateriaisProfessor
);

router.get(
    "/professores/me/materiais/:idMaterial/download",
    autenticarToken,
    baixarMaterialProfessor
);

router.post(
    "/professores/me/quizzes",
    autenticarToken,
    criarQuiz
);

router.get(
    "/professores/me/quizzes",
    autenticarToken,
    listarQuizzesProfessor
);

router.get(
    "/professores/me/quizzes/desempenho",
    autenticarToken,
    consultarDesempenhoQuizzes
);

// Notificações.

router.get(
    "/notificacoes/me",
    autenticarToken,
    listarNotificacoes
);

router.patch(
    "/notificacoes/me/todas/lida",
    autenticarToken,
    marcarTodasComoLidas
);

router.patch(
    "/notificacoes/me/:idNotificacao/lida",
    autenticarToken,
    marcarNotificacaoComoLida
);

export default router;