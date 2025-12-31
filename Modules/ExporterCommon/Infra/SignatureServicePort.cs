using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Infra
{
    public interface ISignatureServicePort
    {
        bool VerifyPlayAtlasServerSignature(byte[] data, byte[] signature, byte[] publicKeyDer);
        string Sign(byte[] data);
        byte[] BuildRequestCanonicalString(
            HttpMethod method,
            string endpoint,
            string bodyHash = null
        );
    }
}
