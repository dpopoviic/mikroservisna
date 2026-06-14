namespace EventPlatformAPI.UsersAPI.Infrastructure.Snapshots
{
    public class Snapshot
    {
        public Guid AggregateId { get; set; }

        public string AggregateType { get; set; } = string.Empty;

        public string SnapshotData { get; set; } = string.Empty;

        public int Version { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
