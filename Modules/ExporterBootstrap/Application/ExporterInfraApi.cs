using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterSystem.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public class ExporterInfraApi
    {
        public readonly IFileSystemServicePort FileSystemService;
        public readonly IHashServicePort HashService;
        public readonly IKeyManagerPort KeyManager;
        public readonly ISignatureServicePort SignatureService;
        public readonly IPlayniteGameRepositoryPort PlayniteGameRepository;

        public ExporterInfraApi(
            IFileSystemServicePort fileSystemService, 
            IHashServicePort hashService, 
            IKeyManagerPort keyManager, 
            ISignatureServicePort signatureService, 
            IPlayniteGameRepositoryPort playniteGameRepository
        )
        {
            FileSystemService = fileSystemService;
            HashService = hashService;
            KeyManager = keyManager;
            SignatureService = signatureService;
            PlayniteGameRepository = playniteGameRepository;
        }
    }
}
