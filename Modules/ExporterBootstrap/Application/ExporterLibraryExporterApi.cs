using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterLibraryExporterApi
    {
        public readonly ILibraryExporterServicePort LibraryExporterService;

        public ExporterLibraryExporterApi(
            IAppLoggerPort appLogger,
            IPlayAtlasHttpClientPort playAtlasHttpClient,
            IHashServicePort hashService,
            IFileSystemServicePort fileSystemService,
            ISystemConfigPort systemConfig
        )
        {
            LibraryExporterService = new LibraryExporterService(
                 appLogger,
                 playAtlasHttpClient,
                 hashService,
                 fileSystemService,
                 systemConfig
            );
        }
    }
}
