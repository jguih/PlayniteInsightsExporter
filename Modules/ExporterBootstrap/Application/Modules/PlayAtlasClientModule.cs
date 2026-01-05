using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Infra;
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

        public PlayAtlasClientModule(
            IFileSystemServicePort fileSystem,
            IExporterPluginContextPort pluginContext,
            IAppLoggerPort appLogger,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService,
            IHashServicePort hashService
        )
        {
            var syncGamesHttpContentBuilder = new SyncGamesHttpContentBuilder();
            var syncMediaFilesHttpContentBuilder = new SyncMediaFilesHttpContentBuilder(fileSystem);
            var syncGamesDtoMapper = new SyncGamesDtoMapper();
            var openGameSessionHttpContentBuilder = new OpenGameSessionHttpContentBuilder();
            var closeGameSessionHttpContentBuilder = new CloseGameSessionHttpContentBuilder();
            var staleGameSessionHttpContentBuilder = new StaleGameSessionHttpContentBuilder();

            Client = new PlayAtlasHttpClient(
                appLogger: appLogger,
                pluginContext: pluginContext,
                systemConfig: systemConfig,
                signatureService: signatureService,
                hashService: hashService,
                syncGamesHttpContentBuilder: syncGamesHttpContentBuilder,
                syncMediaFilesHttpContentBuilder: syncMediaFilesHttpContentBuilder,
                syncGamesDtoMapper: syncGamesDtoMapper,
                openGameSessionHttpContentBuilder: openGameSessionHttpContentBuilder,
                closeGameSessionHttpContentBuilder: closeGameSessionHttpContentBuilder,
                staleGameSessionHttpContentBuilder: staleGameSessionHttpContentBuilder
            );
        }
    }
}
