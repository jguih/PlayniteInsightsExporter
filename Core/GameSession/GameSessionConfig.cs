using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class GameSessionConfig
    {
        public string IN_PROGRESS_SUFFIX { get; set; } = "-in-progress";
        public string COMPLETED_SUFFIX { get; set; } = "-completed";
        public string STALE_SUFFIX { get; set; } = "-stale";
        public string SESSION_FILE_EXTENSION { get; set; } = ".json";
        public int DELETE_FILES_OLDER_THAN_DAYS { get; set; } = 14;
        public int STALE_AFTER_HOURS { get; set; } = 48;
        public string SESSIONS_DIR_PATH { get; set; }

        public GameSessionConfig()
        {
        }
    }
}
