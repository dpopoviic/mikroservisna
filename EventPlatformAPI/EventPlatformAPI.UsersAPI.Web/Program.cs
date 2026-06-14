using EventPlatformAPI.UsersAPI.Application.Commands;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Providers;
using EventPlatformAPI.UsersAPI.Application.Queries;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

builder.Services.AddScoped<IQueryHandler<GetRegistrationsForEventQuery, List<RegistrationReadModel>?>, GetRegistrationsForEventQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetUserRegistrationsQuery, List<RegistrationReadModel>?>, GetUserRegistrationsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetUserByIdQuery, UserReadModel?>, GetUserByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetUsersQuery, List<UserReadModel>?>, GetUsersQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetUserHistoryQuery, List<EventHistoryReadModel>?>, GetUserHistoryQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateUserCommand>,CreateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserEmailCommand>, UpdateUserEmailCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ActivateUserCommand>, ActivateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeactivateUserCommand>, DeactivateUserCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateRegistrationCommand>, CreateRegistrationCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ConfirmRegistrationCommand>, ConfirmRegistrationCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CancelRegistrationCommand>, CancelRegistrationCommandHandler>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
