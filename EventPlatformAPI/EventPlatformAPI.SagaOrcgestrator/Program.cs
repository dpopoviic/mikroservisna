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
    // ── Database ────────────────────────────────────────────────
    services.AddDbContext<SagaDbContext>(options =>
        options.UseSqlServer(ctx.Configuration.GetConnectionString("SagaConnection")));

    // ── RabbitMQ options ────────────────────────────────────────
    services.Configure<SagaRabbitMqOptions>(ctx.Configuration.GetSection("RabbitMq"));

    // ── Saga persistence ────────────────────────────────────────
    services.AddScoped<IRegistrationSagaStateRepository, RegistrationSagaStateRepository>();

    // ── Saga coordinator ────────────────────────────────────────
    services.AddScoped<IRegistrationSagaCoordinator, RegistrationSagaCoordinator>();

    // ── RabbitMQ publisher (singleton – one connection) ─────────
    services.AddSingleton<ISagaOutboxPublisher, SagaOutboxPublisher>();

    // ── Background services ─────────────────────────────────────
    services.AddHostedService<SagaConsumerHostedService>();          // inbound
    services.AddHostedService<SagaOutboxDispatcherBackgroundService>(); // outbound
});

var host = builder.Build();


await host.RunAsync();