namespace EventPlatformAPI.SagaOrcgestrator.Entities
{
    public class RegistrationSagaState
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;

        public RegistrationSagaStatus Status { get; set; }
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
