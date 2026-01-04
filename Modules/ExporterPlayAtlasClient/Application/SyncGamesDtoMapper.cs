using ExporterCommon.Application;
using ExporterCommon.Dtos;
using ExporterPlayAtlasClient.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class SyncGamesDtoMapper : ISyncGamesDtoMapperPort
    {
        public SyncGamesRequestDto Map(SyncGamesCommand command)
        {
            List<GameDto> toAdd = new List<GameDto>();
            List<GameDto> toUpdate = new List<GameDto>();

            foreach (var item in command.AddedItems)
            {
                var dto = GameDto.FromGame(item.Game, item.ContentHash);
                toAdd.Add(dto);
            }

            foreach (var item in command.UpdatedItems)
            {
                var dto = GameDto.FromGame(item.Game, item.ContentHash);
                toUpdate.Add(dto);
            }

            SyncGamesRequestDto requestDto = new SyncGamesRequestDto
            {
                AddedItems = toAdd,
                UpdatedItems = toUpdate,
                RemovedItems = command.RemovedItems
            };

            return requestDto;
        }
    }
}
