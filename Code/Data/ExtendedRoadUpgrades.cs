// <copyright file="ExtendedRoadUpgrades.cs" company="ST-Apps (S. Tenuta)">
// Copyright (c) ST-Apps (S. Tenuta). All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License (the "License"); you may not use this file except in compliance with the License.
// See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ExtendedRoadUpgrades.Data
{
    using System.Collections.Generic;
    using Game.Prefabs;
    using global::ExtendedRoadUpgrades.Models;

    /// <summary>
    /// Main container of all the available upgrade modes.
    /// This is not intended as an example of how to design a proper C# project so I'll
    /// just dump everything I need into a static variable and call it a day.
    ///
    /// Please don't use my code to learn how to program! :).
    /// </summary>
    internal class ExtendedRoadUpgrades
    {
        /// <summary>
        ///     This variable contains all the available upgrade modes that we support.
        /// </summary>
        public static IEnumerable<ExtendedRoadUpgradeModel> Modes = new[]
        {
            // Quay
            new ExtendedRoadUpgradeModel
            {
                Id = "Quay",
                m_SetUpgradeFlags = new CompositionFlags
                {
                    m_Right = CompositionFlags.Side.Raised,
                },
                m_UnsetUpgradeFlags = new CompositionFlags
                {
                    m_General = CompositionFlags.General.Elevated,
                    m_Right = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Lowered,
                },

                // TODO: not sure how this works yet
                m_SetState = new[]
                {
                    NetPieceRequirements.Raised,
                },
                m_UnsetState = new[]
                {
                    NetPieceRequirements.Lowered,
                    NetPieceRequirements.Elevated,
                    NetPieceRequirements.LowTransition,
                    NetPieceRequirements.OppositeLowTransition,
                },
            },

            // Retaining Wall
            new ExtendedRoadUpgradeModel
            {
                Id = "RetainingWall",
                m_SetUpgradeFlags = new CompositionFlags
                {
                    m_Right = CompositionFlags.Side.Lowered,
                },
                m_UnsetUpgradeFlags = new CompositionFlags
                {
                    m_General = CompositionFlags.General.Elevated,
                    m_Right = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Raised,
                },

                // TODO: not sure how this works yet
                m_SetState = new[]
                {
                    NetPieceRequirements.Lowered,
                },
                m_UnsetState = new[]
                {
                    NetPieceRequirements.Raised,
                    NetPieceRequirements.Elevated,
                    NetPieceRequirements.LowTransition,
                    NetPieceRequirements.OppositeLowTransition,
                },
            },

            // Elevated
            new ExtendedRoadUpgradeModel
            {
                Id = "Elevated",
                m_SetUpgradeFlags = new CompositionFlags
                {
                    m_General = CompositionFlags.General.Elevated,
                },
                m_UnsetUpgradeFlags = new CompositionFlags
                {
                    m_Right = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered,
                    m_Left = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered,
                },

                // TODO: not sure how this works yet
                m_SetState = new[]
                {
                    NetPieceRequirements.Elevated,
                },
                m_UnsetState = new[]
                {
                    NetPieceRequirements.Raised,
                    NetPieceRequirements.Lowered,
                    NetPieceRequirements.LowTransition,
                    NetPieceRequirements.OppositeLowTransition,
                },
            },

            // Tunnel
            new ExtendedRoadUpgradeModel
            {
                Id = "Tunnel",
                IsUnderground = true,
                m_SetUpgradeFlags = new CompositionFlags
                {
                    m_General = CompositionFlags.General.Tunnel,
                },
                m_UnsetUpgradeFlags = new CompositionFlags
                {
                    m_Right = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered,
                    m_Left = CompositionFlags.Side.WideSidewalk | CompositionFlags.Side.PrimaryBeautification | CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered,
                },

                // TODO: not sure how this works yet
                m_SetState = new[]
                {
                    NetPieceRequirements.Tunnel,
                },
                m_UnsetState = new[]
                {
                    NetPieceRequirements.Raised,
                    NetPieceRequirements.Lowered,
                    NetPieceRequirements.LowTransition,
                    NetPieceRequirements.OppositeLowTransition,
                },
            },
        };
    }
}
