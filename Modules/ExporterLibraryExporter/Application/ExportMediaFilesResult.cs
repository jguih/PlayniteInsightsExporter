using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterLibraryExporter.Application
{
    public enum ExportMediaFilesResultReasonCode
    {
        OneOrMoreFailed,
        Success,
        OperationCanceledByUser,
    }

    public class ExportMediaFilesResult
    {
        public readonly string Reason;
        public readonly ExportMediaFilesResultReasonCode ReasonCode;
        public readonly bool OperationSuccess;
        public readonly int Skipped;
        public readonly int Success;
        public readonly int Failed;

        public ExportMediaFilesResult(
            string reason, 
            ExportMediaFilesResultReasonCode reasonCode, 
            bool operationSuccess, 
            int skipped, 
            int success, 
            int failed
        )
        {
            Reason = reason;
            ReasonCode = reasonCode;
            OperationSuccess = operationSuccess;
            Skipped = skipped;
            Success = success;
            Failed = failed;
        }
    }
}
