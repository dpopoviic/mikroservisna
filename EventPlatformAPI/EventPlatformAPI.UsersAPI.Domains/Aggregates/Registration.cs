namespace EventPlatformAPI.UsersAPI.Domains.Aggregates
{
    public enum RegistrationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2
    }
    public class Registration
    {
        public Guid EventId { get; private set; }
        public RegistrationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Registration() { }

        internal static Registration Create(Guid eventId, DateTime createdAt)
        {
            return new Registration
            {
                EventId = eventId,
                Status = RegistrationStatus.Pending,
                CreatedAt = createdAt
            };
        }

        internal void Confirm()
        {
            Status = RegistrationStatus.Confirmed;
        }

        internal void Cancel()
        {
            Status = RegistrationStatus.Cancelled;
        }
    }
}
