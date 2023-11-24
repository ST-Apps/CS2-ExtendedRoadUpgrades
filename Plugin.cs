using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.IO;
using System;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace ExtendedRoadUpgrades
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        // Location for our custom icons
        private const string _mediaIconsDirectory = "Cities2_Data\\StreamingAssets\\~UI~\\GameUI\\Media\\Game\\Icons";

        /// <summary>
        /// Either copies the icons to the right target path or deletes them from it.
        /// </summary>
        /// <param name="destroy"></param>
        private void HandleIcons(bool destroy = false)
        {
            var sourceDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), _mediaIconsDirectory);

            if (!destroy)
                Logger.LogInfo($"Copying icons from {sourceDirectory} to {targetDirectory}");
            else
                Logger.LogInfo($"Deleting icons from {targetDirectory}");

            foreach (var upgradeModeId in Data.ExtendedRoadUpgrades.Modes.Select(m => m.Id))
            {
                if (!destroy)
                {
                    var sourceFileName = Path.Combine(sourceDirectory, Path.ChangeExtension(upgradeModeId, ".svg"));
                    var targetFileName = Path.Combine(targetDirectory, Path.ChangeExtension(upgradeModeId, ".svg"));

                    try
                    {
                        File.Copy(sourceFileName, targetFileName);

                        Logger.LogInfo($"Copied {sourceFileName} to {targetFileName}");
                    }
                    catch (Exception e)
                    {
                        Logger.LogInfo($"Failed copying {sourceFileName} to {targetFileName}");
                        Logger.LogError(e);
                    }
                }
                else
                {
                    var targetFileName = Path.Combine(targetDirectory, Path.ChangeExtension(upgradeModeId, ".svg"));

                    try
                    {
                        File.Delete(targetFileName);

                        Logger.LogInfo($"Deleted {targetFileName}");
                    }
                    catch (Exception e)
                    {
                        Logger.LogInfo($"Failed deleting {targetFileName}");
                        Logger.LogError(e);
                    }
                }
            }
        }

        private void Awake()
        {
            Logger = base.Logger;

#if !DEBUG
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
#else
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID}[DEBUG] is loaded!");
#endif

            HandleIcons();

            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");
            var patchedMethods = harmony.GetPatchedMethods().ToArray();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} made patches! Patched methods: " + patchedMethods.Length);

            foreach (var patchedMethod in patchedMethods)
            {
                Logger.LogInfo($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
            }
        }

        private void OnDestroy()
        {
            HandleIcons(true);
        }
    }
}
