using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.HostedServices;
using EventPlatformAPI.SagaOrcgestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<SagaOrchestratorDbContext>(options =>
        {
            options.UseSqlServer(context.Configuration.GetConnectionString("SagaConnection"));
        });
        services.Configure<RabbitMqOptions>(context.Configuration.GetSection("RabbitMq"));
        services.AddSingleton<IOutboxPublisher, OutboxPublisher>();
        services.AddHostedService<OutboxDispatcherHostedService>();        
        services.AddHostedService<PublishEventSagaOrchestratorHostedService>();
    });

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SagaOrchestratorDbContext>();
    await db.Database.MigrateAsync();
}


await host.RunAsync();