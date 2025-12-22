using Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Application
{
    public class SendGamesRequest
    {
        public readonly static string ENDPOINT = "/api/extension/sync/games";
        public IReadOnlyCollection<GameDto> AddedItems { get; set; } = new List<GameDto>();
        public IReadOnlyCollection<string> RemovedItems { get; set; } = new List<string>();
        public IReadOnlyCollection<GameDto> UpdatedItems { get; set; } = new List<GameDto>();

        public SendGamesRequest() { }

        public SendGamesRequest(
            IReadOnlyCollection<GameDto> AddedItems,
            IReadOnlyCollection<string> RemovedItems,
            IReadOnlyCollection<GameDto> UpdatedItems)
        {
            this.AddedItems = AddedItems ?? new List<GameDto>();
            this.RemovedItems = RemovedItems ?? new List<string>();
            this.UpdatedItems = UpdatedItems ?? new List<GameDto>();
        }
    }
}
