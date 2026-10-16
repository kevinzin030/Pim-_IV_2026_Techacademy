
using System.Security.Cryptography;

namespace PIM_IV.Painel_ADM.Seguranca
{
    public static class SenhaHelper
    {
        private const int Iteracoes = 600000;
        private const int TamanhoSalt = 16;
        private const int TamanhoHash = 32;

        // Gera um hash seguro para armazenar no banco
        public static string GerarHash(string senha)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(TamanhoSalt);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                Iteracoes,
                HashAlgorithmName.SHA256,
                TamanhoHash
            );

            return $"PBKDF2-SHA256:{Iteracoes}:" +
                   $"{Convert.ToBase64String(salt)}:" +
                   $"{Convert.ToBase64String(hash)}";
        }

        // Verifica se a senha corresponde ao hash armazenado
        public static bool VerificarSenha(string senha, string senhaHash)
        {
            string[] partes = senhaHash.Split(':');

            if (partes.Length != 4 ||
                partes[0] != "PBKDF2-SHA256" ||
                !int.TryParse(partes[1], out int iteracoes) ||
                iteracoes < 600000)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(partes[2]);
                byte[] hashArmazenado = Convert.FromBase64String(partes[3]);

                if (salt.Length != TamanhoSalt ||
                    hashArmazenado.Length != TamanhoHash)
                {
                    return false;
                }

                // Calcula o hash da senha informada
                byte[] hashInformado = Rfc2898DeriveBytes.Pbkdf2(
                    senha,
                    salt,
                    iteracoes,
                    HashAlgorithmName.SHA256,
                    hashArmazenado.Length
                );

                // Compara os hashes sem depender de diferencas de tempo
                return CryptographicOperations.FixedTimeEquals(
                    hashInformado,
                    hashArmazenado
                );
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
