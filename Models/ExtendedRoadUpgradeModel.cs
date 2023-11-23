using Game.Prefabs;
using System.Collections.Generic;

namespace ExtendedRoadUpgrades.Models
{
    /// <summary>
    /// Just a basic model containing all the information needed to provide the additional
    /// upgrade modes, alongside their UI name and description.
    /// </summary>
    internal class ExtendedRoadUpgradeModel
    {
        public string Id
        {
            get; set;
        }

        public Dictionary<string, string> Name
        {
            get; set;
        }

        public Dictionary<string, string> Description
        {
            get; set;
        }

        public CompositionFlags m_SetUpgradeFlags
        {
            get; set;
        }

        public CompositionFlags m_UnsetUpgradeFlags
        {
            get; set;
        }
    }
}
