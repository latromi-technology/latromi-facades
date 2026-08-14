using System.IO;

namespace LATROMI.Facades.FileManager
{
    internal interface IDocument
    {
        MemoryStream Merge(Stream[] files);
        void Merge(string path, bool recursive = false);
        void Merge(string path, string pathDestiny, bool recursive = false);
        bool TryMerge(string path, bool recursive = false);
        bool TryMerge(string path, string pathDestiny, bool recursive = false);
    }
}
