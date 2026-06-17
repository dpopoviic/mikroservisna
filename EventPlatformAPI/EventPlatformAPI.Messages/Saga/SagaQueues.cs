namespace EventPlatformAPI.Messages.Saga
{
    public class SagaQueues
    {
        // ── Events (published by domain services) ────────────────
        public const string RegistrationRequested = "saga.registration.requested";
        public const string EventSeatReserved = "saga.event.seat.reserved";
        public const string EventSeatReservationFailed = "saga.event.seat.reservation.failed";
        public const string RegistrationConfirmed = "saga.registration.confirmed";
        public const string RegistrationConfirmationFailed = "saga.registration.confirmation.failed";
        public const string RegistrationCancelled = "saga.registration.cancelled";
        public const string RegistrationEmailSent = "saga.registration.email.sent";
        public const string RegistrationEmailFailed = "saga.registration.email.failed";

        // ── Commands (published by SagaOrchestrator) ─────────────
        public const string ReserveEventSeat = "saga.cmd.reserve.event.seat";
        public const string ReleaseEventSeat = "saga.cmd.release.event.seat";
        public const string ConfirmRegistration = "saga.cmd.confirm.registration";
        public const string CancelRegistration = "saga.cmd.cancel.registration";
        public const string SendRegistrationEmail = "saga.cmd.send.registration.email";
    }
}
