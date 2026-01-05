using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Notifications
{
    public enum SyncSeverity
    {
        Error,
        Success,
        Warning
    }

    public class SyncOutcome
    {
        public bool Success { get; set; }
        public SyncSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }

        public SyncOutcome() { }

        public SyncOutcome(bool success, SyncSeverity severity, string message, string title)
        {
            Success = success;
            Severity = severity;
            Message = message;
            Title = title;
        }
    }
}
