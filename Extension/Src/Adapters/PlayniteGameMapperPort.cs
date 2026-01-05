using ExporterCommon.Domain;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Adapters
{
    public interface IPlayniteGameMapperPort
    {
        AppGame Map(Game game);
    }
}
