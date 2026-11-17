
import { Router } from "express";

import { autenticarToken } from "../Middlewares/autenticarToken.js";
import { conectarBanco } from "../Config/database.js";

import { realizarLogin } from "../Controllers/AuthController.js";
import { listarDisciplinas } from "../Controllers/DisciplinaController.js";
import { listarTurmas } from "../Controllers/TurmaController.js";

import {
    consultarMinhasNotas,
    consultarMinhasFrequencias
} from "../Controllers/AlunoController.js";

import { consultarMinhasTurmas } from "../Controllers/ProfessorController.js";

// Inicializa o roteador principal da API.
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
        console.error("Erro ao consultar o banco:", erro.message);

        return res.status(500).json({
            status: "erro",
            mensagem: "Não foi possível consultar o banco de dados."
        });
    }
});

// Autentica usuários utilizando login e senha.
router.post("/auth/login", realizarLogin);

// Retorna os dados do usuário autenticado por JWT.
router.get("/auth/me", autenticarToken, (req, res) => {
    return res.status(200).json({
        mensagem: "Token validado com sucesso.",
        usuario: req.usuario
    });
});

// Retorna os dados do usuário autenticado por JWT.
router.get("/auth/me", autenticarToken, (req, res) => {
    return res.status(200).json({
        mensagem: "Token validado com sucesso.",
        usuario: req.usuario
    });
});

// Permite consultar disciplinas somente com autenticação.
router.get("/disciplinas", autenticarToken, listarDisciplinas);

// Permite consultar turmas mediante autenticação JWT.
router.get("/turmas", autenticarToken, listarTurmas);

// Permite ao aluno consultar exclusivamente suas frequências.
router.get(
    "/alunos/me/frequencias",
    autenticarToken,
    consultarMinhasFrequencias
);

// Permite ao professor consultar suas turmas e disciplinas vinculadas.
router.get(
    "/professores/me/turmas",
    autenticarToken,
    consultarMinhasTurmas
);

export default router;
