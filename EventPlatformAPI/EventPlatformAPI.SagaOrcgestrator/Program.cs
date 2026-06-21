using Microsoft.EntityFrameworkCore;
using EventPlatformAPI.SagaOrcgestrator.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EventPlatformAPI.SagaOrcgestrator.Services;
using Microsoft.Extensions.Configuration;
using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using EventPlatformAPI.SagaOrcgestrator.Repositories;
using EventPlatformAPI.SagaOrcgestrator.HostedServices;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((ctx, services) =>
{
    services.AddDbContext<SagaDbContext>(options =>
        options.UseSqlServer(ctx.Configuration.GetConnectionString("SagaConnection")));

    services.Configure<SagaRabbitMqOptions>(ctx.Configuration.GetSection("RabbitMq"));

    services.AddScoped<IRegistrationSagaStateRepository, RegistrationSagaStateRepository>();

    services.AddScoped<IRegistrationSagaCoordinator, RegistrationSagaCoordinator>();

    services.AddSingleton<ISagaOutboxPublisher, SagaOutboxPublisher>();

    services.AddHostedService<SagaConsumerHostedService>();          
    services.AddHostedService<SagaOutboxDispatcherBackgroundService>(); 
});

var host = builder.Build();


await host.RunAsync();