using ExporterLibraryExporter.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterLibrarySyncApi
    {
        public readonly ILibrarySyncServicePort LibrarySyncService;

        public ExporterLibrarySyncApi(
            ILibrarySyncServicePort librarySyncService
        )
        {
            this.LibrarySyncService = librarySyncService;
        }
    }
}
