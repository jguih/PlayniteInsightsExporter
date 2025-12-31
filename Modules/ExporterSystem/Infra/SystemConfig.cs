using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{
    public class SystemConfig : ISystemConfigPort
    {
        private string libraryFilesDirPath;

        public string LibraryFilesDirPath
        {
            get
            {
                return libraryFilesDirPath;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(LibraryFilesDirPath));
                libraryFilesDirPath = value;
            }
        }

        public SystemConfig() { }
    }
}
