using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterGameSessions.Application;
using ExporterGameSessions.Infra;
using ExporterSystem.Infra;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Lib.Builders;

internal sealed class GameSessionServiceTestBuilder
{
    public Mock<IAppLoggerPort> AppLogger { get; } = new();
    public Mock<IPlayAtlasHttpClientPort> Client { get; } = new();
    public Mock<IHashServicePort> HashService { get; } = new();
    public Mock<IFileSystemServicePort> FileSystem { get; } = new();

    public IGameSessionSerializerPort Serializer { get; } =
        new GameSessionSerializer();

    public ISystemConfigPort SystemConfig { get; }

    private readonly GameSessionConfig sessionConfig = new();

    public GameSessionServiceTestBuilder()
    {
        FileSystem
            .Setup(fs => fs.PathCombine(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        FileSystem
            .Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);

        HashService
            .Setup(hs => hs.ComputeHashForGameSession(
                It.IsAny<string>(),
                It.IsAny<DateTime>()
            ))
            .Returns((string gameId, DateTime startTime) =>
                $"{gameId}-{startTime:yyyyMMddHHmmss}");

        SystemConfig = new SystemConfig(
            FileSystem.Object,
            configDirPath: "/config",
            dataDirPath: "/data"
        );
    }

    public GameSessionService Build()
    {
        return new GameSessionService(
            AppLogger.Object,
            HashService.Object,
            Client.Object,
            FileSystem.Object,
            sessionConfig,
            SystemConfig,
            Serializer
        );
    }
}
