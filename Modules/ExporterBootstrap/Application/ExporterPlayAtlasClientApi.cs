using ExporterCommon.Application;
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
            IPlayAtlasHttpClientPort playAtlasHttpClient
        )
        {
            this.PlayAtlasHttpClient = playAtlasHttpClient;
        }
    }
}
