using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Lib.Models
{
    public class SyncGameListCommand
    {
        public List<string> AddedItems { get; } = new List<string>();
        public List<string> RemovedItems { get; } = new List<string>();
        public List<object> GameList { get; } = new List<object>();

        public SyncGameListCommand(
            List<string> AddedItems,
            List<string> RemovedItems,
            List<object> GameList) 
        {
            this.AddedItems = AddedItems ?? new List<string>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.GameList = GameList ?? new List<object>();
        }
    }
}
