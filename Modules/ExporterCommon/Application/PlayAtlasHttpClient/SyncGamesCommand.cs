using ExporterCommon.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public sealed class SyncGameCommandItem
    {
        public AppGame Game { get; }
        public string ContentHash { get; }

        public SyncGameCommandItem(AppGame game, string contentHash)
        {
            Game = game;
            ContentHash = contentHash;
        }
    }

    public class SyncGamesCommand
    {
        public IEnumerable<SyncGameCommandItem> AddedItems { get; set; } = new List<SyncGameCommandItem>();
        public IEnumerable<string> RemovedItems { get; set; } = new List<string>();
        public IEnumerable<SyncGameCommandItem> UpdatedItems { get; set; } = new List<SyncGameCommandItem>();

        public SyncGamesCommand() { }

        public SyncGamesCommand(
            IReadOnlyCollection<SyncGameCommandItem> AddedItems,
            IReadOnlyCollection<string> RemovedItems,
            IReadOnlyCollection<SyncGameCommandItem> UpdatedItems)
        {
            this.AddedItems = AddedItems ?? new List<SyncGameCommandItem>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<SyncGameCommandItem>();
        }
    }
}
