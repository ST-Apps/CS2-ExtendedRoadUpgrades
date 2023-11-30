using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedRoadUpgrades.Extensions
{
    internal static class NetCompositionDataExtensions
    {

        public static NetCompositionData Clone(this NetCompositionData original)
        {
            return new NetCompositionData
            {
                m_EdgeHeights = original.m_EdgeHeights,
                m_Flags = original.m_Flags,
                m_HeightRange = original.m_HeightRange,
                m_MiddleOffset = original.m_MiddleOffset,
                m_MinLod = original.m_MinLod,
                m_NodeOffset = original.m_NodeOffset,
                m_RoundaboutSize = original.m_RoundaboutSize,
                m_State = original.m_State,
                m_SurfaceHeight = original.m_SurfaceHeight,
                m_SyncVertexOffsetsLeft = original.m_SyncVertexOffsetsLeft,
                m_SyncVertexOffsetsRight = original.m_SyncVertexOffsetsRight,
                m_Width = original.m_Width,
                m_WidthOffset = original.m_WidthOffset
            };
        }

        public static NetCompositionData WithFlags(this NetCompositionData ncd, CompositionFlags flags)
        {
            ncd.m_Flags = flags;
            return ncd;
        }

    }
}
