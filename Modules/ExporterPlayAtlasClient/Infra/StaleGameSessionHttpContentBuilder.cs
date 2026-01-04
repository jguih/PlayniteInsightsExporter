using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface IStaleGameSessionHttpContentBuilderPort : IHttpContentBuilderPort<StaleGameSessionRequestDto> { }

    public class StaleGameSessionHttpContentBuilder
        : BaseHttpContentBuilder<StaleGameSessionRequestDto>,
        IStaleGameSessionHttpContentBuilderPort
    {
    }
}
