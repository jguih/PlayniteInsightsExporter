using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Infra
{
    public interface IJsonHttpContentBuilderPort : IHttpContentBuilderPort<BaseRequestDto> { }

    public class JsonHttpContentBuilder : BaseHttpContentBuilder<BaseRequestDto>, IJsonHttpContentBuilderPort
    {
    }
}
