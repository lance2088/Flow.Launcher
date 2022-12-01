﻿using Flow.Launcher.Infrastructure;
using Flow.Launcher.Infrastructure.Logger;
using Flow.Launcher.Infrastructure.UserSettings;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Flow.Launcher.Core.ExternalPlugins.Environments
{
    public abstract class AbstractPluginEnvironment
    {
        internal abstract string Language { get; }

        internal abstract string EnvName { get; }

        internal abstract string EnvPath { get; }

        internal abstract string InstallPath { get; }

        internal abstract string ExecutablePath { get; }

        internal virtual string FileDialogFilter => string.Empty;

        internal  abstract string PluginsSettingsFilePath { get; set; }

        internal List<PluginMetadata> PluginMetadataList;

        internal PluginsSettings PluginSettings;

        private const string updatePythonIndicatorFilename = ".updatePythonPath";

        private const string updateNodeIndicatorFilename = ".updateNodePath";

        private const string appDataRegex = @"app-\d\.\d\.\d";

        internal AbstractPluginEnvironment(List<PluginMetadata> pluginMetadataList, PluginsSettings pluginSettings)
        {
            PluginMetadataList = pluginMetadataList;
            PluginSettings = pluginSettings;
        }

        internal IEnumerable<PluginPair> Setup()
        {
            if (!PluginMetadataList.Any(o => o.Language.Equals(Language, StringComparison.OrdinalIgnoreCase)))
                return new List<PluginPair>();

            // TODO: Remove. This is backwards compatibility for 1.10.0 release- changed PythonEmbeded to Environments/Python
            if (!string.IsNullOrEmpty(PluginSettings.PythonDirectory) && PluginSettings.PythonDirectory.StartsWith(Path.Combine(DataLocation.DataDirectory(), "PythonEmbeddable")))
            {
                FilesFolders.RemoveFolderIfExists(PluginSettings.PythonDirectory);
                InstallEnvironment();
                PluginSettings.PythonDirectory = string.Empty;
            }

            if (!string.IsNullOrEmpty(PluginsSettingsFilePath) && FilesFolders.FileExists(PluginsSettingsFilePath))
            {
                // Ensure latest only if user is using Flow's environment setup.
                if (PluginsSettingsFilePath.StartsWith(EnvPath, StringComparison.OrdinalIgnoreCase))
                    EnsureLatestInstalled(ExecutablePath, PluginsSettingsFilePath, EnvPath);

                return SetPathForPluginPairs(PluginsSettingsFilePath, Language);
            }

            if (MessageBox.Show($"Flow detected you have installed {Language} plugins, which " +
                                $"will require {EnvName} to run. Would you like to download {EnvName}? " +
                                Environment.NewLine + Environment.NewLine +
                                "Click no if it's already installed, " +
                                $"and you will be prompted to select the folder that contains the {EnvName} executable",
                    string.Empty, MessageBoxButtons.YesNo) == DialogResult.No)
            {
                var msg = $"Please select the {EnvName} executable";
                var selectedFile = string.Empty;

                selectedFile = GetFileFromDialog(msg, FileDialogFilter);

                if (!string.IsNullOrEmpty(selectedFile))
                    PluginsSettingsFilePath = selectedFile;

                // Nothing selected because user pressed cancel from the file dialog window
                if (string.IsNullOrEmpty(selectedFile))
                    InstallEnvironment();
            }
            else
            {
                InstallEnvironment();
            }

            if (FilesFolders.FileExists(PluginsSettingsFilePath))
            {
                return SetPathForPluginPairs(PluginsSettingsFilePath, Language);
            }
            else
            {
                MessageBox.Show(
                    $"Unable to set {Language} executable path, please try from Flow's settings (scroll down to the bottom).");
                Log.Error("PluginsLoader",
                    $"Not able to successfully set {EnvName} path, setting's plugin executable path variable is still an empty string.",
                    $"{Language}Environment");

                return new List<PluginPair>();
            }
        }

        internal abstract void InstallEnvironment();

        private void EnsureLatestInstalled(string expectedPath, string currentPath, string installedDirPath)
        {
            if (expectedPath == currentPath)
                return;

            FilesFolders.RemoveFolderIfExists(installedDirPath);

            InstallEnvironment();

        }

        internal abstract PluginPair CreatePluginPair(string filePath, PluginMetadata metadata);

        private IEnumerable<PluginPair> SetPathForPluginPairs(string filePath, string languageToSet)
        {
            var pluginPairs = new List<PluginPair>();

            foreach (var metadata in PluginMetadataList)
            {
                if (metadata.Language.Equals(languageToSet, StringComparison.OrdinalIgnoreCase))
                    pluginPairs.Add(CreatePluginPair(filePath, metadata));
            }

            return pluginPairs;
        }

        private string GetFileFromDialog(string title, string filter = "")
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Title = title,
                Filter = filter
            };

            var result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                return dlg.FileName;
            }
            else
            {
                return string.Empty;
            }
        }

        public static void IndicatePluginEnvPathsUpdate(Settings settings, string newVer)
        {
            var appVer = $"app-{newVer}";
            var updatePythonIndicatorFilePath
                = Regex.Replace(Path.Combine(DataLocation.PluginEnvironments, updatePythonIndicatorFilename), appDataRegex, appVer);
            var updateNodeIndicatorFilePath
                = Regex.Replace(Path.Combine(DataLocation.PluginEnvironments, updateNodeIndicatorFilename), appDataRegex, appVer);

            if (!string.IsNullOrEmpty(settings.PluginSettings.PythonExecutablePath)
                && settings.PluginSettings.PythonExecutablePath.StartsWith(DataLocation.PluginEnvironments))
                using (var _ = File.CreateText(updatePythonIndicatorFilePath)) { }

            if (!string.IsNullOrEmpty(settings.PluginSettings.NodeExecutablePath)
                && settings.PluginSettings.NodeExecutablePath.StartsWith(DataLocation.PluginEnvironments))
                using (var _ = File.CreateText(updateNodeIndicatorFilePath)) { }
        }

        public static void PreStartPluginFilePathCorrection(Settings settings)
        {
            PreStartCorrectionAfterUpdate(settings);
            PreStartCorrectionAfterModeChange(settings);
        }

        private static void PreStartCorrectionAfterUpdate(Settings settings)
        {
            // After updating flow, update plugin env paths.
            var appVer = $"app-{Constant.Version}";
            var updatePythonIndicatorFilePath = Path.Combine(DataLocation.PluginEnvironments, updatePythonIndicatorFilename);
            var updateNodeIndicatorFilePath = Path.Combine(DataLocation.PluginEnvironments, updateNodeIndicatorFilename);

            if (File.Exists(updatePythonIndicatorFilePath))
            {
                settings.PluginSettings.PythonExecutablePath
                    = Regex.Replace(settings.PluginSettings.PythonExecutablePath, appDataRegex, appVer);

                File.Delete(updatePythonIndicatorFilePath);
            }

            if (File.Exists(updateNodeIndicatorFilePath))
            {
                settings.PluginSettings.NodeExecutablePath
                    = Regex.Replace(settings.PluginSettings.NodeExecutablePath, appDataRegex, appVer);

                File.Delete(updateNodeIndicatorFilePath);
            }
        }

        private static void PreStartCorrectionAfterModeChange(Settings settings)
        {
            // After enabling/disabling portable mode, update plugin env paths.
            if (DataLocation.PortableDataLocationInUse())
            {
                // When user is using portable but has moved flow to a different location
                if (IsUsingPortablePath(settings.PluginSettings.PythonExecutablePath, DataLocation.PythonEnvironmentName)
                    && !settings.PluginSettings.PythonExecutablePath.StartsWith(DataLocation.PortableDataPath))
                {
                    settings.PluginSettings.PythonExecutablePath
                        = GetUpdatedPortablePath(settings.PluginSettings.PythonExecutablePath, DataLocation.PythonEnvironmentName);
                }

                if (IsUsingPortablePath(settings.PluginSettings.NodeExecutablePath, DataLocation.NodeEnvironmentName)
                    && !settings.PluginSettings.NodeExecutablePath.StartsWith(DataLocation.PortableDataPath))
                {
                    settings.PluginSettings.NodeExecutablePath
                        = GetUpdatedPortablePath(settings.PluginSettings.NodeExecutablePath, DataLocation.NodeEnvironmentName);
                }

                // When user has switched from roaming to portable
                if (IsUsingRoamingPath(settings.PluginSettings.PythonExecutablePath))
                {
                    settings.PluginSettings.PythonExecutablePath
                        = settings.PluginSettings.PythonExecutablePath.Replace(DataLocation.RoamingDataPath, DataLocation.PortableDataPath);
                }

                if (IsUsingRoamingPath(settings.PluginSettings.NodeExecutablePath))
                {
                    settings.PluginSettings.NodeExecutablePath
                        = settings.PluginSettings.NodeExecutablePath.Replace(DataLocation.RoamingDataPath, DataLocation.PortableDataPath);
                }
            }
            else
            {
                if (IsUsingPortablePath(settings.PluginSettings.PythonExecutablePath, DataLocation.PythonEnvironmentName))
                    settings.PluginSettings.PythonExecutablePath
                        = GetUpdatedPortablePath(settings.PluginSettings.PythonExecutablePath, DataLocation.PythonEnvironmentName);

                if (IsUsingPortablePath(settings.PluginSettings.NodeExecutablePath, DataLocation.NodeEnvironmentName))
                    settings.PluginSettings.NodeExecutablePath
                        = GetUpdatedPortablePath(settings.PluginSettings.NodeExecutablePath, DataLocation.NodeEnvironmentName);
            }
        }

        private static bool IsUsingPortablePath(string filePath, string pluginEnvironmentName)
        {
            var portableAppEnvLocation = $"UserData\\{DataLocation.PluginEnvironments}\\{pluginEnvironmentName}";

            return filePath.Contains(portableAppEnvLocation);
        }

        private static bool IsUsingRoamingPath(string filePath)
        {
            return filePath.StartsWith(DataLocation.RoamingDataPath);
        }

        private static string GetUpdatedPortablePath(string filePath, string pluginEnvironmentName)
        {
            var index = filePath.IndexOf(DataLocation.PluginEnvironments);
            // get the substring after "Environments" because we can not determine it
            var updatedPath = filePath.Substring(index + DataLocation.PluginEnvironments.Count());
            return $"{DataLocation.PluginEnvironmentsPath}{updatedPath}";
        }
    }
}
