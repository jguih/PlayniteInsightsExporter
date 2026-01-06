using Bogus;
using Bogus.DataSets;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterCommon.Testing;
using ExporterLibraryExporter.Application;
using ExporterSystem.Infra;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Builders;

internal class LibraryExporterTestBuilder
{
    private Mock<IAppLoggerPort> AppLogger = new();
    private Mock<IFileSystemServicePort> FileSystem = new();
    private ISystemConfigPort SystemConfig;

    public Mock<IHashServicePort> HashService { get; } = new();
    public Mock<IPlayniteGameRepositoryPort> GameRepository { get; } = new();
    public Mock<IPlayAtlasHttpClientPort> PlayAtlasHttpClient { get; } = new();
    public Faker Faker { get; } = new();
    public GameFactory GameFactory { get; } = new();

    public LibraryExporterTestBuilder()
    {
        var dataDir = Faker.System.DirectoryPath();
        var configDir = Faker.System.DirectoryPath();

        HashService
            .Setup(hs => hs.ComputeSHA256Base64FromFolderContents(It.IsAny<string>()))
            .Returns(Faker.Random.Hash());

        FileSystem
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));
        FileSystem
            .Setup(fs => fs.DirectoryExists(dataDir))
            .Returns(true);
        FileSystem
            .Setup(fs => fs.DirectoryExists(configDir))
            .Returns(true);

        SystemConfig = new SystemConfig(
            fileSystemService: FileSystem.Object,
            configDirPath: dataDir,
            dataDirPath: configDir
        );
    }

    public LibrarySyncService Build()
    {
        return new LibrarySyncService(
                AppLogger.Object,
                PlayAtlasHttpClient.Object,
                HashService.Object,
                FileSystem.Object,
                SystemConfig,
                GameRepository.Object
            );
    }
}
