using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace PodeKestrel
{
    public class PodeFormFile
    {
        public string ContentType => FormFile.ContentType;
        public string FileName => FormFile.FileName;
        public string Name => FormFile.Name;

        public byte[] Bytes
        {
            get => GetBytes();
        }

        private IFormFile FormFile;

        public PodeFormFile(IFormFile file)
        {
            FormFile = file;
        }

        private byte[] GetBytes()
        {
            var fileName = @"C:\Temp\Up\test.exe";
            FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            FormFile.CopyTo(stream);
            stream.Close();
            return new Byte[0];
            // using (var stream = new MemoryStream())
            // {
            //     stream.Position = 0;
            //     FormFile.CopyTo(stream);
            //     return stream.ToArray();
            // }
        }
    }
}