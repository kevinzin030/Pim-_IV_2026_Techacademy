
import { pbkdf2, timingSafeEqual } from "node:crypto";
import { promisify } from "node:util";

// Configurações de segurança utilizadas pelo sistema.
const pbkdf2Assincrono = promisify(pbkdf2);

const ALGORITMO = "PBKDF2-SHA256";
const ITERACOES_MINIMAS = 600000;
const TAMANHO_SALT = 16;
const TAMANHO_HASH = 32;

const PADRAO_BASE64 =
    /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/;

// Decodifica um valor Base64 e verifica sua integridade.
function decodificarBase64(valorBase64, tamanhoEsperado) {
    if (
        typeof valorBase64 !== "string" ||
        valorBase64.length === 0
    ) {
        return null;
    }

    // Rejeita caracteres e formatos Base64 inválidos.
    if (!PADRAO_BASE64.test(valorBase64)) {
        return null;
    }

    const bytesDecodificados = Buffer.from(
        valorBase64,
        "base64"
    );

    // Confirma o tamanho dos bytes e a codificação original.
    if (
        bytesDecodificados.length !== tamanhoEsperado ||
        bytesDecodificados.toString("base64") !== valorBase64
    ) {
        return null;
    }

    return bytesDecodificados;
}

// Verifica a senha informada utilizando PBKDF2-SHA256.
// Mantém compatibilidade com o formato de hash utilizado pelo Desktop C#.
export async function verificarSenha(
    senhaInformada,
    senhaArmazenada
) {
    // Rejeita senhas vazias e parâmetros inválidos.
    if (
        typeof senhaInformada !== "string" ||
        senhaInformada.length === 0 ||
        typeof senhaArmazenada !== "string"
    ) {
        return false;
    }

    // Separa algoritmo, iterações, salt e hash armazenado.
    const componentes = senhaArmazenada.split(":");

    if (
        componentes.length !== 4 ||
        componentes[0] !== ALGORITMO
    ) {
        return false;
    }

    // Valida a quantidade de iterações utilizada no hash.
    const textoIteracoes = componentes[1];

    if (!/^[0-9]+$/.test(textoIteracoes)) {
        return false;
    }

    const quantidadeIteracoes = Number(textoIteracoes);

    if (
        !Number.isSafeInteger(quantidadeIteracoes) ||
        quantidadeIteracoes < ITERACOES_MINIMAS ||
        quantidadeIteracoes > 2147483647
    ) {
        return false;
    }

    // Recupera o salt e o hash armazenados no banco de dados.
    const salt = decodificarBase64(
        componentes[2],
        TAMANHO_SALT
    );

    const hashOriginal = decodificarBase64(
        componentes[3],
        TAMANHO_HASH
    );

    if (!salt || !hashOriginal) {
        return false;
    }

    // Calcula o hash da senha informada usando os mesmos parâmetros do C#.
    const hashCalculado = await pbkdf2Assincrono(
        senhaInformada,
        salt,
        quantidadeIteracoes,
        TAMANHO_HASH,
        "sha256"
    );

    // Compara os hashes em tempo constante para evitar ataques por temporização.
    return timingSafeEqual(
        hashCalculado,
        hashOriginal
    );
}
