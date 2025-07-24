using Core;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class PlayniteProgressService : IPlayniteProgressService
    {
        private IPlayniteAPI Api { get; }
        private IAppLogger Logger { get; }

        public PlayniteProgressService(IPlayniteAPI playniteApi, IAppLogger logger)
        {
            Api = playniteApi;
            Logger = logger;
        }

        public bool ActivateGlobalProgress(
            string message,
            bool cancelable,
            Func<GlobalProgressActionArgs, Task<bool>> action)
        {
            bool result = false;

            var progressResult = Api.Dialogs.ActivateGlobalProgress(async progress =>
            {
                progress.Text = message;
                result = await action(progress);
            }, new GlobalProgressOptions(message, cancelable));

            if (progressResult.Error != null)
            {
                result = false;
            }

            return result;
        }
    }
}
