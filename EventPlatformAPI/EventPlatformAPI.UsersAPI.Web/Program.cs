using EventPlatformAPI.UsersAPI.Application.Commands;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Providers;
using EventPlatformAPI.UsersAPI.Application.Queries;
using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.Projectors;
using EventPlatformAPI.UsersAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UsersConnection")));

builder.Services.AddScoped<IUserWriteRepository, UserWriteRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IRegistrationReadRepository, RegistrationReadRepository>();

builder.Services.AddScoped<UserProjector>();

builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

builder.Services.AddScoped<IQueryHandler<GetRegistrationsForEventQuery, List<RegistrationRequest>?>, GetRegistrationsForEventQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetUserRegistrationsQuery, List<RegistrationRequest>?>, GetUserRegistrationsQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetUserByIdQuery, UserRequest?>, GetUserByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetUsersQuery, List<UserRequest>?>, GetUsersQueryHandler>();

builder.Services.AddScoped<IQueryHandler<GetUserHistoryQuery, List<EventHistoryRequest>?>, GetUserHistoryQueryHandler>();

builder.Services.AddScoped<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateUserEmailCommand>, UpdateUserEmailCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ActivateUserCommand>, ActivateUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeactivateUserCommand>, DeactivateUserCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateRegistrationCommand>, CreateRegistrationCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ConfirmRegistrationCommand>, ConfirmRegistrationCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CancelRegistrationCommand>, CancelRegistrationCommandHandler>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();


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
