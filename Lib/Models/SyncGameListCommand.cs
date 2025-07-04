using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class SyncGameListCommand
    {
        public List<object> AddedItems { get; } = new List<object>();
        public List<string> RemovedItems { get; } = new List<string>();
        public List<object> UpdatedItems { get; } = new List<object>();

        public SyncGameListCommand(
            List<object> AddedItems,
            List<string> RemovedItems,
            List<object> UpdatedItems) 
        {
            this.AddedItems = AddedItems ?? new List<object>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<object>();
        }
    }
}
