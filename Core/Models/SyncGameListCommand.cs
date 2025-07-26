using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class SyncGameListCommand
    {
        public List<PlayniteGameDTO> AddedItems { get; set; } = new List<PlayniteGameDTO>();
        public List<string> RemovedItems { get; set; } = new List<string>();
        public List<PlayniteGameDTO> UpdatedItems { get; set; } = new List<PlayniteGameDTO>();


        public SyncGameListCommand() { }
        public SyncGameListCommand(
            List<PlayniteGameDTO> AddedItems,
            List<string> RemovedItems,
            List<PlayniteGameDTO> UpdatedItems) 
        {
            this.AddedItems = AddedItems ?? new List<PlayniteGameDTO>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<PlayniteGameDTO>();
        }
    }
}
