﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Flow.Launcher.Plugin.WindowsSettings.Classes;
using Flow.Launcher.Plugin.WindowsSettings.Properties;

namespace Flow.Launcher.Plugin.WindowsSettings.Helper
{
    /// <summary>
    /// Helper class to easier work with translations.
    /// </summary>
    internal static class TranslationHelper
    {
        /// <summary>
        /// Translate all settings of the given list with <see cref="WindowsSetting"/>.
        /// </summary>
        /// <param name="settingsList">The list that contains <see cref="WindowsSetting"/> to translate.</param>
        internal static IEnumerable<WindowsSetting> TranslateAllSettings(in IEnumerable<WindowsSetting>? settingsList)
        {
            var translatedSettings = new List<WindowsSetting>();

            if (settingsList is null)
                return new List<WindowsSetting>();

            foreach (var settings in settingsList)
            {
                var area = Resources.ResourceManager.GetString($"Area{settings.Area}");
                var name = Resources.ResourceManager.GetString(settings.Name);
                var type = Resources.ResourceManager.GetString(settings.Type);

                if (string.IsNullOrEmpty(area))
                {
                    Log.Warn($"Resource string for [Area{settings.Area}] not found", typeof(Main));
                }

                if (string.IsNullOrEmpty(name))
                {
                    Log.Warn($"Resource string for [{settings.Name}] not found", typeof(Main));
                }

                if (string.IsNullOrEmpty(type))
                {
                    Log.Warn($"Resource string for [{settings.Name}] not found", typeof(Main));
                }



                if (!string.IsNullOrEmpty(settings.Note))
                {
                    var note = Resources.ResourceManager.GetString(settings.Note);
                    if (string.IsNullOrEmpty(note))
                    {
                        Log.Warn($"Resource string for [{settings.Note}] not found", typeof(Main));
                    }

                    settings.Note = note ?? settings.Note ?? string.Empty;
                }
                List<string>? translatedAltNames = null;
                if (settings.AltNames is not null && settings.AltNames.Any())
                {
                    translatedAltNames = new List<string>();
                    foreach (var altName in settings.AltNames)
                    {
                        if (string.IsNullOrWhiteSpace(altName))
                        {
                            continue;
                        }

                        var translatedAltName = Resources.ResourceManager.GetString(altName);
                        if (string.IsNullOrEmpty(translatedAltName))
                        {
                            Log.Warn($"Resource string for [{altName}] not found", typeof(Main));
                        }

                        translatedAltNames.Add(translatedAltName ?? altName);
                    }

                }
                var translatedSetting = settings with
                {
                    Area = area ?? settings.Area,
                    Name = name ?? settings.Name,
                    DisplayType = type ?? settings.Type,
                    AltNames = translatedAltNames
                };

                translatedSettings.Add(translatedSetting);
            }
            return translatedSettings;
        }
    }
}
