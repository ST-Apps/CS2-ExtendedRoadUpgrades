namespace ExtendedRoadUpgrades.Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Colossal;
    using Colossal.Json;
    using Game;
    using Game.Common;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;
    using UnityEngine.Scripting;

    /// <summary>
    ///     This <see cref="GameSystemBase"/> iterates over <see cref="Upgraded"/> <see cref="Edge"/>s and applies some fixes to their <see cref="Node"/>'s <see cref="Composition"/>
    ///     to replace some of the <see cref="CompositionFlags"/> that the game wrongfully applied during an upgrade.
    /// </summary>
    internal class NodeUpdateFixerSystem : GameSystemBase
    {
        #region Update Toggling

        /// <summary>
        ///     Flag used to decide if we have to execute the <see cref="OnUpdate"/> method.
        ///     To avoid wasting cycles we execute the method only when user selects one of our custom
        ///     upgrade modes.
        ///     Logic is defined in the <see cref="UpdateCanUpdate"/> method.
        /// </summary>
        private bool canUpdate;

        private PrefabBase currentNetPrefab;

        private ToolBaseSystem currentTool;

        /// <summary>
        ///     Sets the current <see cref="PrefabBase"/> selected in <see cref="NetToolSystem"/>, needed to
        ///     detect if we're using one of our custom upgrade modes.
        /// </summary>
        private PrefabBase CurrentNetPrefab
        {
            set
            {
                this.currentNetPrefab = value;
                this.UpdateCanUpdate();
            }
        }

        /// <summary>
        ///     Sets the current <see cref="ToolBaseSystem"/> selected in-game, needed to detect if we're in
        ///     <see cref="NetToolSystem"/>.
        /// </summary>
        private ToolBaseSystem CurrentNetTool
        {
            set
            {
                this.currentTool = value;
                this.UpdateCanUpdate();
            }
        }

        /// <summary>
        ///     Toggles <see cref="canUpdate"/> on/off based on the value of <see cref="CurrentNetTool"/> and <see cref="CurrentNetPrefab"/>.
        ///     We allow running updates when <see cref="CurrentNetTool"/> is <see cref="NetToolSystem"/> and when <see cref="CurrentNetPrefab"/> is
        ///     one of our custom upgrade modes.
        /// </summary>
        private void UpdateCanUpdate()
        {
            this.canUpdate = this.currentTool is NetToolSystem &&
                Data.ExtendedRoadUpgrades.Modes.Any(m => m.Id == this.currentNetPrefab?.name);

            Plugin.Logger.LogDebug($"[{nameof(NodeUpdateFixerSystem)}.{nameof(this.UpdateCanUpdate)}] Setting canUpdate to {this.canUpdate} because Tool is {this.currentTool} and Prefab is {this.currentNetPrefab?.name}.");
        }

        private void OnPrefabChanged(PrefabBase prefabBase)
        {
            this.CurrentNetPrefab = prefabBase;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            this.CurrentNetTool = tool;
        }

        #endregion

        #region GameSystemBase

        [Preserve]
        public NodeUpdateFixerSystem()
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
#if DEBUG
            this.m_GizmosSystem = this.World.GetOrCreateSystemManaged<GizmosSystem>();
#endif
            this.m_ToolOutputBarrier = this.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            this.m_UpdatedQuery = this.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[] {
                        ComponentType.ReadOnly<Upgraded>(),
                        ComponentType.ReadOnly<Edge>(),

                        // Temp seems to be needed or we won't get edges being upgraded
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadWrite<Composition>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Hidden>(),
                    },
                },
            });
            this.m_UpdatedQuery.AddOrderVersionFilter();
            this.m_UpdatedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Edge>());
            this.m_UpdatedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Temp>());
            this.RequireForUpdate(this.m_UpdatedQuery);

            // Register on ToolSystem.EventToolChanged and EventPrefabChanged to execute our Update method only while using the NetTool and our custom modes
            this.m_ToolSystem = this.World.GetOrCreateSystemManaged<ToolSystem>();
            this.m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(this.m_ToolSystem.EventToolChanged, new Action<ToolBaseSystem>(this.OnToolChanged));
            this.m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(this.m_ToolSystem.EventPrefabChanged, new Action<PrefabBase>(this.OnPrefabChanged));
        }

        [Preserve]
        protected override void OnDestroy()
        {
            // Deregister from ToolSystem.EventToolChanged and EventPrefabChanged
            this.m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Remove(this.m_ToolSystem.EventToolChanged, new Action<ToolBaseSystem>(this.OnToolChanged));
            this.m_ToolSystem.EventPrefabChanged = (Action<PrefabBase>)Delegate.Remove(this.m_ToolSystem.EventPrefabChanged, new Action<PrefabBase>(this.OnPrefabChanged));
            base.OnDestroy();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (!this.canUpdate || this.m_UpdatedQuery.IsEmpty)
            {
                return;
            }

            this.__TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Composition_Data_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Composition_Data_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            var nodeUpdateFixerJob = default(NodeUpdateFixerJob);
            nodeUpdateFixerJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            nodeUpdateFixerJob.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
            nodeUpdateFixerJob.m_EdgeData = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
            nodeUpdateFixerJob.m_CompositionData = this.__TypeHandle.__Game_Composition_Data_RW_ComponentLookup;
            nodeUpdateFixerJob.m_NetCompositionData = this.__TypeHandle.__Game_Net_Composition_Data_RW_ComponentLookup;
            nodeUpdateFixerJob.m_ConnectedEdges = this.__TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup;
            nodeUpdateFixerJob.m_CommandBuffer = this.m_ToolOutputBarrier.CreateCommandBuffer();
#if DEBUG
            nodeUpdateFixerJob.m_NodeData = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
            nodeUpdateFixerJob.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out JobHandle jobHandle);
            var jobHandle2 = nodeUpdateFixerJob.Schedule(this.m_UpdatedQuery, JobHandle.CombineDependencies(this.Dependency, jobHandle));
            this.m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
            this.Dependency = jobHandle2;
#else
            var jobHandle = nodeUpdateFixerJob.Schedule(m_UpdatedQuery, Dependency);
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(true);
                this.__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(true);
                this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(true);
                this.__Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>(true);
                this.__Game_Composition_Data_RW_ComponentLookup = state.GetComponentLookup<Composition>(false);
                this.__Game_Net_Composition_Data_RW_ComponentLookup = state.GetComponentLookup<NetCompositionData>(false);
            }
        }

        private struct NodeUpdateFixerJob : IJobChunk
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
                var updatedEdges = chunk.GetNativeArray(ref this.m_EdgeType);
                var updatedEdgesEntities = chunk.GetNativeArray(this.m_EntityType);

                for (var i = 0; i < updatedEdges.Length; i++)
                {
                    var currentEdgeEntity = updatedEdgesEntities[i];
                    var currentEdge = updatedEdges[i];

                    if (this.m_CompositionData.TryGetComponent(currentEdgeEntity, out var currentEdgeComposition) &&
                        this.m_NetCompositionData.TryGetComponent(currentEdgeComposition.m_StartNode, out var currentEdgeStartNodeCompositionData) &&
                        this.m_NetCompositionData.TryGetComponent(currentEdgeComposition.m_EndNode, out var currentEdgeEndNodeCompositionData))
                    {
                        this.ProcessConnectedEdges(currentEdge.m_Start, currentEdgeStartNodeCompositionData, currentEdgeComposition.m_StartNode, true);
                        this.ProcessConnectedEdges(currentEdge.m_End, currentEdgeEndNodeCompositionData, currentEdgeComposition.m_EndNode, false);
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
                if (!this.m_ConnectedEdges.TryGetBuffer(nodeEntity, out var connectedEdgesBuffer) &&
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
                if (currentNodeCompositionData.m_Flags.m_General.HasFlag(CompositionFlags.General.DeadEnd))
                {
                    this.UpgradeFlags(ref currentNodeCompositionData, ref currentNodeCompositionData);
                    this.m_CommandBuffer.SetComponent(currentEdgeNode, currentNodeCompositionData);
                }

                foreach (var connectedEdge in this.ConnectedEdges(currentNodeEntity))
                {
                    if (this.m_CompositionData.TryGetComponent(connectedEdge, out var connectedEdgeComposition) &&
                        this.m_NetCompositionData.TryGetComponent(isStartNode ? connectedEdgeComposition.m_EndNode : connectedEdgeComposition.m_StartNode, out var connectedEdgeCompositionData) &&

                        // True if flags are updated, otherwise we don't need to do anything else
                        this.UpgradeFlags(
                            ref currentNodeCompositionData,
                            ref connectedEdgeCompositionData,
                            this.FlagsMatch(isStartNode ? currentNodeCompositionData : connectedEdgeCompositionData, isStartNode ? connectedEdgeCompositionData : currentNodeCompositionData, isStartNode)))
                    {
                        // Setting components
                        this.m_CommandBuffer.SetComponent(currentEdgeNode, currentNodeCompositionData);
                        this.m_CommandBuffer.SetComponent(isStartNode ? connectedEdgeComposition.m_EndNode : connectedEdgeComposition.m_StartNode, connectedEdgeCompositionData);
#if DEBUG
                        this.DebugDrawGizmos(connectedEdge, isStartNode ? Color.green : Color.blue);
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
                {
                    // TODO: implement proper logic based on different cases rather than removing LowTransition regardless of the scenario
                    return false;
                }

                // This fix holes in Retaining Walls for DeadEnd nodes
                if (isDeadEndNode &&
                    startNodeComposition.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Lowered) &&
                    startNodeComposition.m_Flags.m_Right.HasFlag(CompositionFlags.Side.Lowered))
                {
                    this.UpgradeFlags(ref startNodeComposition, default, new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });
                }

                // This fixes holes in Retaining Walls when two Lowered edges connect
                if (startNodeComposition.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Lowered) ||
                    endNodeComposition.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Lowered))
                {
                    this.UpgradeFlags(ref startNodeComposition, default, new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                    this.UpgradeFlags(ref endNodeComposition, default, new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });
                }

                // This fixes pillars when Raised edges connect to Elevated ones
                if ((startNodeComposition.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Raised) ||
                        startNodeComposition.m_Flags.m_Left.HasFlag(CompositionFlags.Side.Raised)) &&
                    endNodeComposition.m_Flags.m_General.HasFlag(CompositionFlags.General.Elevated))
                {
                    this.UpgradeFlags(
                        ref startNodeComposition,
                        new CompositionFlags
                        {
                            m_Left = CompositionFlags.Side.LowTransition,
                            m_Right = CompositionFlags.Side.LowTransition,
                        },
                        default);

                    this.UpgradeFlags(
                        ref endNodeComposition,
                        new CompositionFlags
                        {
                            m_Left = CompositionFlags.Side.LowTransition,
                            m_Right = CompositionFlags.Side.LowTransition,
                        },
                        default);
                }

                return true;
            }

#if DEBUG
            private void DebugDrawGizmos(Entity connectedEdge, Color color)
            {
                if (this.m_EdgeData.TryGetComponent(connectedEdge, out var connectedEdgeData) &&
                    this.m_NodeData.TryGetComponent(connectedEdgeData.m_Start, out var startNode) &&
                    this.m_NodeData.TryGetComponent(connectedEdgeData.m_End, out var endNode))
                {
                    this.m_GizmoBatcher.DrawWireCone(startNode.m_Position, 3f, endNode.m_Position, 3f, color);
                    this.m_GizmoBatcher.DrawLine(startNode.m_Position, endNode.m_Position, Color.red);
                }
            }
#endif

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }
        }

        #endregion
    }
}
