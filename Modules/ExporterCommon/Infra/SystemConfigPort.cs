using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Infra
{
    public interface ISystemConfigPort
    {
        string LibraryFilesDirPath { get; }
        string SecurityDirPath { get; }
        string SessionsDirPath { get; }
        string RegistrationIdFilePath { get; }
    }
}
