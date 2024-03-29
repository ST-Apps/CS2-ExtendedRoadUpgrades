// <copyright file="NetToolSystem_InitializeRaycast.cs" company="ST-Apps (S. Tenuta)">
// Copyright (c) ST-Apps (S. Tenuta). All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ExtendedRoadUpgrades.Patches
{
    using Game.Net;
    using Game.Tools;
    using HarmonyLib;

    /// <summary>
    ///     <para>
    ///         This patch just adds more flags to <see cref="NetToolSystem"/>'s <see cref="ToolRaycastSystem"/>.
    ///     </para>
    ///     <para>
    ///         The following flags are added:
    ///         <list type="bullet">
    ///             <ul>
    ///                 <see cref="Layer.Pathway"/> - enables upgrading pedestrian paths
    ///             </ul>
    ///             <ul>
    ///                 <see cref="Layer.TrainTrack"/> - enables upgrading train tracks paths
    ///             </ul>
    ///             <ul>
    ///                 <see cref="Layer.PublicTransportRoad"/> - enables upgrading bus roads
    ///             </ul>
    ///         </list>
    ///     </para>
    /// </summary>
    [HarmonyPatch(typeof(NetToolSystem), "InitializeRaycast")]
    internal class NetToolSystem_InitializeRaycast
    {
        static void Postfix(NetToolSystem __instance)
        {
            var logHeader = $"[{nameof(NetToolSystem_InitializeRaycast)}.{nameof(Postfix)}]";

            var toolRaycastSystem = Traverse.Create(__instance).Field<ToolRaycastSystem>("m_ToolRaycastSystem").Value;
            if (toolRaycastSystem == null)
            {
                Mod.Log.Error($"{logHeader} Failed retrieving ToolRaycastSystem instance, exiting.");
                return;
            }

            if (__instance.actualMode == NetToolSystem.Mode.Replace)
            {
                toolRaycastSystem.netLayerMask |= Layer.Pathway | Layer.TrainTrack | Layer.PublicTransportRoad | Layer.TramTrack | Layer.SubwayTrack;
            }
        }
    }
}
