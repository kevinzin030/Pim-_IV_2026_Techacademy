import { gerarHashSenha, verificarSenha } from "./Src/Utils/senhaHelper.js";

// Utiliza uma senha fictícia para testar a geração do hash.
const senha = "Teste@12345";

// Gera um hash PBKDF2-SHA256 com salt aleatório.
const hash = await gerarHashSenha(senha);

// Confirma que a senha correta é aceita e a incorreta é rejeitada.
console.log("Senha correta:", await verificarSenha(senha, hash));
console.log("Senha incorreta:", await verificarSenha("Errada@123", hash));
