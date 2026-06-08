using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Services;
using EventPlatformAPI.ReferencesAPI.HostedServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ReferenceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReferenceConnection")));

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddHostedService<ReferencesValidationConsumerHostedService>();
builder.Services.AddHostedService<SagaValidationConsumerHostedService>();


var app = builder.Build();

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
