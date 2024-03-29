// <copyright file="Mod.cs" company="ST-Apps (S. Tenuta)">
// Copyright (c) ST-Apps (S. Tenuta). All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ExtendedRoadUpgrades
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Colossal.UI;
    using ExtendedRoadUpgrades.Code;
    using ExtendedRoadUpgrades.Patches;
    using ExtendedRoadUpgrades.Systems;
    using ExtendedRoadUpgrades.Utils;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;

    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public sealed class Mod : IMod
    {
        /// <summary>
        /// The mod's default name.
        /// </summary>
        public const string ModName = "Extended Road Upgrades";

        /// <summary>
        /// Id used for the coui:// protocol.
        /// </summary>
        public const string ModIconsId = "eru";

        // Mod Harmony id.
        private readonly string s_modHarmonyId = $"STApps_{nameof(ExtendedRoadUpgrades)}";

        // Mod assembly path cache.
        private string s_assemblyPath = null;

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        public static Mod Instance { get; private set; }

        /// <summary>
        /// Gets the mod directory file path of the currently executing mod assembly.
        /// </summary>
        public string AssemblyPath
        {
            get
            {
                // Update cached path if the existing one is invalid.
                if (string.IsNullOrWhiteSpace(s_assemblyPath))
                {
                    // No path cached - find current executable asset.
                    string assemblyName = Assembly.GetExecutingAssembly().FullName;
                    ExecutableAsset modAsset = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == assemblyName));
                    if (modAsset is null)
                    {
                        Log.Error("Mod executable asset not found");
                        return null;
                    }

                    // Update cached path.
                    s_assemblyPath = Path.GetDirectoryName(modAsset.GetMeta().path);
                }

                // Return cached path.
                return s_assemblyPath;
            }
        }

        /// <summary>
        /// Gets the mod's active log.
        /// </summary>
        internal static ILog Log { get; private set; }

        /// <summary>
        /// Gets the mod's active settings configuration.
        /// </summary>
        internal ModSettings ActiveSettings { get; private set; }

        /// <summary>
        /// Called by the game when the mod is loaded.
        /// </summary>
        /// <param name="updateSystem">Game update system.</param>
        public void OnLoad(UpdateSystem updateSystem)
        {
            // Set instance reference.
            Instance = this;

            // Initialize logger.
            Log = LogManager.GetLogger(ModName);
#if DEBUG
            Log.Info("Setting logging level to Debug");
            Log.effectivenessLevel = Level.Debug;
#endif
            Log.Info($"Loading {ModName} version {Assembly.GetExecutingAssembly().GetName().Version}");

            // Add mod UI resource directory to UI resource handler.
            UIManager.defaultUISystem.AddHostLocation("uil",  AssemblyPath + "/Icons/");

            // Apply harmony patches.
            new Patcher($"ST-Apps_{nameof(ExtendedRoadUpgrades)}", Log);

            // Don't do anything if Harmony patches weren't applied.
            if (Patcher.Instance is null || !Patcher.Instance.PatchesApplied)
            {
                Log.Critical("Harmony patches not applied; aborting system activation");
                return;
            }

            // Create Settings because is needed by Localization but don't register it in the UI.
            ActiveSettings = new(this);

            // Setup UpdateSystem.
#if DEBUG
            updateSystem.World.GetOrCreateSystem<PickerToolSystem>();
            updateSystem.UpdateAt<PickerToolSystem>(SystemUpdatePhase.ToolUpdate);
            Log.Info($"Created debug tool system");
#endif
            updateSystem.World.GetOrCreateSystem<NodeUpdateFixerSystem>();
            updateSystem.UpdateAt<NodeUpdateFixerSystem>(SystemUpdatePhase.ToolUpdate);
            Log.Info($"Created retaining walls fixer system");

            // Load translations.
            Localization.LoadTranslations(ActiveSettings, Log);

            // Install mod.
            UpgradesManager.Install();

            // Add mod UI resource directory to UI resource handler.
            UIManager.defaultUISystem.AddHostLocation(ModIconsId, AssemblyPath + "/Icons/");
        }

        private void Instance_onGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            Log.Debug("Starting Instance_onGamePreload");

            UpgradesManager.Install();
        }

        /// <summary>
        /// Called by the game when the mod is disposed of.
        /// </summary>
        public void OnDispose()
        {
            Log.Info("Disposing");
            Instance = null;

            // Revert harmony patches.
            Patcher.Instance?.UnPatchAll();
        }
    }
}
