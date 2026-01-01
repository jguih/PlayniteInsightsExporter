using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Infra
{
    public interface IHttpContentBuilderPort<in TRequest>
    {
        HttpContent Build(TRequest request);
    }
}
