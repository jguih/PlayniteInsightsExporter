using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteInsightsExporter.Lib;
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
        //private string option1 = string.Empty;
        //private bool option2 = false;
        //private bool optionThatWontBeSaved = false;

        //public string Option1 { get => option1; set => SetValue(ref option1, value); }
        //public bool Option2 { get => option2; set => SetValue(ref option2, value); }
        //// Playnite serializes settings object to a JSON object and saves it as text file.
        //// If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        //[DontSerialize]
        //public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }

        [DontSerialize]
        public RelayCommand ExportLibraryButton { get; set; }
    }

    public class PlayniteInsightsExporterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteInsightsExporter Plugin;
        private readonly IPlayniteAPI PlayniteApi;
        private PlayniteInsightsExporterSettings editingClone { get; set; }
        private PlayniteInsightsExporterSettings settings;
        private LibExporter LibExporter { get; set; }

        public PlayniteInsightsExporterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                settings.ExportLibraryButton = new RelayCommand(() => OnExportLibrary());
                OnPropertyChanged();
            }
        }

        public PlayniteInsightsExporterSettingsViewModel(PlayniteInsightsExporter plugin)
        {
            this.LibExporter = new LibExporter(plugin);
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

        public void OnExportLibrary()
        {
            var result = LibExporter.ExportGamesToJson();
            var path = LibExporter.ExportedGamesFilePath;
            if (result == null)
            {
                PlayniteApi.Notifications.Add(
                    id: $"{Plugin.Name} Error",
                    text: "Failed to export game library as JSON.",
                    type: NotificationType.Error
                );
                PlayniteApi.Dialogs.ShowMessage("Failed to export game library as JSON");
                return;
            }
            PlayniteApi.Dialogs.ShowMessage($"Exported {result} games to:\n{path}");
        }
    }
}