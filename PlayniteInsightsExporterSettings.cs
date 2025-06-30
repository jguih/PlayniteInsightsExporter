using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Lib;
using PlayniteInsightsExporter.Lib.Models;
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
        public string WebAppURL { get => webAppURL; set => SetValue(ref webAppURL, value); }

        [DontSerialize]
        public RelayCommand ExportLibraryButton { get; set; }
    }

    public class PlayniteInsightsExporterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteInsightsExporter Plugin;
        private readonly IPlayniteAPI PlayniteApi;
        private PlayniteInsightsExporterSettings editingClone { get; set; }
        private PlayniteInsightsExporterSettings settings;
        private readonly LibExporter LibExporter;
        private readonly PlayniteInsightsWebServerService WebServerService;

        public PlayniteInsightsExporterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                settings.ExportLibraryButton = new RelayCommand(async () => await OnExportLibrary());
                OnPropertyChanged();
            }
        }

        public PlayniteInsightsExporterSettingsViewModel(
            PlayniteInsightsExporter plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.Plugin = plugin;
            this.PlayniteApi = plugin.PlayniteApi;
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
            WebServerService = new PlayniteInsightsWebServerService(plugin, Settings);
            LibExporter = new LibExporter(plugin, WebServerService);
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
            Plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

        public async Task OnExportLibrary()
        {
            ValidationResult result;
            result = await LibExporter.SendLibraryJsonToWebAppAsync();
            if (!result.IsValid)
            {
                PlayniteApi
                    .Dialogs
                    .ShowErrorMessage(
                        $"Failed to send library metadata to web server. \nError: {result.Message}", 
                        Plugin.Name);
                return;
            }
            result = await LibExporter.SendLibraryFilesToWebAppAsync();
            if (!result.IsValid)
            {
                PlayniteApi
                    .Dialogs
                    .ShowErrorMessage(
                        $"Failed to send library media files to web server. \nError: {result.Message}",
                        Plugin.Name);
                return;
            }
            PlayniteApi
                .Dialogs
                .ShowMessage(
                "Library synced sucessfully!");
        }
    }
}