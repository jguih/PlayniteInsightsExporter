using ExporterBootstrap.Application;
using Playnite.SDK;
using PlayniteInsightsExporter.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteInsightsExporter.Src
{
    public class RegisterExtensionWorkflow
    {
        private readonly ExporterApi ExporterApi;
        private readonly IPlayniteAPI PlayniteApi;

        public RegisterExtensionWorkflow(
            ExporterApi exporterApi, 
            IPlayniteAPI playniteApi
        )
        {
            ExporterApi = exporterApi;
            PlayniteApi = playniteApi;
        }

        public GlobalProgressResult Register()
        {
            GlobalProgressOptions progressOptions = new GlobalProgressOptions(null)
            {
                Cancelable = true,
                IsIndeterminate = true,
                Text = "Registering extension..."
            };

            return PlayniteApi.Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    await ExporterApi.PlayAtlasClient
                        .Command
                        .RegisterExtensionCommandHandler
                        .ExecuteAsync(progress.CancelToken);
                }, progressOptions);
        }

        public OperationOutcome InterpretRegisterResult(GlobalProgressResult result)
        {
            var title = "Register Extension";

            if (result.Canceled)
            {
                return new OperationOutcome(
                        false,
                        OutcomeSeverity.Warning,
                        "Extension registration was canceled",
                        title
                    );
            }

            if (result.Error != null)
            {
                return new OperationOutcome(
                        false,
                        OutcomeSeverity.Error,
                        $"Failed to register extension: {result.Error.Message}",
                        title
                    );
            }

            return new OperationOutcome(
                        true,
                        OutcomeSeverity.Success,
                        "Extension registered successfully",
                        title
                    );
        }
    }
}
