namespace EventPlatformAPI.EventsAPI.Services
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "references.events";
        public string Queue { get; set; } = "references.events.queue";
        public string RoutingKey { get; set; } = "reference.changed";
        public ushort PrefetchCount { get; set; } = 1;
    }
}
