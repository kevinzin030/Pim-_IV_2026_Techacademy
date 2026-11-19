
import { Router } from "express";

import { autenticarToken } from "../Middlewares/autenticarToken.js";
import { conectarBanco } from "../Config/database.js";

import {
    realizarLogin,
    alterarSenha
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
    consultarFrequenciasDosAlunos
} from "../Controllers/ProfessorController.js";

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
            mensagem: "Não foi possível consultar o banco de dados."
        });
    }
});

// Autentica usuários utilizando login e senha.
router.post("/auth/login", realizarLogin);

// Permite alterar a senha de um usuário autenticado.
// A rota permanece acessível durante a alteração obrigatória.
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

// Consulta exclusivamente as notas do aluno autenticado.
router.get(
    "/alunos/me/notas",
    autenticarToken,
    consultarMinhasNotas
);

// Consulta exclusivamente as frequências do aluno autenticado.
router.get(
    "/alunos/me/frequencias",
    autenticarToken,
    consultarMinhasFrequencias
);

// Consulta as disciplinas vinculadas às matrículas do aluno.
// O identificador do aluno é obtido pelo token JWT.
router.get(
    "/alunos/me/disciplinas",
    autenticarToken,
    consultarMinhasDisciplinas
);

// Consulta as turmas vinculadas às matrículas do aluno.
// Não permite informar o identificador de outro aluno.
router.get(
    "/alunos/me/turmas",
    autenticarToken,
    consultarMinhaTurma
);

// Permite ao professor consultar suas turmas e disciplinas vinculadas.
router.get(
    "/professores/me/turmas",
    autenticarToken,
    consultarMinhasTurmas
);

// Consulta os alunos matriculados nas turmas do professor autenticado.
router.get(
    "/professores/me/alunos",
    autenticarToken,
    consultarMeusAlunos
);

// Consulta as notas dos alunos nas disciplinas do professor autenticado.
router.get(
    "/professores/me/notas",
    autenticarToken,
    consultarNotasDosAlunos
);

// Consulta as frequências dos alunos nas disciplinas do professor autenticado.
router.get(
    "/professores/me/frequencias",
    autenticarToken,
    consultarFrequenciasDosAlunos
);

// Exporta o roteador para utilização no servidor Express.
export default router;
