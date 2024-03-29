// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the Apache Licence, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

// See: https://github.com/algernon-A/FiveTwentyNineTiles/blob/master/Code/Patches/Patcher.cs
namespace ExtendedRoadUpgrades.Patches
{
    using System;
    using System.Linq;
    using Colossal.Logging;
    using HarmonyLib;

    /// <summary>
    /// A basic Harmony patching class.
    /// </summary>
    public class Patcher
    {
        private readonly string _harmonyID;

        /// <summary>
        /// Initializes a new instance of the <see cref="Patcher"/> class.
        /// Doing so applies all annotated patches.
        /// </summary>
        /// <param name="harmonyID">Harmony ID to use.</param>
        /// <param name="log">Log to use for performing patching.</param>
        public Patcher(string harmonyID, ILog log)
        {
            // Set log reference.
            Log = log;

            // Dispose of any existing instance.
            if (Instance != null)
            {
                log.Error("existing Patcher instance detected with ID " + Instance._harmonyID + "; reverting");
                Instance.UnPatchAll();
            }

            // Set instance reference.
            Instance = this;
            _harmonyID = harmonyID;

            // Apply annotated patches.
            PatchAnnotations();
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        public static Patcher Instance { get; private set; }

        /// <summary>
        /// Gets a value indicating whether patches were successfully applied.
        /// </summary>
        public bool PatchesApplied { get; private set; } = false;

        /// <summary>
        /// Gets the logger to use when patching.
        /// </summary>
        public ILog Log { get; private set; }

        /// <summary>
        /// Reverts all applied patches.
        /// </summary>
        public void UnPatchAll()
        {
            if (!string.IsNullOrEmpty(_harmonyID))
            {
                Log.Info("reverting all applied patches for " + _harmonyID);
                Harmony harmonyInstance = new(_harmonyID);

                try
                {
                    harmonyInstance.UnpatchAll("_harmonyID");

                    // Clear applied flag.
                    PatchesApplied = false;
                }
                catch (Exception e)
                {
                    Log.Critical(e, "exception reverting all applied patches for " + _harmonyID);
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// Applies Harmony patches.
        /// </summary>
        private void PatchAnnotations()
        {
            Log.Info("applying annotated Harmony patches for " + _harmonyID);
            Harmony harmonyInstance = new(_harmonyID);

            try
            {
                harmonyInstance.PatchAll();
                Log.Info("patching complete");

                // Log patched methods name for debugging purposes.
                var patchedMethods = harmonyInstance.GetPatchedMethods().ToArray();
                Log.Debug($"Patched {patchedMethods.Length} methods.");
                foreach (var patchedMethod in patchedMethods)
                {
                    Log.Debug($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
                }

                // Set applied flag.
                PatchesApplied = true;
            }
            catch (Exception e)
            {
                Log.Critical(e, "exception applying annotated Harmony patches; reverting");
                harmonyInstance.UnpatchAll(_harmonyID);
            }
        }
    }
}