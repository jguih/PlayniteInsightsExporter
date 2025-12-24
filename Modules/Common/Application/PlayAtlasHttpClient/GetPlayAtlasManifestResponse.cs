using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public class GetPlayAtlasManifestResponse : PlayAtlastHttpClientResponse
    {
        public readonly PlayAtlasLibraryManifest Manifest;

        public GetPlayAtlasManifestResponse(
            bool success,
            string reason,
            ReasonCode reasonCode,
            PlayAtlasLibraryManifest manifest
        ) : base(
            success,
            reason,
            reasonCode
        )
        {
            if (success && manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            Manifest = manifest;
        }

    }
}
