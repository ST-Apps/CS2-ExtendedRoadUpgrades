using Colossal.IO.AssetDatabase;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ExtendedRoadUpgrades.Patches
{
    [HarmonyPatch(typeof(LocaleData))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(string), typeof(Dictionary<string, string>), typeof(Dictionary<string, int>) })]
    class LocaleData_ctor
    {
        /// <summary>
        /// We simply add our custom entries to <see cref="LocaleData.entries"/> by hooking
        /// into its constructor.
        /// 
        /// <b>I don't know the implications on not updating <see cref="LocaleData.indexCounts"/> but it works for now.</b>
        /// </summary>
        /// <param name="__instance"></param>
        static void Postfix(LocaleData __instance)
        {
            Plugin.Logger.LogDebug($"LocaleData[{__instance.localeId}] {__instance.entries.Count} entries, {__instance.indexCounts.Count} indexCounts");

            // Iterate over the available upgrade modes and add their entries
            foreach (var upgradeMode in Data.ExtendedRoadUpgrades.Modes)
            {
                var localeKey = upgradeMode.Name.ContainsKey(__instance.localeId) ? __instance.localeId : "en-US";

                __instance.entries[$"Assets.NAME[{upgradeMode.Id}]"] = upgradeMode.Name[localeKey];
                Plugin.Logger.LogInfo($"LocaleData[{localeKey}] Added: Assets.NAME[{upgradeMode.Id}]");

                __instance.entries[$"Assets.DESCRIPTION[{upgradeMode.Id}]"] = upgradeMode.Description[localeKey];
                Plugin.Logger.LogInfo($"LocaleData[{localeKey}] Added: Assets.DESCRIPTION[{upgradeMode.Id}]");
            }

            Plugin.Logger.LogDebug($"LocaleData[{__instance.localeId}] {__instance.entries.Count} entries, {__instance.indexCounts.Count} indexCounts");
        }
    }
}
