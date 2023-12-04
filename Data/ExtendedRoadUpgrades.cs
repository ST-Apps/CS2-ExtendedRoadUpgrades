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
                Name = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Quay"
                    },
                    {
                        "zh-HANS",
                        "护堤"
                    },
                    {
                        "zh-HANT",
                        "护堤"
                    },
                },
                Description = new Dictionary<string, string> {
                    {
                        "en-US",
                        "A quay, if you installed this mod you know what it is :)"
                    },
                    {
                        "zh-HANS",
                        "护堤模式，为道路生成护堤。"
                    },
                    {
                        "zh-HANT",
                        "护堤模式，爲道路生成护堤。"
                    },
                },
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
                Name = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Retaining Wall"
                    },
                    {
                        "zh-HANS",
                        "挡土墙"
                    },
                    {
                        "zh-HANT",
                        "擋土牆"
                    },
                },
                Description = new Dictionary<string, string> {
                    {
                        "en-US",
                        "A retaining wall, if you installed this mod you know what it is :)"
                    },
                    {
                        "zh-HANS",
                        "挡土墙模式，为道路生成挡土墙。"
                    },
                    {
                        "zh-HANT",
                        "擋土牆模式，爲道路生成擋土牆。"
                    },
                },
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
                Name = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Elevated"
                    },
                    {
                        "zh-HANS",
                        "高架"
                    },
                    {
                        "zh-HANT",
                        "高架"
                    },
                },
                Description = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Elevated mode, kind of similar to bridges"
                    },
                    {
                        "zh-HANS",
                        "高架模式，将道路转换为一段高架道路。"
                    },
                    {
                        "zh-HANT",
                        "高架模式，將道路轉換爲一段高架道路。"
                    },
                },
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
                Name = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Tunnel"
                    },
                    {
                        "zh-HANS",
                        "隧道"
                    },
                    {
                        "zh-HANT",
                        "隧道"
                    },
                },
                Description = new Dictionary<string, string> {
                    {
                        "en-US",
                        "Tunnel mode, it might not look perfect but it works."
                    },
                    {
                        "zh-HANS",
                        "隧道模式，将道路转换为一段隧道。"
                    },
                    {
                        "zh-HANT",
                        "隧道模式，將道路轉換爲一段隧道。"
                    },
                },
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
