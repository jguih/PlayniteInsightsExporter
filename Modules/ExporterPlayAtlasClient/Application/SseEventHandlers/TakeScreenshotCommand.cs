using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application.SseEventHandlers
{
    public class TakeScreenshotCommand
    {
        public string GameId { get; }

        public TakeScreenshotCommand(string gameId)
        {
            GameId = gameId;
        }
    }
}
