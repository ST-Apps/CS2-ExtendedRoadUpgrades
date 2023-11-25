
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
#if DEBUG
            Plugin.Logger.LogInfo($"[{nameof(SystemOrder_Initialize)}.{nameof(Postfix)}] Creating debug tool and adding it to update system...");
            updateSystem.World.GetOrCreateSystem<PickerToolSystem>();
            updateSystem.UpdateAt<PickerToolSystem>(SystemUpdatePhase.ToolUpdate);
#endif
            Plugin.Logger.LogInfo($"[{nameof(SystemOrder_Initialize)}.{nameof(Postfix)}] Creating retaining walls fixer system...");
            updateSystem.World.GetOrCreateSystem<NodeRetainingWallUpdateSystem>();
            updateSystem.UpdateAt<NodeRetainingWallUpdateSystem>(SystemUpdatePhase.ToolUpdate);

            Plugin.Logger.LogInfo($"...done.");
        }
    }
}

