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
        public int EventId { get; private set; }
        public RegistrationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Registration() { }

        internal static Registration Create(int eventId, DateTime createdAt)
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

        internal void RestoreFromCancellation()
        {
            if (Status != RegistrationStatus.Cancelled)
                throw new InvalidOperationException("Only cancelled registrations can be restored.");
            Status = RegistrationStatus.Confirmed;
        }
    }
}
