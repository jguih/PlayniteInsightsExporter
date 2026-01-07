using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using ExporterPlayAtlasClient.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public class PlayAtlasClientModule : IPlayAtlasClientModulePort
    {
        public IPlayAtlasHttpClientPort Client { get; }
        public IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }
        public IPlayAtlasEventStreamPort EventStream { get; }

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
            var requestSigner = new HttpRequestSigner(
                    pluginContext,
                    systemConfig,
                    signatureService
                );
            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            Client = new PlayAtlasHttpClient(
                appLogger,
                hashService,
                syncMediaFilesHttpContentBuilder,
                syncGamesDtoMapper,
                jsonHttpContentBuilder,
                requestSigner,
                httpClient
            );

            EventStream = new PlayAtlasEventStream(
                requestSigner,
                appLogger,
                httpClient
            );

            RegisterExtensionCommandHandler = new RegisterExtensionCommandHandler(
                pluginContext,
                keyManager,
                playAtlasHttpClient: Client
            );
        }
    }
}
