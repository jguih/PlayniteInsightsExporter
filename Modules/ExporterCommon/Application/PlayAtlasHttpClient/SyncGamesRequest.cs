using ExporterCommon.Domain;
using ExporterCommon.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Application
{
    public class SyncGamesRequest
    {
        public readonly static string ENDPOINT = "/api/extension/sync/games";
        public IEnumerable<AppGame> AddedItems { get; set; } = new List<AppGame>();
        public IEnumerable<string> RemovedItems { get; set; } = new List<string>();
        public IEnumerable<AppGame> UpdatedItems { get; set; } = new List<AppGame>();

        public SyncGamesRequest() { }

        public SyncGamesRequest(
            IReadOnlyCollection<AppGame> AddedItems,
            IReadOnlyCollection<string> RemovedItems,
            IReadOnlyCollection<AppGame> UpdatedItems)
        {
            this.AddedItems = AddedItems ?? new List<AppGame>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<AppGame>();
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(
                this,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
            );
        }

    }
}
