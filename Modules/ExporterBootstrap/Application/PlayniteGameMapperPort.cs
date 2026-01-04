using ExporterCommon.Domain;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterBootstrap.Application
{
    public interface IPlayniteGameMapperPort
    {
        AppGame Map(Game game);
    }
}
