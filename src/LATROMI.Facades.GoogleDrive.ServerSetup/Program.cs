using System;

namespace LATROMI.Facades.GoogleDrive.ServerSetup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=============================");
            Console.WriteLine("INICIALIZANDO................");
            Console.WriteLine("=============================");

            Console.Write("Caminho do arquivo de autenticacao: ");
            string authPath = Console.ReadLine().Replace("\"", "");
            Console.WriteLine();

            try
            {
                GoogleDriveUploader uploader = new GoogleDriveUploader();

                Console.WriteLine("'{0}'... Aguandando permissao.", authPath);

                uploader.LoadCredentialFromFile(authPath);

                Console.WriteLine("'{0}'... Recurso permitido.", authPath);
            }
            catch (Exception)
            {
                Console.WriteLine("'{0}'... Recurso nao permitido. Tente novamente.", authPath);
            }

            Console.WriteLine();
            Console.Write("[PRESSIONE QUALQUER TECLA PARA FINALIZAR]");
            Console.ReadLine();
        }
    }
}
