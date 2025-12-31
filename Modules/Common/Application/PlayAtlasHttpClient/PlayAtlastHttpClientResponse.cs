using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
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

        public PlayAtlastHttpClientResponse(bool success, string reason, ReasonCode reasonCode)
        {
            if (!success && string.IsNullOrEmpty(reason))
                throw new ArgumentException(
                    "Reason cannot be null or empty when success is false.",
                    nameof(reason)
                );

            Success = success;
            Reason = reason;
            ReasonCode = reasonCode;
        }
    }
}
