using System;
using System.IO;

namespace LATROMI.Facades.FileManager
{
    public class FileManager
    {
        // TODO: Change to use factory to define current file manipulation
        private IDocument _document;

        public FileManager() { }

        public void MergePDF(string directoryPath)
        {
            _document = new PDFDocument();

            _document.Merge(directoryPath, true);

            (_document as PDFDocument).Dispose();
            _document = null;
        }

        public void MergePDF(string directoryPath, string filePath)
        {
            _document = new PDFDocument(Path.GetFileName(filePath));

            _document.Merge(directoryPath, Path.GetDirectoryName(filePath), true);

            (_document as PDFDocument).Dispose();
            _document = null;
        }

        public bool TryMergePDF(string directoryPath, string filePath) 
        {
            _document = new PDFDocument(Path.GetFileName(filePath));

            string pathDestiny = Path.GetDirectoryName(filePath);
            bool result = _document.TryMerge(directoryPath, pathDestiny, true);

            (_document as PDFDocument).Dispose();
            _document = null;

            return result;
        }
    }
}
