using PdfSharp.Pdf.IO;
using System;
using System.IO;

namespace LATROMI.Facades.FileManager
{
    internal class PDFDocument : IDocument, IDisposable
    {
        private readonly string _fileNameMergedDefault;
        private const string __extension = ".pdf";

        public PDFDocument()
        {
            _fileNameMergedDefault = "ArchivesMerged.pdf";
        }

        public PDFDocument(string fileNameWhenMerged)
        {
            _fileNameMergedDefault = fileNameWhenMerged;
        }

        public MemoryStream Merge(Stream[] files)
            => MergeInternal(files);

        public void Merge(string path, bool recursive = false)
            => Merge(path, path, recursive);

        public void Merge(string path, string pathDestiny, bool recursive = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (string.IsNullOrEmpty(pathDestiny))
                pathDestiny = path;

            if (File.Exists(path))
                File.Copy(path, pathDestiny, true);

            var pdfMerged = Merge(GetDirectoryFiles(path, recursive));

            File.WriteAllBytes(Path.Combine(pathDestiny, _fileNameMergedDefault), pdfMerged.ToArray());

            pdfMerged?.Close();
        }
        public bool TryMerge(string path, bool recursive = false)
            => TryMerge(path, path, recursive);
        public bool TryMerge(string path, string pathDestiny, bool recursive = false)
        {
            try
            {
                Merge(path, pathDestiny, recursive);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private MemoryStream MergeInternal(Stream[] files) 
        {
            MemoryStream pdfResult = new MemoryStream();
            Exception safeException = null;

            using (var pdfMerged = new PdfSharp.Pdf.PdfDocument())
            {
                foreach (var file in files)
                {
                    try
                    {
                        if ((file as FileStream).Name.EndsWith(_fileNameMergedDefault, StringComparison.InvariantCulture))
                            continue;

                        using (var pdf = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                        {
                            for (int pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
                                pdfMerged.AddPage(pdf.Pages[pageIndex]);
                        }
                    }
                    catch (Exception ex)
                    {
                        safeException = ex;
                    }
                    finally
                    {
                        file?.Close();
                    }

                    if (safeException != null)
                    {
                        pdfMerged?.Close();
                        throw safeException;
                    }
                }

                pdfMerged.Save(pdfResult);
            }
            
            pdfResult.Seek(0, SeekOrigin.Begin);
            return pdfResult;
        }

        private Stream[] GetDirectoryFiles(string directoryPath, bool recursive)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException();

            var searchOpt = SearchOption.TopDirectoryOnly;

            if (recursive)
                searchOpt = SearchOption.AllDirectories;

            string[] filesPath = Directory.GetFiles(directoryPath, $"*{__extension}", searchOpt);
            Stream[] streams = new Stream[filesPath.Length];

            for (int i = 0; i < filesPath.Length; i++)
                streams[i] = new FileStream(filesPath[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return streams;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
