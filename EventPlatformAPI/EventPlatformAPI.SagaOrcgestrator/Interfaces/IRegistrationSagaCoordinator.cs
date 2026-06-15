using EventPlatformAPI.Messages.Saga.EventsApiMessages;
using EventPlatformAPI.Messages.Saga.UserApiMessages;

namespace EventPlatformAPI.SagaOrcgestrator.Interfaces
{
    public interface IRegistrationSagaCoordinator
    {
        Task HandleRegistrationRequestedAsync(RegistrationRequestedEvent evt, CancellationToken ct = default);
        Task HandleEventSeatReservedAsync(EventSeatReservedEvent evt, CancellationToken ct = default);
        Task HandleEventSeatReservationFailedAsync(EventSeatReservationFailedEvent evt, CancellationToken ct = default);
        Task HandleRegistrationConfirmedAsync(RegistrationConfirmedEvent evt, CancellationToken ct = default);
        Task HandleRegistrationConfirmationFailedAsync(RegistrationConfirmationFailedEvent evt, CancellationToken ct = default);
        Task HandleRegistrationEmailSentAsync(RegistrationEmailSentEvent evt, CancellationToken ct = default);
        Task HandleRegistrationEmailFailedAsync(RegistrationEmailFailedEvent evt, CancellationToken ct = default);

    }
}
