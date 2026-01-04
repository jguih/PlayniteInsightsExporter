using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface ICloseGameSessionHttpContentBuilderPort : IHttpContentBuilderPort<CloseGameSessionRequestDto> { }

    public class CloseGameSessionHttpContentBuilder 
        : BaseHttpContentBuilder<CloseGameSessionRequestDto>, 
        ICloseGameSessionHttpContentBuilderPort
    {
    }
}
