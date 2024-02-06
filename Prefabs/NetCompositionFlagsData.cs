namespace ExtendedRoadUpgrades.Prefabs
{
    using Colossal.Serialization.Entities;
    using ExtendedRoadUpgrades.Systems;
    using Game.Prefabs;
    using Unity.Entities;

    public struct NetCompositionFlagsData : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int Version;

        public CompositionFlags Flags = default;

        public NetCompositionFlagsData(NetCompositionData netCompositionData)
        {
            this.Version = NodeUpdateFixerSystem.kComponentVersion;
            this.Flags = netCompositionData.m_Flags;
        }

        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(this.Version);
            this.Flags.Serialize(writer);
        }

        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out this.Version);
            this.Flags.Deserialize(reader);
        }
    }
}
