using System.Linq;
using Game.SceneFlow;
using HarmonyLib;
using Game.Prefabs;
using Colossal.Json;
using Unity.Entities;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace ExtendedRoadUpgrades.Patches
{
    [HarmonyPatch(typeof(AssetLibrary), "Load")]
    class AssetLibrary_Load
    {
        // true if we already added our Prefab to the AssetCollection. This happens during the Prefix only.
        private static bool _prefabAdded;

        // true if we already updated our Prefab with the PlaceableNetData flags. This happens during the Postfix only.
        private static bool _prefabUpdated;

        /// <summary>
        /// Prefix just looks for the right <see cref="AssetCollection"/>, which is the one containing 
        /// a <see cref="PrefabBase"/> called <b>Grass</b>, then clones the <see cref="PrefabBase"/> and
        /// adds it back to the collection.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="prefabSystem"></param>
        /// <param name="token"></param>
        static void Prefix(AssetLibrary __instance, PrefabSystem prefabSystem, CancellationToken token)
        {
            if (_prefabAdded) return;

            // Search for the AssetCollection that contains road upgrades.
            // We use the "Grass" prefab as our base item.
            var roadUpgradesAssetCollection = __instance.m_Collections.FirstOrDefault(c => c.m_Prefabs.Any(p => p.name == "Grass"));
            if (roadUpgradesAssetCollection != null && roadUpgradesAssetCollection.m_Prefabs.Any())
            {
                // We can now clone the "Grass" prefab to customize it later
                var grassUpgradePrefab = roadUpgradesAssetCollection.m_Prefabs.FirstOrDefault(p => p.name == "Grass") as FencePrefab;

                var grassUpgradePrefabNetUpgrade = grassUpgradePrefab.GetComponent<NetUpgrade>();
                Plugin.Logger.LogDebug($"grassUpgradePrefabNetUpgrade: {grassUpgradePrefabNetUpgrade.m_SetState.ToJSONString()} - {grassUpgradePrefabNetUpgrade.m_UnsetState.ToJSONString()}");

                var grassUpgradePrefabUIObject = grassUpgradePrefab.GetComponent<UIObject>();
                Plugin.Logger.LogDebug($"grassUpgradePrefabUIObject: {grassUpgradePrefabUIObject.m_Icon} - {grassUpgradePrefabUIObject.m_LargeIcon}");

                // Iterate over the available upgrade modes and clone the Grass prefab
                foreach (var upgradeMode in Data.ExtendedRoadUpgrades.Modes)
                {
                    var clonedGrassPrefab = Object.Instantiate(grassUpgradePrefab);                    
                    clonedGrassPrefab.name = upgradeMode.Id;

                    // Finally, add the cloned prefab to the collection
                    roadUpgradesAssetCollection.m_Prefabs.Add(clonedGrassPrefab);
                    Plugin.Logger.LogInfo($"clonedGrassPrefabData[{upgradeMode.Id}]: Added to the AssetCollection [thumbnail: {clonedGrassPrefab.thumbnailUrl}].");
                }
            }

            _prefabAdded = true;
        }

        /// <summary>
        /// Postfix waits for the original <b>Grass</b> <see cref="PrefabBase"/> to have the correct
        /// <see cref="PlaceableNetData"/> flags, as this is not happening on the first invocation, and
        /// then updates those flags for our cloned <see cref="PrefabBase"/>. After that, changes are
        /// persisted using <see cref="PrefabSystem"/>'s <see cref="EntityManager"/>.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="prefabSystem"></param>
        /// <param name="token"></param>
        static void Postfix(AssetLibrary __instance, PrefabSystem prefabSystem, CancellationToken token)
        {
            if (_prefabUpdated) return;

            // Lookup needed to get PlaceableNetData from our PrefabSystem
            ComponentLookup<PlaceableNetData> placeableNetDataLookup = prefabSystem.GetComponentLookup<PlaceableNetData>(false);

            // Search for the AssetCollection that contains road upgrades.
            // We use the "Grass" prefab as our base item.
            var roadUpgradesAssetCollection = __instance.m_Collections.FirstOrDefault(c => c.m_Prefabs.Any(p => p.name == "Grass"));
            if (roadUpgradesAssetCollection != null && roadUpgradesAssetCollection.m_Prefabs.Any())
            {
                // We can now get the "Grass" prefab and copy its properties to our "GrassClone"
                // For some reasons I'm too lazy to investigate, this method is called 4 times and it has
                // the right data only on the 4th invocation. To avoid counting invocations, since this
                // could change in the future, we just check for the Grass prefab to be fully initialized.
                // By fully initialized I mean that its PlaceableNetData is not empty anymore but instead has
                // the right flags set.
                // Keep in mind that the game inverts the flags when you try to apply the upgrade to the other
                // side of the road, so we only need to deal with the right side (hoping that this is not messed up
                // with left-hand side driving).
                var grassUpgradePrefab = roadUpgradesAssetCollection.m_Prefabs.FirstOrDefault(p => p.name == "Grass");

                // Get extra components that need to be patched
                var grassUpgradePrefabNetUpgrade = grassUpgradePrefab.GetComponent<NetUpgrade>();
                var grassUpgradePrefabUIObject = grassUpgradePrefab.GetComponent<UIObject>();

                // Get extra components data that need to be patched
                var grassUpgradeData = prefabSystem.GetComponentData<PlaceableNetData>(grassUpgradePrefab);

                if (grassUpgradeData.m_SetUpgradeFlags.m_Right.HasFlag(CompositionFlags.Side.PrimaryBeautification))
                {
                    // Entities are now fully set so we can change the flags with the ones we need
                    // Iterate over the available upgrade modes and set their flags
                    foreach (var upgradeMode in Data.ExtendedRoadUpgrades.Modes)
                    {
                        var clonedGrassPrefab = roadUpgradesAssetCollection.m_Prefabs.FirstOrDefault(p => p.name == upgradeMode.Id);
                        var clonedGrassPrefabData = prefabSystem.GetComponentData<PlaceableNetData>(clonedGrassPrefab);

                        // Update the flags
                        clonedGrassPrefabData.m_SetUpgradeFlags = upgradeMode.m_SetUpgradeFlags;
                        // TODO: this works even without the unset flags, keeping them there just in case
                        clonedGrassPrefabData.m_UnsetUpgradeFlags = upgradeMode.m_UnsetUpgradeFlags;

                        // Update the UI component.
                        // To avoid impacting the Grass prefab we need to replace the UIObject with
                        // a fresh instance. Every property besides name and icon can be copied over.
                        clonedGrassPrefab.Remove<UIObject>();

                        var clonedGrassUpgradePrefabUIObject = ScriptableObject.CreateInstance<UIObject>();
                        clonedGrassUpgradePrefabUIObject.m_Icon = grassUpgradePrefabUIObject.m_Icon.Replace("Grass", upgradeMode.Id);
                        clonedGrassUpgradePrefabUIObject.m_IsDebugObject = grassUpgradePrefabUIObject.m_IsDebugObject;
                        clonedGrassUpgradePrefabUIObject.m_Priority = grassUpgradePrefabUIObject.m_Priority;
                        clonedGrassUpgradePrefabUIObject.m_Group = grassUpgradePrefabUIObject.m_Group;
                        clonedGrassUpgradePrefabUIObject.active = grassUpgradePrefabUIObject.active;
                        clonedGrassUpgradePrefabUIObject.name = grassUpgradePrefabUIObject.name.Replace("Grass", upgradeMode.Id);
                        clonedGrassPrefab.AddComponentFrom<UIObject>(clonedGrassUpgradePrefabUIObject);

                        // Update the component
                        // TODO: not sure how this works yet
                        //Plugin.Logger.LogDebug($"BEFORE: {clonedGrassPrefab.GetComponent<NetUpgrade>().m_SetState.ToJSONString()}");
                        //var clonedGrassUpgradePrefabNetUpgrade = GameObject.Instantiate<NetUpgrade>(grassUpgradePrefabNetUpgrade);
                        //clonedGrassUpgradePrefabNetUpgrade.name = $"{upgradeMode.Id}NetUpgrade";
                        //clonedGrassUpgradePrefabNetUpgrade.prefab = clonedGrassPrefab;
                        //clonedGrassUpgradePrefabNetUpgrade.m_SetState = upgradeMode.m_SetState.ToArray();
                        //clonedGrassUpgradePrefabNetUpgrade.m_UnsetState = upgradeMode.m_UnsetState.ToArray();
                        //prefabSystem.EntityManager.RemoveComponent<NetUpgrade>(prefabSystem.GetEntity(clonedGrassPrefab));
                        //clonedGrassPrefab.AddComponentFrom<NetUpgrade>(clonedGrassUpgradePrefabNetUpgrade);
                        //Plugin.Logger.LogDebug($"AFTER: {clonedGrassPrefab.GetComponent<NetUpgrade>().m_SetState.ToJSONString()}");

                        // Persist changes to the EntityManager
                        prefabSystem.EntityManager.SetComponentData(prefabSystem.GetEntity(clonedGrassPrefab), clonedGrassPrefabData);
                    }

                    _prefabUpdated = true;
                }
            }
        }
    }
}