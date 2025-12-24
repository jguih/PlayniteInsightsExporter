using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryExporter.Application
{
    public enum ExportMediaFilesResultReasonCode
    {
        OneOrMoreFailed,
        Success,
        OperationCanceledByUser,
        FailedToFetchManifest
    }

    public class ExportMediaFilesResult
    {
        public string Reason { get; set; }
        public ExportMediaFilesResultReasonCode ReasonCode { get; set; }
        public bool OperationSuccess { get; set; }
        public int Skipped { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }

        public ExportMediaFilesResult() { }

    }
}
