using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterGameSessions.Application
{
    public class GameSessionConfig
    {
        public string IN_PROGRESS_SUFFIX { get; } = "-in-progress";
        public string CLOSED_SUFFIX { get; } = "-closed";
        public string STALE_SUFFIX { get; } = "-stale";
        public string SESSION_FILE_EXTENSION { get; } = ".json";
        public int DELETE_FILES_OLDER_THAN_DAYS { get; set; } = 14;
        public int STALE_AFTER_HOURS { get; set; } = 48;

        public GameSessionConfig()
        {
        }
    }
}
