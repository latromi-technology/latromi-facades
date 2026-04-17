using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace LATROMI.Facades.GeminiAPI
{
    public class GeminiAPITokenGenerator
    {
        private readonly string _jsonKeyPath;

        // Escopo para Google Cloud / Gemini
        private readonly string[] _scopes = { "https://www.googleapis.com/auth/cloud-platform" };

        /// Inicializa o gerador de token.
        /// <param name="jsonKeyPath">Caminho físico para o arquivo .json da conta de serviço.</param>
        public GeminiAPITokenGenerator(string jsonKeyPath)
        {
            if (string.IsNullOrEmpty(jsonKeyPath))
                throw new ArgumentNullException(nameof(jsonKeyPath));

            if (!File.Exists(jsonKeyPath))
                throw new FileNotFoundException("Arquivo de chave JSON não encontrado.", jsonKeyPath);

            _jsonKeyPath = jsonKeyPath;
        }

    
        /// Gera o Access Token de forma assíncrona 
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                using (var stream = new FileStream(_jsonKeyPath, FileMode.Open, FileAccess.Read))
                {
                    // Cria a credencial usando o método ServiceAccountCredential
                    var credential = ServiceAccountCredential.FromServiceAccountData(stream)
                                        .ToGoogleCredential()
                                        .CreateScoped(_scopes);

                    // Solicita o token ao Google
                    var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                    if (string.IsNullOrEmpty(token))
                        throw new Exception("A API do Google retornou um token vazio.");

                    return token;
                }
            }
            catch (Exception ex)
            {
                // Encapsula o erro para facilitar o debug no Back-end
                throw new Exception("Erro ao gerar Access Token Google: " + ex.Message, ex);
            }
        }

        /// Gera o Access Token de forma síncrona. 
        public string GetAccessTokenSync()
        {
            return Task.Run(async () => await GetAccessTokenAsync()).GetAwaiter().GetResult();
        }
    }
}