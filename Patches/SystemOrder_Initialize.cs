namespace ExtendedRoadUpgrades.Patches
{
    using ExtendedRoadUpgrades.Systems;
    using Game;
    using Game.Common;
    using HarmonyLib;

    /// <summary>
    ///     This patch hooks into the initial <see cref="SystemOrder.Initialize(Game.UpdateSystem)"/> to attach out custom
    ///     <see cref="GameSystemBase"/> to the Update workflow.
    /// </summary>
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

