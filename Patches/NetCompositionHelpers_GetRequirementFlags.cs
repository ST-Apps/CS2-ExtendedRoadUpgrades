using Colossal.Json;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedRoadUpgrades.Patches
{
    [HarmonyPatch(
        typeof(NetCompositionHelpers),
        "GetRequirementFlags",
        new[]
        {
            typeof(NetPieceRequirements),
            typeof(CompositionFlags),
            typeof(NetSectionFlags)
        },
        new[]
        {
            ArgumentType.Normal,
            ArgumentType.Ref,
            ArgumentType.Ref
        }
    )]
    internal class NetCompositionHelpers_GetRequirementFlags
    {

        static void Postfix(NetPieceRequirements requirement, ref CompositionFlags compositionFlags, ref NetSectionFlags sectionFlags)
        {
            //if (requirement == NetPieceRequirements.LowTransition)
            //{
            //    Plugin.Logger.LogInfo($"CALLED NETCOMP POSTFIX WITH: {requirement.ToJSONString()} - {compositionFlags.ToJSONString()} - {sectionFlags.ToJSONString()}");
            //    compositionFlags.m_Right &= ~CompositionFlags.Side.LowTransition;
            //    Plugin.Logger.LogInfo($"UPDATED NETCOMP POSTFIX WITH: {requirement.ToJSONString()} - {compositionFlags.ToJSONString()} - {sectionFlags.ToJSONString()}");
            //}
            compositionFlags.m_Right &= ~CompositionFlags.Side.LowTransition;
        }

    }

    [HarmonyPatch(typeof(BatchDataHelpers), "CalculateNodeParameters")]
    internal class TMP
    {
        static void Postfix(object[] __args)
        {
            EdgeNodeGeometry nodeGeometry = (EdgeNodeGeometry)__args[0];
            NetCompositionData prefabCompositionData = (NetCompositionData)__args[1];
            BatchDataHelpers.CompositionParameters compositionParameters = (BatchDataHelpers.CompositionParameters)__args[2];

            Plugin.Logger.LogInfo($"CALLED BatchDataHelpers: {nodeGeometry.ToJSONString()} - {prefabCompositionData.ToJSONString()} - {compositionParameters.ToJSONString()}");
        }
    }
}
