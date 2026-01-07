using ExporterBootstrap.Application;
using Infra;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteInsightsExporter.Notifications;
using PlayniteInsightsExporter.Src;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace PlayniteInsightsExporter
{
    public class PlayniteInsightsExporterSettings : ObservableObject
    {
        private string webAppURL = string.Empty;
        private bool enableLibrarySyncOnUpdate = true;
        private bool enableMediaFilesSyncOnUpdate = true;
        private string shareXExePath = string.Empty;

        public string WebAppURL { get => webAppURL; set => SetValue(ref webAppURL, value); }
        public bool EnableLibrarySyncOnUpdate
        {
            get => enableLibrarySyncOnUpdate;
            set => SetValue(ref enableLibrarySyncOnUpdate, value);
        }
        public bool EnableMediaFilesSyncOnUpdate
        {
            get => enableMediaFilesSyncOnUpdate;
            set => SetValue(ref enableMediaFilesSyncOnUpdate, value);
        }
        public string ShareXExePath
        {
            get => shareXExePath;
            set => SetValue(ref shareXExePath, value);
        }

        [DontSerialize]
        public RelayCommand ExportLibraryButton { get; set; }
        [DontSerialize]
        public RelayCommand BrowseShareXPath { get; set; }
        [DontSerialize]
        public RelayCommand RegisterExtensionButton { get; set; }
    }

    public class PlayniteInsightsExporterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteInsightsExporter plugin;
        private readonly IPlayniteAPI playniteApi;
        private PlayniteInsightsExporterSettings editingClone { get; set; }
        private PlayniteInsightsExporterSettings settings;

        private readonly ExporterApi exporterApi;
        private readonly SyncGameLibraryWorkflow syncGameLibrary;
        private readonly ISyncFeedbackChannelPort dialogFeedbackChannel;
        private readonly RegisterExtensionWorkflow registerExtension;

        public PlayniteInsightsExporterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                settings.ExportLibraryButton = new RelayCommand(() => OnExportLibrary());
                settings.BrowseShareXPath = new RelayCommand(() => OnBrowseShareXPath());
                settings.RegisterExtensionButton = new RelayCommand(() => OnRegisterExtension());
                OnPropertyChanged();
            }
        }

        public PlayniteInsightsExporterSettingsViewModel(
            PlayniteInsightsExporter plugin,
            ExporterApi exporterApi,
            SyncGameLibraryWorkflow syncGameLibraryWorkflow,
            ISyncFeedbackChannelPort dialogFeedbackChannel
        )
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            this.playniteApi = plugin.PlayniteApi;
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<PlayniteInsightsExporterSettings>();
            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new PlayniteInsightsExporterSettings();
            }

            this.exporterApi = exporterApi;
            syncGameLibrary = syncGameLibraryWorkflow;
            this.dialogFeedbackChannel = dialogFeedbackChannel;
            registerExtension = new RegisterExtensionWorkflow(exporterApi, playniteApi);
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

        public void OnExportLibrary()
        {
            var syncGamesProgressResult = syncGameLibrary.SyncGames();
            var syncGamesOutcome = syncGameLibrary.InterpretSyncGamesResult(syncGamesProgressResult);

            if (!syncGamesOutcome.Success)
            {
                plugin.PresentOperationOutcome(syncGamesOutcome, dialogFeedbackChannel);
                return;
            }

            var syncMediaFilesResult = syncGameLibrary.SyncMediaFiles();
            var syncMediaFilesOutcome = syncGameLibrary.InterpretSyncMediaFilesResult(syncMediaFilesResult);

            if (!syncMediaFilesOutcome.Success)
            {
                plugin.PresentOperationOutcome(syncMediaFilesOutcome, dialogFeedbackChannel);
                return;
            }

            var loc_successSyncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
            plugin.PresentOperationOutcome(
                new OperationOutcome(
                    true,
                    OutcomeSeverity.Success,
                    loc_successSyncClientServer,
                    "Library Sync"
                ), dialogFeedbackChannel);
        }

        public void OnBrowseShareXPath()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select ShareX Executable"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                Settings.ShareXExePath = dialog.FileName;
            }
        }

        public void OnRegisterExtension()
        {
            var result = registerExtension.Register();
            var outcome = registerExtension.InterpretRegisterResult(result);
            plugin.PresentOperationOutcome(outcome, dialogFeedbackChannel);
        }
    }
}