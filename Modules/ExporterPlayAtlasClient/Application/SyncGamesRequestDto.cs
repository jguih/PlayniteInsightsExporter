using ExporterCommon.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    [JsonObject]
    public class SyncGamesRequestDto
    {
        [JsonIgnore]
        public readonly static string ENDPOINT = "/api/extension/sync/games";

        public IEnumerable<GameDto> AddedItems { get; set; } = new List<GameDto>();
        public IEnumerable<string> RemovedItems { get; set; } = new List<string>();
        public IEnumerable<GameDto> UpdatedItems { get; set; } = new List<GameDto>();

        public SyncGamesRequestDto() { }

        public SyncGamesRequestDto(
            IReadOnlyCollection<GameDto> AddedItems,
            IReadOnlyCollection<string> RemovedItems,
            IReadOnlyCollection<GameDto> UpdatedItems)
        {
            this.AddedItems = AddedItems ?? new List<GameDto>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<GameDto>();
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
