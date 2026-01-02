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
            ILibraryExporterServicePort libraryExporterService
        )
        {
            this.LibraryExporterService = libraryExporterService;
        }
    }
}
