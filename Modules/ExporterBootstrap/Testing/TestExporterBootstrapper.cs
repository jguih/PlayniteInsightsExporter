using ExporterBootstrap.Application;
using ExporterCommon.Application;
using ExporterCommon.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Testing
{
    public sealed class TestEnvironment
    {
        public IFileSystemServicePort FileSystem { get; set; }
        public IPlayniteGameRepositoryPort GameRepository { get; set; }
        public IAppLoggerPort Logger { get; set; }
        public string ConfigDir { get; set; }
        public string DataDir { get; set; }
    }

    public class TestExporterBootstrapper
    {
        public ExporterApi Bootstrap(
            TestEnvironment env
        )
        {
            // fake filesystem (temp dir)
            // fake Playnite API
            // fake HTTP (in-memory or test server)
            // test config paths
        }
    }

}
