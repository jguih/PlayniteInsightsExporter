using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Application;
using ExporterPlayAtlasClient.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterPlayAtlasClientApi
    {
        public readonly IPlayAtlasHttpClientPort PlayAtlasHttpClient;

        public ExporterPlayAtlasClientApi(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService,
            IHashServicePort hashService,
            IFileSystemServicePort fileSystemService
        )
        {
            var syncGamesHttpContentBuilder = new SyncGamesHttpContentBuilder();
            var syncMediaFilesHttpContentBuilder = new SyncMediaFilesHttpContentBuilder(fileSystemService);

            PlayAtlasHttpClient = new PlayAtlasHttpClient(
                appLogger,
                pluginContext,
                systemConfig,
                signatureService,
                hashService,
                syncGamesHttpContentBuilder,
                syncMediaFilesHttpContentBuilder
            );
        }
    }
}
