namespace ExtendedRoadUpgrades.Utils
{
    using ExtendedRoadUpgrades.Systems;
    using Game.Prefabs;
    using System;

    internal class NetCompositionDataUtils
    {
        private static readonly NodeFixerMethod[] FixerMethods =
        {
            FixDeadEndRetainingWalls,
            FixLoweredRetainingWalls,
            FixConnectedTunnels,
            FixPillarsConnectedRaisedAndElevated,
            FixRaisedRoundabouts,
        };

        private delegate bool NodeFixerMethod(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching);

        public enum SideOption
        {
            Left,
            Right,
            Both,
            Any,
        }

        private static bool HasFlags(NetCompositionData netCompositionData, CompositionFlags.Side sideCompositionFlag, SideOption sideOption)
        {
            return sideOption switch
            {
                SideOption.Left => netCompositionData.m_Flags.m_Left.HasFlag(sideCompositionFlag),
                SideOption.Right => netCompositionData.m_Flags.m_Right.HasFlag(sideCompositionFlag),
                SideOption.Both => netCompositionData.m_Flags.m_Left.HasFlag(sideCompositionFlag) && netCompositionData.m_Flags.m_Right.HasFlag(sideCompositionFlag),
                SideOption.Any => netCompositionData.m_Flags.m_Left.HasFlag(sideCompositionFlag) || netCompositionData.m_Flags.m_Right.HasFlag(sideCompositionFlag),
                _ => throw new ArgumentOutOfRangeException(nameof(sideOption), sideOption, null),
            };
        }

        private static bool HasFlags(NetCompositionData netCompositionData, CompositionFlags.General generalCompositionFlags)
        {
            return netCompositionData.m_Flags.m_General.HasFlag(generalCompositionFlags);
        }

        /// <summary>
        ///     Updates all the <see cref="CompositionFlags"/> for the specified <see cref="NetCompositionData"/>.
        /// </summary>
        /// <param name="netCompositionData"></param>
        /// <param name="added"></param>
        /// <param name="removed"></param>
        internal static void UpgradeFlags(
            ref NetCompositionData netCompositionData,
            CompositionFlags added,
            CompositionFlags removed)
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
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        internal static bool UpgradeFlags(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode = false,
            bool areFlagsMatching = false)
        {
            foreach (var fixerMethod in FixerMethods)
            {
                if (fixerMethod(
                    ref currentEdgeNetCompositionData,
                    ref currentNodeNetCompositionData,
                    ref connectedEdgeNetCompositionData,
                    ref connectedNodeNetCompositionData,
                    isDeadEndNode,
                    areFlagsMatching))
                {
                    Plugin.Logger.LogDebug($"[{nameof(NetCompositionDataUtils)}.{nameof(UpgradeFlags)}] Fixed nodes by using {fixerMethod.Method.Name} method.");
                    return true;
                }
            }

            return false;
        }

        #region Fixers

        /// <summary>
        /// Fixes holes in Retaining Walls for DeadEnd nodes.
        /// </summary>
        /// <param name="currentEdgeNetCompositionData"></param>
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedEdgeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        private static bool FixDeadEndRetainingWalls(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching)
        {
            if (isDeadEndNode &&
                HasFlags(currentNodeNetCompositionData, CompositionFlags.Side.Lowered, SideOption.Both))
            {
                UpgradeFlags(
                    ref currentNodeNetCompositionData,
                    default,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes holes in Retaining Walls when two Lowered edges connect.
        /// </summary>
        /// <param name="currentEdgeNetCompositionData"></param>
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedEdgeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        private static bool FixLoweredRetainingWalls(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching)
        {
            if (HasFlags(currentNodeNetCompositionData, CompositionFlags.Side.Lowered, SideOption.Any) &&
                HasFlags(connectedNodeNetCompositionData, CompositionFlags.Side.Lowered, SideOption.Any))
            {
                UpgradeFlags(
                    ref currentNodeNetCompositionData,
                    default,
                    new CompositionFlags
                    {
                        m_General = CompositionFlags.General.Intersection,
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                UpgradeFlags(
                    ref connectedNodeNetCompositionData,
                    default,
                    new CompositionFlags
                    {
                        m_General = CompositionFlags.General.Intersection,
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes holes when two Tunnel edges connect.
        /// </summary>
        /// <param name="currentEdgeNetCompositionData"></param>
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedEdgeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        private static bool FixConnectedTunnels(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching)
        {
            if (HasFlags(currentNodeNetCompositionData, CompositionFlags.General.Tunnel) &&
                HasFlags(connectedNodeNetCompositionData, CompositionFlags.General.Tunnel))
            {
                UpgradeFlags(
                    ref currentNodeNetCompositionData,
                    default,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.HighTransition,
                        m_Right = CompositionFlags.Side.HighTransition,
                    });

                UpgradeFlags(
                    ref connectedNodeNetCompositionData,
                    default,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.HighTransition,
                        m_Right = CompositionFlags.Side.HighTransition,
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes pillars when Raised edges connect to Elevated ones.
        /// </summary>
        /// <param name="currentEdgeNetCompositionData"></param>
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedEdgeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        private static bool FixPillarsConnectedRaisedAndElevated(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching)
        {
            if (HasFlags(currentNodeNetCompositionData, CompositionFlags.Side.Raised, SideOption.Any) &&
                HasFlags(connectedNodeNetCompositionData, CompositionFlags.General.Elevated))
            {
                UpgradeFlags(
                    ref currentNodeNetCompositionData,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    },
                    default);

                UpgradeFlags(
                    ref connectedNodeNetCompositionData,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    },
                    default);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes roundabouts Raised edges connect to other Raised ones.
        /// </summary>
        /// <param name="currentEdgeNetCompositionData"></param>
        /// <param name="currentNodeNetCompositionData"></param>
        /// <param name="connectedEdgeNetCompositionData"></param>
        /// <param name="connectedNodeNetCompositionData"></param>
        /// <param name="isDeadEndNode"></param>
        /// <param name="areFlagsMatching"></param>
        /// <returns></returns>
        private static bool FixRaisedRoundabouts(
            ref NetCompositionData currentEdgeNetCompositionData,
            ref NetCompositionData currentNodeNetCompositionData,
            ref NetCompositionData connectedEdgeNetCompositionData,
            ref NetCompositionData connectedNodeNetCompositionData,
            bool isDeadEndNode,
            bool areFlagsMatching)
        {
            if (HasFlags(currentNodeNetCompositionData, CompositionFlags.Side.Raised, SideOption.Any) &&
                HasFlags(connectedNodeNetCompositionData, CompositionFlags.Side.Raised, SideOption.Any) &&
                    (HasFlags(currentNodeNetCompositionData, CompositionFlags.General.Roundabout) ||
                    HasFlags(connectedNodeNetCompositionData, CompositionFlags.General.Roundabout)))
            {
                UpgradeFlags(
                    ref currentNodeNetCompositionData,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.Sidewalk | CompositionFlags.Side.Raised,
                        m_Right = CompositionFlags.Side.Sidewalk | CompositionFlags.Side.Raised,
                    },
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                UpgradeFlags(
                    ref connectedNodeNetCompositionData,
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.Sidewalk | CompositionFlags.Side.Raised,
                        m_Right = CompositionFlags.Side.Sidewalk | CompositionFlags.Side.Raised,
                    },
                    new CompositionFlags
                    {
                        m_Left = CompositionFlags.Side.LowTransition,
                        m_Right = CompositionFlags.Side.LowTransition,
                    });

                return true;
            }

            return false;
        }

        #endregion
    }
}
