namespace EventPlatformAPI.SagaOrcgestrator.Services;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string StartEventPublicationSagaCommandQueue { get; set; } = "saga.start-event-publication.command";
    public string ValidateLocationCommandQueue { get; set; } = "references.validate-location.command";
    public string ValidateLecturersCommandQueue { get; set; } = "references.validate-lecturers.command";
    public string LocationValidatedEventQueue { get; set; } = "saga.location-validated.event";
    public string LocationValidationFailedEventQueue { get; set; } = "saga.location-validation-failed.event";
    public string LecturersValidatedEventQueue { get; set; } = "saga.lecturers-validated.event";
    public string LecturersValidationFailedEventQueue { get; set; } = "saga.lecturers-validation-failed.event";
    public string CreateEventCommandQueue { get; set; } = "events.create-event.command";
    public string CancelEventCommandQueue { get; set; } = "events.cancel-event.command";
    public string EventCreatedEventQueue { get; set; } = "saga.event-created.event";
    public string EventCreationFailedEventQueue { get; set; } = "saga.event-creation-failed.event";
    public string SendEventNotificationCommandQueue { get; set; } = "worker.send-event-notification.command";
    public string EmailSentEventQueue { get; set; } = "saga.email-sent.event";
    public string EmailFailedEventQueue { get; set; } = "saga.email-failed.event";
    public string EventCancelledEventQueue { get; set; } = "saga.event-cancelled.event";
    public ushort PrefetchCount { get; set; } = 1;
}