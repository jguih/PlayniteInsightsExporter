using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{
    public interface IKeyManagerPort
    {
        string GetPublicKeyAsPem();
        /// <summary>
        /// Signs a payload, returning the signature bytes.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] Sign(byte[] data);
        void WriteAssymetricKeyPair();
        bool KeyExistsAndIsValid();
    }
}
