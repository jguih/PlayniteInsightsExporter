using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public enum ReasonCode
    {
        Success,
        NotFound,
    }

    public class PlayAtlastHttpClientResponse
    {
        public readonly bool Success;
        public readonly string Reason;
        public readonly ReasonCode ReasonCode;

        public PlayAtlastHttpClientResponse() { }
    }
}
