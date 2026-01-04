using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface IOpenGameSessionHttpContentBuilderPort : IHttpContentBuilderPort<OpenGameSessionRequestDto> { }

    public class OpenGameSessionHttpContentBuilder : 
        BaseHttpContentBuilder<OpenGameSessionRequestDto>, 
        IOpenGameSessionHttpContentBuilderPort
    {
    }
}
