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
        public readonly IPlayAtlasHttpClientPort playAtlasHttpClient;

        public ExporterPlayAtlasClientApi(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext,
            ExporterConfigApi configApi,
            ExporterInfraApi infraApi
        )
        {
            var syncGamesHttpContentBuilder = new SyncGamesHttpContentBuilder();
            var syncMediaFilesHttpContentBuilder = new SyncMediaFilesHttpContentBuilder(infraApi.fileSystemService);

            playAtlasHttpClient = new PlayAtlasHttpClient(
                appLogger,
                pluginContext,
                configApi.SystemConfig,
                infraApi.signatureService,
                infraApi.hashService,
                syncGamesHttpContentBuilder,
                syncMediaFilesHttpContentBuilder
            );
        }
    }
}
