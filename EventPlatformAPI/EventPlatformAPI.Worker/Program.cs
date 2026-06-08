using EventPlatformAPI.Worker.Services;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.Configure<RabbitMqOptions>(context.Configuration.GetSection("RabbitMq"));
    services.Configure<WorkerOptions>(context.Configuration.GetSection("Worker"));

    services.AddHostedService<EmailQueueConsumerHostedService>();
    services.AddHostedService<SagaNotificationConsumerHostedService>();

});

var host = builder.Build();
await host.RunAsync();
