using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface ISyncGamesHttpContentBuilderPort : IHttpContentBuilderPort<SyncGamesRequestDto> { }

    public class SyncGamesHttpContentBuilder : 
        BaseHttpContentBuilder<SyncGamesRequestDto>, 
        ISyncGamesHttpContentBuilderPort
    {
    }
}
