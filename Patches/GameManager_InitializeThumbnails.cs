namespace ExtendedRoadUpgrades.Patches
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using cohtml.Net;
    using Colossal.Json;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.UI;
    using HarmonyLib;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    ///     <para>
    ///         InitializeThumbnails is one of the few synchronous methods that are executed during <see cref="GameManager"/> initialization
    ///     </para>
    ///     <para>
    ///         This is also executed after all the game assets have been loaded, so we can be sure enough that things won't change anymore.
    ///         Unfortunately I'm still missing some bits on how <see cref="IComponentData"/> is attached to <see cref="PrefabBase"/> objects,
    ///         so this means that we also need to handle that part in a separate stage after the patch.
    ///     </para>
    ///     <para>
    ///         I found out that doing it during <see cref="GameManager.onGameLoadingComplete"/>, when the event is a
    ///         <see cref="Game.GameMode.Game"/>, is enough for us to get up-to-date values for all the <see cref="IComponentData"/> on our
    ///         <see cref="PrefabBase"/>. For this reason, the event will be used to perform the actual patch for our upgrade modes, by setting
    ///         the appropriate <see cref="PlaceableNetData.m_SetUpgradeFlags"/> and <see cref="PlaceableNetData.m_UnsetUpgradeFlags"/> flags.
    ///     </para>
    ///
    /// TODO: the event could probably work for <see cref="Game.GameMode.GameOrEditor"/> as well but we'll check it when the editor will be released.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), "InitializeThumbnails")]
    internal class GameManager_InitializeThumbnails
    {
        /// <summary>
        ///     Key used by COUI to load icons from our mod's directory.
        /// </summary>
        private static readonly string IconsResourceKey = $"{MyPluginInfo.PLUGIN_NAME.ToLower()}ui";

        /// <summary>
        ///     Base URI for all of our icons.
        /// </summary>
        private static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

        /// <summary>
        ///     Guard boolean used to check if the Prefix already executed, so that we can prevent executing it multiple times.
        /// </summary>
        private static bool prefixExecuted;

        /// <summary>
        ///     Guard boolean used to check if the Event Handler already executed, so that we can prevent executing it multiple times.
        /// </summary>
        private static bool eventExecuted;

        /// <summary>
        ///     <see cref="world"/> instance used by our patch and by the loading event handler.
        /// </summary>
        private static World world;

        /// <summary>
        ///     <see cref="prefabSystem"/> instance used by our patch and by the loading event handler. 
        /// </summary>
        private static PrefabSystem prefabSystem;

        /// <summary>
        ///     <para>
        ///         Prefix's responsibility is to just add our cloned <see cref="PrefabBase"/> to the global collection in
        ///         <see cref="prefabSystem"/>.
        ///     </para>
        ///     <para>
        ///         To avoid getting the wrong <see cref="world"/> instance we rely on Harmony's <see cref="Traverse"/> to extract the
        ///         <b>m_World</b> field from the injected <see cref="GameManager"/> instance.
        ///     </para>
        ///     <para>
        ///         After that, we leverage <see cref="World.GetOrCreateSystemManaged{T}"/> to get our target <see cref="prefabSystem"/>.
        ///         From there, to get <see cref="prefabSystem"/>'s internal <see cref="PrefabBase"/> list we use <see cref="Traverse"/>
        ///         again and we extract the <b>m_Prefabs</b> field.
        ///     </para>
        ///     <para>
        ///         We now have what it takes to extract our <see cref="PrefabBase"/> object, and as reference we extract the one called
        ///         <b>Grass</b>.
        ///         During this stage we only care for <see cref="ComponentBase"/> and not <see cref="IComponentData"/>.
        ///     </para>
        ///     <para>
        ///         The only <see cref="ComponentBase"/> we need to deal with is the attached <see cref="UIObject"/>, which contains the
        ///         <see cref="UIObject.m_Icon"/> property. This property is a relative URI pointing to a SVG file in your
        ///         <b>Cities2_Data\StreamingAssets\~UI~\GameUI\Media\Game\Icons</b> directory.
        ///     </para>
        ///     <para>
        ///         The <b>Cities2_Data\StreamingAssets\~UI~\GameUI\</b> MUST be omitted from the URI, resulting in a definition similar to:
        ///         <code>
        ///             myUIObject.m_Icon = "Media\Game\Icons\myIcon.svg
        ///         </code>
        ///     </para>
        ///     <para>
        ///         Once the <see cref="UIObject"/> is properly set with an updated <see cref="UIObject.m_Icon"/> and <see cref="UIObject.name"/>
        ///     </para>
        /// </summary>
        /// <param name="__instance"></param>
        static void Prefix(GameManager __instance)
        {
            var logHeader = $"[{nameof(GameManager_InitializeThumbnails)}.{nameof(Prefix)}]";

            if (prefixExecuted)
            {
                Plugin.Logger.LogInfo($"{logHeader} Already executed before, skipping.");
                return;
            }

            Plugin.Logger.LogInfo($"{logHeader} Started.");

            // Getting World instance
            world = Traverse.Create(__instance).Field<World>("m_World").Value;
            if (world == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving World instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved World instance.");

            // Getting PrefabSystem instance from World
            prefabSystem = world.GetExistingSystemManaged<PrefabSystem>();
            if (prefabSystem == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving PrefabSystem instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved PrefabSystem instance.");

            // Getting Prefabs list from PrefabSystem
            var prefabs = Traverse.Create(prefabSystem).Field<List<PrefabBase>>("m_Prefabs").Value;
            if (prefabs == null || !prefabs.Any())
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving Prefabs list, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved Prefabs list.");

            // Getting the original Grass Prefab
            var grassUpgradePrefab = prefabs.FirstOrDefault(p => p.name == "Grass");
            if (grassUpgradePrefab == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving the original Grass Prefab instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved the original Grass Prefab instance.");

            // Getting the original Grass Prefab's UIObject
            var grassUpgradePrefabUIObject = grassUpgradePrefab.GetComponent<UIObject>();
            if (grassUpgradePrefabUIObject == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving the original Grass Prefab's UIObject instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved the original Grass Prefab's UIObject instance.");

            // We can now attach mod's base folder as a resource handler so that we can serve icons.
            // Getting Game's resource handler
            var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
            if (gameUIResourceHandler == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved GameManager's GameUIResourceHandler instance.");

            // Add our mod's folder and name to the available handlers.
            // This works by defining a key (mod's name in this case) and a folder value which contains the resources we want to serve.
            // In our case the mapping will be "extendedroadupgradesui": "mod's folder".
            // This means that the game UI can now reference coui://extendedroadupgradesui/AnySvgIconInModsFolder :)
            gameUIResourceHandler.HostLocationsMap.Add(
                IconsResourceKey,
                new List<string> {
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                }
            );

            // We now have all the needed original objects to build our clones
            foreach (var upgradeMode in Data.ExtendedRoadUpgrades.Modes)
            {
                // Instantiate our clone copying all the properties over
                var clonedGrassUpgradePrefab = GameObject.Instantiate(grassUpgradePrefab);

                // Replace the name our they will be called "Grass (Clone)"
                clonedGrassUpgradePrefab.name = upgradeMode.Id;

                Plugin.Logger.LogDebug($"{logHeader} [{upgradeMode.Id}] Cloned the original Grass Prefab instance.");

                // Update the UI component.
                // To avoid impacting the Grass prefab we need to replace the UIObject with
                // a fresh instance. Every property besides name and icon can be copied over.
                // There is probably a better way of doing this, but I need to be sure that we're not
                // keeping any unintended reference to the source object so I'd rather manually copy
                // over only the thing I need instead of relying on automatic cloning.
                clonedGrassUpgradePrefab.Remove<UIObject>();

                Plugin.Logger.LogDebug($"{logHeader} [{upgradeMode.Id}] Removed the original UIObject instance from the cloned Prefab.");

                // Create and populate the new UIObject for our cloned Prefab
                var clonedGrassUpgradePrefabUIObject = ScriptableObject.CreateInstance<UIObject>();
                clonedGrassUpgradePrefabUIObject.m_Icon = $"{COUIBaseLocation}/{upgradeMode.Id}.svg";
                clonedGrassUpgradePrefabUIObject.name = grassUpgradePrefabUIObject.name.Replace("Grass", upgradeMode.Id);
                clonedGrassUpgradePrefabUIObject.m_IsDebugObject = grassUpgradePrefabUIObject.m_IsDebugObject;
                clonedGrassUpgradePrefabUIObject.m_Priority = grassUpgradePrefabUIObject.m_Priority;
                clonedGrassUpgradePrefabUIObject.m_Group = grassUpgradePrefabUIObject.m_Group;
                clonedGrassUpgradePrefabUIObject.active = grassUpgradePrefabUIObject.active;

                Plugin.Logger.LogDebug($"{logHeader} [{upgradeMode.Id}] Created a custom UIObject for our cloned Prefab with name {clonedGrassUpgradePrefabUIObject.name} and icon {clonedGrassUpgradePrefabUIObject.m_Icon}.");

                // Add the newly created UIObject component and then add the cloned Prefab to our PrefabSystem
                clonedGrassUpgradePrefab.AddComponentFrom(clonedGrassUpgradePrefabUIObject);
                if (!prefabSystem.AddPrefab(clonedGrassUpgradePrefab))
                {
                    Plugin.Logger.LogError($"{logHeader} [{upgradeMode.Id}] Failed adding the cloned Prefab to PrefabSystem, exiting.");
                    return;
                }

                Plugin.Logger.LogInfo($"{logHeader} [{upgradeMode.Id}] Successfully created and added our cloned Prefab to PrefabSystem.");
            }

            // Attach to GameManager's loading event to perform the second phase of our patch
            __instance.onGameLoadingComplete += GameManager_onGameLoadingComplete;
            Plugin.Logger.LogInfo($"{logHeader} Ready to listen to GameManager loading events.");

            // Mark the Prefix as already executed
            prefixExecuted = true;

            Plugin.Logger.LogInfo($"{logHeader} Completed.");
        }

        /// <summary>
        ///     <para>
        ///         This event handler performs the second phase of our custom modes patching.
        ///     </para>
        ///     <para>
        ///         While in the first phase we create the <see cref="PrefabBase"/> without any <see cref="IComponentData"/>, in this one
        ///         we add the <see cref="IComponentData"/> that we need to define the behavior of our custom upgrade modes.
        ///     </para>
        ///     <para>
        ///         This behavior is defined by the <see cref="PlaceableNetData"/> <see cref="IComponentData"/>, which allows us to specify
        ///         a collection of <see cref="PlaceableNetData.m_SetUpgradeFlags"/> and <see cref="PlaceableNetData.m_UnsetUpgradeFlags"/>.
        ///     </para>
        ///     <para>
        ///         These two collection contains all the needed <see cref="CompositionFlags"/> that game will use to compose the final road.
        ///     </para>
        ///     <para>
        ///         <list type="bullet">
        ///             <ul>
        ///                 <see cref="PlaceableNetData.m_SetUpgradeFlags"/> contains the flags that must be added to the target road piece
        ///             </ul>
        ///             <ul>
        ///                 <see cref="PlaceableNetData.m_UnsetUpgradeFlags"/> contains the flags that must be removed from the target road piece
        ///             </ul>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         The goal of this method is then to simply iterate over our cloned <see cref="PrefabBase"/> Prefabs and add to each one of them
        ///         the appropriate <see cref="PlaceableNetData"/>, based on the data set in our <see cref="Data.ExtendedRoadUpgrades.Modes"/> collection.
        ///     </para>
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="mode"></param>
        private static void GameManager_onGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
        {
            var logHeader = $"[{nameof(GameManager_InitializeThumbnails)}.{nameof(GameManager_onGameLoadingComplete)}]";

            if (eventExecuted)
            {
                Plugin.Logger.LogInfo($"{logHeader} Already executed before, skipping.");
                return;
            }

            // Execute in Game mode only
            if (mode != Game.GameMode.Game && mode != Game.GameMode.Editor)
            {
                Plugin.Logger.LogInfo($"{logHeader} Game mode is {mode}, skipping.");
                return;
            }

            Plugin.Logger.LogInfo($"{logHeader} Started.");

            // Getting Prefabs list from PrefabSystem
            var prefabs = Traverse.Create(prefabSystem).Field<List<PrefabBase>>("m_Prefabs").Value;
            if (prefabs == null || !prefabs.Any())
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving Prefabs list, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved Prefabs list.");

            // Getting the original Grass Prefab
            var grassUpgradePrefab = prefabs.FirstOrDefault(p => p.name == "Grass");
            if (grassUpgradePrefab == null)
            {
                Plugin.Logger.LogError($"{logHeader} Failed retrieving the original Grass Prefab instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved the original Grass Prefab instance.");

            // Getting the original Grass Prefab's PlaceableNetData
            var grassUpgradePrefabData = prefabSystem.GetComponentData<PlaceableNetData>(grassUpgradePrefab);
            if (grassUpgradePrefabData.Equals(default(PlaceableNetData)))
            {
                // This type is not nullabe so we check equality with the default empty data
                Plugin.Logger.LogError($"{logHeader} Failed retrieving the original Grass Prefab's PlaceableNetData instance, exiting.");
                return;
            }

            Plugin.Logger.LogDebug($"{logHeader} Retrieved the original Grass Prefab's PlaceableNetData instance.");

            // We now have all the needed original objects to patch our clones
            foreach (var upgradeMode in Data.ExtendedRoadUpgrades.Modes)
            {
                // Getting the cloned Grass Prefab for the current upgrade mode
                var clonedGrassUpgradePrefab = prefabs.FirstOrDefault(p => p.name == upgradeMode.Id);
                if (clonedGrassUpgradePrefab == null)
                {
                    Plugin.Logger.LogError($"{logHeader} [{upgradeMode.Id}] Failed retrieving the cloned Grass Prefab instance, exiting.");
                    return;
                }

                Plugin.Logger.LogDebug($"{logHeader} [{upgradeMode.Id}] Retrieved the cloned Grass Prefab instance.");

                // Getting the cloned Grass Prefab's PlaceableNetData for the current upgrade mode
                var clonedGrassUpgradePrefabData = prefabSystem.GetComponentData<PlaceableNetData>(clonedGrassUpgradePrefab);
                if (clonedGrassUpgradePrefabData.Equals(default(PlaceableNetData)))
                {
                    // This type is not nullabe so we check equality with the default empty data
                    Plugin.Logger.LogError($"{logHeader} [{upgradeMode.Id}] Failed retrieving the cloned Grass Prefab's PlaceableNetData instance, exiting.");
                    return;
                }

                Plugin.Logger.LogDebug($"{logHeader} [{upgradeMode.Id}] Retrieved the cloned Grass Prefab's PlaceableNetData instance.");

                // Update the flags with the ones set in our upgrade mode
                clonedGrassUpgradePrefabData.m_SetUpgradeFlags = upgradeMode.m_SetUpgradeFlags;

                // TODO: this works even without the unset flags, keeping them there just in case
                clonedGrassUpgradePrefabData.m_UnsetUpgradeFlags = upgradeMode.m_UnsetUpgradeFlags;

                // This toggles underground mode for our custom upgrade modes
                if (upgradeMode.IsUnderground)
                {
                    clonedGrassUpgradePrefabData.m_PlacementFlags |= Game.Net.PlacementFlags.UndergroundUpgrade;
                }

                // Persist the updated flags by replacing the ComponentData with the one we just created
                prefabSystem.AddComponentData(clonedGrassUpgradePrefab, clonedGrassUpgradePrefabData);

                Plugin.Logger.LogInfo($"{logHeader} [{upgradeMode.Id}] Successfully set flags for our cloned Prefab to {clonedGrassUpgradePrefabData.m_SetUpgradeFlags.ToJSONString()} and {clonedGrassUpgradePrefabData.m_UnsetUpgradeFlags.ToJSONString()}.");
            }

            // Mark the Prefix as already executed
            eventExecuted = true;

            Plugin.Logger.LogInfo($"{logHeader} Completed.");
        }
    }
}
