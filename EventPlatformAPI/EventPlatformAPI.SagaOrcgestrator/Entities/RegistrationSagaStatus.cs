

namespace EventPlatformAPI.SagaOrcgestrator.Entities
{
    public enum RegistrationSagaStatus
    {
        Started,
        WaitingForSeatReservation,
        SeatReserved,
        WaitingForRegistrationConfirmation,
        RegistrationConfirmed,
        WaitingForEmail,
        EmailSent,
        Completed,
        Compensating,
        Failed
    }
}
