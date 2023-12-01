using Colossal.Json;
using Game.Tools;
using HarmonyLib;

namespace ExtendedRoadUpgrades.Patches
{
    /// <summary>
    ///     <para>
    ///         This patch just adds more flags to <see cref="NetToolSystem"/>'s <see cref="ToolRaycastSystem"/>.
    ///     </para>
    ///     <para>
    ///         The following flags are added:
    ///         <list type="bullet">
    ///             <ul>
    ///                 <see cref="Game.Net.Layer.Pathway"/> - enables upgrading pedestrian paths
    ///             </ul>
    ///             <ul>
    ///                 <see cref="Game.Net.Layer.TrainTrack"/> - enables upgrading train tracks paths
    ///             </ul>
    ///             <ul>
    ///                 <see cref="Game.Net.Layer.PublicTransportRoad"/> - enables upgrading bus roads
    ///             </ul>
    ///         </list>
    ///     </para>
    /// </summary>
    [HarmonyPatch(typeof(NetToolSystem), "InitializeRaycast")]
    internal class NetToolSystem_InitializeRaycast
    {
        static void Postfix(NetToolSystem __instance, ToolRaycastSystem ___m_ToolRaycastSystem)
        {
            if (__instance.actualMode == NetToolSystem.Mode.Replace)
            {
                ___m_ToolRaycastSystem.netLayerMask |= Game.Net.Layer.Pathway | Game.Net.Layer.TrainTrack | Game.Net.Layer.PublicTransportRoad;
            }
        }

    }
}
