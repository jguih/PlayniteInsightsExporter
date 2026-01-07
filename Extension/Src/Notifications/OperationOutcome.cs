using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Notifications
{
    public enum OutcomeSeverity
    {
        Error,
        Success,
        Warning
    }

    public class OperationOutcome
    {
        public bool Success { get; set; }
        public OutcomeSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }

        public OperationOutcome() { }

        public OperationOutcome(bool success, OutcomeSeverity severity, string message, string title)
        {
            Success = success;
            Severity = severity;
            Message = message;
            Title = title;
        }
    }
}
