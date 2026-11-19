
import {
    pbkdf2,
    randomBytes,
    timingSafeEqual
} from "node:crypto";

import { promisify } from "node:util";

// Converte a função PBKDF2 para uma versão assíncrona.
// Evita bloquear a execução da API durante o processamento das senhas.
const pbkdf2Assincrono = promisify(pbkdf2);

// Define os parâmetros de segurança compatíveis com o Desktop C#.
const ALGORITMO = "PBKDF2-SHA256";
const ITERACOES_MINIMAS = 600000;
const TAMANHO_SALT = 16;
const TAMANHO_HASH = 32;

// Define o formato Base64 aceito para os componentes do hash.
const PADRAO_BASE64 =
    /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/;

// Decodifica um valor Base64 e verifica se ele possui o tamanho esperado.
function decodificarBase64(valorBase64, tamanhoEsperado) {
    if (
        typeof valorBase64 !== "string" ||
        valorBase64.length === 0
    ) {
        return null;
    }

    // Impede o processamento de valores Base64 malformados.
    if (!PADRAO_BASE64.test(valorBase64)) {
        return null;
    }

    const bytesDecodificados = Buffer.from(
        valorBase64,
        "base64"
    );

    // Confirma o tamanho e a representação original dos bytes.
    if (
        bytesDecodificados.length !== tamanhoEsperado ||
        bytesDecodificados.toString("base64") !== valorBase64
    ) {
        return null;
    }

    return bytesDecodificados;
}

// Verifica se uma nova senha atende aos requisitos do sistema.
// Exige pelo menos oito caracteres, incluindo letras maiúsculas,
// minúsculas, números e caracteres especiais.
export function validarNovaSenha(novaSenha) {
    if (typeof novaSenha !== "string") {
        return {
            valida: false,
            mensagem: "A senha informada é inválida."
        };
    }

    if (novaSenha.length < 8) {
        return {
            valida: false,
            mensagem: "A senha deve possuir pelo menos 8 caracteres."
        };
    }

    if (!/[A-Z]/.test(novaSenha)) {
        return {
            valida: false,
            mensagem: "A senha deve conter pelo menos uma letra maiúscula."
        };
    }

    if (!/[a-z]/.test(novaSenha)) {
        return {
            valida: false,
            mensagem: "A senha deve conter pelo menos uma letra minúscula."
        };
    }

    if (!/[0-9]/.test(novaSenha)) {
        return {
            valida: false,
            mensagem: "A senha deve conter pelo menos um número."
        };
    }

    if (!/[^A-Za-z0-9]/.test(novaSenha)) {
        return {
            valida: false,
            mensagem: "A senha deve conter pelo menos um caractere especial."
        };
    }

    return {
        valida: true,
        mensagem: "A senha atende aos requisitos de segurança."
    };
}

// Gera um novo hash de senha utilizando PBKDF2-SHA256.
// O salt é gerado aleatoriamente para cada nova senha.
// O resultado utiliza o mesmo formato armazenado pelo Desktop.
export async function gerarHashSenha(novaSenha) {
    const resultadoValidacao = validarNovaSenha(novaSenha);

    // Impede a geração de hashes para senhas fora do padrão.
    if (!resultadoValidacao.valida) {
        throw new Error(resultadoValidacao.mensagem);
    }

    // Gera um salt criptograficamente seguro e exclusivo.
    const salt = randomBytes(TAMANHO_SALT);

    // Calcula o hash usando os parâmetros definidos pelo sistema.
    const hashCalculado = await pbkdf2Assincrono(
        novaSenha,
        salt,
        ITERACOES_MINIMAS,
        TAMANHO_HASH,
        "sha256"
    );

    // Constrói a representação compatível com o SenhaHelper do C#.
    const saltBase64 = salt.toString("base64");
    const hashBase64 = hashCalculado.toString("base64");

    return [
        ALGORITMO,
        ITERACOES_MINIMAS,
        saltBase64,
        hashBase64
    ].join(":");
}

// Verifica uma senha informada comparando-a com o hash armazenado.
// Preserva a compatibilidade com as senhas já existentes no Azure SQL.
export async function verificarSenha(
    senhaInformada,
    senhaArmazenada
) {
    // Rejeita entradas inválidas antes de executar operações criptográficas.
    if (
        typeof senhaInformada !== "string" ||
        senhaInformada.length === 0 ||
        typeof senhaArmazenada !== "string"
    ) {
        return false;
    }

    // Recupera os quatro componentes do hash armazenado.
    const componentes = senhaArmazenada.split(":");

    if (
        componentes.length !== 4 ||
        componentes[0] !== ALGORITMO
    ) {
        return false;
    }

    // Valida a quantidade de iterações registrada no hash.
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

    // Decodifica o salt e o hash registrados no banco.
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

    // Calcula novamente o hash com a senha informada.
    const hashCalculado = await pbkdf2Assincrono(
        senhaInformada,
        salt,
        quantidadeIteracoes,
        TAMANHO_HASH,
        "sha256"
    );

    // Compara os hashes em tempo constante.
    return timingSafeEqual(
        hashCalculado,
        hashOriginal
    );
}
