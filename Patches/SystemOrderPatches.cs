#if DEBUG
using Game.Common;
using Game;
using HarmonyLib;
using ExtendedRoadUpgrades.Systems;

namespace ExtendedRoadUpgrades.Patches
{
    [HarmonyPatch(typeof(SystemOrder), "Initialize")]
    class SystemOrder_Initialize
    {
        static void Postfix(UpdateSystem updateSystem)
        {
            Plugin.Logger.LogInfo($"Creating debug tool and adding it to update system...");

            updateSystem.World.GetOrCreateSystem<PickerToolSystem>();
            updateSystem.UpdateAt<PickerToolSystem>(SystemUpdatePhase.ToolUpdate);

            Plugin.Logger.LogInfo($"...done.");
        }
    }
}
#endif
