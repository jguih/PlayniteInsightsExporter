using Core;
using ExporterBootstrap.Application;
using ExporterLibraryExporter.Application;
using Infra;
using Microsoft.Win32;
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
        private string webAppURL = string.Empty;
        private bool enableLibrarySyncOnUpdate = true;
        private bool enableMediaFilesSyncOnUpdate = true;
        private string shareXExePath = string.Empty;
        private string httpServerPort = string.Empty;
        private bool httpServerStartOnStartUp = false;
        private string playAtlasServerPubKeyPath = string.Empty;

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
        public string HttpServerPort
        {
            get => httpServerPort;
            set => SetValue(ref httpServerPort, value);
        }
        public bool HttpServerStartOnStartUp
        {
            get => httpServerStartOnStartUp;
            set => SetValue(ref httpServerStartOnStartUp, value);
        }
        public string PlayAtlasServerPubKeyPath
        {
            get => playAtlasServerPubKeyPath;
            set => SetValue(ref playAtlasServerPubKeyPath, value);
        }

        [DontSerialize]
        public RelayCommand ExportLibraryButton { get; set; }
        [DontSerialize]
        public RelayCommand BrowseShareXPath { get; set; }
        [DontSerialize]
        public RelayCommand HttpServerReservePort { get; set; }
        [DontSerialize]
        public RelayCommand HttpServerStart { get; set; }
        [DontSerialize]
        public RelayCommand HttpServerStop { get; set; }
        [DontSerialize]
        public RelayCommand BrowsePlayAtlasServerPubKey { get; set; }
    }

    public class PlayniteInsightsExporterSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteInsightsExporter Plugin;
        private readonly IPlayniteAPI PlayniteApi;
        private PlayniteInsightsExporterSettings editingClone { get; set; }
        private PlayniteInsightsExporterSettings settings;
        private readonly ExporterApi ExporterApi;
        // TODO: remove
        private readonly ServiceLocator ServiceLocator;
        private string httpServerStatusText = string.Empty;
        private bool httpServerRunning = false;

        public PlayniteInsightsExporterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                settings.ExportLibraryButton = new RelayCommand(() => OnExportLibrary());
                settings.BrowseShareXPath = new RelayCommand(() => OnBrowseShareXPath());
                settings.HttpServerReservePort = new RelayCommand(() => OnHttpServerReservePort());
                settings.HttpServerStart = new RelayCommand(() => OnHttpServerStart());
                settings.HttpServerStop = new RelayCommand(() => OnHttpServerStop());
                settings.BrowsePlayAtlasServerPubKey = new RelayCommand(() => OnBrowsePlayAtlasServerPubKey());
                OnPropertyChanged();
            }
        }
        public string HttpServerStatusText
        {
            get => httpServerStatusText;
            set
            {
                httpServerStatusText = value;
                OnPropertyChanged(nameof(HttpServerStatusText));
            }
        }

        public bool HttpServerRunning
        {
            get => httpServerRunning;
            set
            {
                httpServerRunning = value;
                OnPropertyChanged(nameof(HttpServerRunning));
            }
        }

        public PlayniteInsightsExporterSettingsViewModel(
            PlayniteInsightsExporter plugin,
            ILogger Logger,
            ServiceLocator locator,
            ExporterApi exporterApi)
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

            ExporterApi = exporterApi;
            // TODO: remove
            ServiceLocator = locator;

            var httpServer = locator.HttpServer;
            var LOC_Label_HttpServer_Server_Is_Running_Status_Text = ResourceProvider.GetString("LOC_Label_HttpServer_Server_Is_Running_Status_Text");
            var LOC_Label_HttpServer_Server_Is_Not_Running_Status_Text = ResourceProvider.GetString("LOC_Label_HttpServer_Server_Is_Not_Running_Status_Text");
            HttpServerStatusText = LOC_Label_HttpServer_Server_Is_Not_Running_Status_Text;
            httpServer.OnStart(() =>
            {
                var port = Settings.HttpServerPort;
                HttpServerStatusText = LOC_Label_HttpServer_Server_Is_Running_Status_Text
                    .Replace("{{port}}", port);
                HttpServerRunning = true;
            });
            httpServer.OnStop(() =>
            {
                HttpServerStatusText = LOC_Label_HttpServer_Server_Is_Not_Running_Status_Text;
                HttpServerRunning = false;
            });
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
            var loc_failed_syncClientServer = ResourceProvider.GetString("LOC_Failed_SyncClientServer");
            var loc_success_syncClientServer = ResourceProvider.GetString("LOC_Success_SyncClientServer");
            var loc_progress_syncing_library = ResourceProvider.GetString("LOC_Loading_SyncClientServer");
            var loc_progress_syncing_media_files = ResourceProvider.GetString("LOC_Progress_SyncingMediaFiles");
            var loc_progress_syncing_games = ResourceProvider.GetString("LOC_Progress_SyncingGames");

            GlobalProgressOptions syncGamesProgressOptions = new GlobalProgressOptions(null)
            {
                Cancelable = true,
                IsIndeterminate = true,
                Text = loc_progress_syncing_library
            };

            var syncGamesProgressResult = PlayniteApi
                .Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    var diff = await ExporterApi.LibrarySync
                        .LibrarySyncService
                        .ComputeGameLibraryDiff();
                    var total = diff.Total;

                    progress.Text = loc_progress_syncing_games
                        .Replace("{{total}}", total.ToString());

                    await ExporterApi.LibrarySync
                        .LibrarySyncService
                        .SyncGamesLibraryAsync(diff, progress.CancelToken);
                }, syncGamesProgressOptions);

            if (syncGamesProgressResult.Canceled)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    "Games database sync was canceled by the user.",
                    "Sync Games"
                );
                return;
            }

            if (syncGamesProgressResult.Error != null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"Unexpected error while syncing games database:\n\n{syncGamesProgressResult.Error.Message}",
                    "Sync Games"
                );
                return;
            }

            SyncMediaFilesResult syncMediaFilesResult = null;

            var syncMediaFilesProgressResult = PlayniteApi
                .Dialogs
                .ActivateGlobalProgress(async progress =>
                {
                    var games = ExporterApi.PlayniteIntegration
                        .Query
                        .GetAllGames
                        .Execute();

                    progress.IsIndeterminate = false;
                    progress.CurrentProgressValue = 0;
                    progress.ProgressMaxValue = games.Count();

                    var context = new SyncMediaFilesContext
                    {
                        OnBeginProcessing = (game) =>
                        {
                            var progressText = loc_progress_syncing_media_files
                                .Replace("{{current}}", (progress.CurrentProgressValue + 1).ToString())
                                .Replace("{{total}}", (progress.ProgressMaxValue).ToString())
                                .Replace("{{gameName}}", game.Name);
                            progress.Text = progressText;
                        },
                        OnFinishProcessing = (game) =>
                        {
                            progress.CurrentProgressValue++;
                        }
                    };

                    syncMediaFilesResult = await ExporterApi.LibrarySync
                        .LibrarySyncService
                        .SyncMediaFilesAsync(
                            games,
                            progress.CancelToken,
                            context
                        );
                },
                new GlobalProgressOptions(loc_progress_syncing_library, true)
            );

            if (syncMediaFilesProgressResult.Canceled)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    "Media files sync was canceled by the user.",
                    "Sync Media Files"
                );
                return;
            }

            if (syncMediaFilesProgressResult.Error != null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"Unexpected error while syncing media files:\n\n{syncMediaFilesProgressResult.Error.Message}",
                    "Sync Media Files"
                );
                return;
            }

            if (syncMediaFilesResult == null)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(
                    "Sync media files was not executed.",
                    "Sync Media Files"
                );
                return;
            }

            if (!syncMediaFilesResult.OperationSuccess)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    $"Media files sync finished with errors.\n\n" +
                    $"Success: {syncMediaFilesResult.Success}\n" +
                    $"Skipped: {syncMediaFilesResult.Skipped}\n" +
                    $"Failed: {syncMediaFilesResult.Failed}",
                    "Sync Media Files"
                );
                return;
            }

            PlayniteApi.Dialogs.ShowMessage(
                $"Media files sync completed successfully.\n\n" +
                $"Success: {syncMediaFilesResult.Success}\n" +
                $"Skipped: {syncMediaFilesResult.Skipped}",
                "Sync Media Files"
            );
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

        public void OnHttpServerReservePort()
        {
            var port = Settings?.HttpServerPort;

            if (string.IsNullOrWhiteSpace(port))
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Please, choose a port to reserve");
                return;
            }

            var prefix = HttpServer.GetPrefix(port);
            var psi = new ProcessStartInfo("netsh", $"http add urlacl url={prefix} user={Environment.UserName}")
            {
                Verb = "runas", // prompts for elevation
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi)?.WaitForExit();
        }

        public void OnHttpServerStart()
        {
            if (HttpServerRunning) return;
            try
            {
                ServiceLocator.HttpServer.Start();
            }
            catch (Exception)
            {
                var LOC_Label_HttpServer_Failed_To_Start = ResourceProvider.GetString("LOC_Label_HttpServer_Failed_To_Start");
                PlayniteApi.Dialogs.ShowErrorMessage(
                        LOC_Label_HttpServer_Failed_To_Start, Plugin.Name);
            }
        }

        public void OnHttpServerStop()
        {
            if (!HttpServerRunning) return;
            try
            {
                ServiceLocator.HttpServer.Stop();
            }
            catch (Exception)
            {
                var LOC_Label_HttpServer_Failed_To_Stop = ResourceProvider.GetString("LOC_Label_HttpServer_Failed_To_Stop");
                PlayniteApi.Dialogs.ShowErrorMessage(
                    LOC_Label_HttpServer_Failed_To_Stop, Plugin.Name);
            }
        }

        public void OnBrowsePlayAtlasServerPubKey()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DER files (*.der)|*.der",
                Title = "Select PlayAtlas Server public key file"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                Settings.PlayAtlasServerPubKeyPath = dialog.FileName;
            }
        }
    }
}