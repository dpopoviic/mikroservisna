namespace EventPlatformAPI.Worker.Services;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string EmailRequestQueue { get; set; } = "email.requests";
    public string EmailDlqQueue { get; set; } = "email.dlq";
}

public class WorkerOptions
{
    public string OutboxFolder { get; set; } = "./outbox";
}
