using ExporterCommon.Application;
using ExporterCommon.Infra;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Net.Http;
using System.Text;

namespace ExporterSystem.Infra
{
    public class SignatureService : ISignatureServicePort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IKeyManagerPort keyManager;
        private readonly ISystemConfigPort systemConfig;

        public SignatureService(
            IAppLoggerPort appLogger,
            IKeyManagerPort keyManager,
            ISystemConfigPort systemConfig
        )
        {
            this.appLogger = appLogger;
            this.keyManager = keyManager;
            this.systemConfig = systemConfig;
        }

        public bool VerifyPlayAtlasServerSignature(byte[] data, byte[] signature, byte[] publicKeyDer)
        {
            try
            {
                AsymmetricKeyParameter pubKey = PublicKeyFactory.CreateKey(publicKeyDer);

                ISigner verifier = SignerUtilities.GetSigner("SHA256withRSA");
                verifier.Init(false, pubKey);
                verifier.BlockUpdate(data, 0, data.Length);

                return verifier.VerifySignature(signature);
            } catch (Exception ex)
            {
                appLogger.Error("Failed to verify server signature", ex);
                return false;
            }
        }

        public string Sign(byte[] data)
        {
            byte[] signature = keyManager.Sign(data);
            return Convert.ToBase64String(signature);
        }

        public byte[] BuildRequestCanonicalString(
            HttpMethod method,
            string endpoint,
            string bodyHash = null
        )
        {
            string extensionId = systemConfig.ExtensionRegistrationId;
            string methodString = method.ToString();
            string canonicalString;
            if (bodyHash != null)
            {
                canonicalString = $"{methodString}|{endpoint}|{extensionId}|{bodyHash}";
            } 
            else
            {
                canonicalString = $"{methodString}|{endpoint}|{extensionId}";
            }
            return Encoding.UTF8.GetBytes(canonicalString);
        }
    }
}
