using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtendedRoadUpgrades.Extensions
{
    internal static class CompositionFlagsExtensions
    {
        public static CompositionFlags Clone(this CompositionFlags flags)
        {
            return new CompositionFlags
            {
                m_General = flags.m_General,
                m_Left = flags.m_Left,
                m_Right = flags.m_Right
            };
        }
    }
}
