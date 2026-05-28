using Google.Apis.Auth.OAuth2;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LATROMI.Facades.GeminiAPI
{
    public class GeminiAPITokenGenerator
    {
        private readonly string _jsonContent;

        // Escopo para Google Cloud / Gemini
        private readonly string[] _scopes = { "https://www.googleapis.com/auth/cloud-platform" };

        /// Inicializa o gerador de token.
        public GeminiAPITokenGenerator(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                throw new ArgumentNullException(nameof(jsonContent));

            _jsonContent = jsonContent;
        }

        public GeminiAPITokenGenerator(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (!File.Exists(file.FullName))
                throw new FileNotFoundException(nameof(file));

            _jsonContent = File.ReadAllText(file.FullName);
        }

        /// Gera o Access Token de forma assíncrona 
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Cria a credencial diretamente a partir da string JSON
                var credential = GoogleCredential.FromJson(_jsonContent)
                                                 .CreateScoped(_scopes);

                // Solicita o token ao Google
                var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

                if (string.IsNullOrEmpty(token))
                    throw new Exception("A API do Google retornou um token vazio.");

                return token;
            }
            catch (Exception ex)
            {
                // Encapsula o erro para facilitar o debug no Back-end
                throw new Exception("Erro ao gerar Access Token Google via JSON: " + ex.Message, ex);
            }
        }

        /// Gera o Access Token de forma síncrona. 
        public string GetAccessTokenSync()
        {
            return Task.Run(async () => await GetAccessTokenAsync()).GetAwaiter().GetResult();
        }
    }
}