using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public interface IInfraModulePort
    {
        IFileSystemServicePort FileSystem { get; }
        IHashServicePort HashService { get; }
        IKeyManagerPort KeyManager { get; }
        ISignatureServicePort SignatureService { get; }
        IPlayniteGameRepositoryPort PlayniteGameRepository { get; }
        IEnvironmentInitializerPort EnvironmentInitializer { get; }
    }

}
