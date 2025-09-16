using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    internal class SignatureVerifier
    {
        public static bool Verify(byte[] data, byte[] signature, byte[] publicKeyDer)
        {
            try
            {
                AsymmetricKeyParameter pubKey = PublicKeyFactory.CreateKey(publicKeyDer);

                ISigner verifier = SignerUtilities.GetSigner("SHA256withRSA");
                verifier.Init(false, pubKey);
                verifier.BlockUpdate(data, 0, data.Length);

                return verifier.VerifySignature(signature);
            } catch (Exception)
            {
                return false;
            }
        }
    }
}
