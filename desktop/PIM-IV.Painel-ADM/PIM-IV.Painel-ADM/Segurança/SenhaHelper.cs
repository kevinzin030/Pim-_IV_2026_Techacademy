using System;
using System.Security.Cryptography;

namespace PIM_IV.Painel_ADM.Seguranca
{
    // Centraliza a geração e a verificação segura de senhas.
    public static class SenhaHelper
    {
        private const string Algoritmo = "PBKDF2-SHA256";

        private const int Iteracoes = 600000;
        private const int TamanhoSalt = 16;
        private const int TamanhoHash = 32;

        private const char Separador = ':';

        // Gera um hash seguro para armazenamento no banco.
        public static string GerarHash(string senha)
        {
            ValidarSenhaInformada(senha);

            // Gera um salt aleatório para cada senha.
            byte[] salt = GerarSalt();

            // Deriva o hash utilizando PBKDF2-SHA256.
            byte[] hash = CalcularHash(
                senha,
                salt,
                Iteracoes);

            // Armazena algoritmo, iterações, salt e hash.
            return FormatarHash(
                Iteracoes,
                salt,
                hash);
        }

        // Verifica se a senha corresponde ao hash armazenado.
        public static bool VerificarSenha(
            string senha,
            string senhaHash)
        {
            if (string.IsNullOrEmpty(senha) ||
                string.IsNullOrWhiteSpace(senhaHash))
            {
                return false;
            }

            // Interpreta os componentes do hash armazenado.
            if (!TentarLerHash(
                senhaHash,
                out int iteracoes,
                out byte[] salt,
                out byte[] hashArmazenado))
            {
                return false;
            }

            // Calcula o hash da senha informada.
            byte[] hashInformado = CalcularHash(
                senha,
                salt,
                iteracoes);

            // Compara os hashes em tempo constante.
            return CryptographicOperations.FixedTimeEquals(
                hashInformado,
                hashArmazenado);
        }

        // Impede a geração de hash para senhas vazias.
        private static void ValidarSenhaInformada(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                throw new ArgumentException(
                    "A senha não pode estar vazia.",
                    nameof(senha));
            }
        }

        // Gera bytes aleatórios criptograficamente seguros.
        private static byte[] GerarSalt()
        {
            return RandomNumberGenerator.GetBytes(
                TamanhoSalt);
        }

        // Calcula o hash utilizando PBKDF2 com SHA-256.
        private static byte[] CalcularHash(
            string senha,
            byte[] salt,
            int iteracoes)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                TamanhoHash);
        }

        // Organiza os componentes no formato utilizado pelo sistema.
        private static string FormatarHash(
            int iteracoes,
            byte[] salt,
            byte[] hash)
        {
            return $"{Algoritmo}{Separador}" +
                   $"{iteracoes}{Separador}" +
                   $"{Convert.ToBase64String(salt)}{Separador}" +
                   $"{Convert.ToBase64String(hash)}";
        }

        // Interpreta e valida o hash recuperado do banco.
        private static bool TentarLerHash(
            string senhaHash,
            out int iteracoes,
            out byte[] salt,
            out byte[] hashArmazenado)
        {
            iteracoes = 0;
            salt = Array.Empty<byte>();
            hashArmazenado = Array.Empty<byte>();

            string[] partes = senhaHash.Split(Separador);

            // Confirma a estrutura e o algoritmo utilizado.
            if (partes.Length != 4 ||
                partes[0] != Algoritmo)
            {
                return false;
            }

            // Rejeita configurações de iteração inferiores ao mínimo.
            if (!int.TryParse(
                    partes[1],
                    out iteracoes) ||
                iteracoes < Iteracoes)
            {
                return false;
            }

            try
            {
                salt = Convert.FromBase64String(partes[2]);

                hashArmazenado =
                    Convert.FromBase64String(partes[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            // Verifica se os componentes possuem tamanhos válidos.
            if (salt.Length != TamanhoSalt ||
                hashArmazenado.Length != TamanhoHash)
            {
                return false;
            }

            return true;
        }
    }
}