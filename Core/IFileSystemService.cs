using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
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
        /// Mimics <see cref="System.IO.File.OpenRead(string)"/>
        /// </summary>
        FileStream FileOpenRead(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.GetFiles(string, string)"/>
        /// </summary>
        string[] DirectoryGetFiles(string path, string searchPattern);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.GetFiles(string)"/>
        /// </summary>
        string[] DirectoryGetFiles(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.Exists(string)"/>
        /// </summary>
        bool DirectoryExists(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Directory.CreateDirectory(string)"/>
        /// </summary>
        void DirectoryCreate(string path);
        /// <summary>
        /// Mimics <see cref="System.IO.Path.Combine(string[])"/>
        /// </summary>
        string PathCombine(params string[] paths);
        /// <summary>
        /// Mimics <see cref="System.IO.Path.GetFileName(string)"/>
        /// </summary>
        string PathGetFileName(string path);
    }
}
