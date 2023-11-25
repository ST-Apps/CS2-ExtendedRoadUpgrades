using Colossal;
using Colossal.Json;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace ExtendedRoadUpgrades.Systems
{
    internal class NodeRetainingWallUpdateSystem : GameSystemBase
    {
        #region Update Toggling

        /// <summary>
        ///     Flag used to decide if we have to execute the <see cref="OnUpdate"/> method.
        ///     To avoid wasting cycles we execute the method only when user selects one of our custom
        ///     upgrade modes.
        ///     Logic is defined in the <see cref="UpdateCanUpdate"/> method.
        /// </summary>
        private bool _canUpdate;

        private PrefabBase _currentNetPrefab;

        /// <summary>
        ///     Current <see cref="PrefabBase"/> selected in <see cref="NetToolSystem"/>, needed to
        ///     detect if we're using one of our custom upgrade modes.
        /// </summary>
        private PrefabBase CurrentNetPrefab
        {
            set
            {
                _currentNetPrefab = value;
                UpdateCanUpdate();
            }
        }

        /// <summary>
        ///     Current <see cref="ToolBaseSystem"/> selected in-game, needed to detect if we're in
        ///     <see cref="NetToolSystem"/>.
        /// </summary>
        private ToolBaseSystem _currentTool;
        private ToolBaseSystem CurrentNetTool
        {
            set
            {
                _currentTool = value;
                UpdateCanUpdate();
            }
        }

        /// <summary>
        ///     Toggles <see cref="_canUpdate"/> on/off based on the value of <see cref="CurrentNetTool"/> and <see cref="CurrentNetPrefab"/>.
        ///     We allow running updates when <see cref="CurrentNetTool"/> is <see cref="NetToolSystem"/> and when <see cref="CurrentNetPrefab"/> is
        ///     one of our custom upgrade modes.
        /// </summary>
        private void UpdateCanUpdate()
        {
            _canUpdate = _currentTool is NetToolSystem &&
                Data.ExtendedRoadUpgrades.Modes.Any(m => m.Id == _currentNetPrefab.name);

            Plugin.Logger.LogDebug($"[{nameof(NodeRetainingWallUpdateSystem)}.{nameof(UpdateCanUpdate)}] Setting canUpdate to {_canUpdate} because Tool is {_currentTool} and Prefab is {_currentNetPrefab.name}.");
        }

        private void OnPrefabChanged(PrefabBase prefabBase)
        {
            CurrentNetPrefab = prefabBase;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            CurrentNetTool = tool;
        }

        #endregion

        #region GameSystemBase

        [Preserve]
        public NodeRetainingWallUpdateSystem()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref CheckedStateRef);
            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
#if DEBUG
            this.m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
#endif
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[] {
                        ComponentType.ReadOnly<Upgraded>(),
                        ComponentType.ReadOnly<Edge>(),
                        // Temp seems to be needed or we won't get edges being upgraded
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadWrite<Composition>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Hidden>()
                    }
                }
            });
            m_UpdatedQuery.AddOrderVersionFilter();
            m_UpdatedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Edge>());
            m_UpdatedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Temp>());
            RequireForUpdate(m_UpdatedQuery);

            // Register on ToolSystem.EventToolChanged and EventPrefabChanged to execute our Update method only while using the NetTool and our custom modes
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(m_ToolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(m_ToolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
        }

        [Preserve]
        protected override void OnDestroy()
        {
            // Deregister from ToolSystem.EventToolChanged and EventPrefabChanged
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Remove(m_ToolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Remove(m_ToolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            base.OnDestroy();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (!_canUpdate || m_UpdatedQuery.IsEmpty) return;

            __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Composition_Data_RW_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Net_Composition_Data_RW_ComponentLookup.Update(ref CheckedStateRef);
            var nodeFixerJob = default(NodeFixerJob);
            nodeFixerJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            nodeFixerJob.m_EdgeType = __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
            nodeFixerJob.m_EdgeData = __TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
            nodeFixerJob.m_CompositionData = __TypeHandle.__Game_Composition_Data_RW_ComponentLookup;
            nodeFixerJob.m_NetCompositionData = __TypeHandle.__Game_Net_Composition_Data_RW_ComponentLookup;
            nodeFixerJob.m_ConnectedEdges = __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup;
            nodeFixerJob.m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
#if DEBUG
            nodeFixerJob.m_NodeData = __TypeHandle.__Game_Net_Node_RO_ComponentLookup;
            nodeFixerJob.m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out JobHandle jobHandle);
            var jobHandle2 = nodeFixerJob.Schedule(m_UpdatedQuery, JobHandle.CombineDependencies(Dependency, jobHandle));
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
            Dependency = jobHandle2;
#else
            var jobHandle = nodeFixerJob.Schedule(m_UpdatedQuery, Dependency);
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
#endif
        }

        #endregion

        #region Jobs

        private EntityQuery m_UpdatedQuery;
        private TypeHandle __TypeHandle;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private ToolSystem m_ToolSystem;
#if DEBUG
        private GizmosSystem m_GizmosSystem;
#endif

        private struct TypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(true);
                __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(true);
                __Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(true);
                __Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>(true);
                __Game_Composition_Data_RW_ComponentLookup = state.GetComponentLookup<Composition>(false);
                __Game_Net_Composition_Data_RW_ComponentLookup = state.GetComponentLookup<NetCompositionData>(false);
            }

            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

            public ComponentLookup<NetCompositionData> __Game_Net_Composition_Data_RW_ComponentLookup;

            public ComponentLookup<Composition> __Game_Composition_Data_RW_ComponentLookup;

            public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferLookup;
        }

        private struct NodeFixerJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Edge> m_EdgeType;

            [ReadOnly]
            public ComponentLookup<Edge> m_EdgeData;

            [ReadOnly]
            public ComponentLookup<Composition> m_CompositionData;

            [ReadOnly]
            public ComponentLookup<NetCompositionData> m_NetCompositionData;

            public BufferLookup<ConnectedEdge> m_ConnectedEdges;

            public EntityCommandBuffer m_CommandBuffer;

#if DEBUG
            public GizmoBatcher m_GizmoBatcher;

            [ReadOnly]
            public ComponentLookup<Node> m_NodeData;
#endif

            /// <summary>
            ///     Main entrypoint for the job, it processes both start <see cref="Node"/> and end <see cref="Node"/> for any <see cref="Updated"/> <see cref="Edge"/>.
            /// </summary>
            /// <param name="chunk"></param>
            /// <param name="unfilteredChunkIndex"></param>
            /// <param name="useEnabledMask"></param>
            /// <param name="chunkEnabledMask"></param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var updatedEdges = chunk.GetNativeArray(ref m_EdgeType);
                var updatedEdgesEntities = chunk.GetNativeArray(m_EntityType);

                for (var i = 0; i < updatedEdges.Length; i++)
                {
                    var currentEdgeEntity = updatedEdgesEntities[i];
                    var currentEdge = updatedEdges[i];

                    Plugin.Logger.LogInfo($"Working with edge {currentEdgeEntity.ToJSONString()} - {currentEdge.ToJSONString()}");

                    if (m_CompositionData.TryGetComponent(currentEdgeEntity, out var currentEdgeComposition) &&
                        m_NetCompositionData.TryGetComponent(currentEdgeComposition.m_StartNode, out var currentEdgeStartNodeCompositionData) &&
                        m_NetCompositionData.TryGetComponent(currentEdgeComposition.m_EndNode, out var currentEdgeEndNodeCompositionData))
                    {
                        ProcessConnectedEdges(currentEdge.m_Start, currentEdgeStartNodeCompositionData, currentEdgeComposition.m_StartNode, true);
                        ProcessConnectedEdges(currentEdge.m_End, currentEdgeEndNodeCompositionData, currentEdgeComposition.m_EndNode, false);
                    }
                }
            }

            /// <summary>
            ///     Enumerates all the <see cref="Edge"/>s connected to the given <see cref="Node"/> <see cref="Entity"/>.
            ///     This is done by querying the <see cref="ConnectedEdge"/>s <see cref="Entity"/> for the given <see cref="Node"/>.
            /// </summary>
            /// <param name="nodeEntity"></param>
            private IEnumerable<Entity> ConnectedEdges(Entity nodeEntity)
            {
                if (!m_ConnectedEdges.TryGetBuffer(nodeEntity, out var connectedEdgesBuffer) &&
                    !connectedEdgesBuffer.IsCreated)
                {
                    yield break;
                }

                for (var i = 0; i < connectedEdgesBuffer.Length; i++)
                {
                    yield return connectedEdgesBuffer[i].m_Edge;
                }
            }

            /// <summary>
            ///     Checks for matches between two <see cref="NetCompositionData.m_Flags"/>.
            ///     Flags match if they're equal or, if on a start <see cref="Node"/> or on a <see cref="Node"/> with the <see cref="CompositionFlags.General.Invert"/>,
            ///     if the inverted start <see cref="CompositionFlags"/> is equal to end's one.
            /// </summary>
            /// <param name="startComposition"></param>
            /// <param name="endComposition"></param>
            /// <param name="isStartNode"></param>
            /// <returns></returns>
            private bool FlagsMatch(NetCompositionData startComposition, NetCompositionData endComposition, bool isStartNode)
            {
                // Start nodes will require flags to be inverted before any operation.
                // TODO: is this true or does this apply to nodes that have the Invert flag only?
                var startCompositionFlags = isStartNode || startComposition.m_Flags.m_General.HasFlag(CompositionFlags.General.Invert) ?
                    NetCompositionHelpers.InvertCompositionFlags(startComposition.m_Flags) :
                    startComposition.m_Flags;

                return startCompositionFlags.m_Left == endComposition.m_Flags.m_Left ||
                    startCompositionFlags.m_Right == endComposition.m_Flags.m_Right;
            }

            /// <summary>
            ///     Iterates over the <see cref="ConnectedEdge"/>s for the current <see cref="Node"/> <see cref="Entity"/> and updates their <see cref="CompositionFlags"/> accordingly.
            ///     For <see cref="CompositionFlags.General.DeadEnd"/> <see cref="Node"/>s we only process the <see cref="Node"/> itself.
            /// </summary>
            /// <param name="currentNodeEntity"></param>
            /// <param name="currentNodeCompositionData"></param>
            /// <param name="currentEdgeNode"></param>
            /// <param name="isStartNode"></param>
            private void ProcessConnectedEdges(Entity currentNodeEntity, NetCompositionData currentNodeCompositionData, Entity currentEdgeNode, bool isStartNode)
            {
                // If we're on a DeadEnd node we need to check both of its sides.
                // If both have a Lowered flag then we need to remove the LowTransition flag on both sides.
                if (currentNodeCompositionData.m_Flags.m_General.HasFlag(CompositionFlags.General.DeadEnd) &&

                    // TODO: move this to the UpgradeFlags method to leave this method as generic as possible
                    currentNodeCompositionData.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Lowered) &&
                    currentNodeCompositionData.m_Flags.m_Right.HasFlag(CompositionFlags.Side.Lowered))
                {
                    UpgradeFlags(ref currentNodeCompositionData, ref currentNodeCompositionData);
                    m_CommandBuffer.SetComponent(currentEdgeNode, currentNodeCompositionData);
                }

                foreach (var connectedEdge in ConnectedEdges(currentNodeEntity))
                {
                    if (m_CompositionData.TryGetComponent(connectedEdge, out var connectedEdgeComposition) &&
                        m_NetCompositionData.TryGetComponent(isStartNode ? connectedEdgeComposition.m_EndNode : connectedEdgeComposition.m_StartNode, out var connectedEdgeCompositionData) &&
                        // True if flags are updated, otherwise we don't need to do anything else
                        UpgradeFlags(ref currentNodeCompositionData,
                            ref connectedEdgeCompositionData,
                            FlagsMatch(isStartNode ? currentNodeCompositionData : connectedEdgeCompositionData, isStartNode ? connectedEdgeCompositionData : currentNodeCompositionData, isStartNode)))
                    {
                        // Setting components
                        m_CommandBuffer.SetComponent(currentEdgeNode, currentNodeCompositionData);
                        m_CommandBuffer.SetComponent(isStartNode ? connectedEdgeComposition.m_EndNode : connectedEdgeComposition.m_StartNode, connectedEdgeCompositionData);
#if DEBUG
                        DebugDrawGizmos(connectedEdge, isStartNode ? Color.green : Color.blue);
#endif
                    }
                }
            }

            /// <summary>
            ///     Updates all the <see cref="CompositionFlags"/> for the specified <see cref="NetCompositionData"/>.
            /// </summary>
            /// <param name="netCompositionData"></param>
            /// <param name="added"></param>
            /// <param name="removed"></param>
            private void UpgradeFlags(ref NetCompositionData netCompositionData, CompositionFlags added, CompositionFlags removed)
            {
                netCompositionData.m_Flags.m_General |= added.m_General;
                netCompositionData.m_Flags.m_General &= ~removed.m_General;

                netCompositionData.m_Flags.m_Left |= added.m_Left;
                netCompositionData.m_Flags.m_Left &= ~removed.m_Left;

                netCompositionData.m_Flags.m_Right |= added.m_Right;
                netCompositionData.m_Flags.m_Right &= ~removed.m_Right;
            }

            /// <summary>
            ///     Performs the actual upgrading logic by comparing two <see cref="NetCompositionData"/> and deciding which <see cref="CompositionFlags"/>
            ///     to set and unset.
            /// </summary>
            /// <param name="startNodeComposition"></param>
            /// <param name="endNodeComposition"></param>
            /// <param name="isDeadEndNode"></param>
            /// <param name="areFlagsMatching"></param>
            /// <returns></returns>
            private bool UpgradeFlags(ref NetCompositionData startNodeComposition, ref NetCompositionData endNodeComposition, bool isDeadEndNode = false, bool areFlagsMatching = false)
            {
                if (!isDeadEndNode && !areFlagsMatching)
                    // TODO: implement proper logic based on different cases rather than removing LowTransition regardless of the scenario
                    return false;

                UpgradeFlags(ref startNodeComposition, default, new CompositionFlags
                {
                    m_Left = CompositionFlags.Side.LowTransition,
                    m_Right = CompositionFlags.Side.LowTransition,
                });

                UpgradeFlags(ref endNodeComposition, default, new CompositionFlags
                {
                    m_Left = CompositionFlags.Side.LowTransition,
                    m_Right = CompositionFlags.Side.LowTransition,
                });

                return true;
            }

#if DEBUG
            private void DebugDrawGizmos(Entity connectedEdge, Color color)
            {
                if (m_EdgeData.TryGetComponent(connectedEdge, out var connectedEdgeData) &&
                    m_NodeData.TryGetComponent(connectedEdgeData.m_Start, out var startNode) &&
                    m_NodeData.TryGetComponent(connectedEdgeData.m_End, out var endNode))
                {
                    m_GizmoBatcher.DrawWireCone(startNode.m_Position, 3f, endNode.m_Position, 3f, color);
                    m_GizmoBatcher.DrawLine(startNode.m_Position, endNode.m_Position, Color.red);
                }
            }
#endif

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }
        }

        #endregion
    }
}
