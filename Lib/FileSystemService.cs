using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib
{
    public interface IFileSystemService
    {
        /// <summary>
        /// Mimics <see cref="System.IO.File.Exists(string)"/>
        /// </summary>
        bool FileExists(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.File.WriteAllText(string, string)"/>
        /// </summary>
        void FileWriteAllText(string path, string contents);
        /// <summary>
        /// Mimics <see cref="System.IO.File.ReadAllText(string)"/>
        /// </summary>
        string FileReadAllText(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.File.Delete(string)"/>
        /// </summary>
        void FileDelete(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.File.GetCreationTimeUtc(string)"/>
        /// </summary>
        DateTime FileGetCreationTimeUtc(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.GetFiles(string, string)"/>
        /// </summary>
        string[] DirectoryGetFiles(string path, string searchPattern);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.Exists(string)"/>
        /// </summary>
        bool DirectoryExists(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.CreateDirectory(string)"/>
        /// </summary>
        void DirectoryCreate(string path);
    }
    public class FileSystemService : IFileSystemService
    {

        public string[] DirectoryGetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        public void FileDelete(string path)
        {
            File.Delete(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public DateTime FileGetCreationTimeUtc(string path)
        {
            return File.GetCreationTimeUtc(path);
        }

        public string FileReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void FileWriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public void DirectoryCreate(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
    }
}
