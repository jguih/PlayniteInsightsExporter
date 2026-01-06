using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using ExporterPlayAtlasClient.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public class PlayAtlasClientModule : IPlayAtlasClientModulePort
    {
        public IPlayAtlasHttpClientPort Client { get; }
        public IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }

        public PlayAtlasClientModule(
            IFileSystemServicePort fileSystem,
            IExporterPluginContextPort pluginContext,
            IAppLoggerPort appLogger,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService,
            IHashServicePort hashService,
            IKeyManagerPort keyManager
        )
        {
            var syncMediaFilesHttpContentBuilder = new SyncMediaFilesHttpContentBuilder(fileSystem);
            var syncGamesDtoMapper = new SyncGamesDtoMapper();
            var jsonHttpContentBuilder = new JsonHttpContentBuilder();

            Client = new PlayAtlasHttpClient(
                appLogger: appLogger,
                pluginContext: pluginContext,
                systemConfig: systemConfig,
                signatureService: signatureService,
                hashService: hashService,
                syncMediaFilesHttpContentBuilder: syncMediaFilesHttpContentBuilder,
                syncGamesDtoMapper: syncGamesDtoMapper,
                jsonHttpContentBuilder: jsonHttpContentBuilder
            );

            RegisterExtensionCommandHandler = new RegisterExtensionCommandHandler(
                pluginContext: pluginContext,
                keyManager: keyManager,
                playAtlasHttpClient: Client
            );
        }
    }
}
