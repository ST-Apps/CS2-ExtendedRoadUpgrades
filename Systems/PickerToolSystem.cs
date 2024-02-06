#if DEBUG
using Game.Areas;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine.InputSystem;
using Unity.Collections;
using UnityEngine.Scripting;
using Unity.Jobs;
using Colossal.Json;
using static Colossal.IO.AssetDatabase.AtlasFrame;
using ExtendedRoadUpgrades.Prefabs;

namespace ExtendedRoadUpgrades.Systems
{
    [CompilerGenerated]
    internal class PickerToolSystem : ToolBaseSystem
    {
        private ToolBaseSystem m_previousActiveTool;

        /// <summary>
        /// Previously used <see cref="ToolBaseSystem"/>, needed to restore it when disabling this mod.
        /// </summary>
        public ToolBaseSystem previousActiveTool
        {
            get => m_previousActiveTool;
            private set { m_previousActiveTool = value; }
        }

        [ReadOnly]
        ComponentLookup<Edge> edgeDataLookup;

        [ReadOnly]
        ComponentLookup<Composition> compositionDataLookup;

        [ReadOnly]
        ComponentLookup<NetCompositionData> netCompositionDataLookup;

        [ReadOnly]
        ComponentLookup<NetCompositionFlagsData> netCompositionFlagsDataLookup;

        private void CreateKeyBinding()
        {
            var inputAction = new InputAction("MyModHotkeyPress");
            inputAction.AddBinding("<Keyboard>/n");
            inputAction.performed += OnHotkeyPress;
            inputAction.Enable();
        }

        private void OnHotkeyPress(InputAction.CallbackContext obj)
        {
            Plugin.Logger.LogInfo($"Toggling tool...");

            if (m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.activeTool = previousActiveTool;
            }
            else
            {
                previousActiveTool = m_ToolSystem.activeTool;
                m_ToolSystem.activeTool = this;
            }

            Plugin.Logger.LogInfo($"Active tool is {m_ToolSystem.activeTool.toolID}, previous tool was {previousActiveTool.toolID}");
        }

        #region ToolBaseSystem
        public override string toolID => "Picker Tool";

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            CreateKeyBinding();

            edgeDataLookup = GetComponentLookup<Edge>(true);
            compositionDataLookup = GetComponentLookup<Composition>(true);
            netCompositionDataLookup = GetComponentLookup<NetCompositionData>(true);
            netCompositionFlagsDataLookup = GetComponentLookup<NetCompositionFlagsData>(true);
        }

        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
            {
                if (edgeDataLookup.TryGetComponent(controlPoint.m_OriginalEntity, out var edge) &&
                    compositionDataLookup.TryGetComponent(controlPoint.m_OriginalEntity, out var composition) &&
                    netCompositionDataLookup.TryGetComponent(composition.m_Edge, out var edgeComposition) &&
                    netCompositionDataLookup.TryGetComponent(composition.m_StartNode, out var startNodeComposition) &&
                    netCompositionDataLookup.TryGetComponent(composition.m_EndNode, out var endNodeComposition))
                {
                    Plugin.Logger.LogInfo($"Found edge: {controlPoint.m_OriginalEntity.ToJSONString()} {edge.ToJSONString()}");
                    Plugin.Logger.LogInfo($"\tFound edge composition: {edgeComposition.ToJSONString()}");
                    Plugin.Logger.LogInfo($"\tFound start composition: {startNodeComposition.ToJSONString()}");
                    Plugin.Logger.LogInfo($"\tFound end composition: {endNodeComposition.ToJSONString()}");

                    if (netCompositionFlagsDataLookup.TryGetComponent(composition.m_Edge, out var edgeFlagCompositionData))
                    {
                        Plugin.Logger.LogInfo($"\tFound edge FLAG: {edgeFlagCompositionData.ToJSONString()}");
                    }

                    if (netCompositionFlagsDataLookup.TryGetComponent(composition.m_StartNode, out var startFlagCompositionData))
                    {
                        Plugin.Logger.LogInfo($"\tFound start FLAG: {startFlagCompositionData.ToJSONString()}");
                    }

                    if (netCompositionFlagsDataLookup.TryGetComponent(composition.m_EndNode, out var endFlagCompositionData))
                    {
                        Plugin.Logger.LogInfo($"\tFound end FLAG: {endFlagCompositionData.ToJSONString()}");
                    }
                }
            }

            return inputDeps;
        }

        public override PrefabBase GetPrefab()
        {
            return default;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net;
            m_ToolRaycastSystem.netLayerMask = Layer.All;
            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.Underground;

            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;

            if (m_ToolSystem.actionMode.IsEditor())
            {
                m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers | RaycastFlags.UpgradeIsMain | RaycastFlags.EditorContainers;
                m_ToolRaycastSystem.typeMask |= TypeMask.Areas;
                m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.Lots | AreaTypeMask.Spaces | AreaTypeMask.Surfaces;
            }

            m_ToolRaycastSystem.typeMask |= TypeMask.MovingObjects;

            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders | RaycastFlags.Decals;
        }

        protected override bool GetRaycastResult(out ControlPoint controlPoint)
        {
            if (GetRaycastResult(out Entity entity, out RaycastHit hit))
            {
                if (EntityManager.HasComponent<Game.Net.Node>(entity) && EntityManager.HasComponent<Edge>(hit.m_HitEntity))
                {
                    entity = hit.m_HitEntity;
                }

                controlPoint = new ControlPoint(entity, hit);
                return true;
            }

            controlPoint = default(ControlPoint);
            return false;
        }

        protected override bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
        {
            if (GetRaycastResult(out var entity, out var hit, out forceUpdate))
            {
                if (EntityManager.HasComponent<Game.Net.Node>(entity) && EntityManager.HasComponent<Edge>(hit.m_HitEntity))
                {
                    entity = hit.m_HitEntity;
                }

                controlPoint = new ControlPoint(entity, hit);
                return true;
            }

            controlPoint = default(ControlPoint);
            return false;
        }

        #endregion
    }
}
#endif