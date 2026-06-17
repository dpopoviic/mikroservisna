using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Services;
using EventPlatformAPI.EventsAPI.HostedServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventsConnection")));

// RabbitMQ options
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

    builder.Services.AddHostedService<RabbitMqConsumerHostedService>();
   builder.Services.AddHostedService<EventsSagaCommandConsumerHostedService>();
   builder.Services.AddHostedService<RegistrationCancelledChoreographyConsumerHostedService>();
    builder.Services.AddSingleton<ReferencesValidationRequestClient>();
builder.Services.AddSingleton<IOutboxPublisher, OutboxPublisher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
