using ExporterCommon.Application;
using ExporterCommon.Application.PlayAtlasHttpClient;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Application.SseEventHandlers;
using ExporterPlayAtlasClient.Commands.RegisterExtension;
using ExporterPlayAtlasClient.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application.Module
{
    public class PlayAtlasClientModule : IPlayAtlasClientModulePort
    {
        public IPlayAtlasHttpClientPort Client { get; }
        public IRegisterExtensionCommandHandlerPort RegisterExtensionCommandHandler { get; }
        public IPlayAtlasEventStreamPort EventStream { get; }
        public IExtensionRegistrationFileHandlerPort ExtensionRegistrationFileHandler { get; }

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

            ExtensionRegistrationFileHandler = new ExtensionRegistrationFileHandler(
                systemConfig,
                fileSystem
            );

            var requestSigner = new HttpRequestSigner(
                    pluginContext,
                    systemConfig,
                    signatureService,
                    ExtensionRegistrationFileHandler
                );

            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            var sseHttpClient = new HttpClient()
            {
                Timeout = Timeout.InfiniteTimeSpan
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

            var handlers = new Dictionary<string, ISseEventHandlerPort>
            {
                ["take-screenshot"] = new TakeScreenshotSseHandler(appLogger),
            };
            EventStream = new PlayAtlasEventStream(
                requestSigner,
                appLogger,
                handlers,
                httpClient: sseHttpClient
            );

            RegisterExtensionCommandHandler = new RegisterExtensionCommandHandler(
                pluginContext,
                keyManager,
                playAtlasHttpClient: Client,
                ExtensionRegistrationFileHandler
            );
        }
    }
}
