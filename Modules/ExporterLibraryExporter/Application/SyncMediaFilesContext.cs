using ExporterCommon.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public class SyncMediaFilesContext
    {
        public Action<AppGame> OnBeginProcessing { get; set; }
        public Action<AppGame> OnFinishProcessing { get; set; }

        public SyncMediaFilesContext(Action<AppGame> onBeginProcessing, Action<AppGame> onFinishProcessing)
        {
            OnBeginProcessing = onBeginProcessing;
            OnFinishProcessing = onFinishProcessing;
        }

        public SyncMediaFilesContext() { }
    }
}
