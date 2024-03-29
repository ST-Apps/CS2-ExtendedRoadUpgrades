// <copyright file="Localization.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

// See: https://github.com/algernon-A/FiveTwentyNineTiles/blob/master/Code/Localization.cs
namespace ExtendedRoadUpgrades.Code
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game.Modding;
    using Game.SceneFlow;

    /// <summary>
    /// Translation handling.
    /// </summary>
    public static class Localization
    {
        /// <summary>
        /// Loads settings translations from embedded .csv files.
        /// Files must be embedded under l10n in the assembly and be named for their game locale, e.g. '{Project}/l10n/en-US.csv', '{Project}/l10n/zh-HANS.csv'.
        /// Filenames not matching a supported game locale are ignored.
        ///
        /// Files can be either comma- or tab- delimeted, with two columns in each line:
        ///     - The first column contains the translation key.
        ///     - The second column contains the translation string for that key.
        ///
        /// Strings can be either quoted with quotation marks (") or non-quoted.
        ///     - Quotes MUST be used if the string contains a comma, tab, or newline (so multi-line literals are allowed within quotes), otherwise the file won't be parsed correctly.
        ///     - Use a double quotation mark ("") to insert (single) quotation marks within a string.
        ///     - Otherwise, all characters are copied literally.
        ///
        /// Packing of translation keys for options menu use is supported to reduce key length and make things more readable - see the documentation for <see cref="Localization.UnpackOptionsKey(string, ModSetting)"/>.
        /// </summary>
        /// <param name="settings">Settings file to use.</param>
        /// <param name="log">Log to use.</param>
        public static void LoadTranslations(ModSetting settings, ILog log)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            try
            {
                foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    string resourceName = $"{thisAssembly.GetName().Name}.l10n.{localeID}.csv";

                    if (resourceNames.Contains(resourceName))
                    {
                        try
                        {
                            log.Info($"Reading embedded translation file {resourceName}");

                            // Read embedded file.
                            using StreamReader reader = new(thisAssembly.GetManifestResourceStream(resourceName));
                            {
                                // Dictionary to store translations.
                                Dictionary<string, string> translations = new();

                                // Parsing fields.
                                StringBuilder builder = new();
                                string key = null;
                                bool parsingKey = true;
                                bool quoting = false;

                                // Iterate through each line of file.
                                while (true)
                                {
                                    // Read nex line of file, stopping when we've reached the end.
                                    string line = reader.ReadLine();
                                    if (line is null)
                                    {
                                        break;
                                    }

                                    // Skip empty lines.
                                    if (string.IsNullOrWhiteSpace(line) || line.Length == 0)
                                    {
                                        continue;
                                    }

                                    // Iterate through each character in line.
                                    for (int i = 0; i < line.Length; ++i)
                                    {
                                        // Local reference.
                                        char thisChar = line[i];

                                        // Are we parsing quoted text?
                                        if (quoting)
                                        {
                                            // Is this character a quote?
                                            if (thisChar == '"')
                                            {
                                                // Is this a double quote?
                                                int j = i + 1;
                                                if (j < line.Length && line[j] == '"')
                                                {
                                                    // Yes - append single quote to output and continue.
                                                    i = j;
                                                    builder.Append('"');
                                                    continue;
                                                }

                                                // It's a single quote - stop quoting here.
                                                quoting = false;

                                                // If we're parsing a value, this is also the end of parsing this line (discard everything else).
                                                if (!parsingKey)
                                                {
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                // Not a closing quote - just append character to our parsed value.
                                                builder.Append(thisChar);
                                            }
                                        }
                                        else
                                        {
                                            // Not parsing quoted text - is this a tab or comma?
                                            if (thisChar == '\t' || thisChar == ',')
                                            {
                                                // Tab or comma - if we're parsing a value, this is also the end of parsing this line (discard everything else).
                                                if (!parsingKey)
                                                {
                                                    break;
                                                }

                                                // Otherwise, what we've parsed is the key - store value and reset the builder.
                                                parsingKey = false;
                                                key = UnpackOptionsKey(builder.ToString(), settings);
                                                builder.Length = 0;
                                            }
                                            else if (thisChar == '"' & builder.Length == 0)
                                            {
                                                // If this is a quotation mark at the start of a field (immediately after comma), then we start parsing this as quoted text.
                                                quoting = true;
                                            }
                                            else
                                            {
                                                // Otherwise, just append character to our parsed string.
                                                builder.Append(thisChar);
                                            }
                                        }
                                    }

                                    // Finished looping through chars - are we still parsing quoted text?
                                    if (quoting)
                                    {
                                        // Still quoting; continue, after adding a newline.
                                        builder.AppendLine();
                                        continue;
                                    }

                                    // If we got here, then we've reached the end of the line - reset parsing status.
                                    parsingKey = true;

                                    // Was key empty?
                                    if (string.IsNullOrWhiteSpace(key))
                                    {
                                        log.Info($" - Invalid key in line {line}");
                                        continue;
                                    }

                                    // Otherwise, did we get two delimited fields (key and value?)
                                    if (builder.Length == 0)
                                    {
                                        log.Info($" - No value field found in line {line}");
                                        continue;
                                    }

                                    // Otherwise, all good.
                                    // Convert value to string and reset builder.
                                    string value = builder.ToString();
                                    builder.Length = 0;

                                    // Check for duplicates.
                                    if (!translations.ContainsKey(key))
                                    {
                                        translations.Add(key, value);
                                    }
                                    else
                                    {
                                        log.Info($" - Ignoring duplicate translation key {key} in embedded file {resourceName}");
                                    }
                                }

                                // Add translation..
                                log.Info($" - Adding translation for {localeID} with {translations.Count} entries");
                                GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(translations));
                            }
                        }
                        catch (Exception e)
                        {
                            // Don't let a single failure stop us.
                            log.Error(e, $"Exception reading localization from embedded file {resourceName}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Exception reading embedded settings localization files");
            }
        }

        /// <summary>
        /// Upacks any settings option context packed into a localization key.
        /// Packing avoids the need to put a full-length options menu translation key for each line, which gets quite long.
        /// Packing is a shortened prefix, delimited from the key value by a colon, e.g. "Options.OPTION:MyOptionsControlName".
        /// If no colon delimter is found, the key is returned unchanged.
        ///
        /// Recognised packing prefixes are:
        ///     Options.OPTION - basic options menu control labels.
        ///     Options.OPTION_DESCRIPTION - options menu detailed control descriptions, displayed in the panel to the right of the controls.
        ///     Options.WARNING - options menu warning messaged displayed in pop-up dialogs.
        /// </summary>
        /// <param name="translationKey">Translation key.</param>
        /// <param name="settings">Mod settings instance.</param>
        /// <returns>Unpacked option localization key, or unchanged original key if no packing was detected.</returns>
        private static string UnpackOptionsKey(string translationKey, ModSetting settings)
        {
            // Find any colon divider.
            int divider = translationKey.IndexOf(':');

            // If no divider, just return the original key unchanged.
            if (divider < 0)
            {
                return translationKey;
            }

            // Divider found; split key into context and raw key.
            string context = translationKey.Remove(divider);
            string key = translationKey.Substring(divider + 1);

            // Unpack key to full game settings key.
            return context switch
            {
                "Options.OPTION" => settings.GetOptionLabelLocaleID(key),
                "Options.OPTION_DESCRIPTION" => settings.GetOptionDescLocaleID(key),
                "Options.WARNING" => settings.GetOptionWarningLocaleID(key),
                _ => settings.GetSettingsLocaleID(),
            };
        }
    }
}